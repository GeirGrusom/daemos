using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AutoResetEvent = Daemos.Threading.AutoResetEvent;

namespace Daemos.Tests.Threading
{
    public class AutoResetEventTests
    {

        private static void Timeout(Action @event, int ms)
        {
            var timer = new Timer(o => @event(), null, ms, System.Threading.Timeout.Infinite);
        }

        [Fact]
        public async Task WaitOne_ReleasesOnce_ReturnsTrue()
        {
            var ev = new AutoResetEvent(false);


            var stp = Stopwatch.StartNew();
            Timeout(() => ev.Set(), 10);

            var result = await ev.WaitOne();
            stp.Stop();

            Assert.True(result);
            Assert.True(stp.ElapsedMilliseconds >= 10);
        }

        [Fact]
        public async Task WaitOne_TimeoutZero_Signalled_ReturnsTrue()
        {
            var ev = new AutoResetEvent(true);

            var result = await ev.WaitOne(0);

            Assert.True(result);
        }

        [Fact]
        public async Task WaitOne_TimeoutZero_SetSignalled_ReturnsTrue()
        {
            var ev = new AutoResetEvent(false);

            ev.Set();

            var result = await ev.WaitOne(0);

            Assert.True(result);
        }

        [Fact]
        public async Task WaitOne_TimeoutZero_NotSignalled_ReturnsFalse()
        {
            var ev = new AutoResetEvent(false);

            var result = await ev.WaitOne(0);

            Assert.False(result);
        }

        [Fact]
        public async Task WaitOne_Cancelled_ThrowsCancel()
        {
            var ev = new AutoResetEvent(false);

            var stp = Stopwatch.StartNew();
            var cancel = new CancellationTokenSource(10);
            await Assert.ThrowsAsync<TaskCanceledException>(() => ev.WaitOne(cancel.Token));
            stp.Stop();

            Assert.True(stp.ElapsedMilliseconds >= 10);
        }

        [Fact]
        public async Task WaitOne_Timeout_ReturnsFalse()
        {
            var ev = new AutoResetEvent(false);

            var stp = Stopwatch.StartNew();
            var result = await ev.WaitOne(10);
            stp.Stop();

            Assert.False(result);
            Assert.True(stp.ElapsedMilliseconds >= 10);
        }

        // Tests that Dispose does not throw when task has timed out
        [Fact]
        public async Task WaitOne_Timeout_Dispose()
        {
            var ev = new AutoResetEvent(false);

            var result = await ev.WaitOne(10);

            Assert.False(result);
            ev.Dispose();
        }

        // Tests that Dispose does not throw when task has been cancelled
        [Fact]
        public async Task WaitOne_Cancel_Dispose()
        {
            var ev = new AutoResetEvent(false);
            var cancel = new CancellationTokenSource(10);

            try
            {
                await ev.WaitOne(cancel.Token);
                throw new InvalidOperationException("WaitOne should throw a TaskCancelledException");
            }
            catch (TaskCanceledException)
            {
            }

            ev.Dispose();
        }

        // Checks that Dispose works even when both cancellation and timeout occurred
        [Fact]
        public async Task WaitOne_CancelAndTimeout_Dispose()
        {
            var ev = new AutoResetEvent(false);
            var cancel = new CancellationTokenSource(25);

            var result = await ev.WaitOne(10, cancel.Token);

            await Task.Delay(25);

            Assert.False(result);
            ev.Dispose();
        }

        // Checks that Dispose works even when both timeout and cancellation occurred
        [Fact]
        public async Task WaitOne_TimeoutAndCancel_Dispose()
        {
            var ev = new AutoResetEvent(false);
            var cancel = new CancellationTokenSource(10);

            try
            {
                await ev.WaitOne(50, cancel.Token);
                throw new InvalidOperationException("WaitOne should throw a TaskCancelledException");
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(50);

            ev.Dispose();
        }
    }
}
