using Automation.Framework.Core.Enums;
using Automation.Framework.Core.Http;
using Automation.Framework.Helpers.Almusher;
using Automation.Framework.Services.Almusher.Models;
using Automation.Test.Almusher;
using Automation.Test.Fixtures;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher.USD_TO_EGB
{
    [Collection("Almusher Collection")]
    public class ForeignToForeignExchangeTests : BaseAlmusherTest
    {
        public ForeignToForeignExchangeTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        // ================================================================
        // ✅ اختبار: صرف أجنبي بسيط (USD → EGB) بدون إضافات
        // ================================================================
        [Fact]
        public async Task ForeignToForeignSimpleExchange_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف أجنبي بسيط (USD → EGB)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyAmount = 100m;      // شراء EGB
            decimal sellAmount = 50m;      // بيع USD

            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ البيع (USD): {sellAmount:F2}");
            Output.WriteLine($"   💰 مبلغ الشراء (EGB): {buyAmount:F2}");

   


            // ================================================================
            // 3. جلب المحافظ المطلوبة
            // ================================================================
            Output.WriteLine("\n📝 جلب المحافظ...");

            var anisCardEgbWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCard.UserKey,
                CurrencyType.EGB);

            var anisCardUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardUsd.UserKey,
                CurrencyType.USD);

            var anisPayUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisPay.UserKey,
                CurrencyType.USD);

            var hreyshEgbWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Hreysh.UserKey,
                CurrencyType.EGB);

            Output.WriteLine($"   📋 AnisCard EGB Wallet ID: {anisCardEgbWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay USD Wallet ID: {anisPayUsdWalletId}");
            Output.WriteLine($"   📋 Hreysh EGB Wallet ID: {hreyshEgbWalletId}");

            // ================================================================
            // 4. جلب الأرصدة القديمة
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة...");

            var anisPayUsdOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayUsdWalletId);
            var aniscardEgbOld = await Wallet.GetBalanceAsync(AnisCard.UserKey, anisCardEgbWalletId);
            var aniscardUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshEgbOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshEgbWalletId);

            Output.WriteLine($"   💰 AnisPay USD (مستقبل USD): {anisPayUsdOld:F10}");
            Output.WriteLine($"   💰 AnisCard EGB (مرسل EGB): {aniscardEgbOld:F10}");
            Output.WriteLine($"   💰 AnisCard USD (مرسل USD): {aniscardUsdOld:F10}");
            Output.WriteLine($"   💰 Hreysh EGB (مستقبل EGB): {hreyshEgbOld:F10}");

            // ================================================================
            // 5. جلب LydRate
            // ================================================================
            Output.WriteLine("\n📈 جلب LydRate...");

            decimal lydRate = 0;
            decimal oldAvgBalance = 0;
          
            // ✅ جلب متوسط السعر القديم مع التعامل مع 404


            try
            {
                var avgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardUsdWalletId.ToString());

                Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

                lydRate = avgResponse.Data.AverageRate;
                oldAvgBalance = avgResponse.Data.Balance;

                Output.WriteLine($"   📊 LydRate (LYD/USD): {lydRate:F10}");
                Output.WriteLine($"   📊 AVGUSDBALANCE (LYD/USD): {oldAvgBalance:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ LydRate not found - باستخدام القيمة الافتراضية 0");
                lydRate = 0;
                oldAvgBalance = 0;
            }
            // ================================================================
            // 5.1 جلب بيانات محفظة المشتري بالعملة المصرية
            // ================================================================

            decimal oldEGBEstimatedBalance = 0;
            decimal oldEGBAvgBalance = 0;
            try
            {
                var avgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardEgbWalletId.ToString());

                Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

                oldEGBAvgBalance = avgResponse.Data.Balance;
                oldEGBEstimatedBalance = avgResponse.Data.KnownRateBalanceEstimatedLydAmount;

                Output.WriteLine($"   📊 oldEGBAvgBalance (LYD/USD): {oldEGBAvgBalance:F10}");
                Output.WriteLine($"   📊 oldEGBEstimatedBalance (LYD/USD): {oldEGBEstimatedBalance:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ LydRate not found - باستخدام القيمة الافتراضية 0");
                oldEGBEstimatedBalance = 0;
                oldEGBAvgBalance = 0;
            }
            // ================================================================
            // . حساب سعر الصرف المتوقع
            // ================================================================
            var lydcost = sellAmount * lydRate;
            Output.WriteLine($"   💰 LydCost: {lydcost:F10}");
            decimal expectedRate = lydcost / buyAmount;  // 100 / 50 = 2.0

            Output.WriteLine($"   📈 سعر الصرف المتوقع: {expectedRate:F10}");

            // ================================================================
            // 6. إنشاء سلسلة الدفع
            // ================================================================
            Output.WriteLine("\n🔗 إنشاء سلسلة الدفع...");

            var chainResponse = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

            Output.WriteLine($"   📋 Status Code: {(int)chainResponse.StatusCode} - {chainResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);

            var chainId = Guid.Parse(chainResponse.Data.Id);
            Output.WriteLine($"   📋 Chain ID: {chainId}");
            Assert.NotEqual(Guid.Empty, chainId);

            // ================================================================
            // 7. بناء طلب الصرف الأجنبي
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف الأجنبي...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateForeignExchangeRequest(
                operationId: operationId,
                buyCreditorWalletId: hreyshEgbWalletId.ToString(),
                buyDebitorWalletId: anisCardEgbWalletId.ToString(),
                sellCreditorWalletId: anisCardUsdWalletId.ToString(),
                sellDebitorWalletId: anisPayUsdWalletId.ToString(),
                buyAmount: buyAmount,
                sellAmount: sellAmount,
                lydRate: lydRate,
                detailedStatement: "Simple foreign to foreign exchange")
                .Build();

            Output.WriteLine($"   📋 Operation ID (sent): {operationId}");
            Output.WriteLine($"   🔹 Buy (EGB): {hreyshEgbWalletId} → {anisCardEgbWalletId}");
            Output.WriteLine($"   🔹 Sell (USD): {anisCardUsdWalletId} → {anisPayUsdWalletId}");

            // ================================================================
            // 8. تنفيذ الصرف
            // ================================================================
            Output.WriteLine("\n💱 تنفيذ الصرف...");

            var exchangeResponse = await Almusher.ForeignToForeignExchangeAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                request);

            Output.WriteLine($"   📋 Status Code: {(int)exchangeResponse.StatusCode} - {exchangeResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, exchangeResponse.StatusCode);

            var idFromResponse = exchangeResponse.Data?.Data?.OperationId;
            Output.WriteLine($"   📋 Operation ID from response: {idFromResponse}");

            // ================================================================
            // ⏳ 9. انتظار 5 ثواني
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 5 ثواني...");
            await Task.Delay(5000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 10. التحقق من تفاصيل العملية
            // ================================================================
            Output.WriteLine("\n🔍 التحقق من تفاصيل العملية...");

            var detailsResponse = await Almusher.GetExchangeDetailsAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Output.WriteLine($"   📋 Status Code: {(int)detailsResponse.StatusCode} - {detailsResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

            var details = detailsResponse.Data;
            var actualBuy = details.CurrencyExchangeBuy?.Amount ?? 0m;
            var actualSell = details.CurrencyExchangeSell?.Amount ?? 0m;
            var actualRate = details.ConversionRate;

            Output.WriteLine($"\n📊 المبالغ الفعلية:");
            Output.WriteLine($"   💵 مبلغ الشراء الفعلي: {actualBuy:F10}");
            Output.WriteLine($"   💰 مبلغ البيع الفعلي: {actualSell:F10}");
            Output.WriteLine($"   📈 سعر الصرف الفعلي: {actualRate:F10}");

            // ================================================================
            // 11. التحقق من القيم
            // ================================================================
            Output.WriteLine("\n✅ التحقق من القيم:");

            // ✅ التحقق 1: مبلغ الشراء
            var (isBuyEqual, _, buyMsg) = DecimalComparer.Compare(buyAmount, actualBuy, "مبلغ الشراء");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ البيع
            var (isSellEqual, _, sellMsg) = DecimalComparer.Compare(sellAmount, actualSell, "مبلغ البيع");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 3: سعر الصرف
            var (isRateEqual, _, rateMsg) = DecimalComparer.Compare(expectedRate, actualRate, "سعر الصرف");
            Assert.True(isRateEqual, rateMsg);
            Output.WriteLine($"   ✅ {rateMsg}");

            // ================================================================
            // 12. تأكيد العملية
            // ================================================================
            Output.WriteLine("\n✅ تأكيد العملية...");

            var confirmResponse = await Almusher.ConfirmExchangeAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Output.WriteLine($"   📋 Status Code: {(int)confirmResponse.StatusCode} - {confirmResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

            var exchangeId = confirmResponse.Data?.Id;
            Output.WriteLine($"   📋 Exchange ID: {exchangeId ?? "(null)"}");
            Assert.NotEmpty(exchangeId);

            Output.WriteLine($"   ✅ تم تأكيد العملية بنجاح");

            // ================================================================
            // ⏳ 13. انتظار 6 ثواني لتحديث الأرصدة
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 6 ثواني لتحديث الأرصدة...");
            await Task.Delay(6000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 14. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayUsdNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayUsdWalletId);
            var aniscardEgbNew = await Wallet.GetBalanceAsync(AnisCard.UserKey, anisCardEgbWalletId);
            var aniscardUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshEgbNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshEgbWalletId);

            Output.WriteLine($"   💰 AnisPay USD: {anisPayUsdOld:F3} → {anisPayUsdNew:F3}");
            Output.WriteLine($"   💰 AnisCard EGB: {aniscardEgbOld:F3} → {aniscardEgbNew:F3}");
            Output.WriteLine($"   💰 AnisCard USD: {aniscardUsdOld:F3} → {aniscardUsdNew:F3}");
            Output.WriteLine($"   💰 Hreysh EGB: {hreyshEgbOld:F3} → {hreyshEgbNew:F3}");

            // ✅ التحقق 4: AnisPay USD (زاد بمقدار sellAmount)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.Compare(
                anisPayUsdOld + sellAmount,
                anisPayUsdNew,
                "AnisPay USD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 5: AnisCard EGB (نقص بمقدار buyAmount)
            var (isAniscardEgbEqual, _, aniscardEgbMsg) = DecimalComparer.Compare(
                aniscardEgbOld + buyAmount,
                aniscardEgbNew,
                "AnisCard EGB");
            Assert.True(isAniscardEgbEqual, aniscardEgbMsg);
            Output.WriteLine($"   ✅ {aniscardEgbMsg}");

            // ✅ التحقق 6: AnisCard USD (نقص بمقدار sellAmount)
            var (isAniscardUsdEqual, _, aniscardUsdMsg) = DecimalComparer.Compare(
                aniscardUsdOld - sellAmount,
                aniscardUsdNew,
                "AnisCard USD");
            Assert.True(isAniscardUsdEqual, aniscardUsdMsg);
            Output.WriteLine($"   ✅ {aniscardUsdMsg}");

            // ✅ التحقق 7: Hreysh EGB (نقص بمقدار buyAmount)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.Compare(
                hreyshEgbOld - buyAmount,
                hreyshEgbNew,
                "Hreysh EGB");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ================================================================
            // ✅ 14.5 التحقق من USD Average Balance بعد الصرف
            // ================================================================
            Output.WriteLine("\n📊 التحقق من Average Balance بعد الصرف...");

            try
            {
                // ✅ جلب New Average Balance بعد الصرف
                var avgResponseAfter = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardUsdWalletId.ToString());

                if (avgResponseAfter.StatusCode == HttpStatusCode.OK)
                {
                    var newAvgBalance = avgResponseAfter.Data.Balance;
                    var newLydRate = avgResponseAfter.Data.AverageRate;
                    Output.WriteLine($"   📊 New Average Balance: {newAvgBalance:F4}");
                    Output.WriteLine($"   📊 New Average Rate: {newLydRate:F10}");

                    // ✅ حساب Expected Average Balance
                    var expAvgBalance = oldAvgBalance - sellAmount;
                    Output.WriteLine($"   📊 Expected Average Balance: {expAvgBalance:F4} = {oldAvgBalance:F4} - {buyAmount:F4}");

                    // ✅ مقارنة New Balance مع Expected Balance
                    var (isAvgBalanceEqual, _, avgBalanceMsg) = DecimalComparer.CompareBalance(
                        expAvgBalance,
                        newAvgBalance,
                        "USD Average Balance");

                    Assert.True(isAvgBalanceEqual, avgBalanceMsg);
                    Output.WriteLine($"   ✅ {avgBalanceMsg}");
                    // يجب ان لا بتأثر سعر صرف الدولار
                    var (isAvgBalanceEqualanisUsd, _, anisandavgBalanceMsg) = DecimalComparer.CompareBalance(
                  lydRate,
                  newLydRate,
                  "USD Avrage Rate");
                    Assert.True(isAvgBalanceEqual, anisandavgBalanceMsg);
                    Output.WriteLine($"   ✅ {anisandavgBalanceMsg}");
                }
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ Average Rate not found بعد الصرف");
            }


            // ================================================================
            // ✅ 14.6 التحقق من EGB Average Balance بعد الصرف
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف العملة المصريى (بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع (إذا كانت القيم القديمة موجودة)
            decimal expectedNewAverageRate = 0;
            if (oldEGBAvgBalance != 0 || oldEGBEstimatedBalance != 0)
            {
                expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                    oldEGBAvgBalance,
                    oldEGBEstimatedBalance,
                    buyAmount,
                    lydcost
                );
                Output.WriteLine($"   📊 Expected New EGB Average Rate: {expectedNewAverageRate:F10}");
            }
            else
            {
                Output.WriteLine("   ℹ️ لا توجد بيانات سابقة لحساب Average Rate المتوقع، سيتم تجاهل التحقق");
            }

            // ✅ جلب متوسط السعر الجديد مع التعامل مع 404
            decimal newAverageRate = 0;
            decimal newBalance = 0;
            decimal newEstimatedLyd = 0;

            try
            {
                var newAvgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardEgbWalletId.ToString());

                newAverageRate = newAvgResponse.Data.AverageRate;
                newBalance = newAvgResponse.Data.Balance;
                newEstimatedLyd = newAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

                Output.WriteLine($"   📊 New EGB Average Rate (actual): {newAverageRate:F10}");
                Output.WriteLine($"   📊 New EGB Balance: {newBalance:F10}");
                Output.WriteLine($"   📊 New EGB Estimated LYD: {newEstimatedLyd:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ New Average Rate info not found (404) - using default values (0)");
            }

            // ✅ التحقق من متوسط سعر الصرف الجديد (فقط إذا كانت البيانات متوفرة)
            if (oldEGBAvgBalance != 0 || oldEGBEstimatedBalance != 0)
            {
                var (isAvgRateEqual, _, avgRateMsg) = DecimalComparer.CompareRate(
                    expectedNewAverageRate,
                    newAverageRate,
                    "متوسط سعر الصرف الجديد");
                Assert.True(isAvgRateEqual, avgRateMsg);
                Output.WriteLine($"   ✅ {avgRateMsg}");
            }
            else
            {
                Output.WriteLine("   ⏭️ تم تخطي التحقق من Average Rate لعدم وجود بيانات سابقة");
            }
            // ================================================================
            // 15. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف الأجنبي البسيط:");
            Output.WriteLine($"   💵 مبلغ البيع (USD): {sellAmount:F2}");
            Output.WriteLine($"   💰 مبلغ الشراء (EGB): {buyAmount:F2}");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {expectedRate:F10}");
            Output.WriteLine($"   📈 سعر الصرف الفعلي: {actualRate:F10}");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID: {operationId}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ForeignToForeignSimpleExchange_ShouldSucceed), true);
        }
    }
}