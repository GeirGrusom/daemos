using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Daemos.Threading
{
    // Based on Stephen Toub's "Building Async Coordination Primitives, Part 2: AsyncAutoResetEvent"
    // https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/

    public sealed class AutoResetEvent : IDisposable, IAwaitableEvent
    {
        private static readonly Task<bool> Completed = Task.FromResult(true);
        private static readonly Task<bool> TimedOut = Task.FromResult(false);
        private readonly List<EventRegistration> _taskCompletions;
        private bool _signalled;

        private class EventRegistration
        {
            public EventRegistration(AutoResetEvent autoResetEvent, TaskCompletionSource<bool> completionSource)
            {
                AutoResetEvent = autoResetEvent;
                CompletionSource = completionSource;
            }
            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; }
            public CancellationTokenSource TimeoutTokenSource { get; set; }
            public CancellationTokenRegistration TimeoutTokenRegistration { get; set; }
            public AutoResetEvent AutoResetEvent { get; }
            public void Cancel()
            {
                CompletionSource.TrySetCanceled();
                CancellationTokenRegistration.Dispose();
            }
        }

        public AutoResetEvent(bool signalled)
        {
            _signalled = signalled;
            _taskCompletions = new List<EventRegistration>();
        }

        public Task<bool> WaitOne(CancellationToken cancel)
        {
            return WaitOne(Timeout.Infinite, cancel);
        }

        public Task<bool> WaitOne(int timeout)
        {
            return WaitOne(timeout, CancellationToken.None);
        }

        public Task<bool> WaitOne()
        {
            return WaitOne(Timeout.Infinite, CancellationToken.None);
        }

        public Task<bool> WaitOne(int timeout, CancellationToken cancel)
        {
            if (timeout < 0 && timeout != Timeout.Infinite)
            {
                throw new ArgumentException("Timeout cannot be less than zero", nameof(timeout));
            }

            if (cancel.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(cancel);
            }

            lock (_taskCompletions)
            {
                if (_signalled)
                {
                    _signalled = false;
                    return Completed;
                }
                if (timeout == 0)
                {
                    return TimedOut;
                }

                var task = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var reg = new EventRegistration(this, task);

                if (cancel.CanBeCanceled)
                {
                    var cancelReg = cancel.Register(OnCancelCallback, reg, useSynchronizationContext: true);
                    reg.CancellationTokenRegistration = cancelReg;
                }

                if (timeout != Timeout.Infinite)
                {
                    reg.TimeoutTokenSource = new CancellationTokenSource(timeout);
                    reg.TimeoutTokenRegistration = reg.TimeoutTokenSource.Token.Register(OnTimeoutCallback, reg, useSynchronizationContext: true);
                }

                _taskCompletions.Add(reg);

                return task.Task;
            }
        }

        private static void OnCancelCallback(object state)
        {
            ActionCallback((EventRegistration)state, isTimeout: false);
        }

        private static void OnTimeoutCallback(object state)
        {
            ActionCallback((EventRegistration)state, isTimeout: true);
        }

        private static void ActionCallback(EventRegistration state, bool isTimeout)
        {
            bool removed;
            lock (state.AutoResetEvent._taskCompletions)
            {
                removed = state.AutoResetEvent._taskCompletions.Remove(state);
            }

            if (removed)
            {
                if (isTimeout)
                {
                    state.CancellationTokenRegistration.Dispose();
                    state.CompletionSource.SetResult(false);
                }
                else
                {
                    state.CompletionSource.SetCanceled();
                }

                state.TimeoutTokenRegistration.Dispose();
                state.TimeoutTokenSource?.Dispose();
            }
        }

        private EventRegistration Dequeue()
        {
            var result = _taskCompletions[_taskCompletions.Count - 1];
            _taskCompletions.Remove(result);
            return result;
        }

        public void Set()
        {
            EventRegistration toRelease = null;
            lock (_taskCompletions)
            {
                if (_taskCompletions.Count > 0)
                    toRelease = Dequeue();
                else if (!_signalled)
                    _signalled = true;
            }
            if (toRelease != null)
            {
                toRelease.TimeoutTokenRegistration.Dispose();
                toRelease.CancellationTokenRegistration.Dispose();
                toRelease.TimeoutTokenSource?.Dispose();
                toRelease.CompletionSource.SetResult(true);
            }
        }

        public void Dispose()
        {
            lock (_taskCompletions)
            {
                while (_taskCompletions.Count > 0)
                {
                    var item = Dequeue();
                    item.TimeoutTokenRegistration.Dispose();
                    item.CancellationTokenRegistration.Dispose();
                    item.TimeoutTokenSource?.Dispose();
                    item.Cancel();
                }
            }
        }
    }
}
