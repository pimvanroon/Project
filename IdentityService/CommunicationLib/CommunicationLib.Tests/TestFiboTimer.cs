using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Xunit;
using Communication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace Communication.Tests
{
    public class TestFiboTimer
    {

        [Fact]
        public void TestFibo()
        {
            int a = 0, b = 1;
            for (int n = 0; n < 10; n++)
            {
                Assert.Equal(a, Communication.ObservableExtensions.GetFibo(n));
                int c = a;
                a = a + b;
                b = c;
            }
        }

        [Fact]
        // This test is time-critical, it could fail when debugging
        public async Task TestFiboRetry()
        {
            // Test parameters
            int nrOfRetries = 10;
            TimeSpan firstTimeSpan = TimeSpan.FromMilliseconds(100);
            TimeSpan maxTimeSpan = TimeSpan.FromMilliseconds(1000);
            double range = 50; // [ms]

            int count = 0;

            var sw = new Stopwatch();
            var observable1 = Observable.Create<int>((s) =>
            {
                // measure the actual elapsed time
                double elapsedMs = sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

                // calculate the expected elapsed time
                double expectedMs = TimeSpan.FromTicks(
                    Math.Min(
                        firstTimeSpan.Ticks * ObservableExtensions.GetFibo(count++),
                        maxTimeSpan.Ticks
                    )).TotalMilliseconds;

                // ElapsedMs should be in range of the calculated time
                Assert.InRange(elapsedMs, expectedMs - range, expectedMs + range);
                Trace.WriteLine($"Subscribed after {elapsedMs} ms ({expectedMs} ms)");
                sw.Restart();

                // Cause an error to trigger the retry
                s.OnError(new InvalidOperationException("Snap 't nie!"));

                // Return a dispose handler
                return () => Trace.WriteLine("Terminated");
            });

            // Start measuring
            sw.Start();

            Task complete = new Task(() => { });
            using (observable1

                // The function to test
                .RetryWithDelay(firstTimeSpan, maxTimeSpan, nrOfRetries)
                .Subscribe(
                    // onNext should never occur, because we cause an error
                    next => throw new InvalidOperationException("Should not call onNext"),

                    // onError should only occur after the retries have been exhausted
                    error => { Trace.WriteLine($"Error: {error}"); complete.RunSynchronously(); },

                    // onComplete should never occur, because the observable cannot complete
                    () => throw new InvalidOperationException("Should not call onCompleted")
                )
            )
            {
                await complete;
            }
            Trace.WriteLine("Done");
        }
    }
}
