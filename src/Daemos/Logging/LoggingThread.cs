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
        }

        private void CancellationRequested()
        {
            this.autoResetEvent.Set();
        }

        public void Add(LogObject obj)
        {
            this.logEvents.Enqueue(obj);
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

                while (this.logEvents.TryDequeue(out var result))
                {
                    
                }
            }
        }
    }
}
