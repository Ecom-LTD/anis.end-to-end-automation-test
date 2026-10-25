using Automation.Framework.Services.Account.Flow;
using Automation.Framework.Services.AccountReadiness.Flow;
using Automation.Framework.Services.CashFlowReport.Flow;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.Region.Flow;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Fazza.Base
{
    [Collection(CollectionNames.Fazza)]
    public abstract class BaseFazzaTest
    {
        public readonly ITestOutputHelper Output;
        public readonly FazzaFixture Fixture;

        protected BaseFazzaTest(ITestOutputHelper output, FazzaFixture fixture)
        {
            Output = output;
            Fixture = fixture;
          //  Fixture.Log = msg => Output.WriteLine(msg);
            //         Fixture.Output = output;
        }

        // الجلسات
        public TestSession Dashboard => Fixture.Dashboard;
        public TestSession FazzaUser1 => Fixture.FazzaUser1;
        public TestSession FazzaUser2 => Fixture.FazzaUser2;

        // الـ Flows
        public FazzaTopUpFlow FazzaTopUp => Fixture.Flow<FazzaTopUpFlow>();
        public WalletFlow Wallet => Fixture.Flow<WalletFlow>();
       
        public TransferFlow Transfer => Fixture.Flow<TransferFlow>();
        public RegionFlow Region => Fixture.Flow<RegionFlow>();
        public CashflowReportFlow CashFlow => Fixture.Flow<CashflowReportFlow>();

        public AccountReadinessFlow AccountReadiness => Fixture.Flow<AccountReadinessFlow>();
        public FazzaLimitReadinessFlow FazzaLimitReadiness => Fixture.Flow<FazzaLimitReadinessFlow>();

        // Helper للـ Retry
        public Task<T> ExecuteWithRetryAsync<T>(TestSession session, Func<Task<T>> action)
            => Fixture.Resilience.ExecuteAsync(session, action);

        // Helper للطباعة
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