using Automation.Framework.Core.Http;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Infrastructure;
using Automation.Test.Infrastructure.Reporting;
using Automation.Test.Tests.Fazza.Base;
using Automation.Test.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Fazza.FazzaLimitTests
{
    [Collection(CollectionNames.Fazza)]
    [Trait("Product", "Fazza")]
    [Trait("Category", "Slow")]
    [Trait("MutatesState", "true")]
    [Trait("Category", "Regression")]
    public class FazzaLimitTests : BaseFazzaTest
    {
        public FazzaLimitTests(ITestOutputHelper output, FazzaFixture fixture)
            : base(output, fixture) { }

        // ============================================================
        // 1. تعيين سقف أكبر من سقف المنطقة — يجب أن يُرفض ولا يتغيّر شيء
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_GreaterThanRegionLimit_ShouldFail()
        {
            Output.Scenario(
                "تعيين سقف حساب أكبر من سقف المنطقة",
                "رفض الطلب (ApiException بجسم غير فارغ) وبقاء سقف الحساب دون تغيير");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var region = await this.GetRegionAsync(FazzaUser1.RegionId);
            var invalidLimit = region.FazaaMaxLimit + 0.01m;

            Assert.True(invalidLimit > region.FazaaMaxLimit,
                "Test setup error: invalidLimit not actually invalid.");

            Output.Inputs(
                ("الحساب", FazzaUser1.UserKey),
                ("سقف المنطقة (Region Max)", region.FazaaMaxLimit),
                ("السقف المطلوب (غير صالح)", invalidLimit));
            Output.Before(before.Describe("الحساب"));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey, FazzaUser1.AccountIdGuid, invalidLimit));

            var after = await this.TakeSnapshotAsync(FazzaUser1);
            Output.After(("حالة الرفض", exception.ApiStatusCode), ("جسم الرد", exception.Body));
            Output.After(after.Describe("الحساب"));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body), "API rejected but returned empty body.");
            Output.Verify("السقف بعد الرفض", before.Account.MaxFazaaDebtLimit, after.Account.MaxFazaaDebtLimit);
        }

        // ============================================================
        // 2. تعيين سقف بقيمة محددة (10,000) بعد ضمان سعة المنطقة
        // (هذا الاختبار يغير الحالة؛ الـ Fixture يستعيدها عند الانتهاء)
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_To_10000_ShouldSucceed()
        {
            const decimal requestedLimit = 10_000m;

            Output.Scenario(
                "تعيين سقف الحساب إلى 10,000",
                $"سقف الحساب يصبح {requestedLimit:N0} بعد التنفيذ");

            var before = await this.TakeSnapshotAsync(FazzaUser1);
            var originalLimit = before.Account.MaxFazaaDebtLimit;

            Output.Inputs(
                ("الحساب", FazzaUser1.UserKey),
                ("السقف المطلوب", requestedLimit),
                ("المنطقة", FazzaUser1.RegionId));
            Output.Before(before.Describe("الحساب"));

            // السقف الحالي محسوب أصلًا ضمن المخصص، فنطلب استيعاب الزيادة فقط.
            await this.EnsureRegionCapacityAsync(
                FazzaUser1.RegionId,
                Math.Max(0m, requestedLimit - originalLimit));

            var response = await ExecuteWithRetryAsync(
                Dashboard,
                () => FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey, FazzaUser1.AccountIdGuid, requestedLimit));

            Assert.False(string.IsNullOrWhiteSpace(response), "API returned empty response.");

            await this.WaitForAccountLimitAsync(FazzaUser1, requestedLimit);

            var after = await this.TakeSnapshotAsync(FazzaUser1);
            Output.After(("رد الـ API", response));
            Output.After(after.Describe("الحساب"));

            this.AssertEqualTo(requestedLimit, after.Account.MaxFazaaDebtLimit, "MaxFazaaDebtLimit");
        }

        // ============================================================
        // 3. سقف سالب — يجب أن يُرفض
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_NegativeValue_ShouldFail()
            => await AssertInvalidLimitIsRejectedAsync(
                "تعيين سقف حساب بقيمة سالبة", -1m);

        // ============================================================
        // 4. سقف بقيمة قصوى (decimal.MaxValue) — يجب أن يُرفض
        // ============================================================
        [Fact]
        public async Task SetAccountFazaaLimit_MaxValue_ShouldFail()
            => await AssertInvalidLimitIsRejectedAsync(
                "تعيين سقف حساب بقيمة قصوى (decimal.MaxValue)", decimal.MaxValue);

        [Fact]
        public async Task ActivateAccount_ShouldNotChangeBalances()
        {
            Output.Scenario(
                "تفعيل حساب عبر تحويل ذهاب وعودة",
                "رصيدا الدافع والمدين يعودان لقيمتيهما الأصليتين");

            await AssertBalancesUnchangedAsync(
                () => AccountReadinessHelper.ActivateAccountAsync(
                    Transfer, payerSession: FazzaUser1, debtorSession: FazzaUser2, log: Output.WriteLine));
        }

        [Fact]
        public async Task ActivateAccount_ShouldNotChangeBalance2()
        {
            Output.Scenario(
                "تفعيل الحساب عبر EnsureDebtFree",
                "رصيدا الدافع والمدين يعودان لقيمتيهما الأصليتين");

            await AssertBalancesUnchangedAsync(() => this.EnsureDebtFreeAsync(FazzaUser2));
        }

        [Fact]
        public async Task GetForeignTransfers_ShouldSucceed()
        {
            Output.Scenario(
                "جلب قائمة التحويلات الأجنبية لمستخدم Business",
                "ردّ يحتوي تحويلًا واحدًا على الأقل");

            Output.Inputs(("المستخدم", FazzaUser2.UserKey));

            var response = await ForeignTransfer.GetForeignTransfersAsync(FazzaUser2.UserKey);

            Assert.NotNull(response);
            Assert.NotNull(response.Results);

            var first = response.Results.FirstOrDefault();
            Output.After(
                ("عدد التحويلات (Total)", response.Total),
                ("معرّف أول تحويل", first?.TransferId),
                ("التصنيف", first?.CategoryName),
                ("السعر", first?.Prices.FirstOrDefault()?.Price));

            Assert.True(response.Total > 0, "Expected at least one transfer");
            Output.Compare("Total > 0", true, response.Total > 0);
        }

        // ============================================================
        // مساعدات خاصة بهذا الملف (تُبقي الاختبارات قصيرة وبعرض موحّد)
        // ============================================================

        /// <summary>سقف غير صالح: يجب رفض الطلب بـ ApiException وبقاء السقف كما هو.</summary>
        private async Task AssertInvalidLimitIsRejectedAsync(string scenario, decimal invalidLimit)
        {
            Output.Scenario(scenario, "رفض الطلب (ApiException بجسم غير فارغ) وبقاء السقف دون تغيير");

            var before = await this.TakeSnapshotAsync(FazzaUser1);

            Output.Inputs(("الحساب", FazzaUser1.UserKey), ("السقف المطلوب (غير صالح)", invalidLimit));
            Output.Before(before.Describe("الحساب"));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    Dashboard.UserKey, FazzaUser1.AccountIdGuid, invalidLimit));

            var after = await this.TakeSnapshotAsync(FazzaUser1);
            Output.After(("حالة الرفض", exception.ApiStatusCode), ("جسم الرد", exception.Body));
            Output.After(after.Describe("الحساب"));

            Assert.False(string.IsNullOrWhiteSpace(exception.Body), "API rejected but returned empty body.");
            Output.Verify("السقف بعد الرفض", before.Account.MaxFazaaDebtLimit, after.Account.MaxFazaaDebtLimit);
        }

        /// <summary>ينفّذ العملية ثم يتحقق أن رصيدي FazzaUser1 وFazzaUser2 عادا لما كانا عليه.</summary>
        private async Task AssertBalancesUnchangedAsync(Func<Task> operation)
        {
            var payer = FazzaUser1;
            var debtor = FazzaUser2;

            var beforePayer = await this.TakeSnapshotAsync(payer);
            var beforeDebtor = await this.TakeSnapshotAsync(debtor);

            Output.Inputs(("الدافع", payer.UserKey), ("المدين", debtor.UserKey));
            Output.Before(beforePayer.Describe(payer.UserKey));
            Output.Before(beforeDebtor.Describe(debtor.UserKey));

            await operation();

            // WaitForBalanceAsync تعيد اللقطة التي حققت الشرط، فلا حاجة لقراءتها مرة ثانية.
            var afterPayer = await this.WaitForBalanceAsync(payer, beforePayer.WalletBalance);
            var afterDebtor = await this.WaitForBalanceAsync(debtor, beforeDebtor.WalletBalance);

            Output.After(afterPayer.Describe(payer.UserKey));
            Output.After(afterDebtor.Describe(debtor.UserKey));

            this.AssertDelta(beforePayer.WalletBalance, afterPayer.WalletBalance, 0m,
                $"رصيد {payer.UserKey}");
            this.AssertDelta(beforeDebtor.WalletBalance, afterDebtor.WalletBalance, 0m,
                $"رصيد {debtor.UserKey}");
        }
    }
}
