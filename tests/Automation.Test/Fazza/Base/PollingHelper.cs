using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Test.Fazza.Base
{
    public static class PollingHelper
    {
        /// <summary>
        /// يُعيد المحاولة حتى يعيد الشرط (predicate) القيمة true.
        /// </summary>
        public static async Task<T> UntilAsync<T>(
            Func<Task<T>> action,
            Func<T, bool> predicate,
            string description,
            int maxAttempts = 30,       // 30 محاولة
            int intervalMs = 500)       // كل 500 ملي ثانية (أي إجمالي 15 ثانية انتظار)
        {
            T lastResult = default!;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // 1. نفذ العملية (مثلاً: اجلب بيانات الحساب)
                lastResult = await action();

                // 2. هل تحقق الشرط؟ (مثلاً: هل السقف أصبح 1000؟)
                if (predicate(lastResult))
                {
                    return lastResult; // انتهى بنجاح
                }

                // 3. إذا لم يتحقق، انتظر قليلاً قبل المحاولة التالية
                if (attempt < maxAttempts)
                {
                    await Task.Delay(intervalMs);
                }
            }

            // 4. إذا وصلنا هنا، فهذا يعني أننا حاولنا 30 مرة وفشلنا
            throw new InvalidOperationException(
                $"Polling failed: '{description}' was not satisfied " +
                $"after {maxAttempts} attempts " +
                $"({maxAttempts * intervalMs / 1000.0:F1}s). " +
                $"Last result: {lastResult}");
        }
    }
}
