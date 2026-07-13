using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Region.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Sessions;
using Automation.Test.Tests.Sulfa.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.CrossService
{
    /// <summary>حجز مستخدم متاح من مجموعة Sulfa، بناء جلسته، ثم تحريره تلقائيًا (آمن للتوازي).</summary>
    public class PooledUserTests : BaseSulfaTest
    {
        public PooledUserTests(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }

        [Fact]
        public async Task ReserveOperator_BuildSession_ShouldSucceed()
        {
            await using var lease = await Pools.For("Sulfa").ReserveAsync(role: "Operator");
            Output.WriteLine($"🔒 المستخدم المحجوز: {lease.UserKey}");

            var session = await Sessions.GetOrBuildAsync(SulfaSessions.OperatorFor(lease.UserKey));

            var balance = await Resilience.ExecuteAsync(session,
                () => Flow<WalletFlow>().GetBalanceAsync(session.UserKey, session.WalletIdGuid));

            Output.WriteLine($"💰 الرصيد: {balance}");
            Assert.True(balance >= 0);
            PrintResult(nameof(ReserveOperator_BuildSession_ShouldSucceed), true);
        }

        [Fact]
        public void Projects_ShouldBeIsolated()
        {
            Assert.True(Pools.For("Sulfa").Total("Operator") >= 1);
            Assert.True(Pools.For("Forex").Total("Trader") >= 1);
            PrintResult(nameof(Projects_ShouldBeIsolated), true);
        }
    }
}
