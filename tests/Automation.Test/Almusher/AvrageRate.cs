using Automation.Framework.Core.Enums;
using Automation.Test.Almusher;
using Automation.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Almusher
{
    [Collection("Almusher Collection")]
    public class AverageRateDisplayTests : BaseAlmusherTest
    {
        public AverageRateDisplayTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task DisplayAverageRateInfo()
        {
            // جلب معرف محفظة USD
            var walletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardUsd.UserKey,
                CurrencyType.USD);

            // جلب بيانات متوسط السعر
            var response = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                walletId.ToString());

            var data = response.Data;

            // عرض البيانات
            Output.WriteLine("\n📊 Average Rate Data:");
            Output.WriteLine($"   Wallet ID:      {data.WalletId}");
            Output.WriteLine($"   Balance:        {data.Balance:F4}");
            Output.WriteLine($"   Average Rate:   {data.AverageRate:F10}");
            Output.WriteLine($"   Known Amount:   {data.KnownRateAmount:F4}");
            Output.WriteLine($"   Unknown Amount: {data.UnknownRateAmount:F4}");
            Output.WriteLine($"   Estimated LYD:  {data.KnownRateBalanceEstimatedLydAmount:F10}");
            Output.WriteLine($"   Subscription:   {data.SubscriptionType}");
        }
    }
}