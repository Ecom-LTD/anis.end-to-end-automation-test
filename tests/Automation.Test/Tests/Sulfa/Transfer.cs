using Automation.Framework.Core.Enums;
using Automation.Framework.Services.Region.Flow;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Automation.Test.Scenarios;
using Automation.Test.Tests.Sulfa.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa
{
    public class Transfer : BaseSulfaTest
    {

        public Transfer(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }


        [Fact]
        public async Task UpdateDefaultWallet_ShouldSucceed()
        {
            // Arrange
            Output.WriteLine("\n💰 اختبار: تحديث المحفظة الافتراضية");

            var userKey = SulfaOperator.UserKey;
            var walletId = SulfaOperator.WalletId;  // المحفظة الجديدة

            Output.WriteLine($"📋 المستخدم: {userKey}");
            Output.WriteLine($"📋 المحفظة الجديدة: {walletId}");

            // Act
            var result = await Wallet.UpdateDefaultWalletAsync(userKey, walletId);

            // Assert
            Output.WriteLine($"📋 النتيجة: {result}");
            Assert.Equal("Wallet updated successfully", result);

            PrintResult(nameof(UpdateDefaultWallet_ShouldSucceed), true);
        }


        [Fact]

        public async Task GetWalletBalance_ShouldSucceed()
        {
            Output.WriteLine("\n💰 اختبار: جلب رصيد المحفظة");
            var userKey = SulfaOperator.UserKey;
            var walletId = SulfaOperator.WalletIdGuid;
            Output.WriteLine($"📋 المستخدم: {userKey}");
            Output.WriteLine($"📋 معرف المحفظة: {walletId}");
            var balance = await Wallet.GetBalanceAsync(userKey, walletId);
            Output.WriteLine($"💰 الرصيد الحالي: {balance}");
            Assert.NotNull(balance);
            PrintResult(nameof(GetWalletBalance_ShouldSucceed), true);
        }


        [Fact]
        public async Task TransferMoney_ShouldSucceed()
        {
            Output.WriteLine("\n💸 اختبار: تحويل أموال");

            // ========== 1. جلب البيانات الأساسية ==========
            Output.WriteLine($" business subscreption id: {SulfaBusiness.SubscriptionId}");
            Output.WriteLine($" operator walletid id: {SulfaOperator.WalletId}");
            // جلب معرف المنطقة
            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Output.WriteLine($"📋 Region ID: {regionId}");

            var amount = 6000m;  // المبلغ المراد تحويله

            // ========== 2. جلب الرصيد قبل التحويل ==========

            var senderBalanceBefore = await Wallet.GetBalanceAsync(
                SulfaOperator.UserKey,
                SulfaOperator.WalletIdGuid);

            var receiverBalanceBefore = await Wallet.GetBalanceAsync(
                SulfaBusiness.UserKey,
                SulfaBusiness.WalletIdGuid);

            Output.WriteLine($"💰 رصيد المرسل (SulfaOperator) قبل التحويل: {senderBalanceBefore}");
            Output.WriteLine($"💰 رصيد المستقبل (SulfaBusiness) قبل التحويل: {receiverBalanceBefore}");

            // ========== 3. تنفيذ عملية التحويل ==========

            var result = await Transfer.TransferAsync(
                userKey: SulfaOperator.UserKey,
                fromWalletId: SulfaOperator.WalletId,
                toSubscriptionId: SulfaBusiness.SubscriptionId,
                amount: amount,
                destinationRegionId: regionId);

            Output.WriteLine($"📋 نتيجة التحويل: {result.Message}");
            Assert.True(result.Success, $"Transfer succeed: {result.Message}");

            // ========== 4. جلب الرصيد بعد التحويل ==========

            var senderBalanceAfter = await Wallet.GetBalanceAsync(
                SulfaOperator.UserKey,
                SulfaOperator.WalletIdGuid);

            var receiverBalanceAfter = await Wallet.GetBalanceAsync(
                SulfaBusiness.UserKey,
                SulfaBusiness.WalletIdGuid);

            Output.WriteLine($"💰 رصيد المرسل (SulfaOperator) بعد التحويل: {senderBalanceAfter}");
            Output.WriteLine($"💰 رصيد المستقبل (SulfaBusiness) بعد التحويل: {receiverBalanceAfter}");

            // ========== 5. التحقق من صحة الرصيد ==========

            // التحقق 1: رصيد المرسل انخفض بمقدار المبلغ المحول
            var expectedSenderBalance = senderBalanceBefore - amount;
            Output.WriteLine($"📊 المتوقع للمرسل: {expectedSenderBalance} = {senderBalanceBefore} - {amount}");
            Assert.Equal(expectedSenderBalance, senderBalanceAfter);

            // التحقق 2: رصيد المستقبل زاد بمقدار المبلغ المحول
            var expectedReceiverBalance = receiverBalanceBefore + amount;
            Output.WriteLine($"📊 المتوقع للمستقبل: {expectedReceiverBalance} = {receiverBalanceBefore} + {amount}");
            Assert.Equal(expectedReceiverBalance, receiverBalanceAfter);

            // ========== 6. طباعة النتيجة النهائية ==========

            Output.WriteLine($"✅ التحقق من الرصيد: نجاح");
            Output.WriteLine($"   - المرسل: {senderBalanceBefore} → {senderBalanceAfter} (انخفض بمقدار {amount})");
            Output.WriteLine($"   - المستقبل: {receiverBalanceBefore} → {receiverBalanceAfter} (زاد بمقدار {amount})");

            PrintResult(nameof(TransferMoney_ShouldSucceed), true);
        }



        [Fact]
        public async Task TransferMoney2_ShouldSucceed()
        {
            Output.WriteLine("\n💸 اختبار: تحويل أموال");

            var ctx = new ScenarioContext(TestHost.Services);

            // جلب معرف المنطقة (سنستخدمه في عدة خطوات)
            string regionId = null!;
            decimal amount = 6000;
            Output.WriteLine("\n💸 اختبار: تحويل أموال");



            await new Scenario(ctx)

                .Step("IDS", async c =>
                {
                    // Implementation for IDS step

                    Output.WriteLine($" business subscreption id: {SulfaBusiness.SubscriptionId}");
                    Output.WriteLine($" operator walletid id: {SulfaOperator.WalletId}");
                })
                .Step("جلب معرف المنطقة (Tripoli)", async c =>
                {
                    regionId = await c.Flow<RegionFlow>().GetRegionIdByNameAsync(
                        Dashboard.UserKey,
                        "Tripoli");

                    c.Set("regionId", regionId);
                    Output.WriteLine($"📋 Region ID: {regionId}");
                })

                .Step("جلب رصيد المرسل والمستقبل قبل التحويل", async c =>
                {
                    var senderBalance = await c.Flow<WalletFlow>().GetBalanceAsync(
                        SulfaOperator.UserKey,
                        SulfaOperator.WalletIdGuid);

                    var receiverBalance = await c.Flow<WalletFlow>().GetBalanceAsync(
                        SulfaBusiness.UserKey,
                        SulfaBusiness.WalletIdGuid);

                    c.Set("senderBalanceBefore", senderBalance);
                    c.Set("receiverBalanceBefore", receiverBalance);

                    Output.WriteLine($"💰 رصيد المرسل (SulfaOperator) قبل التحويل: {senderBalance}");
                    Output.WriteLine($"💰 رصيد المستقبل (SulfaBusiness) قبل التحويل: {receiverBalance}");
                })

                .Step($"تنفيذ عملية التحويل بقيمة {amount}", async c =>
                {
                    var result = await c.Flow<TransferFlow>().TransferAsync(
                        userKey: SulfaOperator.UserKey,
                        fromWalletId: SulfaOperator.WalletId,
                        toSubscriptionId: SulfaBusiness.SubscriptionId,
                        amount: amount,
                        destinationRegionId: c.Get<string>("regionId"));

                    c.Set("transferResult", result);
                    c.Set("transferSuccess", result.Success);

                    Output.WriteLine($"📋 نتيجة التحويل: {result.Message}");
                    Assert.True(result.Success, $"فشل التحويل: {result.Message}");
                })

                .Step("جلب رصيد المرسل والمستقبل بعد التحويل", async c =>
                {
                    var senderBalance = await c.Flow<WalletFlow>().GetBalanceAsync(
                        SulfaOperator.UserKey,
                        SulfaOperator.WalletIdGuid);

                    var receiverBalance = await c.Flow<WalletFlow>().GetBalanceAsync(
                        SulfaBusiness.UserKey,
                        SulfaBusiness.WalletIdGuid);

                    c.Set("senderBalanceAfter", senderBalance);
                    c.Set("receiverBalanceAfter", receiverBalance);

                    Output.WriteLine($"💰 رصيد المرسل (SulfaOperator) بعد التحويل: {senderBalance}");
                    Output.WriteLine($"💰 رصيد المستقبل (SulfaBusiness) بعد التحويل: {receiverBalance}");
                })

                .Step("التحقق من صحة الأرصدة بعد التحويل", async c =>
                {
                    var senderBefore = c.Get<decimal>("senderBalanceBefore");
                    var senderAfter = c.Get<decimal>("senderBalanceAfter");
                    var receiverBefore = c.Get<decimal>("receiverBalanceBefore");
                    var receiverAfter = c.Get<decimal>("receiverBalanceAfter");

                    // التحقق 1: رصيد المرسل انخفض بمقدار المبلغ المحول
                    var expectedSenderBalance = senderBefore - amount;
                    Output.WriteLine($"📊 المتوقع للمرسل: {expectedSenderBalance} = {senderBefore} - {amount}");
                    Assert.Equal(expectedSenderBalance, senderAfter);

                    // التحقق 2: رصيد المستقبل زاد بمقدار المبلغ المحول
                    var expectedReceiverBalance = receiverBefore + amount;
                    Output.WriteLine($"📊 المتوقع للمستقبل: {expectedReceiverBalance} = {receiverBefore} + {amount}");
                    Assert.Equal(expectedReceiverBalance, receiverAfter);

                    // طباعة النتيجة النهائية
                    Output.WriteLine($"✅ التحقق من الرصيد: نجاح");
                    Output.WriteLine($"   - المرسل: {senderBefore} → {senderAfter} (انخفض بمقدار {amount})");
                    Output.WriteLine($"   - المستقبل: {receiverBefore} → {receiverAfter} (زاد بمقدار {amount})");

                    // تخزين النتائج للخطوات التالية (لو أردنا إضافة المزيد)
                    c.Set("verificationPassed", true);
                })

                .Step("طباعة النتيجة النهائية", async c =>
                {
                    PrintResult(nameof(TransferMoney_ShouldSucceed),
                        c.Get<bool>("verificationPassed"));
                })

                .RunAsync(Output.WriteLine);
        }


        [Fact]
        public async Task GetIdsForNewUser_ShouldSucceed()
        {
            // ✅ استخدام GetAllIdsAsync لجلب بيانات مستخدم غير موجود في Fixture
            var (walletId, subscriptionId, regionId) = await Wallet.GetAllIdsAsync(
                userKey: SulfaBusiness.UserKey,                    // مفتاح المستخدم
                currencyType: CurrencyType.LYD,
                regionName: "Tripoli",
                holderName: "Cash",
                subscriptionType: SubscriptionType.Business,
                subscriptionName: "sulfa bu");

            Output.WriteLine($"💰 WalletId: {walletId}");
            Output.WriteLine($"📦 SubscriptionId: {subscriptionId}");
            Output.WriteLine($"🗺️ RegionId: {regionId}");

            Assert.NotEqual(Guid.Empty, walletId);
        }
    }
}
