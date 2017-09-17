using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Daemos.Threading
{
    public sealed class AutoResetEvent : IDisposable
    {
        private static readonly Task<bool> Completed = Task.FromResult(true);
        private static readonly Task<bool> TimedOut = Task.FromResult(false);
        private readonly List<EventRegistration> _taskCompletions;
        private bool _signalled;

        private class EventRegistration
        {
            public EventRegistration(AutoResetEvent autoResetEvent)
            {
                AutoResetEvent = autoResetEvent;
            }
            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
            public TaskCompletionSource<bool> Task { get; set; }
            public Timer Timeout { get; set; }
            public AutoResetEvent AutoResetEvent { get; }
            public void Cancel()
            {
                Task.TrySetCanceled();
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
            lock (_taskCompletions)
            {
                if (cancel.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancel);
                }

                if (_signalled)
                {
                    _signalled = false;
                    return Completed;
                }
                if (timeout == 0)
                {
                    return TimedOut;
                }
                var task = new TaskCompletionSource<bool>();

                var reg = new EventRegistration(this) { Task = task };
                var cancelReg = cancel.Register(OnCancelCallback, reg);
                reg.CancellationTokenRegistration = cancelReg;

                _taskCompletions.Add(reg);

                if (timeout != Timeout.Infinite)
                {
                    reg.Timeout = new Timer(OnTimeoutCallback, reg, timeout, Timeout.Infinite);
                }
                return task.Task;
            }
        }

        private static void OnCancelCallback(object state)
        {
            var re = (EventRegistration)state;
            lock (re.AutoResetEvent._taskCompletions)
            {
                re.Timeout?.Dispose();
                re.CancellationTokenRegistration.Dispose();
                re.AutoResetEvent.Remove(re);
                re.Task.TrySetCanceled();
            }
        }

        private static void OnTimeoutCallback(object state)
        {
            var re = (EventRegistration)state;
            lock (re.AutoResetEvent._taskCompletions)
            {
                re.AutoResetEvent.Remove(re);
                re.CancellationTokenRegistration.Dispose();
                re.Timeout.Dispose();
                re.Task.TrySetResult(false);
            }
        }

        private void Remove(EventRegistration reg)
        {
            lock (_taskCompletions)
            {
                _taskCompletions.Remove(reg);
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
                toRelease.Timeout?.Dispose();
                toRelease.CancellationTokenRegistration.Dispose();
                toRelease.Task.TrySetResult(true);
            }
        }

        public void Dispose()
        {
            lock (_taskCompletions)
            {
                while(_taskCompletions.Count > 0)
                {
                    var item = Dequeue();
                    item.Timeout?.Dispose();
                    item.Cancel();
                }
            }
        }
    }
}
