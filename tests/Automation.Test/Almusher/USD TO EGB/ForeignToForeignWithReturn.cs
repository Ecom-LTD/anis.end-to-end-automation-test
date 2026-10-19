using Automation.Framework.Core.Enums;
using Automation.Framework.Core.Http;
using Automation.Framework.Helpers.Almusher;
using Automation.Framework.Services.Almusher.Models;
using Automation.Test.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Almusher.USD_TO_EGB
{
    public class ForeignToForeignWithReturn : BaseAlmusherTest
    {
        public ForeignToForeignWithReturn(ITestOutputHelper output, AlmuhserFixture fixture)
            : base(output, fixture) { }

        [Fact]
        public async Task ForeignToForeignExchange_WithReturn_ShouldSucceed()
        {
            Output.WriteLine("\n🔄 اختبار: صرف أجنبي مع إرجاع (Return)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // 1. بيانات الاختبار
            // ================================================================
            decimal buyAmount = 1000m;      // مبلغ الشراء (USD) - AnisPay → AnisCard
            decimal sellAmount = 300M;     // مبلغ البيع (EGB) - Hreysh → AnisCard
            decimal returnAmount = 100m;    // مبلغ الإرجاع (USD) - Hreysh → AnisCard
            decimal buyAmountwithoutReturn = buyAmount - returnAmount; // مبلغ الشراء بدون الإرجاع (USD)

            Output.WriteLine($"\n📊 بيانات الاختبار:");
            Output.WriteLine($"   💵 مبلغ الشراء (USD): {buyAmount:F2}");
            Output.WriteLine($"   💰 مبلغ البيع (EGB): {sellAmount:F2}");
            Output.WriteLine($"   🔄 مبلغ الإرجاع (USD): {returnAmount:F2}");

            // ================================================================
            // 2. جلب المحافظ
            // ================================================================
            Output.WriteLine("\n📝 جلب المحافظ...");

            var anisCardUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCardUsd.UserKey,
                CurrencyType.USD);
            Output.WriteLine($"   ✅ AnisCard USD Wallet ID: {anisCardUsdWalletId}");


            var anisCardEGBWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisCard.UserKey,
                CurrencyType.EGB);
            Output.WriteLine($"   ✅ AnisCard EGB Wallet ID: {anisCardEGBWalletId}");

            var anisPayUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                AnisPay.UserKey,
                CurrencyType.USD);
            Output.WriteLine($"   ✅ AnisPay USD Wallet ID: {anisPayUsdWalletId}");

            var hreshUsdWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Hreysh.UserKey,
                CurrencyType.USD);
            Output.WriteLine($"   ✅ Hresh USD Wallet ID: {hreshUsdWalletId}");

            var hreshEGBWalletId = await Wallet.GetOrLoadWalletIdAsync(
                Hreysh.UserKey,
                CurrencyType.EGB);
            Output.WriteLine($"   ✅ Hresh EGB Wallet ID: {hreshEGBWalletId}");

            Output.WriteLine($"   📋 AnisCard USD: {anisCardUsdWalletId}");
            Output.WriteLine($"   📋 AnisCard EGB: {anisCardEGBWalletId}");
            Output.WriteLine($"   📋 AnisPay USD: {anisPayUsdWalletId}");
            Output.WriteLine($"   📋 Hresh USD: {hreshUsdWalletId}");
            Output.WriteLine($"   📋 Hresh EGB: {hreshEGBWalletId}");

            // ================================================================
            // 3. جلب الأرصدة القديمة (قبل الصرف)
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة القديمة...");

            var anisCardUsdOld = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var anisCardEGBOld = await Wallet.GetBalanceAsync(AnisCard.UserKey, anisCardEGBWalletId);
            var anisPayUsdOld = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayUsdWalletId);
            var hreshUsdOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreshUsdWalletId);
            var hreshEGBOld = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreshEGBWalletId);

            Output.WriteLine($"   💰 AnisCard USD (قبل): {anisCardUsdOld:F4}");
            Output.WriteLine($"   💰 AnisCard EGB (قبل): {anisCardEGBOld:F4}");
            Output.WriteLine($"   💰 AnisPay USD (قبل): {anisPayUsdOld:F4}");
            Output.WriteLine($"   💰 Hresh USD (قبل): {hreshUsdOld:F4}");
            Output.WriteLine($"   💰 Hresh EGB (قبل): {hreshEGBOld:F4}");
            // ================================================================
            // 4. جلب LydRate
            // ================================================================
            Output.WriteLine("\n📈 جلب LydRate...");

            decimal lydRate = 0;

            try
            {
                var avgResponse = await Almusher.GetAverageRateInfoAsync(
                    Dashboard.UserKey,
                    anisCardUsdWalletId.ToString());

                Assert.Equal(HttpStatusCode.OK, avgResponse.StatusCode);
                Output.WriteLine($"   📋 Raw Response: {avgResponse.RawBody}");

                // ✅ إذا كان AverageRate موجوداً، استخدمه
                lydRate = avgResponse.Data.AverageRate;
                Output.WriteLine($"   📊 LydRate (LYD/USD): {lydRate:F10}");
            }
            catch (ApiException ex) when (ex.ApiStatusCode == HttpStatusCode.NotFound)
            {
                // ✅ إذا لم يتم العثور على Average Rate، استخدم 0
                Output.WriteLine("   ⚠️ LydRate not found - باستخدام القيمة 0 (أول عملية صرف)");
                lydRate = 0;  // ✅ هذا هو المطلوب
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
                    anisCardEGBWalletId.ToString());

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
            // 5. حساب سعر الصرف المتوقع
            // ================================================================
            decimal lydCost = sellAmount * lydRate;
            decimal expectedRate = lydCost /(buyAmount + returnAmount);

            Output.WriteLine($"\n📊 الحسابات المتوقعة:");
            Output.WriteLine($"   💰 LydCost: {lydCost:F10}");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {expectedRate:F10}");

            // ================================================================
            // 6. إنشاء سلسلة الدفع
            // ================================================================
            Output.WriteLine("\n🔗 إنشاء سلسلة الدفع...");

            var chainResponse = await Almusher.CreatePaymentChainAsync(Dashboard.UserKey);
            Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);

            var chainId = Guid.Parse(chainResponse.Data.Id);
            Output.WriteLine($"   📋 Chain ID: {chainId}");

            // ================================================================
            // 7. بناء طلب الصرف مع Return
            // ================================================================
            Output.WriteLine("\n💱 بناء طلب الصرف مع Return...");

            var operationId = Guid.NewGuid().ToString();

            var request = Almusher.CreateForeignExchangeRequest(
                operationId: operationId,
    // ✅ Buy Side (EGB) - Hresh → AnisCard
    buyCreditorWalletId: hreshEGBWalletId.ToString(),      // ✅ Hresh EGB (يرسل EGB)
    buyDebitorWalletId: anisCardEGBWalletId.ToString(),    // ✅ AnisCard EGB (يستقبل EGB)
    buyAmount: buyAmount,

    // ✅ Sell Side (USD) - AnisCard → AnisPay
    sellCreditorWalletId: anisCardUsdWalletId.ToString(),  // ✅ AnisCard USD (يرسل USD)
    sellDebitorWalletId: anisPayUsdWalletId.ToString(),    // ✅ AnisPay USD (يستقبل USD)
    sellAmount: sellAmount,

    lydRate: lydRate,
    detailedStatement: "Foreign exchange with return")

    // ✅ Return (EGB) - Hresh → AnisCard
    .WithReturn(
        creditorReturnWalletId: hreshEGBWalletId.ToString(),     // ✅ Hresh EGB (يرسل الإرجاع)
        debitorReturnWalletId: anisCardEGBWalletId.ToString(),   // ✅ AnisCard EGB (يستقبل الإرجاع)
        totalAmount: returnAmount,
        ("Return 1", returnAmount))
    .Build();

            Output.WriteLine($"   📋 Operation ID: {operationId}");
            Output.WriteLine($"   🔹 Buy (USD): AnisPay → AnisCard = {buyAmount:F2} USD");
            Output.WriteLine($"   🔹 Sell (EGB): Hresh → AnisCard = {sellAmount:F2} EGB");
            Output.WriteLine($"   🔄 Return (USD): Hresh → AnisCard = {returnAmount:F2} USD");

            // ================================================================
            // 8. تنفيذ الصرف
            // ================================================================
            Output.WriteLine("\n💱 تنفيذ الصرف...");

            ApiResponse<ForeignToForeignExchangeResponse>? response = null;

            try
            {
                response = await Almusher.ForeignToForeignExchangeAsync(
                    Dashboard.UserKey,
                    chainId.ToString(),
                    request);

                Output.WriteLine($"   📋 Status Code: {(int)response.StatusCode} - {response.StatusCode}");

                // ✅ عرض الـ Raw Body للتصحيح
                if (!string.IsNullOrEmpty(response.RawBody))
                {
                    Output.WriteLine($"   📋 Raw Response: {response.RawBody}");
                }

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Output.WriteLine($"   ✅ تم تنفيذ الصرف بنجاح");
            }
            catch (ApiException ex)
            {
                // ✅ عرض الرسالة الخام من الـ API كما هي في Postman
                Output.WriteLine($"   ❌ فشل الصرف (API Exception):");
                Output.WriteLine($"   📋 Status Code: {(int)ex.ApiStatusCode} - {ex.ApiStatusCode}");
                Output.WriteLine($"   📋 Raw Body: {ex.Body}");

                // ✅ عرض الرسالة المخصصة (إذا وجدت)
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    Output.WriteLine($"   📋 Message: {ex.Message}");
                }

                // ✅ إعادة رمي الاستثناء
                throw;
            }
                Output.WriteLine($"   ✅ تم تنفيذ الصرف بنجاح");

            // ================================================================
            // 9. التحقق من التفاصيل
            // ================================================================


            // ⏳ انتظار 5 ثواني للتأكد من اكتمال العملية في النظام
            Output.WriteLine("\n⏳ انتظار 5 ثواني لتحديث البيانات...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            Output.WriteLine("\n🔍 التحقق من التفاصيل...");

            var detailsResponse = await Almusher.GetExchangeDetailsAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

            var details = detailsResponse.Data;
            Output.WriteLine($"   📈 سعر الصرف الفعلي: {details.ConversionRate:F10}");

            // ✅ التحقق من سعر الصرف
            var (isRateEqual, _, rateMsg) = DecimalComparer.CompareRate(
                expectedRate,
                details.ConversionRate,
                "سعر الصرف النهائي");
            Assert.True(isRateEqual, rateMsg);
            Output.WriteLine($"   ✅ {rateMsg}");

            // ================================================================
            // 10. تأكيد العملية
            // ================================================================
            Output.WriteLine("\n✅ تأكيد العملية...");

            var confirmResponse = await Almusher.ConfirmExchangeAsync(
                Dashboard.UserKey,
                chainId.ToString(),
                operationId);

            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
            Output.WriteLine("   ✅ تم تأكيد العملية بنجاح");

            // ================================================================
            // 11. انتظار تحديث الأرصدة
            // ================================================================
            Output.WriteLine("\n⏳ انتظار 7 ثواني لتحديث الأرصدة...");
            await Task.Delay(7000);
            Output.WriteLine("   ✅ تم الانتهاء من الانتظار");

            // ================================================================
            // 12. جلب الأرصدة الجديدة (بعد الصرف)
            // ================================================================
            Output.WriteLine("\n💰 جلب الأرصدة الجديدة...");

            var anisCardUsdNew = await Wallet.GetBalanceAsync(AnisCardUsd.UserKey, anisCardUsdWalletId);
            var anisCardEGBNew = await Wallet.GetBalanceAsync(AnisCard.UserKey, anisCardEGBWalletId);
            var anisPayUsdNew = await Wallet.GetBalanceAsync(AnisPay.UserKey, anisPayUsdWalletId);
            var hreshUsdNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreshUsdWalletId);
            var hreshEGBNew = await Wallet.GetBalanceAsync(Hreysh.UserKey, hreshEGBWalletId);

            Output.WriteLine($"   💰 AnisCard USD (بعد): {anisCardUsdNew:F4}");
            Output.WriteLine($"   💰 AnisCard EGB (بعد): {anisCardEGBNew:F4}");
            Output.WriteLine($"   💰 AnisPay USD (بعد): {anisPayUsdNew:F4}");
            Output.WriteLine($"   💰 Hresh USD (بعد): {hreshUsdNew:F4}");
            Output.WriteLine($"   💰 Hresh EGB (بعد): {hreshEGBNew:F4}");

            // ================================================================
            // 13. التحقق من الأرصدة
            // ================================================================
            Output.WriteLine("\n✅ التحقق من الأرصدة:");

            // ✅ التحقق 1: AnisPay USD (نقص بمقدار
            // sellAmount - يرسل دولار)
            var expectedAnisPayUsd = anisPayUsdOld + sellAmount;
            var (isAnisPayUsdEqual, _, anisPayUsdMsg) = DecimalComparer.Compare(
                expectedAnisPayUsd,
                anisPayUsdNew,
                "AnisPay USD (مرسل)");
            Assert.True(isAnisPayUsdEqual, anisPayUsdMsg);
            Output.WriteLine($"   ✅ {anisPayUsdMsg}");

            // ✅ التحقق 2: AnisCard USD (نقص بمقدار sellamount - returnAmount)
            // AnisCard يستقبل buyAmount دولار من AnisPay ويستقبل returnAmount دولار من Hresh
            var expectedAnisCardUsd = anisCardUsdOld -  sellAmount ;
            var (isAnisCardUsdEqual, _, anisCardUsdMsg) = DecimalComparer.Compare(
                expectedAnisCardUsd,
                anisCardUsdNew,
                "AnisCard USD (مستقبل)");
            Assert.True(isAnisCardUsdEqual, anisCardUsdMsg);
            Output.WriteLine($"   ✅ {anisCardUsdMsg}");

            // ✅ التحقق 3: AnisCard EGB (زاد بمقدار sellAmount - يستقبل EGB من Hresh)
            var expectedAnisCardEgb = anisCardEGBOld + buyAmount + returnAmount;
            var (isAnisCardEgbEqual, _, anisCardEgbMsg) = DecimalComparer.Compare(
                expectedAnisCardEgb,
                anisCardEGBNew,
                "AnisCard EGB (مستقبل)");
            Assert.True(isAnisCardEgbEqual, anisCardEgbMsg);
            Output.WriteLine($"   ✅ {anisCardEgbMsg}");

 

            // ✅ التحقق 5: Hresh EGB (نقص بمقدار sellAmount - يرسل EGB إلى AnisCard)
            var expectedHreshEgb = hreshEGBOld -( buyAmount + returnAmount);
            var (isHreshEgbEqual, _, hreshEgbMsg) = DecimalComparer.Compare(
                expectedHreshEgb,
                hreshEGBNew,
                "Hresh EGB (مرسل)");
            Assert.True(isHreshEgbEqual, hreshEgbMsg);
            Output.WriteLine($"   ✅ {hreshEgbMsg}");




            // ================================================================
            // ✅ 14.6 التحقق من EGB Average Balance بعد الصرف
            // ================================================================
            Output.WriteLine("\n📈 التحقق من متوسط سعر الصرف العملة المصريى (بعد الصرف)...");

            // ✅ حساب متوسط السعر المتوقع (إذا كانت القيم القديمة موجودة)
            decimal expectedEGBNewAverageRate = 0;
            if (oldEGBAvgBalance != 0 || oldEGBEstimatedBalance != 0)
            {
                expectedEGBNewAverageRate = ExchangeCalculator.CalcNewAvgRate(
                    oldEGBAvgBalance,
                    oldEGBEstimatedBalance + returnAmount,
                    buyAmount,
                    lydCost
                );
                Output.WriteLine($"   📊 Expected New EGB Average Rate: {expectedEGBNewAverageRate:F10}");
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
                    anisCardEGBWalletId.ToString());

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
                    expectedEGBNewAverageRate,
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
            Output.WriteLine($"📊 ملخص الصرف مع Return:");
            Output.WriteLine($"   💵 AnisPay → AnisCard (USD): +{buyAmount:F2} USD");
            Output.WriteLine($"   💰 Hresh → AnisCard (EGB): +{sellAmount:F2} EGB");
            Output.WriteLine($"   🔄 Hresh → AnisCard (Return): +{returnAmount:F2} USD");
            Output.WriteLine($"   📈 سعر الصرف المتوقع: {expectedRate:F10}");
            Output.WriteLine($"   📈 سعر الصرف الفعلي: {details.ConversionRate:F10}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(ForeignToForeignExchange_WithReturn_ShouldSucceed), true);
        }
    }
}