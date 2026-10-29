using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace Automation.Test.Infrastructure.Reporting
{
    /// <summary>
    /// يبني Extent Report: جدول ملخص بالأعمدة المطلوبة، ثم عقدة تفصيلية لكل اختبار
    /// (البيانات المدخلة، قبل/بعد التنفيذ، المقارنات) كما سُجّلت عبر TestOutputExtensions.
    ///
    /// الجداول HTML خاصة (وليست MarkupHelper) داخل حاوية قابلة للتمرير أفقيًا وعموديًا،
    /// لأن عمود Details في Extent ضيّق ويكسر الكلمات.
    /// </summary>
    public static class ExtentReportWriter
    {
        private static readonly string[] Header =
            { "رقم الاختبار", "السيناريو المنفذ", "النتيجة المتوقعة", "النتيجة الحقيقية", "حالة الاختبار", "سبب الفشل" };

        // الأعمدة الطويلة (المتوقع، الفعلي، سبب الفشل) تلتف داخل عرض محدد؛ البقية في سطر واحد.
        private static readonly int[] WrapColumns = { 2, 3, 5 };

        private const string Css = @"
.rpt-scroll { overflow: auto; max-height: 70vh; max-width: 75vw; border: 1px solid #d1d5db; border-radius: 6px; margin: 4px 0; }
.rpt-table { border-collapse: collapse; direction: rtl; font-size: 13px; min-width: 100%; }
.rpt-table th { position: sticky; top: 0; z-index: 1; background: #f3f4f6; font-weight: 600; }
.rpt-table th, .rpt-table td { padding: 8px 12px; border: 1px solid #e5e7eb; text-align: right; vertical-align: top;
  white-space: nowrap; word-break: keep-all; overflow-wrap: normal; }
.rpt-table td.wrap { white-space: pre-wrap; min-width: 240px; max-width: 520px; overflow-wrap: anywhere; }
.rpt-table tr.failed td { background: #fef2f2; }
.rpt-table tr.passed td:nth-child(5) { color: #15803d; font-weight: 600; }
.rpt-table tr.failed td:nth-child(5) { color: #b91c1c; font-weight: 600; }
.rpt-log { overflow: auto; max-height: 60vh; max-width: 75vw; background: #f9fafb; border: 1px solid #e5e7eb;
  border-radius: 6px; padding: 10px; direction: rtl; text-align: right; white-space: pre; font-size: 12.5px; margin: 0; }
.rpt-summary { direction: rtl; font-weight: 600; margin: 6px 0; }
";

        public static string? Write(IReadOnlyList<TestResultRow> results)
        {
            if (results.Count == 0) return null;

            var rows = results
                .OrderBy(r => r.ClassName, StringComparer.Ordinal)
                .ThenBy(r => r.DisplayName, StringComparer.Ordinal)
                .Select((r, i) => (Id: $"TC-{i + 1:000}", Row: r))
                .ToList();

            var dir = ResolveOutputDirectory();
            Directory.CreateDirectory(dir);

            var baseName = ReportBaseName(results);
            var serial = NextSerial(dir, baseName);
            var reportTitle = $"{baseName} #{serial:000}";
            var path = Path.Combine(dir, $"{baseName}_{serial:000}.html");

            var spark = new ExtentSparkReporter(path);
            spark.Config.DocumentTitle = reportTitle;
            spark.Config.ReportName = reportTitle;
            spark.Config.Theme = Theme.Standard;
            spark.Config.Encoding = "utf-8";
            spark.Config.CSS = Css;

            var extent = new ExtentReports();
            extent.AttachReporter(spark);
            extent.AddSystemInfo("Report", reportTitle);
            extent.AddSystemInfo("Machine", Environment.MachineName);
            extent.AddSystemInfo("Run date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // ----- جدول الملخص (الأعمدة المطلوبة) -----
            var summary = extent.CreateTest("📋 ملخص التنفيذ");
            summary.Info(
                $"<div class=\"rpt-summary\">الإجمالي: {rows.Count} | نجح: {rows.Count(x => x.Row.Status == "Passed")} | " +
                $"فشل: {rows.Count(x => x.Row.Status == "Failed")} | تخطّي: {rows.Count(x => x.Row.Status == "Skipped")}</div>");
            summary.Info(Table(Header, rows.Select(x => (SummaryCells(x.Id, x.Row, 400), x.Row.Status)), WrapColumns));

            // ----- تفاصيل كل اختبار -----
            foreach (var (id, r) in rows)
            {
                var node = extent.CreateTest($"{id} · {Scenario(r)}", r.DisplayName);
                foreach (var cat in r.Categories) node.AssignCategory(cat);
                node.AssignCategory(ShortClass(r.ClassName));

                node.Info(Table(Header, new[] { (SummaryCells(id, r, 1500), r.Status) }, WrapColumns));

                if (r.Record is { Comparisons.Count: > 0 } rec)
                {
                    var cmpRows = rec.Comparisons.Select(c =>
                        (new[] { c.Label, c.Expected, c.Actual, c.Passed ? "✅ مطابق" : "❌ غير مطابق" },
                         c.Passed ? "Passed" : "Failed"));
                    node.Info(Table(new[] { "المقارنة", "المتوقع", "الفعلي", "النتيجة" }, cmpRows, new[] { 1, 2 }));
                }

                if (r.Record is { Log.Count: > 0 } log)
                    node.Info($"<pre class=\"rpt-log\">{Enc(string.Join(Environment.NewLine, log.Log))}</pre>");

                switch (r.Status)
                {
                    case "Passed": node.Pass("نجح"); break;
                    case "Skipped": node.Skip(Enc(Trim(r.FailureMessage, 300))); break;
                    default:
                        node.Fail($"<pre class=\"rpt-log\">{Enc(r.FailureMessage + Environment.NewLine + r.StackTrace)}</pre>");
                        break;
                }
            }

            extent.Flush();
            Console.WriteLine($"[Reporting] Extent report: {path}");
            return path;
        }

        private static string[] SummaryCells(string id, TestResultRow r, int failureMax)
        {
            var (expected, actual) = ExpectedActual(r);
            return new[]
            {
                id,
                Scenario(r),
                expected,
                actual,
                StatusText(r.Status),
                r.Status == "Failed" ? Trim(r.FailureMessage, failureMax) : "-"
            };
        }

        /// <summary>جدول HTML داخل حاوية تمرير (أفقي + عمودي) مع ترويسة ثابتة.</summary>
        private static string Table(string[] headers, IEnumerable<(string[] Cells, string Status)> rows, int[] wrapColumns)
        {
            var sb = new StringBuilder("<div class=\"rpt-scroll\"><table class=\"rpt-table\"><thead><tr>");
            foreach (var h in headers) sb.Append("<th>").Append(Enc(h)).Append("</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var (cells, status) in rows)
            {
                sb.Append("<tr class=\"").Append(status.ToLowerInvariant()).Append("\">");
                for (var c = 0; c < cells.Length; c++)
                {
                    sb.Append(wrapColumns.Contains(c) ? "<td class=\"wrap\">" : "<td>")
                      .Append(Enc(cells[c]))
                      .Append("</td>");
                }
                sb.Append("</tr>");
            }

            return sb.Append("</tbody></table></div>").ToString();
        }

        private static string Scenario(TestResultRow r)
            => !string.IsNullOrWhiteSpace(r.Record?.Scenario) ? r.Record!.Scenario! : Humanize(r.MethodName);

        /// <summary>
        /// المتوقع/الفعلي: من المقارنات المسجّلة إن وُجدت، وإلا من نص Expected/Actual في رسالة xUnit،
        /// وإلا نص عام يصف النجاح أو سبب الفشل.
        /// </summary>
        private static (string Expected, string Actual) ExpectedActual(TestResultRow r)
        {
            var comps = r.Record?.Comparisons;
            if (comps is { Count: > 0 })
            {
                var shown = r.Status == "Failed" && comps.Any(c => !c.Passed)
                    ? comps.Where(c => !c.Passed).ToList()
                    : comps.ToList();
                return (string.Join(Environment.NewLine, shown.Select(c => $"{c.Label} = {c.Expected}")),
                        string.Join(Environment.NewLine, shown.Select(c => $"{c.Label} = {c.Actual}")));
            }

            var expectedText = r.Record?.ExpectedText;

            if (r.Status == "Failed" && r.FailureMessage is { } msg)
            {
                // يدعم صيغة xUnit (سطر لكل قيمة) وصيغة المشير "Expected X, Actual Y, Diff Z".
                var e = Regex.Match(msg, @"Expected:?\s*([^,\r\n]+)");
                var a = Regex.Match(msg, @"Actual:?\s*([^,\r\n]+)");
                if (e.Success || a.Success)
                    return (e.Success ? e.Groups[1].Value.Trim() : expectedText ?? "-",
                            a.Success ? a.Groups[1].Value.Trim() : "-");
                return (expectedText ?? "اكتمال التنفيذ دون أخطاء", Trim(FirstLine(msg), 200));
            }

            return r.Status == "Passed"
                ? (expectedText ?? "اكتمال التنفيذ دون أخطاء", "اكتمل التنفيذ دون أخطاء")
                : (expectedText ?? "-", "لم يُنفَّذ");
        }

        // ---------- تسمية الملف: اسم الاختبار + رقم تسلسلي ----------

        /// <summary>
        /// اختبار واحد ← اسم الدالة؛ عدة اختبارات من صنف واحد ← اسم الصنف؛ غير ذلك ← FullRun.
        /// </summary>
        private static string ReportBaseName(IReadOnlyList<TestResultRow> results)
        {
            var methods = results.Select(r => $"{r.ClassName}.{r.MethodName}").Distinct().ToList();
            var classes = results.Select(r => r.ClassName).Distinct().ToList();

            var name = methods.Count == 1 ? results[0].MethodName
                : classes.Count == 1 ? ShortClass(classes[0])
                : "FullRun";

            return Regex.Replace(name, @"[^\w\-]", "_");
        }

        private static int NextSerial(string dir, string baseName)
        {
            var pattern = new Regex($@"^{Regex.Escape(baseName)}_(\d+)\.html$", RegexOptions.IgnoreCase);
            var max = Directory.EnumerateFiles(dir, $"{baseName}_*.html")
                .Select(f => pattern.Match(Path.GetFileName(f)))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max();
            return max + 1;
        }

        // ---------- أدوات صغيرة ----------

        private static string StatusText(string status) => status switch
        {
            "Passed" => "✅ ناجح",
            "Failed" => "❌ فاشل",
            _ => "⏭ متخطّى"
        };

        private static string Enc(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        private static string Humanize(string method)
            => Regex.Replace(method.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])", " ");

        private static string ShortClass(string full) => full[(full.LastIndexOf('.') + 1)..];
        private static string FirstLine(string s) => s.Split('\n', 2)[0].Trim();
        private static string Trim(string? s, int max)
            => string.IsNullOrEmpty(s) ? "-" : s.Length <= max ? s : s[..max] + "…";

        // التقرير يُكتب بجوار الحل (TestReports) إن وُجد .sln، وإلا بجوار الملف التنفيذي.
        private static string ResolveOutputDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (dir.GetFiles("*.sln").Length > 0) return Path.Combine(dir.FullName, "TestReports");
                dir = dir.Parent;
            }
            return Path.Combine(AppContext.BaseDirectory, "TestReports");
        }
    }
}
