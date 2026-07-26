using Automation.Framework.Core.Enums;
using Automation.Framework.Core.Http;
using Automation.Framework.Helpers.Almusher;
using Automation.Framework.Services.Almusher.Models;
using Automation.Test.Almusher;
using Automation.Test.Fixtures;
using Newtonsoft.Json.Linq;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Almusher
{
    [Collection("Almusher Collection")]
    public class ExchangeWithCommissionExcludedTests : BaseAlmusherTest
    {
        public ExchangeWithCommissionExcludedTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task ExchangeWithCommissionExcluded_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف LYD → USD مع عمولة غير مضمنة (Commission Excluded)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyUsd = 50;           // مبلغ الشراء الأساسي بالدولار
            decimal sellLyd = 100;          // مبلغ البيع بالدينار
            decimal commissionAmount =10.5m;   // قيمة العمولة بـ USD

            // ✅ القيم الثابتة التي نعرفها مسبقاً
            decimal expectedBuyFinalAmount = buyUsd + commissionAmount;              // 10,100
            decimal expectedConversionRate = sellLyd / buyUsd;
            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {commissionAmount:F2}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ USD (Buy Final): {expectedBuyFinalAmount:F2}");
            Output.WriteLine($"   💰 متوسط السعر (Conversion Rate): {expectedConversionRate:F10}");

            // ================================================================
            // 2. جلب المحافظ المطلوبة
            // ================================================================
            Output.WriteLine("\n📝 جلب المحافظ...");

            var anisCardLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardLyd.UserKey,
                CurrencyType.LYD);

            var anisCardUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardUsd.UserKey,
                CurrencyType.USD);

            var anisPayLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisPay.UserKey,
                CurrencyType.LYD);

            var hreyshWalletId = Hreysh.WalletIdGuid;

            var commissionWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Commission.UserKey,
                CurrencyType.USD);

            Output.WriteLine($"   📋 AnisCard LYD Wallet ID: {anisCardLydWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay LYD Wallet ID: {anisPayLydWalletId}");
            Output.WriteLine($"   📋 Hreysh USD Wallet ID: {hreyshWalletId}");
            Output.WriteLine($"   📋 Commission Wallet ID: {commissionWalletId}");

            // ================================================================
            // 3. جلب الأرصدة القديمة ومتوسط السعر قبل الصرف
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة ومتوسط السعر...");

            var anisPayOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydOld = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);
            var commissionWalletOld = await Wallet.GetBalanceAsync(Commission.UserKey, commissionWalletId);

            Output.WriteLine($"   💰 AnisPay LYD (مستقبل LYD): {anisPayOld:F4}");
            Output.WriteLine($"   💰 AnisCard LYD (مرسل LYD): {hthLydOld:F4}");
            Output.WriteLine($"   💰 AnisCard USD (مستقبل USD): {hthUsdOld:F4}");
            Output.WriteLine($"   💰 Hreysh USD (مرسل USD): {hreyshOld:F4}");
            Output.WriteLine($"   💰 Commission Wallet: {commissionWalletOld:F4}");

            // ✅ جلب متوسط السعر القديم (Average Rate)
            var oldAvgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, oldAvgResponse.StatusCode);

            var oldAverageRate = oldAvgResponse.Data.AverageRate;
            var oldBalance = oldAvgResponse.Data.Balance;
            var oldEstimatedLyd = oldAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

            Output.WriteLine($"\n📊 متوسط السعر القديم (Average Rate - قبل الصرف):");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 Old Balance (USD): {oldBalance:F10}");
            Output.WriteLine($"   📊 Old Estimated LYD: {oldEstimatedLyd:F10}");

            // ================================================================
            // 4. إنشاء سلسلة الدفع
            // ================================================================
            Output.WriteLine("\n🔗 إنشاء سلسلة الدفع...");

            var chainResponse = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

            Output.WriteLine($"   📋 Status Code: {(int)chainResponse.StatusCode} - {chainResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);

            var chainId = Guid.Parse(chainResponse.Data.Id);
            Output.WriteLine($"   📋 Chain ID: {chainId}");
            Assert.NotEqual(Guid.Empty, chainId);

            // ================================================================
            // 5. جلب LydRate
            // ================================================================
            var avgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

            var lydRate = avgResponse.Data.AverageRate;
            Output.WriteLine($"\n📊 LydRate المستخدم في الصرف: {lydRate:F10}");

            // ================================================================
            // 6. بناء طلب الصرف مع العمولة غير المضمنة
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف مع العمولة غير المضمنة...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateExchangeRequest(
                operationId: operationId,
                buyCreditorWalletId: hreyshWalletId.ToString(),
                buyDebitorWalletId: anisCardUsdWalletId.ToString(),
                sellCreditorWalletId: anisCardLydWalletId.ToString(),
                sellDebitorWalletId: anisPayLydWalletId.ToString(),
                buyAmount: buyUsd,        // 10,100 USD
                sellAmount: sellLyd,         // 80,000 LYD
                lydRate: lydRate,
                detailedStatement: "Exchange with Commission Excluded Test")
                .WithCommission(
                    walletId: commissionWalletId.ToString(),
                    amount: commissionAmount,
                    description: $"Commission {commissionAmount} USD (excluded)",
                    isIncluded: false)
                .Build();

            Output.WriteLine($"   📋 Operation ID: {operationId}");
            Output.WriteLine($"   💳 Commission (USD): {commissionAmount:F2} (Excluded)");
            Output.WriteLine($"   💳 Commission Wallet: {commissionWalletId}");

            // ================================================================
            // 7. تنفيذ عملية الصرف
            // ================================================================
            Output.WriteLine("\n💱 تنفيذ عملية الصرف...");

            var exchangeResponse = await Almusher.RegularCurrencyExchangeAsync(
                userKey: Dashboard.UserKey,
                chainId: chainId.ToString(),
                request: request);

            Output.WriteLine($"   📋 Status Code: {(int)exchangeResponse.StatusCode} - {exchangeResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, exchangeResponse.StatusCode);

            var idFromResponse = exchangeResponse.Data?.Id;
            Output.WriteLine($"   📋 ID from response: {idFromResponse}");

            // ================================================================
            // ⏳ 8. انتظار 7 ثواني
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني للتأكد من اكتمال العملية...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 9. التحقق من تفاصيل العملية
            // ================================================================
            Output.WriteLine("\n🔍 التحقق من تفاصيل العملية...");

            var detailsResponse = await Almusher.GetExchangeDetailsAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Output.WriteLine($"   📋 Status Code: {(int)detailsResponse.StatusCode} - {detailsResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

            var details = detailsResponse.Data;

            Output.WriteLine("\n📊 تفاصيل العملية من الـ Response:");
            Output.WriteLine("═══════════════════════════════════════════════════════════");
            Output.WriteLine($"   🆔 ID:                      {details.Id}");
            Output.WriteLine($"   📈 Conversion Rate:         {details.ConversionRate:F10}");
            Output.WriteLine($"   📝 Statement:               {details.DetailedStatement}");
            Output.WriteLine("─────────────────────────────────────────────────────────────");

            var buySide = details.CurrencyExchangeBuy;
            Output.WriteLine("\n💵 جانب الشراء (Buy - USD):");
            Output.WriteLine($"   💰 Amount:                  {buySide?.Amount:F4}");
            Output.WriteLine($"   💰 Final Amount:            {buySide?.FinalAmount:F4}");

            var sellSide = details.CurrencyExchangeSell;
            Output.WriteLine("\n💰 جانب البيع (Sell - LYD):");
            Output.WriteLine($"   💰 Amount:                  {sellSide?.Amount:F4}");
            Output.WriteLine($"   💰 Final Amount:            {sellSide?.FinalAmount:F4}");

            var commission = details.Commission;
            if (commission != null)
            {
                Output.WriteLine("\n💳 معلومات العمولة (Commission):");
                Output.WriteLine($"   💳 Commission Wallet:      {commission.Wallet?.WalletId}");
                if (commission.CommissionElements != null && commission.CommissionElements.Count > 0)
                {
                    foreach (var element in commission.CommissionElements)
                    {
                        Output.WriteLine($"   📝 {element.Description}: {element.Amount:F4} (Included: {element.IsIncluded})");
                    }
                }
            }

            // ================================================================
            // 10. التحقق من القيم
            // ================================================================
            Output.WriteLine("\n✅ التحقق من القيم:");

            // ✅ التحقق 1: مبلغ الشراء (Buy Amount)
            var actualBuy = buySide?.Amount ?? 0m;
            var (isBuyEqual, _, buyMsg) = DecimalComparer.CompareBalance(buyUsd, actualBuy, "مبلغ الشراء (بدون العمولة)");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ الشراء النهائي (Buy Final Amount)
            var actualBuyFinal = buySide?.FinalAmount ?? 0m;
            var (isBuyFinalEqual, _, buyFinalMsg) = DecimalComparer.CompareBalance(expectedBuyFinalAmount, actualBuyFinal, "مبلغ الشراء النهائي");
            Assert.True(isBuyFinalEqual, buyFinalMsg);
            Output.WriteLine($"   ✅ {buyFinalMsg}");

            // ✅ التحقق 3: مبلغ البيع (Sell Amount)
            var actualSell = sellSide?.Amount ?? 0m;
            var (isSellEqual, _, sellMsg) = DecimalComparer.CompareBalance(sellLyd, actualSell, "مبلغ البيع (LYD)");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 4: المبلغ النهائي بالـ LYD (Sell Final Amount)
            // نأخذ القيمة الفعلية من الـ API مباشرة
            var actualSellFinal = sellSide?.FinalAmount ?? 0m;

            // ✅ التحقق من أن Sell Final Amount = Buy Final Amount × Conversion Rate
            var expectedSellFinal = Math.Round(expectedBuyFinalAmount * details.ConversionRate, 3);

            var (isSellFinalEqual, _, sellFinalMsg) = DecimalComparer.CompareBalance(
                expectedSellFinal,
                actualSellFinal,
                "المبلغ النهائي بالـ LYD (Buy Final × Conversion Rate)");
            Assert.True(isSellFinalEqual, sellFinalMsg);
            Output.WriteLine($"   ✅ {sellFinalMsg}");

            // ✅ التحقق 5: سعر الصرف (Conversion Rate) - نعرض القيمة من الـ API
            var actualRate = details.ConversionRate;
            Output.WriteLine($"   📈 سعر الصرف (Conversion Rate): {actualRate:F10}");

            // ✅ التحقق 6: قيمة العمولة
            var actualCommission = commission?.CommissionElements?.FirstOrDefault()?.Amount ?? 0m;
            var (isCommissionEqual, _, commissionMsg) = DecimalComparer.CompareBalance(commissionAmount, actualCommission, "قيمة العمولة (USD)");
            Assert.True(isCommissionEqual, commissionMsg);
            Output.WriteLine($"   ✅ {commissionMsg}");

            // ================================================================
            // 11. تأكيد العملية
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
            // ⏳ 12. انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 13. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);
            var commissionWalletNew = await Wallet.GetBalanceAsync(Commission.UserKey, commissionWalletId);

            Output.WriteLine($"   💰 AnisPay LYD: {anisPayOld:F4} → {anisPayNew:F4}");
            Output.WriteLine($"   💰 AnisCard LYD: {hthLydOld:F4} → {hthLydNew:F4}");
            Output.WriteLine($"   💰 AnisCard USD: {hthUsdOld:F4} → {hthUsdNew:F4}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshOld:F4} → {hreyshNew:F4}");
            Output.WriteLine($"   💰 Commission Wallet: {commissionWalletOld:F4} → {commissionWalletNew:F4}");
        
            // ✅ التحقق 7: AnisPay LYD (زاد بمقدار Sell Final Amount)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.CompareBalance(
                anisPayOld + actualSellFinal,
                anisPayNew,
                "AnisPay LYD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 8: AnisCard LYD (نقص بمقدار Sell Final Amount)
            var (isAnisCardLydEqual, _, anisCardLydMsg) = DecimalComparer.CompareBalance(
                hthLydOld - actualSellFinal,
                hthLydNew,
                "AnisCard LYD");
            Assert.True(isAnisCardLydEqual, anisCardLydMsg);
            Output.WriteLine($"   ✅ {anisCardLydMsg}");

            // ✅ التحقق 9: AnisCard USD (زاد بمقدار Buy Final Amount)
            var (isAnisCardUsdEqual, _, anisCardUsdMsg) = DecimalComparer.CompareBalance(
                hthUsdOld + actualBuyFinal,
                hthUsdNew,
                "AnisCard USD");
            Assert.True(isAnisCardUsdEqual, anisCardUsdMsg);
            Output.WriteLine($"   ✅ {anisCardUsdMsg}");

            // ✅ التحقق 10: Hreysh USD (نقص بمقدار Buy Amount)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.CompareBalance(
                hreyshOld - actualBuy,
                hreyshNew,
                "Hreysh USD");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ✅ التحقق 11: Commission Wallet (زاد بمقدار commissionAmount)
            var (isCommissionWalletEqual, _, commissionWalletMsg) = DecimalComparer.CompareBalance(
                commissionWalletOld - commissionAmount,
                commissionWalletNew,
                "Commission Wallet");
            Assert.True(isCommissionWalletEqual, commissionWalletMsg);
            Output.WriteLine($"   ✅ {commissionWalletMsg}");

            // ✅ التحقق 12 conversion Rate = Sell Final / Buy Final
            var (isConversionRateEqual, _, conversionRateMsg) = DecimalComparer.CompareRate(
                expectedConversionRate,
                actualRate,
                "معدل الصرف");
            Assert.True(isConversionRateEqual, conversionRateMsg);
            Output.WriteLine($"   ✅ {conversionRateMsg}");

            // ================================================================
            // 14. التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع
            var expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                oldBalance,
                oldEstimatedLyd,
                expectedBuyFinalAmount,        // 10,000 (بدون العمولة)
                expectedSellFinal        // 80,000
            );

            Output.WriteLine($"   📊 Expected New Average Rate: {expectedNewAverageRate:F10}");
            Output.WriteLine($"   📊 المعادلة: ({oldEstimatedLyd:F4} + {sellLyd:F4}) / ({oldBalance:F4} + {buyUsd:F4})");

            // ✅ جلب متوسط السعر الجديد من الـ API
            var newAvgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, newAvgResponse.StatusCode);

            var newAverageRate = newAvgResponse.Data.AverageRate;
            var newBalance = newAvgResponse.Data.Balance;
            var newEstimatedLyd = newAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

            Output.WriteLine($"   📊 New Average Rate (actual): {newAverageRate:F10}");
            Output.WriteLine($"   📊 New Balance: {newBalance:F10}");
            Output.WriteLine($"   📊 New Estimated LYD: {newEstimatedLyd:F10}");

            // ✅ التحقق من متوسط سعر الصرف الجديد
            var (isAvgRateEqual, _, avgRateMsg) = DecimalComparer.CompareRate(
                expectedNewAverageRate,
                newAverageRate,
                "متوسط سعر الصرف الجديد");
            Assert.True(isAvgRateEqual, avgRateMsg);
            Output.WriteLine($"   ✅ {avgRateMsg}");

            // ================================================================
            // 15. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف مع العمولة غير المضمنة:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {commissionAmount:F2} (غير مضمنة)");
            Output.WriteLine($"   💰 مبلغ الشراء النهائي (Buy Final): {expectedBuyFinalAmount:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   📈 سعر الصرف (Conversion Rate): {actualRate:F10}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD (Sell Final): {actualSellFinal:F4}");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 New Average Rate: {newAverageRate:F10}");
            Output.WriteLine($"   📊 Expected Average Rate: {expectedNewAverageRate:F10}");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID: {operationId}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ExchangeWithCommissionExcluded_ShouldSucceed), true);
        }
    }
}