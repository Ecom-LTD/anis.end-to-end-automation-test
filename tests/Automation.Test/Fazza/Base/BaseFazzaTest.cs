using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Services.AccountReadiness.Flow;
using Automation.Framework.Shared;
using Automation.Test.Fixtures.Fazza;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Fazza.Base
{
    [Collection("Fazza Collection")]
    public abstract class BaseFazzaTest
    {
        protected readonly ITestOutputHelper Output;
        protected readonly FazzaFixture Fixture;

        protected BaseFazzaTest(ITestOutputHelper output, FazzaFixture fixture)
        {
            Output = output;
            Fixture = fixture;

            // نمرر الـ Output للـ Fixture لكي نرى رسائل التهيئة
            Fixture.Output = output;
        }

        // ================================================================
        // الجلسات
        // ================================================================
        protected TestSession Dashboard => Fixture.Dashboard;
        protected TestSession FazzaUser1 => Fixture.FazzaUser1;
        protected TestSession FazzaUser2 => Fixture.FazzaUser2;

        // ================================================================
        // الـ Flows
        // ================================================================
        protected FazzaTopUpFlow FazzaTopUp => Fixture.Flow<FazzaTopUpFlow>();
        protected WalletFlow Wallet => Fixture.Flow<WalletFlow>();
        protected AccountReadinessFlow AccountReadiness => Fixture.Flow<AccountReadinessFlow>();

        // دوال مساعدة للـ Retry
        protected Task<T> ExecuteWithRetryAsync<T>(TestSession session, Func<Task<T>> action)
            => Fixture.Resilience.ExecuteAsync(session, action);

        // ================================================================
        // Helper: طباعة النتيجة
        // ================================================================
        protected void PrintResult(string testName, bool success, string? message = null)
        {
            var separator = new string('=', 60);
            Output.WriteLine($"\n{separator}");
            Output.WriteLine($"🧪 {testName}: {(success ? "✅ PASSED" : "❌ FAILED")}");
            if (!string.IsNullOrWhiteSpace(message))
                Output.WriteLine($"📝 {message}");
            Output.WriteLine(separator);
        }
    }
}