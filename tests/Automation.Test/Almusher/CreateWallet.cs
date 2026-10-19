using Automation.Framework.Core.Enums;
using Automation.Framework.Helpers.Almusher;
using Automation.Test.Almusher;
using Automation.Test.Fixtures;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Wallet
{
    [Collection("Almusher Collection")]
    public class CreateWalletTests : BaseAlmusherTest
    {
        public CreateWalletTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        // ================================================================
        // ✅ اختبار: إنشاء محفظة USDT جديدة
        // ================================================================
        [Fact]
        public async Task CreateWallet_ShouldSucceed()
        {
            var RegionName = "Tripoli";

            var RegionId= await Region.GetRegionIdByNameAsync(Dashboard.UserKey, RegionName);



            // 1. استدعاء الـ Flow
            var response = await Wallet.CreateWalletAsync(
                userKey: Dashboard.UserKey,
                subscriptionId: Hreysh.SubscriptionIdGuid,
                regionId: Guid.Parse(RegionId),
                currencyType: CurrencyType.EGB);

            // 2. التحقق من حالة الطلب
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // 3. التحقق من البيانات
            Assert.True(response.Data.Success);
            Assert.Contains("enabled successfully", response.Data.Message);

            // 4. طباعة الرد الخام (للتصحيح)
            Output.WriteLine($"Raw Response: {response.RawBody}");
        }



    }
}