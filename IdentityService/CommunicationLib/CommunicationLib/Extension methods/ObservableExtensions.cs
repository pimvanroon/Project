using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Communication
{
    public static class ObservableExtensions
    {
        public static int GetFibo(int n)
        {
            return (int)((Math.Pow(1.6180339, n) - Math.Pow(-0.6180339, n)) / 2.236067977 + 0.5);
        }

        private static IEnumerable<IObservable<TSource>> ToEnumerableForRetryWithDelay<TSource>(IObservable<TSource> source, TimeSpan firstTimeSpan, TimeSpan maxTimeSpan, int? maxRetries = null, IScheduler scheduler = null)
        {
            // Don't delay the first time
            yield return source;

            for (int count = 1; !maxRetries.HasValue || count < maxRetries.Value; count++)
            {
                TimeSpan dueTime = TimeSpan.FromTicks(Math.Min(GetFibo(count) * firstTimeSpan.Ticks, maxTimeSpan.Ticks));
                yield return source.DelaySubscription(dueTime, scheduler ?? DefaultScheduler.Instance);
            }
        }

        public static IObservable<TSource> RetryWithDelay<TSource>(this IObservable<TSource> source, TimeSpan firstTimeSpan, TimeSpan maxTimeSpan, int? maxRetries = null, IScheduler scheduler = null)
        {
            return ToEnumerableForRetryWithDelay(source, firstTimeSpan, maxTimeSpan, maxRetries, scheduler).Catch();
        }
    }
}
