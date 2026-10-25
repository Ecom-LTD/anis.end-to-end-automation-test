using Automation.Framework.Core.Http;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
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
        // 1. زيادة السقف بمقدار ثابت (500) ثم استعادة الأصل
        // ============================================================
        [Fact]
        public async Task SetAccountFazzaLimit_ShouldUpdateLimitSuccessfully()
        {
            Output.WriteLine("\n🛡️ اختبار: زيادة سقف الحساب بمقدار 500 ثم استعادة الأصل");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;
            var newLimit = originalLimit + 500m;

            Output.WriteLine($"📊 السقف الأصلي: {originalLimit}");
            Output.WriteLine($"🎯 السقف المطلوب: {newLimit}");

            try
            {
                var response = await ExecuteWithRetryAsync(
                    Dashboard,
                    () => FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                        Dashboard.UserKey,
                        FazzaUser1.AccountIdGuid,
                        newLimit));

                Assert.False(string.IsNullOrWhiteSpace(response),
                    "API returned empty response.");

                var after = await this.WaitForAccountLimitAsync(FazzaUser1, newLimit);

                this.AssertDelta(
                    originalLimit,
                    after.MaxFazaaDebtLimit,
                    expectedDelta: 500m,
                    label: "MaxFazaaDebtLimit");
            }
            finally
            {
                // نعيد السقف الأصلي دائمًا
                await this.RestoreAccountLimitAsync(FazzaUser1, originalLimit);
            }
        }

        // ============================================================
        // 2. السقف أكبر من سقف المنطقة — يجب أن يُرفض ولا يتغيّر شيء
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_GreaterThanRegionLimit_ShouldFail()
        {
            Output.WriteLine("\n🧪 اختبار: سقف الحساب أكبر من سقف المنطقة");

            // 1. أخذ لقطة قبل المحاولة
            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var region = await this.GetRegionAsync(FazzaUser1.RegionId);

            // 2. السقف المرفوض (أكبر من سقف المنطقة)
            var invalidLimit = region.FazaaMaxLimit + 0.01m;

            Assert.True(invalidLimit > region.FazaaMaxLimit,
                "Test setup error: invalidLimit not actually invalid.");

            Output.WriteLine($"📊 Account Limit (قبل): {before.Account.MaxFazaaDebtLimit}");
            Output.WriteLine($"📊 Region Max: {region.FazaaMaxLimit}");
            Output.WriteLine($"❌ Invalid Limit: {invalidLimit}");

            // 3. محاولة تعيين السقف — نتوقع رفضًا
            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    invalidLimit));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body),
                "API rejected but returned empty body.");

            Output.WriteLine($"✅ Rejected. Status: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            // 4. التحقق الوحيد المطلوب: السقف لم يتغيّر
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
 
        [Fact]
        public async Task SetAccountFazaaLimit_WithValidValue_ShouldSucceed()
        {
            Output.WriteLine("\n🛡️ اختبار: تعيين سقف بقيمة 10,000 من المتاح في المنطقة");

            // 1. لقطة قبل التعديل
            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;

            // 2. جلب المنطقة وحساب المتاح
            var region = await this.GetRegionAsync(FazzaUser1.RegionId);
            var allowedLimit = region.FazaaMaxLimit - region.TotalAllocatedFazaaAmount;

            Output.WriteLine($"📊 السقف الحالي: {originalLimit}");
            Output.WriteLine($"📊 سقف المنطقة: {region.FazaaMaxLimit}");
            Output.WriteLine($"📊 المخصص في المنطقة: {region.TotalAllocatedFazaaAmount}");
            Output.WriteLine($"📊 القيمة المتاحة: {allowedLimit}");

            // 3. الهدف: 10,000 (أو المتاح إن كان أقل)
            const decimal requestedAmount = 10_000m;
            var targetLimit = Math.Min(requestedAmount, allowedLimit);

            Assert.True(targetLimit > 0m,
                $"No available limit in region. MaxLimit={region.FazaaMaxLimit}, " +
                $"Allocated={region.TotalAllocatedFazaaAmount}.");

            Output.WriteLine($"🎯 السقف المطلوب: {targetLimit}");

            // 4. تعيين السقف (بلا finally، بلا استعادة)
            var response = await ExecuteWithRetryAsync(
                Dashboard,
                () => FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    targetLimit));

            Assert.False(string.IsNullOrWhiteSpace(response),
                "API returned empty response.");

            // 5. انتظار ظهور القيمة الجديدة والتحقق
            var after = await this.WaitForAccountLimitAsync(FazzaUser1, targetLimit);
            Assert.Equal(targetLimit, after.MaxFazaaDebtLimit);

            Output.WriteLine($"✅ تم تعيين السقف إلى {after.MaxFazaaDebtLimit}.");
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

            // التحقق أن السقف لم يتغيّر
            var after = await this.TakeSnapshotAsync(FazzaUser1);
            this.AssertDelta(
                before.Account.MaxFazaaDebtLimit,
                after.Account.MaxFazaaDebtLimit,
                expectedDelta: 0m,
                label: "MaxFazaaDebtLimit (Should be unchanged)");
        }

        // ============================================================
        // 5. سقف بقيمة قصوى (decimal.MaxValue) — يجب أن يُرفض
        //    (بديل عملي لـ "إدخال أحرف": قيمة غير واقعية)
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

            // 1. Snapshot قبل
            var beforePayer = await this.TakeSnapshotAsync(payer);
            var beforeDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("📊 قبل التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {beforePayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {beforeDebtor.WalletBalance:F3} LYD");

            // 2. التفعيل
            await AccountReadinessHelper.ActivateAccountAsync(
               Transfer,
                payerSession: payer,
                debtorSession: debtor,
                log: Output.WriteLine);

            // 3. الانتظار حتى تعود الأرصدة (باستخدام FazzaHelper.WaitForBalanceAsync)
            await this.WaitForBalanceAsync(payer, beforePayer.WalletBalance);
            await this.WaitForBalanceAsync(debtor, beforeDebtor.WalletBalance);

            // 4. Snapshot بعد
            var afterPayer = await this.TakeSnapshotAsync(payer);
            var afterDebtor = await this.TakeSnapshotAsync(debtor);

            Output.WriteLine("\n📊 بعد التفعيل:");
            Output.WriteLine($"   {payer.UserKey}:   {afterPayer.WalletBalance:F3} LYD");
            Output.WriteLine($"   {debtor.UserKey}: {afterDebtor.WalletBalance:F3} LYD");

            // 5. التحقق
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
    }
}