using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Automation.Test.Scenarios;
using Automation.Test.Tests.Sulfa.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.CrossService
{
    /// <summary>سيناريو متعدّد الخدمات: رصيد (Wallet) ← تحويل (Transfer) ← تحقق من التطابق.</summary>
    public class TransferConsistencyScenario : BaseSulfaTest
    {
        public TransferConsistencyScenario(ITestOutputHelper output, SulfaFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task Transfer_ShouldDecreaseSenderBalance_ByAmount()
        {
            const decimal amount = 5m;
            var ctx = new ScenarioContext(TestHost.Services);

            await new Scenario(ctx)
                .Step("جلب رصيد المرسل قبل التحويل", async c =>
                {
                    var before = await c.Flow<WalletFlow>()
                        .GetBalanceAsync(SulfaOperator.UserKey, SulfaOperator.WalletIdGuid);
                    c.Set("before", before);
                    Output.WriteLine($"💰 قبل: {before}");
                })
                .Step("تنفيذ التحويل", async c =>
                {
                    var result = await c.Flow<TransferFlow>().TransferAsync(
                        userKey: SulfaOperator.UserKey,
                        fromWalletId: SulfaOperator.WalletId,
                        toSubscriptionId: SulfaBusiness.SubscriptionId,
                        amount: amount,
                        destinationRegionId: SulfaOperator.RegionId);

                    Output.WriteLine($"📋 {result.Message}");
                    Assert.True(result.Success, $"فشل التحويل: {result.Message}");
                })
                .Step("التحقق من نقص الرصيد بمقدار المبلغ", async c =>
                {
                    var after = await c.Flow<WalletFlow>()
                        .GetBalanceAsync(SulfaOperator.UserKey, SulfaOperator.WalletIdGuid);
                    Output.WriteLine($"💰 بعد: {after}");
                    Assert.Equal(c.Get<decimal>("before") - amount, after);
                })
                .RunAsync(Output.WriteLine);

            PrintResult(nameof(Transfer_ShouldDecreaseSenderBalance_ByAmount), true);
        }
    }
}
