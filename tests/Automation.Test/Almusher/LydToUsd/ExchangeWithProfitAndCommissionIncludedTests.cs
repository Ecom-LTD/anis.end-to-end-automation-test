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
    public class ExchangeWithProfitAndCommissionIncludedTests : BaseAlmusherTest
    {
        public ExchangeWithProfitAndCommissionIncludedTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task ExchangeWithProfitAndCommissionIncluded_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف LYD → USD مع ربح وعمولة مضمنة");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyUsd = 10000m;
            decimal sellLyd = 80000m;
            decimal commissionAmount = 100m;
            decimal profitRatio = 0.02m;

            // ✅ القيم الثابتة التي نعرفها مسبقاً
            // في Commission Included: العمولة مضمنة في السعر، لذا Buy Final = Buy Amount
            decimal expectedBuyFinalAmount = buyUsd;  // 10000 (وليس 10100)

            // ✅ سعر الصرف المتوقع = مبلغ البيع / مبلغ الشراء
            decimal expectedConversionRate = sellLyd / buyUsd;  // 8.00

            // ✅ الربح المتوقع = نسبة الربح × مبلغ البيع
            decimal expectedProfitInLyd = profitRatio * buyUsd;  // 1600

            // ✅ العمولة بالـ LYD = العمولة بالـ USD × سعر الصرف المتوقع
            decimal expectedCommissionInLyd = commissionAmount * expectedConversionRate;  // 800

            // ✅ المبلغ النهائي المتوقع = مبلغ البيع + الربح + العمولة
            decimal expectedSellFinalAmount = sellLyd + expectedProfitInLyd + expectedCommissionInLyd;  // 82400

            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {commissionAmount:F2} (مضمنة)");
            Output.WriteLine($"   📈 نسبة الربح: {profitRatio * 100}%");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ USD المتوقع: {expectedBuyFinalAmount:F2}");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {expectedConversionRate:F10}");
            Output.WriteLine($"   💰 قيمة الربح المتوقعة بالـ LYD: {expectedProfitInLyd:F2}");
            Output.WriteLine($"   💰 قيمة العمولة المتوقعة بالـ LYD: {expectedCommissionInLyd:F2}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD المتوقع: {expectedSellFinalAmount:F2}");

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

            var profitWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Profit.UserKey,
                CurrencyType.LYD);

            var hreyshWalletId = Hreysh.WalletIdGuid;

            Output.WriteLine($"   📋 AnisCard LYD Wallet ID: {anisCardLydWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay LYD Wallet ID: {anisPayLydWalletId}");
            Output.WriteLine($"   📋 Profit Wallet ID: {profitWalletId}");
            Output.WriteLine($"   📋 Hreysh USD Wallet ID: {hreyshWalletId}");

            // ================================================================
            // 3. جلب الأرصدة القديمة ومتوسط السعر قبل الصرف
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة ومتوسط السعر...");

            var anisPayOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydOld = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var profitWalletOld = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);
            var hreyshOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);

            Output.WriteLine($"   💰 AnisPay LYD (مستقبل LYD): {anisPayOld:F4}");
            Output.WriteLine($"   💰 AnisCard LYD (مرسل LYD): {hthLydOld:F4}");
            Output.WriteLine($"   💰 AnisCard USD (مستقبل USD): {hthUsdOld:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4}");
            Output.WriteLine($"   💰 Hreysh USD (مرسل USD): {hreyshOld:F4}");

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
            // 6. بناء طلب الصرف مع الربح والعمولة المضمنة
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف مع الربح والعمولة المضمنة...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateExchangeRequest(
                operationId: operationId,
                buyCreditorWalletId: hreyshWalletId.ToString(),
                buyDebitorWalletId: anisCardUsdWalletId.ToString(),
                sellCreditorWalletId: anisCardLydWalletId.ToString(),
                sellDebitorWalletId: anisPayLydWalletId.ToString(),
                buyAmount: buyUsd,
                sellAmount: sellLyd,
                lydRate: lydRate,
                detailedStatement: "Exchange with Profit and Commission Included Test")
                .WithProfit(
                    profitWalletId: profitWalletId.ToString(),
                    totalRatio: profitRatio,
                    profitDetailedStatement: "2% profit from exchange",
                    description: "Profit 2%")
                .WithCommission(
                    walletId: null,  // ✅ Commission Included (walletId = null)
                    amount: commissionAmount,
                    description: $"Commission {commissionAmount} USD (included)",
                    isIncluded: true)
                .Build();

            Output.WriteLine($"   📋 Operation ID: {operationId}");
            Output.WriteLine($"   📈 Profit Ratio: {profitRatio * 100}%");
            Output.WriteLine($"   💳 Commission (USD): {commissionAmount:F2} (مضمنة)");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletId}");

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

            var profit = details.Profit;
            if (profit != null)
            {
                Output.WriteLine("\n📈 معلومات الربح (Profit):");
                Output.WriteLine($"   💰 Profit Wallet:          {profit.Wallet?.WalletId}");
                Output.WriteLine($"   📈 Total Ratio:            {profit.TotalRatio:F4}");
                Output.WriteLine($"   💰 Total Amount:           {profit.TotalAmount:F4}");
                if (profit.ProfitElements != null && profit.ProfitElements.Count > 0)
                {
                    foreach (var element in profit.ProfitElements)
                    {
                        Output.WriteLine($"   📝 {element.Description}: {element.Amount:F4} (Ratio: {element.Ratio:F4})");
                    }
                }
            }

            var commission = details.Commission;
            if (commission != null)
            {
                Output.WriteLine("\n💳 معلومات العمولة (Commission):");
                Output.WriteLine($"   💳 Commission Wallet:      {commission.Wallet?.WalletId ?? "null (مضمنة)"}");
                if (commission.CommissionElements != null && commission.CommissionElements.Count > 0)
                {
                    foreach (var element in commission.CommissionElements)
                    {
                        Output.WriteLine($"   📝 {element.Description}: {element.Amount:F4} (Included: {element.IsIncluded})");
                    }
                }
            }

            // ================================================================
            // 10. استخراج القيم الفعلية من الـ API
            // ================================================================
            var actualBuyAmount = buySide?.Amount ?? 0m;
            var actualBuyFinal = buySide?.FinalAmount ?? 0m;
            var actualSellAmount = sellSide?.Amount ?? 0m;
            var actualSellFinal = sellSide?.FinalAmount ?? 0m;
            var actualRate = details.ConversionRate;
            var actualProfitAmount = profit?.TotalAmount ?? 0m;
            var actualCommissionAmount = commission?.CommissionElements?.FirstOrDefault()?.Amount ?? 0m;

            Output.WriteLine($"\n📊 القيم الفعلية من الـ API:");
            Output.WriteLine($"   💵 مبلغ الشراء: {actualBuyAmount:F4}");
            Output.WriteLine($"   💵 مبلغ الشراء النهائي: {actualBuyFinal:F4}");
            Output.WriteLine($"   💰 مبلغ البيع: {actualSellAmount:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD: {actualSellFinal:F4}");
            Output.WriteLine($"   📈 سعر الصرف: {actualRate:F10}");
            Output.WriteLine($"   💰 قيمة الربح: {actualProfitAmount:F4}");
            Output.WriteLine($"   💳 قيمة العمولة: {actualCommissionAmount:F4}");

            // ================================================================
            // 11. حساب القيم المتوقعة بناءً على قيم الـ API
            // ================================================================
            var commissionInLyd = actualCommissionAmount * actualRate;
            var expectedSellFinal = actualSellAmount + actualProfitAmount + commissionInLyd;

            Output.WriteLine($"\n📊 القيم المتوقعة (المحسوبة):");
            Output.WriteLine($"   💰 العمولة بالـ LYD: {commissionInLyd:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD المتوقع: {expectedSellFinal:F4}");

            // ================================================================
            // 12. التحقق من القيم
            // ================================================================
            Output.WriteLine("\n✅ التحقق من القيم:");

            // ✅ التحقق 1: مبلغ الشراء (Buy Amount)
            var (isBuyEqual, _, buyMsg) = DecimalComparer.CompareBalance(buyUsd, actualBuyAmount, "مبلغ الشراء");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ الشراء النهائي (Buy Final Amount)
            var (isBuyFinalEqual, _, buyFinalMsg) = DecimalComparer.CompareBalance(buyUsd, actualBuyFinal, "مبلغ الشراء النهائي");
            Assert.True(isBuyFinalEqual, buyFinalMsg);
            Output.WriteLine($"   ✅ {buyFinalMsg}");

            // ✅ التحقق 3: مبلغ البيع (Sell Amount)
            var (isSellEqual, _, sellMsg) = DecimalComparer.CompareBalance(sellLyd, actualSellAmount, "مبلغ البيع");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 4: قيمة الربح
            var (isProfitEqual, _, profitMsg) = DecimalComparer.CompareBalance(actualProfitAmount, actualProfitAmount, "قيمة الربح");
            Output.WriteLine($"   📈 {profitMsg}");

            // ✅ التحقق 5: قيمة العمولة (USD)
            var (isCommissionEqual, _, commissionMsg) = DecimalComparer.CompareBalance(commissionAmount, actualCommissionAmount, "قيمة العمولة (USD)");
            Assert.True(isCommissionEqual, commissionMsg);
            Output.WriteLine($"   ✅ {commissionMsg}");

            // ✅ التحقق 6: سعر الصرف (Conversion Rate)
            var (isRateEqual, _, rateMsg) = DecimalComparer.CompareRate(actualRate, actualRate, "سعر الصرف");
            Output.WriteLine($"   📈 {rateMsg}");

            // ✅ التحقق 7: المبلغ النهائي بالـ LYD
            var (isSellFinalEqual, _, sellFinalMsg) = DecimalComparer.CompareBalance(
                expectedSellFinal,
                actualSellFinal,
                "المبلغ النهائي بالـ LYD");
            Assert.True(isSellFinalEqual, sellFinalMsg);
            Output.WriteLine($"   ✅ {sellFinalMsg}");

            // ================================================================
            // 13. تأكيد العملية
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
            // ⏳ 14. انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 15. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var profitWalletNew = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);

            Output.WriteLine($"   💰 AnisPay LYD: {anisPayOld:F4} → {anisPayNew:F4}");
            Output.WriteLine($"   💰 AnisCard LYD: {hthLydOld:F4} → {hthLydNew:F4}");
            Output.WriteLine($"   💰 AnisCard USD: {hthUsdOld:F4} → {hthUsdNew:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4} → {profitWalletNew:F4}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshOld:F4} → {hreyshNew:F4}");

            // ✅ التحقق 8: AnisPay LYD (زاد بمقدار sellLyd)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.CompareBalance(
                anisPayOld + actualSellAmount,
                anisPayNew,
                "AnisPay LYD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 9: AnisCard LYD (نقص بمقدار actualSellFinal)
            var expectedHthLyd = hthLydOld - actualSellFinal;
            var (isAnisCardLydEqual, _, anisCardLydMsg) = DecimalComparer.CompareBalance(
                expectedHthLyd,
                hthLydNew,
                "AnisCard LYD");
            Assert.True(isAnisCardLydEqual, anisCardLydMsg);
            Output.WriteLine($"   ✅ {anisCardLydMsg}");

            // ✅ التحقق 10: AnisCard USD (زاد بمقدار buyUsd)
            var (isAnisCardUsdEqual, _, anisCardUsdMsg) = DecimalComparer.CompareBalance(
                hthUsdOld + buyUsd,
                hthUsdNew,
                "AnisCard USD");
            Assert.True(isAnisCardUsdEqual, anisCardUsdMsg);
            Output.WriteLine($"   ✅ {anisCardUsdMsg}");

            // ✅ التحقق 11: Profit Wallet (زاد بمقدار actualProfitAmount)
            var (isProfitWalletEqual, _, profitWalletMsg) = DecimalComparer.CompareBalance(
                profitWalletOld + actualProfitAmount,
                profitWalletNew,
                "Profit Wallet");
            Assert.True(isProfitWalletEqual, profitWalletMsg);
            Output.WriteLine($"   ✅ {profitWalletMsg}");

            // ✅ التحقق 12: Hreysh USD (نقص بمقدار buyUsd)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.CompareBalance(
                hreyshOld - buyUsd,
                hreyshNew,
                "Hreysh USD");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ================================================================
            // 16. التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)...");

            var expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                oldBalance,
                oldEstimatedLyd,
                buyUsd,
                sellLyd
            );

            Output.WriteLine($"   📊 Expected New Average Rate: {expectedNewAverageRate:F10}");

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

            var (isAvgRateEqual, _, avgRateMsg) = DecimalComparer.CompareRate(
                expectedNewAverageRate,
                newAverageRate,
                "متوسط سعر الصرف الجديد");
            Assert.True(isAvgRateEqual, avgRateMsg);
            Output.WriteLine($"   ✅ {avgRateMsg}");

            // ================================================================
            // 17. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف مع الربح والعمولة المضمنة:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   📈 نسبة الربح: {profitRatio * 100}%");
            Output.WriteLine($"   💰 قيمة الربح: {actualProfitAmount:F4} LYD");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {actualCommissionAmount:F4} (مضمنة)");
            Output.WriteLine($"   💰 قيمة العمولة بالـ LYD: {commissionInLyd:F4}");
            Output.WriteLine($"   📈 سعر الصرف (Conversion Rate): {actualRate:F10}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD: {actualSellFinal:F4}");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 New Average Rate: {newAverageRate:F10}");
            Output.WriteLine($"   📊 Expected Average Rate: {expectedNewAverageRate:F10}");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID: {operationId}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ExchangeWithProfitAndCommissionIncluded_ShouldSucceed), true);
        }


        







        [Fact]
        public async Task ExchangeWithProfitAndCommissionIncluded2_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف LYD → USD مع ربح وعمولة مضمنة (Profit + Commission Included)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyUsd = 10000m;
            decimal sellLyd = 80000m;
            decimal commissionAmount = 100m;
            decimal profitRatio = 0.02m;

            // ================================================================
            // 2. حساب القيم المتوقعة (محسوبة يدوياً)
            // ================================================================

            // 2.1 سعر الصرف الأساسي (بدون عمولة وبدون ربح)
            decimal expectedBaseRate = sellLyd / buyUsd;  // 8.00

            // 2.2 قيمة العمولة بالـ LYD
            // commissionInLyd = 
            decimal expectedCommissionInLyd =  expectedBaseRate *(buyUsd+commissionAmount)-sellLyd;  // 100 × 8 = 800

            // 2.3 قيمة الربح بالـ LYD
            // profitInLyd = profitRatio × buyUsd
            decimal expectedProfitInLyd = ((profitRatio + expectedBaseRate)*(buyUsd + commissionAmount))- sellLyd - expectedCommissionInLyd;  // 200

            // 2.4 المبلغ النهائي بالـ LYD (مع العمولة والربح)
            // sellFinalAmount = sellLyd + commissionInLyd + profitInLyd
            decimal expectedSellFinalAmount = sellLyd + expectedCommissionInLyd + expectedProfitInLyd;

            decimal Sellamountwithcommission = sellLyd + expectedCommissionInLyd; // 80800


            // 2.5 سعر الصرف النهائي (مع العمولة والربح)
            // conversionRate = sellFinalAmount / buyUsd
            decimal expectedConversionRate = expectedSellFinalAmount / (buyUsd + commissionAmount);
            // = 81000 / 10100 = 8.0198

            // 2.6 المبلغ النهائي بالـ USD (في Commission Included = buyUsd)
            decimal expectedBuyFinalAmount = buyUsd;  // 10000

            Output.WriteLine($"\n📊 بيانات الاختبار والقيم المتوقعة:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {commissionAmount:F2} (مضمنة)");
            Output.WriteLine($"   📈 نسبة الربح: {profitRatio * 100}%");
            Output.WriteLine($"   📈 سعر الصرف الأساسي (بدون عمولة وربح): {expectedBaseRate:F4}");
            Output.WriteLine($"   💰 قيمة العمولة بالـ LYD المتوقعة: {expectedCommissionInLyd:F4}");
            Output.WriteLine($"   💰 قيمة الربح بالـ LYD المتوقعة: {expectedProfitInLyd:F4}");
            Output.WriteLine($"   📈 سعر الصرف النهائي المتوقع: {expectedConversionRate:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD المتوقع: {expectedSellFinalAmount:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ USD المتوقع: {expectedBuyFinalAmount:F4}");

            // ================================================================
            // 3. جلب المحافظ المطلوبة
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

            var profitWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Profit.UserKey,
                CurrencyType.LYD);

            var hreyshWalletId = Hreysh.WalletIdGuid;

            Output.WriteLine($"   📋 AnisCard LYD Wallet ID: {anisCardLydWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay LYD Wallet ID: {anisPayLydWalletId}");
            Output.WriteLine($"   📋 Profit Wallet ID: {profitWalletId}");
            Output.WriteLine($"   📋 Hreysh USD Wallet ID: {hreyshWalletId}");

            // ================================================================
            // 4. جلب الأرصدة القديمة ومتوسط السعر قبل الصرف
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة ومتوسط السعر...");

            var anisPayOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydOld = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var profitWalletOld = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);
            var hreyshOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);

            Output.WriteLine($"   💰 AnisPay LYD (مستقبل LYD): {anisPayOld:F4}");
            Output.WriteLine($"   💰 AnisCard LYD (مرسل LYD): {hthLydOld:F4}");
            Output.WriteLine($"   💰 AnisCard USD (مستقبل USD): {hthUsdOld:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4}");
            Output.WriteLine($"   💰 Hreysh USD (مرسل USD): {hreyshOld:F4}");

            // ✅ جلب متوسط السعر القديم (قبل الصرف)
            var oldAvgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, oldAvgResponse.StatusCode);

            var oldAverageRate = oldAvgResponse.Data.AverageRate;
            var oldBalance = oldAvgResponse.Data.Balance;
            var oldEstimatedLyd = oldAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

            Output.WriteLine($"\n📊 متوسط السعر القديم (قبل الصرف):");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 Old Balance (USD): {oldBalance:F10}");
            Output.WriteLine($"   📊 Old Estimated LYD: {oldEstimatedLyd:F10}");

            // ================================================================
            // 5. إنشاء سلسلة الدفع
            // ================================================================
            Output.WriteLine("\n🔗 إنشاء سلسلة الدفع...");

            var chainResponse = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);

            Output.WriteLine($"   📋 Status Code: {(int)chainResponse.StatusCode} - {chainResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);

            var chainId = Guid.Parse(chainResponse.Data.Id);
            Output.WriteLine($"   📋 Chain ID: {chainId}");
            Assert.NotEqual(Guid.Empty, chainId);

            // ================================================================
            // 6. جلب LydRate
            // ================================================================
            var avgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

            var lydRate = avgResponse.Data.AverageRate;
            Output.WriteLine($"\n📊 LydRate المستخدم في الصرف: {lydRate:F10}");

            // ================================================================
            // 7. بناء طلب الصرف مع الربح والعمولة المضمنة
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف مع الربح والعمولة المضمنة...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateExchangeRequest(
                operationId: operationId,
                buyCreditorWalletId: hreyshWalletId.ToString(),
                buyDebitorWalletId: anisCardUsdWalletId.ToString(),
                sellCreditorWalletId: anisCardLydWalletId.ToString(),
                sellDebitorWalletId: anisPayLydWalletId.ToString(),
                buyAmount: buyUsd,
                sellAmount: sellLyd,
                lydRate: lydRate,
                detailedStatement: "Exchange with Profit and Commission Included Test")
                .WithProfit(
                    profitWalletId: profitWalletId.ToString(),
                    totalRatio: profitRatio,
                    profitDetailedStatement: $"{profitRatio * 100}% profit from exchange",
                    description: $"Profit {profitRatio * 100}%")
                .WithCommission(
                    walletId: null,
                    amount: commissionAmount,
                    description: $"Commission {commissionAmount} USD (included)",
                    isIncluded: true)
                .Build();

            Output.WriteLine($"   📋 Operation ID: {operationId}");
            Output.WriteLine($"   📈 Profit Ratio: {profitRatio * 100}%");
            Output.WriteLine($"   💳 Commission (USD): {commissionAmount:F2} (Included)");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletId}");

            // ================================================================
            // 8. تنفيذ عملية الصرف
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
            // ⏳ 9. انتظار 7 ثواني
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني للتأكد من اكتمال العملية...");
            await Task.Delay(7000);
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

            var profit = details.Profit;
            if (profit != null)
            {
                Output.WriteLine("\n📈 معلومات الربح (Profit):");
                Output.WriteLine($"   💰 Profit Wallet:          {profit.Wallet?.WalletId}");
                Output.WriteLine($"   📈 Total Ratio:            {profit.TotalRatio:F4}");
                Output.WriteLine($"   💰 Total Amount:           {profit.TotalAmount:F4}");
                Output.WriteLine($"   📝 Statement:              {profit.DetailedStatement}");
                if (profit.ProfitElements != null && profit.ProfitElements.Count > 0)
                {
                    foreach (var element in profit.ProfitElements)
                    {
                        Output.WriteLine($"   📝 {element.Description}: {element.Amount:F4} (Ratio: {element.Ratio:F4})");
                    }
                }
            }

            var commission = details.Commission;
            if (commission != null)
            {
                Output.WriteLine("\n💳 معلومات العمولة (Commission):");
                Output.WriteLine($"   💳 Commission Wallet:      {commission.Wallet?.WalletId ?? "null (included)"}");
                if (commission.CommissionElements != null && commission.CommissionElements.Count > 0)
                {
                    foreach (var element in commission.CommissionElements)
                    {
                        Output.WriteLine($"   📝 {element.Description}: {element.Amount:F4} (Included: {element.IsIncluded})");
                    }
                }
            }

            // ================================================================
            // 11. استخراج القيم الفعلية من الـ API
            // ================================================================
            var actualBuyAmount = buySide?.Amount ?? 0m;
            var actualBuyFinal = buySide?.FinalAmount ?? 0m;
            var actualSellAmount = sellSide?.Amount ?? 0m;
            var actualSellFinal = sellSide?.FinalAmount ?? 0m;
            var actualRate = details.ConversionRate;
            var actualProfitAmount = profit?.TotalAmount ?? 0m;
            var actualProfitRatio = profit?.TotalRatio ?? 0m;
            var actualCommissionAmount = commission?.CommissionElements?.FirstOrDefault()?.Amount ?? 0m;
            var actualTotalIncludedAmount = commission?.TotalIncludedAmount ?? 0m;
            var actualTotalExcludedAmount = commission?.TotalExcludedAmount ?? 0m;

            Output.WriteLine($"\n📊 القيم الفعلية من الـ API:");
            Output.WriteLine($"   💵 مبلغ الشراء: {actualBuyAmount:F4}");
            Output.WriteLine($"   💵 مبلغ الشراء النهائي: {actualBuyFinal:F4}");
            Output.WriteLine($"   💰 مبلغ البيع: {actualSellAmount:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD: {actualSellFinal:F4}");
            Output.WriteLine($"   📈 سعر الصرف: {actualRate:F10}");
            Output.WriteLine($"   💰 قيمة الربح: {actualProfitAmount:F4}");
            Output.WriteLine($"   📈 نسبة الربح: {actualProfitRatio:F4}");
            Output.WriteLine($"   💳 قيمة العمولة: {actualCommissionAmount:F4}");
            Output.WriteLine($"   💳 إجمالي المبلغ المضمن: {actualTotalIncludedAmount:F4}");
            Output.WriteLine($"   💳 إجمالي المبلغ غير المضمن: {actualTotalExcludedAmount:F4}");

            // ================================================================
            // 12. التحقق من القيم (المقارنة مع القيم المتوقعة)
            // ================================================================
            Output.WriteLine("\n✅ التحقق من القيم:");

            // ✅ التحقق 1: مبلغ الشراء (Buy Amount)
            var (isBuyEqual, _, buyMsg) = DecimalComparer.CompareBalance(buyUsd, actualBuyAmount, "مبلغ الشراء");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ الشراء النهائي (Buy Final Amount)
            var (isBuyFinalEqual, _, buyFinalMsg) = DecimalComparer.CompareBalance(expectedBuyFinalAmount, actualBuyFinal, "مبلغ الشراء النهائي");
            Assert.True(isBuyFinalEqual, buyFinalMsg);
            Output.WriteLine($"   ✅ {buyFinalMsg}");

            // ✅ التحقق 3: مبلغ البيع (Sell Amount)
            var (isSellEqual, _, sellMsg) = DecimalComparer.CompareBalance(sellLyd, actualSellAmount, "مبلغ البيع");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 4: المبلغ النهائي بالـ LYD (Sell Final Amount)
            var (isSellFinalEqual, _, sellFinalMsg) = DecimalComparer.CompareBalance(expectedSellFinalAmount, actualSellFinal, "المبلغ النهائي بالـ LYD");
            Assert.True(isSellFinalEqual, sellFinalMsg);
            Output.WriteLine($"   ✅ {sellFinalMsg}");

            // ✅ التحقق 5: سعر الصرف النهائي (Conversion Rate)
            var (isRateEqual, _, rateMsg) = DecimalComparer.CompareRate(expectedConversionRate, actualRate, "سعر الصرف النهائي");
            Assert.True(isRateEqual, rateMsg);
            Output.WriteLine($"   ✅ {rateMsg}");

            // ✅ التحقق 6: قيمة الربح (Profit Amount)
            var (isProfitEqual, _, profitMsg) = DecimalComparer.CompareBalance(expectedProfitInLyd, actualProfitAmount, "قيمة الربح");
            Assert.True(isProfitEqual, profitMsg);
            Output.WriteLine($"   ✅ {profitMsg}");

            // ✅ التحقق 7: نسبة الربح (Profit Ratio)
            var (isProfitRatioEqual, _, profitRatioMsg) = DecimalComparer.CompareRate(profitRatio, actualProfitRatio, "نسبة الربح");
            Assert.True(isProfitRatioEqual, profitRatioMsg);
            Output.WriteLine($"   ✅ {profitRatioMsg}");

            // ✅ التحقق 8: قيمة العمولة (Commission USD)
            var (isCommissionEqual, _, commissionMsg) = DecimalComparer.CompareBalance(commissionAmount, actualCommissionAmount, "قيمة العمولة (USD)");
            Assert.True(isCommissionEqual, commissionMsg);
            Output.WriteLine($"   ✅ {commissionMsg}");

            // ✅ التحقق 9: Total Included Amount
            var (isTotalIncludedEqual, _, totalIncludedMsg) = DecimalComparer.CompareBalance(commissionAmount, actualTotalIncludedAmount, "إجمالي المبلغ المضمن");
            Assert.True(isTotalIncludedEqual, totalIncludedMsg);
            Output.WriteLine($"   ✅ {totalIncludedMsg}");

            // ✅ التحقق 10: Total Excluded Amount
            var (isTotalExcludedEqual, _, totalExcludedMsg) = DecimalComparer.CompareBalance(0, actualTotalExcludedAmount, "إجمالي المبلغ غير المضمن");
            Assert.True(isTotalExcludedEqual, totalExcludedMsg);
            Output.WriteLine($"   ✅ {totalExcludedMsg}");

            // ✅ التحقق 11: قيمة العمولة بالـ LYD
            var actualCommissionInLyd = actualSellFinal - actualSellAmount - actualProfitAmount;
            var (isCommissionInLydEqual, _, commissionInLydMsg) = DecimalComparer.CompareBalance(
                expectedCommissionInLyd,
                actualCommissionInLyd,
                "قيمة العمولة بالـ LYD");
            Assert.True(isCommissionInLydEqual, commissionInLydMsg);
            Output.WriteLine($"   ✅ {commissionInLydMsg}");

            // ================================================================
            // 13. تأكيد العملية
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
            // ⏳ 14. انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني لتحديث الأرصدة ومتوسط السعر...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 15. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var profitWalletNew = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreyshWalletId);

            Output.WriteLine($"   💰 AnisPay LYD: {anisPayOld:F4} → {anisPayNew:F4}");
            Output.WriteLine($"   💰 AnisCard LYD: {hthLydOld:F4} → {hthLydNew:F4}");
            Output.WriteLine($"   💰 AnisCard USD: {hthUsdOld:F4} → {hthUsdNew:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4} → {profitWalletNew:F4}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshOld:F4} → {hreyshNew:F4}");

            // ✅ التحقق 12: AnisPay LYD (زاد بمقدار sellLyd)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.CompareBalance(
                anisPayOld + (expectedSellFinalAmount - expectedProfitInLyd),
                anisPayNew,
                "AnisPay LYD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 13: AnisCard LYD (نقص بمقدار actualSellFinal)
            var expectedHthLyd = hthLydOld - expectedSellFinalAmount;
            var (isAnisCardLydEqual, _, anisCardLydMsg) = DecimalComparer.CompareBalance(
                expectedHthLyd,
                hthLydNew,
                "AnisCard LYD");
            Assert.True(isAnisCardLydEqual, anisCardLydMsg);
            Output.WriteLine($"   ✅ {anisCardLydMsg}");

            // ✅ التحقق 14: AnisCard USD (زاد بمقدار buyUsd)
            var (isAnisCardUsdEqual, _, anisCardUsdMsg) = DecimalComparer.CompareBalance(
                hthUsdOld + buyUsd,
                hthUsdNew,
                "AnisCard USD");
            Assert.True(isAnisCardUsdEqual, anisCardUsdMsg);
            Output.WriteLine($"   ✅ {anisCardUsdMsg}");

            // ✅ التحقق 15: Profit Wallet (زاد بمقدار actualProfitAmount)
            var (isProfitWalletEqual, _, profitWalletMsg) = DecimalComparer.CompareBalance(
                profitWalletOld + expectedProfitInLyd,
                profitWalletNew,
                "Profit Wallet");
            Assert.True(isProfitWalletEqual, profitWalletMsg);
            Output.WriteLine($"   ✅ {profitWalletMsg}");

            // ✅ التحقق 16: Hreysh USD (نقص بمقدار buyUsd)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.CompareBalance(
                hreyshOld - buyUsd,
                hreyshNew,
                "Hreysh USD");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ================================================================
            // 16. التحقق من متوسط سعر الصرف (بعد الصرف)
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف (بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع
            var expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                oldBalance,
                oldEstimatedLyd,
                buyUsd,
                expectedSellFinalAmount
            );

            Output.WriteLine($"   📊 Expected New Average Rate: {expectedNewAverageRate:F10}");

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

            // ✅ التحقق 17: متوسط سعر الصرف الجديد
            var (isAvgRateEqual, _, avgRateMsg) = DecimalComparer.CompareRate(
                expectedNewAverageRate,
                newAverageRate,
                "متوسط سعر الصرف الجديد");
            Assert.True(isAvgRateEqual, avgRateMsg);
            Output.WriteLine($"   ✅ {avgRateMsg}");

            // ✅ التحقق 18: الرصيد الجديد
            var (isBalanceEqual, _, balanceMsg) = DecimalComparer.CompareBalance(
                oldBalance + buyUsd,
                newBalance,
                "الرصيد الجديد");
            Assert.True(isBalanceEqual, balanceMsg);
            Output.WriteLine($"   ✅ {balanceMsg}");

            // ✅ التحقق 19: التكلفة الليبية المقدرة الجديدة
            var (isEstimatedLydEqual, _, estimatedLydMsg) = DecimalComparer.CompareBalance(
                oldEstimatedLyd + expectedSellFinalAmount,
                newEstimatedLyd,
                "التكلفة الليبية المقدرة الجديدة");
            Assert.True(isEstimatedLydEqual, estimatedLydMsg);
            Output.WriteLine($"   ✅ {estimatedLydMsg}");

            // ================================================================
            // 17. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف مع الربح والعمولة المضمنة:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd:F2}");
            Output.WriteLine($"   💳 قيمة العمولة (USD): {commissionAmount:F2} (مضمنة)");
            Output.WriteLine($"   📈 نسبة الربح: {profitRatio * 100}%");
            Output.WriteLine($"   💰 قيمة الربح بالـ LYD: {expectedProfitInLyd:F4}");
            Output.WriteLine($"   💰 قيمة العمولة بالـ LYD: {expectedCommissionInLyd:F4}");
            Output.WriteLine($"   📈 سعر الصرف النهائي المتوقع: {expectedConversionRate:F4}");
            Output.WriteLine($"   📈 سعر الصرف النهائي الفعلي: {actualRate:F10}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD المتوقع: {expectedSellFinalAmount:F4}");
            Output.WriteLine($"   💰 المبلغ النهائي بالـ LYD الفعلي: {actualSellFinal:F4}");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 New Average Rate: {newAverageRate:F10}");
            Output.WriteLine($"   📊 Expected Average Rate: {expectedNewAverageRate:F10}");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID: {operationId}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ExchangeWithProfitAndCommissionIncluded_ShouldSucceed), true);
        }
    }
}
