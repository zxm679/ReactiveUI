// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Android.App;
using Android.OS;
using ReactiveUI;
using Splat;

namespace ReactiveUI
{
    /// <summary>
    /// HandlerScheduler is a scheduler that schedules items on a running
    /// Activity's main thread. This is the moral equivalent of
    /// DispatcherScheduler.
    /// </summary>
    public class HandlerScheduler : IScheduler, IEnableLogger
    {
        /// <summary>
        /// A common instance to avoid allocations to the MainThread for the HandlerScheduler.
        /// </summary>
        public static IScheduler MainThreadScheduler = new HandlerScheduler(new Handler(Looper.MainLooper), Looper.MainLooper.Thread.Id);
        private Handler _handler;
        private long _looperId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="threadIdAssociatedWithHandler"></param>
        public HandlerScheduler(Handler handler, long? threadIdAssociatedWithHandler)
        {
            _handler = handler;
            _looperId = threadIdAssociatedWithHandler ?? -1;
        }

        /// <inheritdoc/>
        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            bool isCancelled = false;
            var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

            if (_looperId > 0 && _looperId == Java.Lang.Thread.CurrentThread().Id)
            {
                return action(this, state);
            }

            _handler.Post(() =>
            {
                if (isCancelled)
                {
                    return;
                }

                innerDisp.Disposable = action(this, state);
            });

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp);
        }

        /// <inheritdoc/>
        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            bool isCancelled = false;
            var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

            _handler.PostDelayed(
                () =>
            {
                if (isCancelled)
                {
                    return;
                }

                innerDisp.Disposable = action(this, state);
            }, dueTime.Ticks / 10 / 1000);

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp);
        }

        /// <inheritdoc/>
        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= Now)
            {
                return Schedule(state, action);
            }

            return Schedule(state, dueTime - Now, action);
        }

        /// <inheritdoc/>
        public DateTimeOffset Now
        {
            get { return DateTimeOffset.Now; }
        }
    }
}

// vim: tw=120 ts=4 sw=4 et :