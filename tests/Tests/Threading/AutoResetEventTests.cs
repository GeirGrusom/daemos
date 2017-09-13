using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AutoResetEvent = Daemos.Threading.AutoResetEvent;

namespace Daemos.Tests.Threading
{
    public class AutoResetEventTests
    {
        [Fact]
        public async Task WaitOne_ReleasesOnce_ReturnsTrue()
        {
            var ev = new AutoResetEvent(false);
            var thread = new Thread(() =>
            {
                Thread.Sleep(100);
                ev.Set();
            });

            var stp = Stopwatch.StartNew();
            thread.Start();
            var result = await ev.WaitOne();
            stp.Stop();

            Assert.True(result);
            Assert.True(stp.ElapsedMilliseconds >= 100);
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
            var cancel = new CancellationTokenSource(100);
            await Assert.ThrowsAsync<TaskCanceledException>( () => ev.WaitOne(cancel.Token));
            stp.Stop();

            Assert.True(stp.ElapsedMilliseconds >= 100);
        }

        [Fact]
        public async Task WaitOne_Timeout_ReturnsFalse()
        {
            var ev = new AutoResetEvent(false);

            var stp = Stopwatch.StartNew();
            var result = await ev.WaitOne(100);
            stp.Stop();

            Assert.False(result);
            Assert.True(stp.ElapsedMilliseconds >= 100);
        }
    }
}
