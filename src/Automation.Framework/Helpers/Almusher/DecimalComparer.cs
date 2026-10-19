// ✅ بدون أي Assert - فقط منطق الحسابات
namespace Automation.Framework.Helpers.Almusher
{
    public static class DecimalComparer
    {
        private const decimal Tolerance = 0.0001m;
        private const decimal RateTolerance = 0.00000001m;
        private const decimal BalanceTolerance = 0.001m;  // 0.001


        /// <summary>
        /// مقارنة رقمين عشريين وإرجاع الفرق
        /// </summary>
        public static bool AreEqual(decimal expected, decimal actual, out decimal diff)
        {
            diff = Math.Abs(expected - actual);
            return diff < Tolerance;
        }

        /// <summary>
        /// مقارنة رقمين عشريين وإرجاع الفرق مع رسالة
        /// </summary>
        public static (bool IsEqual, decimal Diff, string Message) Compare(
            decimal expected,
            decimal actual,
            string label = "")
        {
            var diff = Math.Abs(expected - actual);
            var isEqual = diff < Tolerance;

            var message = isEqual
                ? $"✅ {label}: Expected {expected:F10}, Actual {actual:F10}"
                : $"❌ {label}: Expected {expected:F10}, Actual {actual:F10}, Diff {diff:F10}";

            return (isEqual, diff, message);
        }
        // ================================================================
        // ✅ دالة مقارنة للأرصدة (3 خانات عشرية)
        // ================================================================

        public static (bool IsEqual, decimal Diff, string Message) CompareBalance(
            decimal expected,
            decimal actual,
            string label = "")
        {
            // ✅ تقريب القيم إلى 3 خانات عشرية
            var expectedRounded = Math.Round(expected, 3);
            var actualRounded = Math.Round(actual, 3);

            var diff = Math.Abs(expectedRounded - actualRounded);
            var isEqual = diff < BalanceTolerance;

            var message = isEqual
                ? $"✅ {label}: Expected {expectedRounded:F3}, Actual {actualRounded:F3}"
                : $"❌ {label}: Expected {expectedRounded:F3}, Actual {actualRounded:F3}, Diff {diff:F10}";

            return (isEqual, diff, message);
        }


        // ================================================================
        // ✅ دالة مقارنة للـ Rate (10 خانات عشرية)
        // ================================================================

        public static (bool IsEqual, decimal Diff, string Message) CompareRate(
            decimal expected,
            decimal actual,
            string label = "")
        {
            // ✅ تقريب القيم إلى 10 خانات عشرية
            var expectedRounded = Math.Round(expected, 10);
            var actualRounded = Math.Round(actual, 10);

            var diff = Math.Abs(expectedRounded - actualRounded);
            var isEqual = diff < RateTolerance;

            var message = isEqual
                ? $"✅ {label}: Expected {expectedRounded:F10}, Actual {actualRounded:F10}"
                : $"❌ {label}: Expected {expectedRounded:F10}, Actual {actualRounded:F10}, Diff {diff:F10}";

            return (isEqual, diff, message);
        }
        public static decimal Truncate(decimal value, int digits = 10)
        {
            var multiplier = (decimal)Math.Pow(10, digits);
            return Math.Floor(value * multiplier) / multiplier;
        }
    }
}