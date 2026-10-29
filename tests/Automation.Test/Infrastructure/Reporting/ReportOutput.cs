using System.Reflection;
using Xunit.Abstractions;

namespace Automation.Test.Infrastructure.Reporting
{
    /// <summary>
    /// غلاف حول ITestOutputHelper يمرّر كل سطر للمخرجات الأصلية ويسجله في TestRecord
    /// ليظهر لاحقًا في Extent Report دون تعديل أي استدعاء Output.WriteLine موجود.
    /// </summary>
    public sealed class ReportOutput : ITestOutputHelper
    {
        private readonly ITestOutputHelper _inner;
        private TestRecord? _record;

        private ReportOutput(ITestOutputHelper inner) => _inner = inner;

        public static ITestOutputHelper Wrap(ITestOutputHelper inner)
            => inner is ReportOutput ? inner : new ReportOutput(inner);

        public TestRecord Record => _record ??= TestRegistry.RecordFor(ResolveDisplayName());

        public void WriteLine(string message)
        {
            _inner.WriteLine(message);
            Record.AddLog(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _inner.WriteLine(format, args);
            Record.AddLog(string.Format(format, args));
        }

        // xUnit 2 يحفظ ITest في حقل خاص داخل TestOutputHelper؛ نقرأه بالانعكاس لنربط السجل باسم الاختبار.
        private string ResolveDisplayName()
        {
            foreach (var f in _inner.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                if (typeof(ITest).IsAssignableFrom(f.FieldType) && f.GetValue(_inner) is ITest t)
                    return t.DisplayName;
            return "unknown";
        }
    }
}
