using Automation.Framework.Core.Session;
using Automation.Framework.Core.UserPool;
using Automation.Framework.Services.Account.Flow;
using Automation.Framework.Services.Almusher.Flow;
using Automation.Framework.Services.Identity.Flow;
using Automation.Framework.Services.Region.Flow;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;
using Automation.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher
{
    [Collection("Almusher Collection")]
    public class BaseAlmusherTest
    {
        protected readonly ITestOutputHelper Output;
        protected readonly AlmuhserFixture Fixture;

        protected BaseAlmusherTest(ITestOutputHelper output, AlmuhserFixture fixture)
        {
            Output = output;
            Fixture = fixture;
        }

        // ================================================================
        // ✅ الجلسات الأساسية
        // ================================================================

        protected TestSession AnisPay => Fixture.AnisPay;
        protected TestSession AnisCard => Fixture.AnisCard;
        protected TestSession Hreysh => Fixture.Hreysh;
        protected TestSession Commission => Fixture.Commission;
        protected TestSession Profit => Fixture.Profit;
        protected TestSession Dashboard => Fixture.Dashboard;

        // ================================================================
        // ✅ محافظ Anis Card (LYD و USD)
        // ================================================================

        /// <summary>
        /// جلسة Anis Card مع محفظة LYD
        /// </summary>
        protected TestSession AnisCardLyd => Fixture.AnisCardLyd;

        /// <summary>
        /// جلسة Anis Card مع محفظة USD
        /// </summary>
        protected TestSession AnisCardUsd => Fixture.AnisCardUsd;

        // ================================================================
        // ✅ معرفات المحافظ
        // ================================================================

        /// <summary>
        /// معرف محفظة Anis Card بالـ LYD
        /// </summary>
        protected string AnisCardLydWalletId => Fixture.AnisCardLydWalletId;

        /// <summary>
        /// معرف محفظة Anis Card بالـ USD
        /// </summary>
        protected string AnisCardUsdWalletId => Fixture.AnisCardUsdWalletId;

        // ================================================================
        // ✅ Flows
        // ================================================================

        protected WalletFlow Wallet => Fixture.Flow<WalletFlow>();
        protected TransferFlow Transfer => Fixture.Flow<TransferFlow>();
        protected AccountFlow Accounts => Fixture.Flow<AccountFlow>();
        protected AuthenticationFlow Auth => Fixture.Flow<AuthenticationFlow>();
        protected RegionFlow Region => Fixture.Flow<RegionFlow>();
        protected AlMusheerFlow Almusher => Fixture.Flow<AlMusheerFlow>();

        // ================================================================
        // ✅ البنية
        // ================================================================

        protected SessionCache Sessions => Fixture.Sessions;
        protected UserPoolRegistry Pools => Fixture.Pools;
        protected ResilientSession Resilience => Fixture.Resilience;
        protected T Flow<T>() where T : notnull => Fixture.Flow<T>();

        // ================================================================
        // ✅ إعادة المحاولة
        // ================================================================

        protected Task<T> ExecuteWithRetryAsync<T>(TestSession session, Func<Task<T>> action, int maxRetries = 1)
            => Resilience.ExecuteAsync(session, action, maxRetries);

        // ================================================================
        // ✅ طباعة النتيجة
        // ================================================================

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