using System.Globalization;
using Automation.Framework.Helpers.Almusher;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.Test.Infrastructure.Reporting
{
    /// <summary>
    /// عرض موحّد لكل الاختبارات: السيناريو ← البيانات المدخلة ← قبل التنفيذ ← بعد التنفيذ ← المقارنات ← النتيجة.
    /// يعمل على أي ITestOutputHelper، ويُسجَّل أيضًا في التقرير عندما يكون الغلاف ReportOutput.
    /// </summary>
    public static class TestOutputExtensions
    {
        private const string Line = "────────────────────────────────────────────────────────────";

        public static void Scenario(this ITestOutputHelper o, string title, string? expected = null)
        {
            if (o is ReportOutput r)
            {
                r.Record.Scenario = title;
                r.Record.ExpectedText = expected;
            }
            o.WriteLine($"\n{Line}\n🧪 السيناريو: {title}");
            if (!string.IsNullOrWhiteSpace(expected)) o.WriteLine($"🎯 النتيجة المتوقعة: {expected}");
            o.WriteLine(Line);
        }

        /// <summary>البيانات المدخلة للاختبار.</summary>
        public static void Inputs(this ITestOutputHelper o, params (string Name, object? Value)[] items)
            => Section(o, "📥 البيانات المدخلة", items);

        /// <summary>الحالة قبل التنفيذ.</summary>
        public static void Before(this ITestOutputHelper o, params (string Name, object? Value)[] items)
            => Section(o, "📊 قبل التنفيذ", items);

        /// <summary>الحالة بعد التنفيذ.</summary>
        public static void After(this ITestOutputHelper o, params (string Name, object? Value)[] items)
            => Section(o, "📈 بعد التنفيذ", items);

        public static void Step(this ITestOutputHelper o, string text) => o.WriteLine($"▶ {text}");

        /// <summary>مقارنة متوقع/فعلي تُطبع وتُسجَّل في التقرير. لا ترمي استثناءً؛ تعيد نتيجة المقارنة.</summary>
        public static bool Compare(this ITestOutputHelper o, string label, object? expected, object? actual, decimal? tolerance = null)
        {
            var ok = AreEqual(expected, actual, tolerance);
            var e = Format(expected);
            var a = Format(actual);
            if (o is ReportOutput r) r.Record.AddComparison(new ComparisonEntry(label, e, a, ok));
            o.WriteLine($"   {(ok ? "✅" : "❌")} {label}: المتوقع = {e} | الفعلي = {a}");
            return ok;
        }

        /// <summary>مثل Compare لكنها ترمي XunitException عند عدم التطابق.</summary>
        public static void Verify(this ITestOutputHelper o, string label, object? expected, object? actual, decimal? tolerance = null)
        {
            if (!o.Compare(label, expected, actual, tolerance))
                throw new XunitException($"{label}\nExpected: {Format(expected)}\nActual:   {Format(actual)}");
        }

        public static void Result(this ITestOutputHelper o, bool success, string? message = null)
        {
            o.WriteLine($"{Line}\n{(success ? "✅ النتيجة: نجح" : "❌ النتيجة: فشل")}");
            if (!string.IsNullOrWhiteSpace(message)) o.WriteLine($"📝 {message}");
            o.WriteLine(Line);
        }

        private static void Section(ITestOutputHelper o, string title, (string Name, object? Value)[] items)
        {
            o.WriteLine(title);
            foreach (var (name, value) in items)
                o.WriteLine($"   • {name}: {Format(value)}");
        }

        internal static string Format(object? v) => v switch
        {
            null => "(null)",
            decimal d => d.ToString("0.####", CultureInfo.InvariantCulture),
            double d => d.ToString("0.####", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => v.ToString() ?? "(null)"
        };

        private static bool AreEqual(object? expected, object? actual, decimal? tolerance)
        {
            if (expected is null || actual is null) return expected is null && actual is null;

            if (TryDecimal(expected, out var e) && TryDecimal(actual, out var a))
                return DecimalComparer.AreClose(e, a, tolerance ?? 0.0001m);

            return Equals(expected, actual) || string.Equals(expected.ToString(), actual.ToString(), StringComparison.Ordinal);
        }

        private static bool TryDecimal(object v, out decimal d)
        {
            switch (v)
            {
                case decimal x: d = x; return true;
                case int or long or short or byte or double or float:
                    d = Convert.ToDecimal(v, CultureInfo.InvariantCulture); return true;
                default: d = 0; return false;
            }
        }
    }
}
