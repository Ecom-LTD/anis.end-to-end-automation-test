namespace Automation.Test.Infrastructure.Reporting
{
    public sealed record ComparisonEntry(string Label, string Expected, string Actual, bool Passed);

    /// <summary>كل ما يُجمع عن اختبار واحد أثناء تنفيذه (سجل، مقارنات، سيناريو، متوقع).</summary>
    public sealed class TestRecord
    {
        private readonly object _gate = new();
        private readonly List<string> _log = new();
        private readonly List<ComparisonEntry> _comparisons = new();

        public string? Scenario { get; set; }
        public string? ExpectedText { get; set; }

        public void AddLog(string line) { lock (_gate) _log.Add(line); }
        public void AddComparison(ComparisonEntry c) { lock (_gate) _comparisons.Add(c); }

        public IReadOnlyList<string> Log { get { lock (_gate) return _log.ToList(); } }
        public IReadOnlyList<ComparisonEntry> Comparisons { get { lock (_gate) return _comparisons.ToList(); } }
    }

    /// <summary>نتيجة اختبار منتهٍ كما تصل من xUnit.</summary>
    public sealed record TestResultRow(
        string DisplayName,
        string ClassName,
        string MethodName,
        string Status,            // Passed | Failed | Skipped
        decimal DurationSeconds,
        string? FailureMessage,
        string? StackTrace,
        IReadOnlyList<string> Categories,
        TestRecord? Record);
}
