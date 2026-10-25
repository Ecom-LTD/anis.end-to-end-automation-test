using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Test.TestSupport
{
    /// <summary>
    /// إعادة محاولة دورية حتى يتحقق شرط. تطبع وصفًا للإخراج عند الفشل.
    /// تعيد المحاولة تلقائيًا عند أخطاء الشبكة العابرة (5xx).
    /// </summary>
    public static class PollingHelper
    {
        public static async Task<T> UntilAsync<T>(
            Func<Task<T>> action,
            Func<T, bool> predicate,
            string description,
            Func<T, string>? describe = null,
            int maxAttempts = 30,
            int initialIntervalMs = 250,
            int maxIntervalMs = 1000,
            Func<Exception, bool>? shouldRetry = null)
        {
            T last = default!;
            Exception? lastException = null;
            var interval = initialIntervalMs;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    last = await action();
                    lastException = null;

                    if (predicate(last)) return last;
                }
                catch (Exception ex) when (shouldRetry?.Invoke(ex) ?? false)
                {
                    lastException = ex;
                }

                if (attempt < maxAttempts)
                {
                    await Task.Delay(interval);
                    interval = Math.Min(interval * 2, maxIntervalMs);   // backoff خفيف
                }
            }

            var lastValue = describe is not null
                ? describe(last)
                : last?.ToString() ?? "null";

            if (lastException is not null)
                throw new InvalidOperationException(
                    $"Polling '{description}' failed after {maxAttempts} attempts. " +
                    $"Last exception: {lastException.Message}", lastException);

            throw new InvalidOperationException(
                $"Polling '{description}' failed after {maxAttempts} attempts. " +
                $"Last value: {lastValue}");
        }
    }
}
