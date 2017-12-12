// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    // Based on Stephen Toub's "Building Async Coordination Primitives, Part 2: AsyncAutoResetEvent"
    // https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/

    /// <summary>
    /// Implements an AutoResetEvent with cancellation and timeout support.
    /// </summary>
    public sealed class AutoResetEvent : IDisposable, IAwaitableEvent
    {
        private static readonly Task<bool> Completed = Task.FromResult(true);
        private static readonly Task<bool> TimedOut = Task.FromResult(false);
        private readonly List<EventRegistration> taskCompletions;
        private bool signalled;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoResetEvent"/> class.
        /// </summary>
        /// <param name="signalled">Indicates whether the AutoResetEvent starts in a signalled state</param>
        public AutoResetEvent(bool signalled)
        {
            this.signalled = signalled;
            this.taskCompletions = new List<EventRegistration>();
        }

        /// <summary>
        /// Waits for signal or until task is cancelled
        /// </summary>
        /// <param name="cancel">Cancel token used to cancel waiting</param>
        /// <returns>True if the AutoResetEvent was signalled, otherwise false.</returns>
        public Task<bool> WaitOne(CancellationToken cancel)
        {
            return this.WaitOne(Timeout.Infinite, cancel);
        }

        /// <summary>
        /// Waits for signal or until timeout
        /// </summary>
        /// <param name="timeout">Signal timeout in milliseconds</param>
        /// <returns>True of the AutoResetEvent was signalled, otherwise false.</returns>
        public Task<bool> WaitOne(int timeout)
        {
            return this.WaitOne(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Wait for signal
        /// </summary>
        /// <returns>Returns true</returns>
        public Task<bool> WaitOne()
        {
            return this.WaitOne(Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Waits for signal, timeout or task cancellation. Whichever comes first.
        /// </summary>
        /// <param name="timeout">Signal timeout in milliseconds</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>True if the AutoResetEvent was signalled, otherwise false.</returns>
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

            lock (this.taskCompletions)
            {
                if (this.signalled)
                {
                    this.signalled = false;
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

                this.taskCompletions.Add(reg);

                return task.Task;
            }
        }

        /// <summary>
        /// Signals he AutoResetEvent
        /// </summary>
        public void Set()
        {
            EventRegistration toRelease = null;
            lock (this.taskCompletions)
            {
                if (this.taskCompletions.Count > 0)
                {
                    toRelease = this.Dequeue();
                }
                else if (!this.signalled)
                {
                    this.signalled = true;
                }
            }

            if (toRelease != null)
            {
                toRelease.TimeoutTokenRegistration.Dispose();
                toRelease.CancellationTokenRegistration.Dispose();
                toRelease.TimeoutTokenSource?.Dispose();
                toRelease.CompletionSource.SetResult(true);
            }
        }

        /// <summary>
        /// Disposes the AutoResetEvent. This clears up any waiting WaitOne requests.
        /// </summary>
        public void Dispose()
        {
            lock (this.taskCompletions)
            {
                while (this.taskCompletions.Count > 0)
                {
                    var item = this.Dequeue();
                    item.TimeoutTokenRegistration.Dispose();
                    item.CancellationTokenRegistration.Dispose();
                    item.TimeoutTokenSource?.Dispose();
                    item.Cancel();
                }
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
            lock (state.AutoResetEvent.taskCompletions)
            {
                removed = state.AutoResetEvent.taskCompletions.Remove(state);
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
            var result = this.taskCompletions[this.taskCompletions.Count - 1];
            this.taskCompletions.Remove(result);
            return result;
        }

        private class EventRegistration
        {
            public EventRegistration(AutoResetEvent autoResetEvent, TaskCompletionSource<bool> completionSource)
            {
                this.AutoResetEvent = autoResetEvent;
                this.CompletionSource = completionSource;
            }

            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }

            public TaskCompletionSource<bool> CompletionSource { get; }

            public CancellationTokenSource TimeoutTokenSource { get; set; }

            public CancellationTokenRegistration TimeoutTokenRegistration { get; set; }

            public AutoResetEvent AutoResetEvent { get; }

            public void Cancel()
            {
                this.CompletionSource.TrySetCanceled();
                this.CancellationTokenRegistration.Dispose();
            }
        }
    }
}
