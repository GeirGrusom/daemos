// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Provides a logging thread handler
    /// </summary>
    public sealed class LoggingThread
    {
        private readonly Thread thread;

        private readonly System.Collections.Concurrent.ConcurrentQueue<LogObject> logEvents;

        private readonly AutoResetEvent autoResetEvent;

        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingThread"/> class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token used to stop this class</param>
        public LoggingThread(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.cancellationToken.Register(this.CancellationRequested);
            this.thread = new Thread(this.LogLoop);
            this.logEvents = new System.Collections.Concurrent.ConcurrentQueue<LogObject>();
            this.autoResetEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Enqueues a Log event for processing
        /// </summary>
        /// <param name="obj">Log event to enqueue</param>
        public void Add(LogObject obj)
        {
            this.logEvents.Enqueue(obj);
        }

        private void CancellationRequested()
        {
            this.autoResetEvent.Set();
        }

        private void LogLoop()
        {
            while (true)
            {
                this.autoResetEvent.WaitOne();

                if (this.cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
