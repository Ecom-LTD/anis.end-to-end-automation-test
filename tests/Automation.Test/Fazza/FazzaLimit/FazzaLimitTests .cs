using Automation.Framework.Core.Http;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Automation.Test.Support;
using Automation.Test.Tests.Fazza.Base;
using Automation.Test.TestSupport;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Fazza.FazzaLimitTests
{
    [Collection(CollectionNames.Fazza)]
    [Trait("Product", "Fazza")]
    [Trait("Category", "Slow")]
    [Trait("MutatesState", "true")]
    public class FazzaLimitTests : BaseFazzaTest
    {
        public FazzaLimitTests(ITestOutputHelper output, FazzaFixture fixture)
            : base(output, fixture) { }

      

        // ============================================================
        // 2. السقف أكبر من سقف المنطقة — يجب أن يُرفض ولا يتغيّر شيء
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_GreaterThanRegionLimit_ShouldFail()
        {
            Output.WriteLine("\n🧪 اختبار: سقف الحساب أكبر من سقف المنطقة");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var region = await this.GetRegionAsync(FazzaUser1.RegionId);

            var invalidLimit = region.FazaaMaxLimit + 0.01m;

            Assert.True(invalidLimit > region.FazaaMaxLimit,
                "Test setup error: invalidLimit not actually invalid.");

            Output.WriteLine($"📊 Account Limit (قبل): {before.Account.MaxFazaaDebtLimit}");
            Output.WriteLine($"📊 Region Max: {region.FazaaMaxLimit}");
            Output.WriteLine($"❌ Invalid Limit: {invalidLimit}");

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    invalidLimit));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body),
                "API rejected but returned empty body.");

            Output.WriteLine($"✅ Rejected. Status: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            // التحقق: السقف لم يتغيّر (الفرق = 0) -> نستخدم AssertDelta هنا
            var after = await this.TakeSnapshotAsync(FazzaUser1);
            this.AssertDelta(
                before.Account.MaxFazaaDebtLimit,
                after.Account.MaxFazaaDebtLimit,
                expectedDelta: 0m,
                label: "MaxFazaaDebtLimit (Should be unchanged)");
        }

        // ============================================================
        // 3. تعيين سقف بقيمة محددة (10,000) من القيمة المتاحة في المنطقة
        // ============================================================
        // ============================================================
        // تعيين سقف بقيمة محددة (10,000) بعد ضمان سعة المنطقة
        // (هذا الاختبار يغير الحالة ويتركها على 10,000)
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_To_10000_ShouldSucceed()
        {
            Output.WriteLine("\n🛡️ اختبار: تعيين سقف الحساب إلى 10,000 وتثبيته");

            const decimal requestedLimit = 10_000m;

            // 1. أخذ لقطة قبل التعديل
            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;

            Output.WriteLine($"📊 السقف الحالي للحساب: {originalLimit}");
            Output.WriteLine($"🎯 السقف المطلوب (النهائي): {requestedLimit}");

            // 2. ضمان سعة المنطقة
            // نمرر requestedLimit لأننا سنعين السقف لهذه القيمة.
            // الـ Helper سيحسب الفرق (إن وجد) ويرفع سقف المنطقة تلقائياً.
            await this.EnsureRegionCapacityAsync(FazzaUser1.RegionId, requestedLimit);

            // 3. تنفيذ طلب التعيين
            var response = await ExecuteWithRetryAsync(
                Dashboard,
                () => FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    requestedLimit));

            Assert.False(string.IsNullOrWhiteSpace(response), "API returned empty response.");

            // 4. الانتظار حتى يظهر السقف الجديد في النظام
            await this.WaitForAccountLimitAsync(FazzaUser1, requestedLimit);

            // 5. التحقق من المطابقة التامة (القيمة الفعلية == 10,000)
            var after = await this.TakeSnapshotAsync(FazzaUser1);
            this.AssertEqualTo(
                expected: requestedLimit,
                actual: after.Account.MaxFazaaDebtLimit,
                label: "MaxFazaaDebtLimit");

            Output.WriteLine($"✅ تم تعيين السقف إلى {requestedLimit} بنجاح وسيبقى عليه.");
        }
        // ============================================================
        // 4. سقف سالب — يجب أن يُرفض
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_NegativeValue_ShouldFail()
        {
            Output.WriteLine("\n🧪 اختبار: تعيين سقف بقيمة سالبة");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;

            const decimal invalidLimit = -1m;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"❌ Invalid Negative Limit: {invalidLimit}");

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    invalidLimit));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body),
                "API rejected but returned empty body.");

            Output.WriteLine($"✅ Rejected. Status: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            var after = await this.TakeSnapshotAsync(FazzaUser1);
            this.AssertDelta(
                before.Account.MaxFazaaDebtLimit,
                after.Account.MaxFazaaDebtLimit,
                expectedDelta: 0m,
                label: "MaxFazaaDebtLimit (Should be unchanged)");
        }

        // ============================================================
        // 5. سقف بقيمة قصوى (decimal.MaxValue) — يجب أن يُرفض
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_MaxValue_ShouldFail()
        {
            Output.WriteLine("\n🧪 اختبار: تعيين سقف بقيمة قصوى (decimal.MaxValue)");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;

            var invalidLimit = decimal.MaxValue;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"❌ Invalid Max Limit: {invalidLimit}");

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    invalidLimit));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body),
                "API rejected but returned empty body.");

            Output.WriteLine($"✅ Rejected. Status: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            var after = await this.TakeSnapshotAsync(FazzaUser1);
            this.AssertDelta(
                before.Account.MaxFazaaDebtLimit,
                after.Account.MaxFazaaDebtLimit,
                expectedDelta: 0m,
                label: "MaxFazaaDebtLimit (Should be unchanged)");
        }

        [Fact]
        public async Task ActivateAccount_ShouldNotChangeBalances()
        {
            Output.WriteLine(
                "\n🧪 اختبار: التفعيل لا يغيّر أرصدة FazzaUser1 و FazzaUser2");

            var payer = FazzaUser1;
            var debtor = FazzaUser2;

            var beforePayer = await this.TakeSnapshotAsync(payer);
            var beforeDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("📊 قبل التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {beforePayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {beforeDebtor.WalletBalance:F3} LYD");

            await AccountReadinessHelper.ActivateAccountAsync(
               Transfer,
                payerSession: payer,
                debtorSession: debtor,
                log: Output.WriteLine);

            await this.WaitForBalanceAsync(payer, beforePayer.WalletBalance);
            await this.WaitForBalanceAsync(debtor, beforeDebtor.WalletBalance);

            var afterPayer = await this.TakeSnapshotAsync(payer);
            var afterDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("\n📊 بعد التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {afterPayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {afterDebtor.WalletBalance:F3} LYD");

            // التحقق: الأرصدة لم تتغير (الفرق = 0)
            this.AssertDelta(
                beforePayer.WalletBalance,
                afterPayer.WalletBalance,
                expectedDelta: 0m,
                label: $"{payer.UserKey} balance (should be unchanged)");

            this.AssertDelta(
                beforeDebtor.WalletBalance,
                afterDebtor.WalletBalance,
                expectedDelta: 0m,
                label: $"{debtor.UserKey} balance (should be unchanged)");

            PrintResult(nameof(ActivateAccount_ShouldNotChangeBalances), true);
        }

        [Fact]
        public async Task ActivateAccount_ShouldNotChangeBalance2()
        {
            Output.WriteLine("\n🧪 اختبار: التفعيل (عبر EnsureDebtFree) لا يغيّر الأرصدة");

            var payer = FazzaUser1;
            var debtor = FazzaUser2;

            var beforePayer = await this.TakeSnapshotAsync(payer);
            var beforeDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("📊 قبل التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {beforePayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {beforeDebtor.WalletBalance:F3} LYD");

            Output.WriteLine("⏳ جاري تجهيز الحساب (قد يتم تفعيله إذا لم يكن موجوداً)...");

            await this.EnsureDebtFreeAsync(debtor);

            Output.WriteLine("⏳ في انتظار استقرار الأرصدة...");
            await this.WaitForBalanceAsync(payer, beforePayer.WalletBalance);
            await this.WaitForBalanceAsync(debtor, beforeDebtor.WalletBalance);

            var afterPayer = await this.TakeSnapshotAsync(payer);
            var afterDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("\n📊 بعد التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {afterPayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {afterDebtor.WalletBalance:F3} LYD");

            this.AssertDelta(
                beforePayer.WalletBalance,
                afterPayer.WalletBalance,
                expectedDelta: 0m,
                label: $"{payer.UserKey} balance (should be unchanged)");

            this.AssertDelta(
                beforeDebtor.WalletBalance,
                afterDebtor.WalletBalance,
                expectedDelta: 0m,
                label: $"{debtor.UserKey} balance (should be unchanged)");

            PrintResult(nameof(ActivateAccount_ShouldNotChangeBalance2), true);
        }

    }
}