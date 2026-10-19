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
    public class SimpleExchangeTests : BaseAlmusherTest
    {
        public SimpleExchangeTests(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }




        [Fact]
        public async Task GetBalance_shouldReturnValidBalance()
        {

            var anisCardLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardLyd.UserKey,
                CurrencyType.LYD);

            var anisCardUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardUsd.UserKey,
                CurrencyType.USD);

            var anisPayLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisPay.UserKey,
                CurrencyType.LYD);
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var commissionPayLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Commission.UserKey,
                CurrencyType.LYD);
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");


            var profitPayLydWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Profit.UserKey,
                CurrencyType.LYD);
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var hthLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var hthUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);
            var commissionPayNew = await Wallet.GetBalanceAsync(Commission.UserKey, commissionPayLydWalletId);
            var profitPayNew = await Wallet.GetBalanceAsync(Profit.UserKey, profitPayLydWalletId);

            Output.WriteLine($"   💰 AnisPay LYD:  {anisPayNew:F10}");
            Output.WriteLine($"   💰 HTH LYD:  {hthLydNew:F10}");
            Output.WriteLine($"   💰 HTH USD:  {hthUsdNew:F10}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshNew:F10}");
            Output.WriteLine($"   💰 Commission LYD: {commissionPayNew:F10}");
            Output.WriteLine($"   💰 Profit LYD: {profitPayNew:F10}");
        }
        // ================================================================
        // ✅ اختبار: صرف LYD → USD بدون أرباح أو عمولات
        // ================================================================
        [Fact]
        public async Task SimpleExchange_ShouldSucceed()
        {
            Output.WriteLine("\n💱 اختبار: صرف LYD → USD (بدون أرباح)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyUsd = 700m;
            decimal sellLyd = 1000m;

            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyUsd}");
            Output.WriteLine($"   💰 مبلغ البيع (LYD): {sellLyd}");

            // ================================================================
            // 2. حساب القيم المتوقعة
            // ================================================================
            var calc = ExchangeCalculator.SimpleExchange(buyUsd, sellLyd);

            Output.WriteLine($"\n📊 الحسابات المتوقعة:");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {calc.Rate:F10}");
            Output.WriteLine($"   💰 التكلفة بالدينار: {calc.LydCost:F10}");

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

            Output.WriteLine($"   📋 AnisCard LYD Wallet ID: {anisCardLydWalletId}");
            Output.WriteLine($"   📋 AnisCard USD Wallet ID: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisPay LYD Wallet ID: {anisPayLydWalletId}");

            // ================================================================
            // 4. جلب الأرصدة القديمة ومتوسط السعر قبل الصرف
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة ومتوسط السعر...");

            var anisPayOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var aniscardLydOld = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var aniscardUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);

            Output.WriteLine($"   💰 AnisPay LYD (مستقبل LYD): {anisPayOld:F10}");
            Output.WriteLine($"   💰 AnisCard LYD (مرسل LYD): {aniscardLydOld:F10}");
            Output.WriteLine($"   💰 AnisCard USD (مستقبل USD): {aniscardUsdOld:F10}");
            Output.WriteLine($"   💰 Hreysh USD (مرسل USD): {hreyshOld:F10}");

            // ✅ جلب متوسط السعر القديم مع التعامل مع 404
            decimal oldAverageRate = 0;
            decimal oldBalance = 0;
            decimal oldEstimatedLyd = 0;

            try
            {
                var oldAvgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardUsdWalletId.ToString());

                oldAverageRate = oldAvgResponse.Data.AverageRate;
                oldBalance = oldAvgResponse.Data.Balance;
                oldEstimatedLyd = oldAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

                Output.WriteLine($"\n📊 متوسط السعر القديم:");
                Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
                Output.WriteLine($"   📊 Old Balance (USD): {oldBalance:F10}");
                Output.WriteLine($"   📊 Old Estimated LYD: {oldEstimatedLyd:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("\n⚠️ Average Rate info not found (404) - using default values (0)");
                Output.WriteLine("   ℹ️ قد تكون هذه أول عملية صرف لهذه المحفظة");
            }
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
            // 6. جلب متوسط سعر الصرف (LydRate)
            // ================================================================
            Output.WriteLine("\n📈 جلب متوسط سعر الصرف (LydRate)...");

            decimal lydRate = 0;  // ✅ القيمة الافتراضية = 0 تعني "لا توجد قيمة"

            try
            {
                var avgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardUsdWalletId.ToString());

                Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);

                lydRate = avgResponse.Data.AverageRate;
                Output.WriteLine($"   📊 Average Sell Rate (LydRate): {lydRate:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ LydRate not found (404) - using default value (0)");
                Output.WriteLine("   ℹ️ هذه أول عملية صرف لهذه المحفظة، لا يوجد LydRate سابق");
                lydRate = 0;  // ✅ 0 تعني "لا توجد قيمة"
            }
            // ================================================================
            // 7. بناء طلب الصرف
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف...");

            // ✅ هذا هو الـ Operation ID الذي سيتم إرساله واستخدامه
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
                detailedStatement: "Simple Exchange Test")
                .Build();

            Output.WriteLine($"   📋 Operation ID (sent): {operationId}");
            Output.WriteLine($"   🔹 Buy (USD): {Hreysh.WalletId} → {anisCardUsdWalletId}");
            Output.WriteLine($"   🔹 Sell (LYD): {anisCardLydWalletId} → {anisPayLydWalletId}");

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

            // ✅ استخراج الـ Id من الاستجابة (هذا هو ChainId وليس OperationId)
            var idFromResponse = exchangeResponse.Data?.Id;
            Output.WriteLine($"   📋 ID from response (ChainId): {idFromResponse}");

            // ⚠️ ملاحظة: الـ ID الذي يتم إرجاعه هو ChainId، وليس OperationId
            // لذلك نستخدم operationId في جميع الطلبات اللاحقة (التفاصيل والتأكيد)

            // ================================================================
            // ⏳ 9. انتظار 5 ثواني قبل جلب التفاصيل
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 5 ثواني للتأكد من اكتمال العملية في النظام...");
            await Task.Delay(5000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 10. التحقق من تفاصيل العملية (باستخدام operationId المرسل)
            // ================================================================
            Output.WriteLine("\n🔍 التحقق من تفاصيل العملية...");

            var detailsResponse = await Almusher.GetExchangeDetailsAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);  // ✅ استخدام operationId المرسل

            Output.WriteLine($"   📋 Status Code: {(int)detailsResponse.StatusCode} - {detailsResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

            var details = detailsResponse.Data;
            var actualBuy = details.CurrencyExchangeBuy?.Amount ?? 0m;
            var actualSell = details.CurrencyExchangeSell?.Amount ?? 0m;
            var actualRate = details.ConversionRate;

            Output.WriteLine($"   📊 المبالغ الفعلية:");
            Output.WriteLine($"      💵 مبلغ الشراء الفعلي: {actualBuy:F10}");
            Output.WriteLine($"      💰 مبلغ البيع الفعلي: {actualSell:F10}");
            Output.WriteLine($"      📈 سعر الصرف الفعلي: {actualRate:F10}");

            // ✅ التحقق 1: مبلغ الشراء
            var (isBuyEqual, _, buyMsg) = DecimalComparer.Compare(buyUsd, actualBuy, "مبلغ الشراء");
            Assert.True(isBuyEqual, buyMsg);
            Output.WriteLine($"   ✅ {buyMsg}");

            // ✅ التحقق 2: مبلغ البيع
            var (isSellEqual, _, sellMsg) = DecimalComparer.Compare(sellLyd, actualSell, "مبلغ البيع");
            Assert.True(isSellEqual, sellMsg);
            Output.WriteLine($"   ✅ {sellMsg}");

            // ✅ التحقق 3: سعر الصرف
            var (isRateEqual, _, rateMsg) = DecimalComparer.Compare(calc.Rate, actualRate, "سعر الصرف");
            Assert.True(isRateEqual, rateMsg);
            Output.WriteLine($"   ✅ {rateMsg}");

            // ================================================================
            // 11. تأكيد العملية (باستخدام operationId المرسل)
            // ================================================================
            Output.WriteLine("\n✅ تأكيد العملية...");

            var confirmResponse = await Almusher.ConfirmExchangeAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);  // ✅ استخدام operationId المرسل

            Output.WriteLine($"   📋 Status Code: {(int)confirmResponse.StatusCode} - {confirmResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

            var exchangeId = confirmResponse.Data?.Id;
            Output.WriteLine($"   📋 Exchange ID: {exchangeId ?? "(null)"}");
            Assert.NotEmpty(exchangeId);

            Output.WriteLine($"   ✅ تم تأكيد العملية بنجاح (Exchange ID: {exchangeId})");
            // ================================================================
            // Delayed 2000
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 5 ثواني للتأكد من اكتمال العملية في النظام...");
            await Task.Delay(6000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");
            // ================================================================
            // 12. التحقق من الأرصدة الجديدة
            // ================================================================
            Output.WriteLine("\n💰 التحقق من الأرصدة الجديدة...");

            var anisPayNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayLydWalletId);
            var aniscardLydNew = await Wallet.GetBalanceAsync(AnisCardLyd.UserKey, anisCardLydWalletId);
            var aniscardUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var hreyshNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, Hreysh.WalletIdGuid);

            Output.WriteLine($"   💰 AnisPay LYD: {anisPayOld:F3} → {anisPayNew:F3}");
            Output.WriteLine($"   💰 AnisCard LYD: {aniscardLydOld:F3} → {aniscardLydNew:F3}");
            Output.WriteLine($"   💰 AnisCard USD: {aniscardUsdOld:F3} → {aniscardUsdNew:F3}");
            Output.WriteLine($"   💰 Hreysh USD: {hreyshOld:F3} → {hreyshNew:F3}");

            // ✅ التحقق 4: AnisPay LYD (زاد بمقدار sellLyd)
            var (isAnisPayEqual, _, anisPayMsg) = DecimalComparer.Compare(
                anisPayOld + sellLyd,
                anisPayNew,
                "AnisPay LYD");
            Assert.True(isAnisPayEqual, anisPayMsg);
            Output.WriteLine($"   ✅ {anisPayMsg}");

            // ✅ التحقق 5: AnisCard LYD (نقص بمقدار sellLyd)
            var (isAniscardLydEqual, _, aniscardLydMsg) = DecimalComparer.Compare(
                aniscardLydOld - sellLyd,
                aniscardLydNew,
                "AnisCard LYD");
            Assert.True(isAniscardLydEqual, aniscardLydMsg);
            Output.WriteLine($"   ✅ {aniscardLydMsg}");

            // ✅ التحقق 6: AnisCard USD (زاد بمقدار buyUsd)
            var (isAniscardUsdEqual, _, aniscardUsdMsg) = DecimalComparer.Compare(
                aniscardUsdOld + buyUsd,
                aniscardUsdNew,
                "AnisCard USD");
            Assert.True(isAniscardUsdEqual, aniscardUsdMsg);
            Output.WriteLine($"   ✅ {aniscardUsdMsg}");

            // ✅ التحقق 7: Hreysh USD (نقص بمقدار buyUsd)
            var (isHreyshEqual, _, hreyshMsg) = DecimalComparer.Compare(
                hreyshOld - buyUsd,
                hreyshNew,
                "Hreysh USD");
            Assert.True(isHreyshEqual, hreyshMsg);
            Output.WriteLine($"   ✅ {hreyshMsg}");

            // ================================================================
            // 13. التحقق من متوسط سعر الصرف (بعد الصرف)
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف (بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع (إذا كانت القيم القديمة موجودة)
            decimal expectedNewAverageRate = 0;
            if (oldBalance != 0 || oldEstimatedLyd != 0)
            {
                expectedNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                    oldBalance,
                    oldEstimatedLyd,
                    buyUsd,
                    calc.LydCost
                );
                Output.WriteLine($"   📊 Expected New Average Rate: {expectedNewAverageRate:F10}");
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
                    anisCardUsdWalletId.ToString());

                newAverageRate = newAvgResponse.Data.AverageRate;
                newBalance = newAvgResponse.Data.Balance;
                newEstimatedLyd = newAvgResponse.Data.KnownRateBalanceEstimatedLydAmount;

                Output.WriteLine($"   📊 New Average Rate (actual): {newAverageRate:F10}");
                Output.WriteLine($"   📊 New Balance: {newBalance:F10}");
                Output.WriteLine($"   📊 New Estimated LYD: {newEstimatedLyd:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                Output.WriteLine("   ⚠️ New Average Rate info not found (404) - using default values (0)");
            }

            // ✅ التحقق من متوسط سعر الصرف الجديد (فقط إذا كانت البيانات متوفرة)
            if (oldBalance != 0 || oldEstimatedLyd != 0)
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
            // 14. النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n════════════════════════════════════════════════════");
            Output.WriteLine("✅ جميع التحققات نجحت!");
            Output.WriteLine($"📊 ملخص الصرف:");
            Output.WriteLine($"   💵 Buy: {buyUsd} USD");
            Output.WriteLine($"   💰 Sell: {sellLyd} LYD");
            Output.WriteLine($"   📈 Rate: {calc.Rate:F10}");
            Output.WriteLine($"   🆔 Chain ID: {chainId}");
            Output.WriteLine($"   🆔 Operation ID (sent): {operationId}");
            Output.WriteLine($"   📋 Status Code: {(int)exchangeResponse.StatusCode} - {exchangeResponse.StatusCode}");
            Output.WriteLine($"   📊 Old Average Rate: {oldAverageRate:F10}");
            Output.WriteLine($"   📊 New Average Rate: {newAverageRate:F10}");
            Output.WriteLine($"   📊 Expected Average Rate: {expectedNewAverageRate:F10}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(SimpleExchange_ShouldSucceed), true);
        }
    }
}