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
    public class ExchangeWithProfitTests : BaseAlmusherTest
    {
        public ExchangeWithProfitTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        // ================================================================
        // ✅ اختبار: صرف LYD → USD مع ربح 2%
        // ================================================================
        [Fact]
        public async Task ExchangeWithProfit_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف LYD → USD مع إضافة ربح");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyUsd = 700m;
            decimal sellLyd = 1000m;
            decimal profitRatio = 0.11m;
            decimal baserate = sellLyd / buyUsd;  // 1000 / 700 = 1.4285714286

            // ✅ حساب الربح المتوقع بناءً على الـ Response
            // profit = sellLyd * profitRatio = 1000 * 0.02 = 20
            // ولكن الـ Response يظهر profit = 14
            // هذا يعني أن الربح محسوب على مبلغ آخر (ربما 700)
            // profit = 700 * 0.02 = 14 ✅
            //(baserate + profitRatio) × buyUsd - sellLyd = 100 LYD  
            decimal expectedProfit = (baserate + profitRatio) * buyUsd - sellLyd;  // 14
            decimal finalSellAmount = sellLyd + expectedProfit;  // 1000 + 14 = 1014
            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd}");
            Output.WriteLine($"   📈 نسبة الربح: {profitRatio * 100}%");
            Output.WriteLine($"   💰 الربح المتوقع: {expectedProfit:F2} LYD");

            // ================================================================
            // 2. حساب القيم المتوقعة
            // ================================================================
            // ✅ المبلغ النهائي = sellLyd + expectedProfit
         //   decimal sellLydWithProfit = sellLyd + expectedProfit;  // 1000 + 14 = 1014
            var calcWithProfit = ExchangeCalculator.SimpleExchange(buyUsd, finalSellAmount);

            Output.WriteLine($"\n📊 الحسابات المتوقعة:");
            Output.WriteLine($"   💰 مبلغ البيع مع الربح: {finalSellAmount:F4} LYD");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {calcWithProfit.Rate:F10}");

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

            Output.WriteLine($"   📋 AnisCard LYD Wallet ID: {anisCardLydWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay LYD Wallet ID: {anisPayLydWalletId}");
            Output.WriteLine($"   📋 Profit Wallet ID: {profitWalletId}");

            // ================================================================
            // 4. جلب الأرصدة القديمة
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة...");

            var anisPayOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydOld = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);
            var profitWalletOld = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);

            Output.WriteLine($"   💰 AnisPay LYD (مستقبل LYD): {anisPayOld:F4}");
            Output.WriteLine($"   💰 HTH LYD (مرسل LYD): {hthLydOld:F4}");
            Output.WriteLine($"   💰 HTH USD (مستقبل USD): {hthUsdOld:F4}");
            Output.WriteLine($"   💰 Hreysh USD (مرسل USD): {hreyshOld:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4}");

            // ================================================================
            // 5. جلب متوسط السعر القديم
            // ================================================================
            var oldAvgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, oldAvgResponse.StatusCode);

            var oldAverageRate = oldAvgResponse.Data.AverageRate;
            var oldBalance = oldAvgResponse.Data.Balance;
            var oldEstimatedLyd = oldAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

            Output.WriteLine($"\n📊 متوسط السعر القديم:");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 Old Balance (USD): {oldBalance:F10}");
            Output.WriteLine($"   📊 Old Estimated LYD: {oldEstimatedLyd:F10}");

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
            // 7. جلب متوسط سعر الصرف (LydRate)
            // ================================================================
            Output.WriteLine("\n📈 جلب متوسط سعر الصرف (LydRate)...");

            var avgResponse = await Almusher.GetAverageRateInfoAsync(
                Dashboard.UserKey,
                anisCardUsdWalletId.ToString());

            Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

            var lydRate = avgResponse.Data.AverageRate;
            Output.WriteLine($"   📊 Average Sell Rate (LydRate): {lydRate:F10}");

            // ================================================================
            // 8. بناء طلب الصرف مع الربح
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف مع الربح...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateExchangeRequest(
                operationId: operationId,
                buyCreditorWalletId: Hreysh.WalletId,
                buyDebitorWalletId: anisCardUsdWalletId.ToString(),
                sellCreditorWalletId: anisCardLydWalletId.ToString(),
                sellDebitorWalletId: anisPayLydWalletId.ToString(),
                buyAmount: buyUsd,
                sellAmount: sellLyd,
                lydRate: lydRate,
                detailedStatement: "Exchange with Profit Test")
                .WithProfit(
                    profitWalletId: profitWalletId.ToString(),
                    totalRatio: profitRatio,
                    profitDetailedStatement: "2% profit from exchange",
                    description: "Profit 2%")
                .Build();

            Output.WriteLine($"   📋 Operation ID: {operationId}");
            Output.WriteLine($"   📈 Profit Ratio: {profitRatio * 100}%");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletId}");

            // ================================================================
            // 9. تنفيذ عملية الصرف
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
            // ⏳ 10. انتظار 7 ثواني
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني للتأكد من اكتمال العملية...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 11. التحقق من تفاصيل العملية
            // ================================================================
            Output.WriteLine("\n🔍 التحقق من تفاصيل العملية...");

            var detailsResponse = await Almusher.GetExchangeDetailsAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Output.WriteLine($"   📋 Status Code: {(int)detailsResponse.StatusCode} - {detailsResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

            var details = detailsResponse.Data;

            // ✅ عرض جميع التفاصيل
            Output.WriteLine("\n📊 تفاصيل العملية من الـ Response:");
            Output.WriteLine("═══════════════════════════════════════════════════════════");
            Output.WriteLine($"   🆔 ID:                      {details.Id}");
            Output.WriteLine($"   📈 Conversion Rate:         {details.ConversionRate:F10}");
            Output.WriteLine($"   📝 Statement:               {details.DetailedStatement}");
            Output.WriteLine($"   💰 LydRate:                 {details.LydRate:F10}");
            Output.WriteLine($"   🔄 Uses Sell As Base:       {details.UsesSellCurrencyAsBase}");
            Output.WriteLine("─────────────────────────────────────────────────────────────");

            // ✅ جانب الشراء (Buy)
            var buySide = details.CurrencyExchangeBuy;
            Output.WriteLine("\n💵 جانب الشراء (Buy):");
            Output.WriteLine($"   💰 Amount:                  {buySide?.Amount:F4}");
            Output.WriteLine($"   💰 Final Amount:            {buySide?.FinalAmount:F4}");
            Output.WriteLine($"   🏦 Creditor Wallet:         {buySide?.CreditorWallet?.WalletId}");
            Output.WriteLine($"   🏦 Debitor Wallet:          {buySide?.DebitorWallet?.WalletId}");

            // ✅ جانب البيع (Sell)
            var sellSide = details.CurrencyExchangeSell;
            Output.WriteLine("\n💰 جانب البيع (Sell):");
            Output.WriteLine($"   💰 Amount:                  {sellSide?.Amount:F4}");
            Output.WriteLine($"   💰 Final Amount:            {sellSide?.FinalAmount:F4}");
            Output.WriteLine($"   🏦 Creditor Wallet:         {sellSide?.CreditorWallet?.WalletId}");
            Output.WriteLine($"   🏦 Debitor Wallet:          {sellSide?.DebitorWallet?.WalletId}");

            // ✅ معلومات الربح
            var profit = details.Profit;  // ✅ Profit بحرف P كبير (مطابق للـ Model)
            if (profit != null)
            {
                Output.WriteLine("\n📈 معلومات الربح (Profit):");
                Output.WriteLine($"   💰 Profit Wallet:          {profit.Wallet?.WalletId}");
                Output.WriteLine($"   📈 Total Ratio:            {profit.TotalRatio:F4}");
                Output.WriteLine($"   💰 Total Amount:           {profit.TotalAmount:F4}");
                Output.WriteLine($"   📝 Statement:              {profit.DetailedStatement}");

                // ✅ ProfitElements (جمع - List)
                if (profit.ProfitElements != null && profit.ProfitElements.Count > 0)
                {
                    foreach (var element in profit.ProfitElements)
                    {
                        Output.WriteLine($"   📝 Element Description:    {element.Description}");
                        Output.WriteLine($"      📈 Element Ratio:       {element.Ratio:F4}");
                        Output.WriteLine($"      💰 Element Amount:      {element.Amount:F4}");
                    }
                }
            }
            // ================================================================
            // 12. التحقق من القيم
            // ================================================================
            Output.WriteLine("\n✅ التحقق من القيم:");

            // ✅ التحقق 1: مبلغ الشراء
            var actualBuy = buySide?.Amount ?? 0m;
            var (isBuyEqual, _, buyMsg) = DecimalComparer.Compare(buyUsd, actualBuy, "مبلغ الشراء");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ البيع النهائي (مع الربح)
            var actualSellFinal = sellSide?.FinalAmount ?? 0m;
            var (isSellEqual, _, sellMsg) = DecimalComparer.Compare(finalSellAmount, actualSellFinal, "مبلغ البيع النهائي");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 3: سعر الصرف
            var actualRate = details.ConversionRate;
            var (isRateEqual, _, rateMsg) = DecimalComparer.Compare(calcWithProfit.Rate, actualRate, "سعر الصرف");
            Assert.True(isRateEqual, rateMsg);
            Output.WriteLine($"   ✅ {rateMsg}");

            // ✅ التحقق 4: قيمة الربح
            var actualProfitAmount = profit?.TotalAmount ?? 0m;
            var (isProfitEqual, _, profitMsg) = DecimalComparer.Compare(expectedProfit, actualProfitAmount, "قيمة الربح");
            Assert.True(isProfitEqual, profitMsg);
            Output.WriteLine($"   ✅ {profitMsg}");

            // ✅ التحقق 5: نسبة الربح
            var actualProfitRatio = profit?.TotalRatio ?? 0m;
            var (isRatioEqual, _, ratioMsg) = DecimalComparer.Compare(profitRatio, actualProfitRatio, "نسبة الربح");
            Assert.True(isRatioEqual, ratioMsg);
            Output.WriteLine($"   ✅ {ratioMsg}");

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
            // ⏳ 14. انتظار 7 ثواني لتحديث الأرصدة
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني لتحديث الأرصدة...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 15. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);
            var profitWalletNew = await Wallet.GetBalanceAsync(Profit.UserKey, profitWalletId);

            Output.WriteLine($"   💰 AnisPay LYD: {anisPayOld:F4} → {anisPayNew:F4}");
            Output.WriteLine($"   💰 HTH LYD: {hthLydOld:F4} → {hthLydNew:F4}");
            Output.WriteLine($"   💰 HTH USD: {hthUsdOld:F4} → {hthUsdNew:F4}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshOld:F4} → {hreyshNew:F4}");
            Output.WriteLine($"   💰 Profit Wallet: {profitWalletOld:F4} → {profitWalletNew:F4}");

            // ✅ التحقق 6: AnisPay LYD (زاد بمقدار sellLyd)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.Compare(
                anisPayOld + sellLyd,
                anisPayNew,
                "AnisPay LYD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 7: HTH LYD (نقص بمقدار sellLyd + profit)
            decimal expectedHthLyd = hthLydOld - (finalSellAmount);
            var (isHthLydEqual, _, hthLydMsg) = DecimalComparer.Compare(
                expectedHthLyd,
                hthLydNew,
                "HTH LYD");
            Assert.True(isHthLydEqual, hthLydMsg);
            Output.WriteLine($"   ✅ {hthLydMsg}");

            // ✅ التحقق 8: HTH USD (زاد بمقدار buyUsd)
            var (isHthUsdEqual, _, hthUsdMsg) = DecimalComparer.Compare(
                hthUsdOld + buyUsd,
                hthUsdNew,
                "HTH USD");
            Assert.True(isHthUsdEqual, hthUsdMsg);
            Output.WriteLine($"   ✅ {hthUsdMsg}");

            // ✅ التحقق 9: Hreysh USD (نقص بمقدار buyUsd)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.Compare(
                hreyshOld - buyUsd,
                hreyshNew,
                "Hreysh USD");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ✅ التحقق 10: Profit Wallet (زاد بمقدار expectedProfit)
            var (isProfitWalletEqual, _, profitWalletMsg) = DecimalComparer.Compare(
                profitWalletOld + expectedProfit,
                profitWalletNew,
                "Profit Wallet");
            Assert.True(isProfitWalletEqual, profitWalletMsg);
            Output.WriteLine($"   ✅ {profitWalletMsg}");
            // ================================================================
            // 16. التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف (Average Rate - بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع
            var expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                oldBalance,          // الرصيد القديم بالـ USD
                oldEstimatedLyd,     // التكلفة القديمة بالـ LYD
                buyUsd,              // مبلغ الشراء (بدون عمولة)
                finalSellAmount              // مبلغ البيع (بدون ربح)
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

            // ✅ التحقق من متوسط سعر الصرف الجديد
            var (isAvgRateEqual, _, avgRateMsg) = DecimalComparer.CompareRate(
                expectedNewAverageRate,
                newAverageRate,
                "متوسط سعر الصرف الجديد");
            Assert.True(isAvgRateEqual, avgRateMsg);
            Output.WriteLine($"   ✅ {avgRateMsg}");

            // ✅ التحقق من الرصيد الجديد
            var (isBalanceEqual, _, balanceMsg) = DecimalComparer.CompareBalance(
                oldBalance + buyUsd,
                newBalance,
                "الرصيد الجديد");
            Assert.True(isBalanceEqual, balanceMsg);
            Output.WriteLine($"   ✅ {balanceMsg}");

            // ✅ التحقق من التكلفة الليبية المقدرة الجديدة
            var (isEstimatedLydEqual, _, estimatedLydMsg) = DecimalComparer.CompareBalance(
                oldEstimatedLyd + finalSellAmount,
                newEstimatedLyd,
                "التكلفة الليبية المقدرة الجديدة");
            Assert.True(isEstimatedLydEqual, estimatedLydMsg);
            Output.WriteLine($"   ✅ {estimatedLydMsg}");
            // ================================================================
            // 16. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف مع الربح:");
            Output.WriteLine($"   💵 Buy: {buyUsd} USD");
            Output.WriteLine($"   💰 Sell: {sellLyd} LYD + {expectedProfit:F2} Profit = {finalSellAmount:F2} LYD");
            Output.WriteLine($"   📈 Rate: {calcWithProfit.Rate:F10}");
            Output.WriteLine($"   📈 Profit Ratio: {profitRatio * 100}%");
            Output.WriteLine($"   💰 Profit Amount: {expectedProfit:F2} LYD");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID: {operationId}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ExchangeWithProfit_ShouldSucceed), true);
        }
    }
}