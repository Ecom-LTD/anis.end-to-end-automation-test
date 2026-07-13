using Automation.Framework.Core.Session;
using Automation.Framework.Core.UserPool;
using Automation.Framework.Shared;
using Automation.Test.Fixtures.Fazza;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.CashFlowReport.Flow;
using Automation.Framework.Services.OperatorCashback.Flow;
using Automation.Framework.Services.Cart.Flow;
using Automation.Framework.Services.Catalog.Flow;
using Automation.Framework.Services.Region.Flow;    
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Services.Account.Flow;
using Automation.Framework.Services.Identity.Flow;


using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.Base
{
    [Collection("Sulfa Collection")]
    public abstract class BaseSulfaTest
    {
        protected readonly ITestOutputHelper Output;
        protected readonly SulfaFixture Fixture;

        protected BaseSulfaTest(ITestOutputHelper output, SulfaFixture fixture)
        {
            Output = output;
            Fixture = fixture;
        }

        // الجلسات
        protected TestSession Dashboard => Fixture.Dashboard;
        protected TestSession SulfaBusiness => Fixture.SulfaBusiness;
        protected TestSession SulfaOperator => Fixture.SulfaOperator;
        protected TestSession AdminSulfaOperator => Fixture.AdminSulfaOperator;

        // الخدمات
        protected WalletFlow Wallet => Fixture.Flow<WalletFlow>();
        protected TransferFlow Transfer => Fixture.Flow<TransferFlow>();
        protected AccountFlow Accounts => Fixture.Flow<AccountFlow>();
        protected AuthenticationFlow Auth => Fixture.Flow<AuthenticationFlow>();
        protected CatalogFlow Catalog => Fixture.Flow<CatalogFlow>();
        protected CartFlow Cart => Fixture.Flow<CartFlow>();
        protected FazzaTopUpFlow FazzaTopUp => Fixture.Flow<FazzaTopUpFlow>();
        protected CashflowReportFlow CashFlow => Fixture.Flow<CashflowReportFlow>();
        protected OperatorCashbackFlow OperatorCashback => Fixture.Flow<OperatorCashbackFlow>();
        protected RegionFlow Region => Fixture.Flow<RegionFlow>();
    

        // البنية
        protected SessionCache Sessions => Fixture.Sessions;
        protected UserPoolRegistry Pools => Fixture.Pools;
        protected ResilientSession Resilience => Fixture.Resilience;
        protected T Flow<T>() where T : notnull => Fixture.Flow<T>();

        // إعادة المحاولة المركزية عند 401
        protected Task<T> ExecuteWithRetryAsync<T>(TestSession session, Func<Task<T>> action, int maxRetries = 1)
            => Resilience.ExecuteAsync(session, action, maxRetries);

        protected void PrintResult(string testName, bool success, string? message = null)
        {
            var sep = new string('=', 50);
            Output.WriteLine($"\n{sep}");
            Output.WriteLine($"🧪 {testName}: {(success ? "✅ PASSED" : "❌ FAILED")}");
            if (!string.IsNullOrEmpty(message)) Output.WriteLine($"📝 {message}");
            Output.WriteLine(sep);
        }
    }
}
