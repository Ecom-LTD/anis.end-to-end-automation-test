using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.Test.Infrastructure.Reporting
{
    /// <summary>
    /// TestFramework مخصص: يتدخل في رسائل xUnit فقط لالتقاط نتيجة كل اختبار
    /// ثم يكتب Extent Report عند انتهاء التشغيل. لا يغيّر طريقة تنفيذ الاختبارات.
    /// </summary>
    public sealed class ReportingTestFramework : XunitTestFramework
    {
        public ReportingTestFramework(IMessageSink messageSink) : base(messageSink) { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
            => new ReportingExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

        private sealed class ReportingExecutor : XunitTestFrameworkExecutor
        {
            public ReportingExecutor(AssemblyName name, ISourceInformationProvider source, IMessageSink diagnostics)
                : base(name, source, diagnostics) { }

            protected override async void RunTestCases(
                IEnumerable<IXunitTestCase> testCases,
                IMessageSink executionMessageSink,
                ITestFrameworkExecutionOptions executionOptions)
            {
                var sink = new CapturingSink(executionMessageSink);
                using var runner = new XunitTestAssemblyRunner(
                    TestAssembly, testCases, DiagnosticMessageSink, sink, executionOptions);
                await runner.RunAsync();
            }
        }

        private sealed class CapturingSink : LongLivedMarshalByRefObject, IMessageSink
        {
            private readonly IMessageSink _inner;
            public CapturingSink(IMessageSink inner) => _inner = inner;

            public bool OnMessage(IMessageSinkMessage message)
            {
                try
                {
                    switch (message)
                    {
                        case ITestPassed p:
                            Add(p.Test, "Passed", p.ExecutionTime, null, null);
                            break;
                        case ITestFailed f:
                            Add(f.Test, "Failed", f.ExecutionTime,
                                ExceptionUtility.CombineMessages(f), ExceptionUtility.CombineStackTraces(f));
                            break;
                        case ITestSkipped s:
                            Add(s.Test, "Skipped", 0m, s.Reason, null);
                            break;
                        case ITestAssemblyFinished:
                            // يُكتب التقرير قبل تمرير رسالة الانتهاء كي لا تنتهي العملية قبله.
                            ExtentReportWriter.Write(TestRegistry.Results);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // التقرير خدمة مساعدة: لا يجوز أن يُفشل التشغيل.
                    Console.Error.WriteLine($"[Reporting] {ex.Message}");
                }

                return _inner.OnMessage(message);
            }

            private static void Add(ITest test, string status, decimal seconds, string? message, string? stack)
            {
                var method = test.TestCase.TestMethod;
                var categories = test.TestCase.Traits.TryGetValue("Category", out var c)
                    ? c.ToList()
                    : new List<string>();

                TestRegistry.AddResult(new TestResultRow(
                    test.DisplayName,
                    method.TestClass.Class.Name,
                    method.Method.Name,
                    status,
                    seconds,
                    message,
                    stack,
                    categories,
                    TestRegistry.TryGet(test.DisplayName)));
            }
        }
    }
}
