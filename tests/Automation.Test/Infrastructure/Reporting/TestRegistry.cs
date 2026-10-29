using System.Collections.Concurrent;

namespace Automation.Test.Infrastructure.Reporting
{
    /// <summary>سجل عام (على مستوى العملية) يربط اسم عرض الاختبار بسجله ونتيجته.</summary>
    public static class TestRegistry
    {
        private static readonly ConcurrentDictionary<string, TestRecord> _records = new();
        private static readonly ConcurrentBag<TestResultRow> _results = new();

        public static TestRecord RecordFor(string displayName) => _records.GetOrAdd(displayName, _ => new TestRecord());
        public static TestRecord? TryGet(string displayName) => _records.TryGetValue(displayName, out var r) ? r : null;

        public static void AddResult(TestResultRow row) => _results.Add(row);
        public static IReadOnlyList<TestResultRow> Results => _results.ToList();
    }
}
