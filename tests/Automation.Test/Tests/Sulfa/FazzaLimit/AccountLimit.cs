using Automation.Framework.Core.Http;
using Automation.Framework.Services.AccountReadiness.Flow;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.FazzaLimit
{
    public class AccountLimit : BaseSulfaTest
    {
        private const decimal DebtTolerance = 0.0001m;

        public AccountLimit(ITestOutputHelper output, SulfaFixture fixture): base(output, fixture) { }

        // Basic session check
        [Fact]
        public void Sessions_ShouldBePrewarmed()
        {
            Assert.True(Dashboard.IsAuthenticated);
            Assert.True(string.IsNullOrEmpty(Dashboard.AccountId));

            Assert.True(SulfaOperator.IsAuthenticated);
            Assert.True(SulfaOperator.HasWallet);

            Assert.True(SulfaBusiness.IsAuthenticated);
            Assert.False(string.IsNullOrEmpty(SulfaBusiness.SubscriptionId));
            Assert.NotEqual(Guid.Empty, SulfaBusiness.AccountIdGuid);
        }

        // Account data check
        [Fact]
        public async Task GetSulfaAccounts_ForBusinessAccount_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "📊 جلب بيانات Fazza/Sulfa لحساب الأعمال");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            var account = await GetSulfaAccountAsync(SulfaBusiness);

            Assert.NotNull(account);
            Assert.False(string.IsNullOrWhiteSpace(account.Id));
            Assert.False(string.IsNullOrWhiteSpace(account.Phone));

            Output.WriteLine($"🆔 Account ID: {account.Id}");
            Output.WriteLine($"📞 Phone: {account.Phone}");
            Output.WriteLine($"👤 Owner Name: {account.OwnerName}");
            Output.WriteLine($"💰 Current Fazaa Debt: {account.CurrentFazaaDebt}");
            Output.WriteLine($"💰 Current Sulfa Debt: {account.CurrentSulfaDebt}");
            Output.WriteLine($"💰 Confirmed Debt: {account.ConfirmedDebt}");
            Output.WriteLine($"📈 Max Fazaa Limit: {account.MaxFazaaDebtLimit}");

            PrintResult(
                nameof(GetSulfaAccounts_ForBusinessAccount_ShouldSucceed),
                true);
        }

        // 1. Limit inside available region capacity
        [Fact]
        public async Task SetAccountFazaaLimit_WithinAvailableLimit_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف حساب داخل القيمة المتاحة");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            /*
             * نبدأ من حساب خالٍ من الدين.
             */
            await PrepareBusinessDebtFreeAsync();

            var before = await GetSulfaAccountAsync(SulfaBusiness);

            var originalAccountLimit = before.MaxFazaaDebtLimit;

            /*
             * نضمن وجود مساحة إضافية مقدارها 1000
             * في سقف المنطقة.
             */
            var regionPreparation = await EnsureRegionAdditionalCapacityAsync(1000m);

            decimal valueSetByThisTest = originalAccountLimit;

            try
            {
                /*
                 * نعيد القراءة بعد تجهيز المنطقة حتى لا نعتمد
                 * على بيانات قديمة.
                 */
                var account = await GetSulfaAccountAsync(SulfaBusiness);

                var region = await GetBusinessRegionAsync();

                var regionAvailable =
                    region.FazaaMaxLimit -
                    region.TotalAllocatedFazaaAmount;

                Assert.True(
                    regionAvailable >= 1000m,
                    $"Region does not have the required available capacity. " +
                    $"Available: {regionAvailable}");

                var newLimit = account.MaxFazaaDebtLimit + 500m;

                /*
                 * الزيادة المطلوبة في التخصيص هي 500 فقط.
                 */
                Assert.True(
                    newLimit - account.MaxFazaaDebtLimit <=
                    regionAvailable,
                    "Requested account limit must be inside " +
                    "the available region capacity.");

                Output.WriteLine(
                    $"📊 Current Account Limit: {account.MaxFazaaDebtLimit}");

                Output.WriteLine(
                    $"📊 Region Max Limit: {region.FazaaMaxLimit}");

                Output.WriteLine(
                    $"📊 Region Allocated: {region.TotalAllocatedFazaaAmount}");

                Output.WriteLine(
                    $"📊 Region Available: {regionAvailable}");

                Output.WriteLine(
                    $"🎯 Requested Account Limit: {newLimit}");

                var exception =
                    await Record.ExceptionAsync(() =>
                        ExecuteWithRetryAsync(
                            Dashboard,
                            () =>
                                FazzaTopUp
                                    .SetAccountFazzaDeptMaxLimitAsync(
                                        Dashboard.UserKey,
                                        SulfaBusiness.AccountIdGuid,
                                        newLimit)));

                Assert.Null(exception);

                var after =
                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        newLimit);

                valueSetByThisTest = newLimit;

                Assert.Equal(
                    newLimit,
                    after.MaxFazaaDebtLimit);

                Output.WriteLine(
                    $"✅ Account limit updated successfully: " +
                    $"{after.MaxFazaaDebtLimit}");

                PrintResult(
                    nameof(SetAccountFazaaLimit_WithinAvailableLimit_ShouldSucceed),
                    true);
            }
            finally
            {
                /*
                 * الترتيب مهم:
                 * نعيد سقف الحساب أولًا ثم سقف المنطقة.
                 */
                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    originalAccountLimit,
                    valueSetByThisTest);

                await RestoreRegionLimitSafelyAsync(
                    regionPreparation);
            }
        }

        // 2. Account limit greater than the whole region maximum
        [Fact]
        public async Task SetAccountFazaaLimit_GreaterThanRegionLimit_ShouldFail()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: سقف الحساب أكبر من سقف المنطقة");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var account = await GetSulfaAccountAsync(SulfaBusiness);

            var region = await GetBusinessRegionAsync();

            var originalLimit = account.MaxFazaaDebtLimit;

            /*
             * أكبر من سقف المنطقة نفسه ولو بجزء بسيط.
             */
            var invalidLimit = region.FazaaMaxLimit + 0.01m;

            Assert.True(invalidLimit > region.FazaaMaxLimit);

            Output.WriteLine(
                $"📊 Account Current Limit: {originalLimit}");

            Output.WriteLine(
                $"📊 Region Max Limit: {region.FazaaMaxLimit}");

            Output.WriteLine(
                $"❌ Requested Invalid Limit: {invalidLimit}");

            var exception =
                await Assert.ThrowsAsync<ApiException>(() =>
                    FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                        Dashboard.UserKey,
                        SulfaBusiness.AccountIdGuid,
                        invalidLimit));

            Assert.False(
                string.IsNullOrWhiteSpace(exception.Body),
                "The API rejected the request but returned an empty error body.");

            Output.WriteLine(
                $"✅ Request rejected. Status: " +
                $"{exception.ApiStatusCode}");

            Output.WriteLine(
                $"📋 Response: {exception.Body}");

            /*
             * لا يكفي حدوث Exception.
             * نتأكد أن السقف لم يتغير فعليًا.
             */
            var after = await GetSulfaAccountAsync(SulfaBusiness);

            Assert.Equal(originalLimit, after.MaxFazaaDebtLimit);

            PrintResult(nameof(SetAccountFazaaLimit_GreaterThanRegionLimit_ShouldFail), true);
        }

        // 3. Greater than available unreserved capacity
        //    but still not greater than RegionMaxLimit
        [Fact]
        public async Task SetAccountFazaaLimit_GreaterThanAvailableUnreservedLimit_ShouldFail()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: سقف أكبر من القيمة غير المحجوزة المتاحة");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var account = await GetSulfaAccountAsync(SulfaBusiness);

            var region = await GetBusinessRegionAsync();

            var originalLimit = account.MaxFazaaDebtLimit;

            /*
             * TotalAllocated يحتوي على سقف الحساب الحالي أيضًا.
             *
             * لذلك نحذف سقف الحساب الحالي لمعرفة المبلغ
             * المحجوز بواسطة بقية الحسابات.
             */
            var allocatedByOtherAccounts = region.TotalAllocatedFazaaAmount - account.MaxFazaaDebtLimit;

            Assert.True(
                allocatedByOtherAccounts >= 0m,
                $"Region allocation is inconsistent. " +
                $"TotalAllocated: {region.TotalAllocatedFazaaAmount}, " +
                $"AccountLimit: {account.MaxFazaaDebtLimit}");

            /*
             * لكي يكون هذا التست مختلفًا فعلًا عن
             * GreaterThanRegionLimit يجب أن توجد قيمة
             * محجوزة لحسابات أخرى.
             */
            Assert.True(
                allocatedByOtherAccounts > 0.01m,
                "This test requires the region to have Fazaa allocation " +
                "reserved by other accounts.");

            /*
             * أكبر سقف يمكن منحه لهذا الحساب دون تجاوز المنطقة:
             *
             * RegionMax -
             * (TotalAllocated - CurrentAccountLimit)
             */
            var maximumAllowedForThisAccount = region.FazaaMaxLimit - allocatedByOtherAccounts;

            var invalidLimit = maximumAllowedForThisAccount + 0.01m;

            /*
             * يجب أن نظل داخل RegionMax حتى نضمن أن سبب
             * هذا السيناريو هو السقف غير المحجوز وليس
             * تجاوز RegionMax نفسه.
             */
            Assert.True(
                invalidLimit <= region.FazaaMaxLimit,
                $"The calculated test value ({invalidLimit}) " +
                $"must not exceed the region maximum " +
                $"({region.FazaaMaxLimit}).");

            Output.WriteLine(
                $"📊 Region Max: {region.FazaaMaxLimit}");

            Output.WriteLine(
                $"📊 Total Allocated: {region.TotalAllocatedFazaaAmount}");

            Output.WriteLine(
                $"📊 Current Account Limit: {account.MaxFazaaDebtLimit}");

            Output.WriteLine(
                $"📊 Allocated By Other Accounts: {allocatedByOtherAccounts}");

            Output.WriteLine(
                $"📊 Maximum Allowed For This Account: " +
                $"{maximumAllowedForThisAccount}");

            Output.WriteLine(
                $"❌ Invalid Requested Limit: {invalidLimit}");

            var exception =
                await Assert.ThrowsAsync<ApiException>(() =>
                    FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                        Dashboard.UserKey,
                        SulfaBusiness.AccountIdGuid,
                        invalidLimit));

            Assert.False(
                string.IsNullOrWhiteSpace(exception.Body));

            Output.WriteLine(
                $"✅ Request rejected. Status: {exception.ApiStatusCode}");

            Output.WriteLine(
                $"📋 Response: {exception.Body}");

            var after = await GetSulfaAccountAsync(
                SulfaBusiness);

            Assert.Equal(
                originalLimit,
                after.MaxFazaaDebtLimit);

            PrintResult(
                nameof(SetAccountFazaaLimit_GreaterThanAvailableUnreservedLimit_ShouldFail),
                true);
        }

        // 4. Exactly all remaining available capacity
        [Fact]
        public async Task SetAccountFazaaLimit_EqualToMaximumAvailableLimit_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين الحساب إلى كامل القيمة المتبقية المسموحة");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var before = await GetSulfaAccountAsync(SulfaBusiness);

            var originalAccountLimit = before.MaxFazaaDebtLimit;

            /*
             * نضمن وجود مساحة فعلية حتى لا يتحول التست
             * إلى SameAsCurrent.
             */
            var regionPreparation = await EnsureRegionAdditionalCapacityAsync(1000m);

            decimal valueSetByThisTest = originalAccountLimit;

            try
            {
                var account = await GetSulfaAccountAsync(SulfaBusiness);

                var region = await GetBusinessRegionAsync();

                var allocatedByOtherAccounts =
                    region.TotalAllocatedFazaaAmount -
                    account.MaxFazaaDebtLimit;

                Assert.True(allocatedByOtherAccounts >= 0m);

                var maximumAllowedForThisAccount =
                    region.FazaaMaxLimit -
                    allocatedByOtherAccounts;

                Assert.True(
                    maximumAllowedForThisAccount >
                    account.MaxFazaaDebtLimit,
                    "The test requires available region capacity.");

                Output.WriteLine(
                    $"📊 Region Max: {region.FazaaMaxLimit}");

                Output.WriteLine(
                    $"📊 Total Allocated: {region.TotalAllocatedFazaaAmount}");

                Output.WriteLine(
                    $"📊 Current Account Limit: {account.MaxFazaaDebtLimit}");

                Output.WriteLine(
                    $"🎯 Maximum Allowed For Account: " +
                    $"{maximumAllowedForThisAccount}");

                var exception =
                    await Record.ExceptionAsync(() =>
                        ExecuteWithRetryAsync(
                            Dashboard,
                            () =>
                                FazzaTopUp
                                    .SetAccountFazzaDeptMaxLimitAsync(
                                        Dashboard.UserKey,
                                        SulfaBusiness.AccountIdGuid,
                                        maximumAllowedForThisAccount)));

                Assert.Null(exception);

                var after =
                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        maximumAllowedForThisAccount);

                valueSetByThisTest =
                    maximumAllowedForThisAccount;

                Assert.Equal(
                    maximumAllowedForThisAccount,
                    after.MaxFazaaDebtLimit);

                /*
                 * بعد استهلاك كل المساحة يجب أن يصبح
                 * RegionAvailable = 0 تقريبًا.
                 */
                var regionAfter = await WaitForRegionAvailableAmountAsync(0m);

                var availableAfter =
                    regionAfter.FazaaMaxLimit -
                    regionAfter.TotalAllocatedFazaaAmount;

                Assert.True(
                    Math.Abs(availableAfter) <= 0.01m,
                    $"Expected all available region capacity to be allocated. " +
                    $"Remaining: {availableAfter}");

                Output.WriteLine(
                    $"✅ Account received the full remaining allowed limit.");

                PrintResult(
                    nameof(SetAccountFazaaLimit_EqualToMaximumAvailableLimit_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    originalAccountLimit,
                    valueSetByThisTest);

                await RestoreRegionLimitSafelyAsync(
                    regionPreparation);
            }
        }

        // 5. Same as current
        [Fact]
        public async Task SetAccountFazaaLimit_SameAsCurrent_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: إعادة تعيين نفس سقف الحساب الحالي");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            /*
             * القراءة تتم بعد تجهيز الدين مباشرة.
             */
            var before = await GetSulfaAccountAsync(SulfaBusiness);

            var currentLimit = before.MaxFazaaDebtLimit;

            Assert.True(
                currentLimit >= before.ConfirmedDebt,
                $"Current limit ({currentLimit}) is lower than " +
                $"confirmed debt ({before.ConfirmedDebt}).");

            Output.WriteLine(
                $"📊 Current Limit: {currentLimit}");

            Output.WriteLine(
                $"📊 Confirmed Debt: {before.ConfirmedDebt}");

            var exception =
                await Record.ExceptionAsync(() =>
                    ExecuteWithRetryAsync(
                        Dashboard,
                        () =>
                            FazzaTopUp
                                .SetAccountFazzaDeptMaxLimitAsync(
                                    Dashboard.UserKey,
                                    SulfaBusiness.AccountIdGuid,
                                    currentLimit)));

            Assert.Null(exception);

            var after =
                await WaitForAccountLimitAsync(
                    SulfaBusiness,
                    currentLimit);

            Assert.Equal(
                currentLimit,
                after.MaxFazaaDebtLimit);

            Output.WriteLine(
                $"✅ Same account limit accepted: " +
                $"{after.MaxFazaaDebtLimit}");

            PrintResult(
                nameof(SetAccountFazaaLimit_SameAsCurrent_ShouldSucceed),
                true);
        }

        // 6. Negative value
        [Fact]
        public async Task SetAccountFazaaLimit_NegativeValue_ShouldFail()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف حساب بقيمة سالبة");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var before = await GetSulfaAccountAsync(SulfaBusiness);

            var originalLimit = before.MaxFazaaDebtLimit;

            const decimal invalidLimit = -1m;

            Output.WriteLine(
                $"📊 Current Limit: {originalLimit}");

            Output.WriteLine(
                $"❌ Invalid Limit: {invalidLimit}");

            var exception =
                await Assert.ThrowsAsync<ApiException>(() =>
                    FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                        Dashboard.UserKey,
                        SulfaBusiness.AccountIdGuid,
                        invalidLimit));

            Assert.False(
                string.IsNullOrWhiteSpace(exception.Body));

            Output.WriteLine(
                $"✅ Negative value rejected. Status: " +
                $"{exception.ApiStatusCode}");

            Output.WriteLine(
                $"📋 Response: {exception.Body}");

            var after = await GetSulfaAccountAsync(
                SulfaBusiness);

            Assert.Equal(
                originalLimit,
                after.MaxFazaaDebtLimit);

            PrintResult(
                nameof(SetAccountFazaaLimit_NegativeValue_ShouldFail),
                true);
        }

        // 7. Zero while account has NO debt
        [Fact]
        public async Task SetAccountFazaaLimit_ZeroWithNoDebt_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف الحساب إلى صفر بدون وجود دين");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var before = await GetSulfaAccountAsync(SulfaBusiness);

            Assert.True(
                before.ConfirmedDebt <= DebtTolerance,
                $"Account must be debt-free. " +
                $"ConfirmedDebt: {before.ConfirmedDebt}");

            var originalAccountLimit = before.MaxFazaaDebtLimit;

            RegionPreparation regionPreparation =
                await GetUnchangedRegionPreparationAsync();

            var valueSetByThisTest =  originalAccountLimit;

            try
            {
                /*
                 * لو كان السقف أصلًا صفرًا، نرفعه أولًا حتى
                 * يكون التست فعلًا تغييرًا من قيمة موجبة إلى صفر.
                 */
                if (originalAccountLimit <= 0m)
                {
                    regionPreparation =
                        await EnsureRegionAdditionalCapacityAsync(
                            1000m);

                    const decimal preparationLimit = 500m;

                    var preparationException =
                        await Record.ExceptionAsync(() =>
                            ExecuteWithRetryAsync(
                                Dashboard,
                                () =>
                                    FazzaTopUp
                                        .SetAccountFazzaDeptMaxLimitAsync(
                                            Dashboard.UserKey,
                                            SulfaBusiness.AccountIdGuid,
                                            preparationLimit)));

                    Assert.Null(preparationException);

                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        preparationLimit);

                    valueSetByThisTest =
                        preparationLimit;
                }

                var exception =
                    await Record.ExceptionAsync(() =>
                        ExecuteWithRetryAsync(
                            Dashboard,
                            () =>
                                FazzaTopUp
                                    .SetAccountFazzaDeptMaxLimitAsync(
                                        Dashboard.UserKey,
                                        SulfaBusiness.AccountIdGuid,
                                        0m)));

                Assert.Null(exception);

                var after =
                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        0m);

                valueSetByThisTest = 0m;

                Assert.Equal(
                    0m,
                    after.MaxFazaaDebtLimit);

                Output.WriteLine(
                    "✅ Zero limit accepted for debt-free account.");

                PrintResult(
                    nameof(SetAccountFazaaLimit_ZeroWithNoDebt_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    originalAccountLimit,
                    valueSetByThisTest);

                await RestoreRegionLimitSafelyAsync(
                    regionPreparation);
            }
        }

        // 8. Zero while existing debt is present
        [Fact]
        public async Task SetAccountFazaaLimit_ZeroWithExistingDebt_ShouldFail()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف صفر مع وجود دين قائم");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            var debtPreparation = await PrepareExistingBusinessDebtAsync(100m);

            try
            {
                var before = await GetSulfaAccountAsync(SulfaBusiness);

                Assert.True(
                    before.ConfirmedDebt > DebtTolerance,
                    "This test requires existing confirmed debt.");

                var currentLimit = before.MaxFazaaDebtLimit;

                Output.WriteLine(
                    $"📊 Current Limit: {currentLimit}");

                Output.WriteLine(
                    $"📊 Confirmed Debt: {before.ConfirmedDebt}");

                Output.WriteLine(
                    "❌ Requested Limit: 0");

                var exception =
                    await Assert.ThrowsAsync<ApiException>(() =>
                        FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                            Dashboard.UserKey,
                            SulfaBusiness.AccountIdGuid,
                            0m));

                Assert.False(string.IsNullOrWhiteSpace(exception.Body));

                /*
                 * هذا النص موجود أصلًا في التست القديم بالمشروع،
                 * لذلك نحتفظ بالتحقق منه.
                 */
                Assert.Contains(
                    "greater than current debt",
                    exception.Body,
                    StringComparison.OrdinalIgnoreCase);

                Output.WriteLine(
                    $"✅ Zero rejected because debt exists.");

                Output.WriteLine(
                    $"📋 Status: {exception.ApiStatusCode}");

                Output.WriteLine(
                    $"📋 Response: {exception.Body}");

                var after = await GetSulfaAccountAsync(
                    SulfaBusiness);

                Assert.Equal(
                    currentLimit,
                    after.MaxFazaaDebtLimit);

                PrintResult(
                    nameof(SetAccountFazaaLimit_ZeroWithExistingDebt_ShouldFail),
                    true);
            }
            finally
            {
                /*
                 * أولًا نسدد الدين الذي أنشأه التست.
                 */
                await PrepareBusinessDebtFreeAsync();

                /*
                 * ثم نعيد سقوف الحسابات التي قد اضطررنا
                 * لرفعها لتجهيز الدين.
                 */
                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    debtPreparation.OriginalBusinessLimit,
                    debtPreparation.PreparedBusinessLimit);

                await RestoreAccountLimitSafelyAsync(
                    SulfaOperator,
                    debtPreparation.OriginalOperatorLimit,
                    debtPreparation.PreparedOperatorLimit);

                /*
                 * وأخيرًا نعيد سقف المنطقة إذا كنا قد رفعناه.
                 */
                await RestoreRegionLimitSafelyAsync(
                    debtPreparation.RegionPreparation);
            }
        }

        // 9. Limit below consumed / confirmed debt
        [Fact]
        public async Task SetAccountFazaaLimit_BelowConsumedAmount_ShouldFail()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف أقل من المبلغ المستهلك");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            var debtPreparation = await PrepareExistingBusinessDebtAsync(100m);

            try
            {
                var before = await GetSulfaAccountAsync(
                    SulfaBusiness);

                var consumed =
                    before.ConfirmedDebt;

                Assert.True(
                    consumed > DebtTolerance,
                    "This test requires consumed/confirmed debt greater than zero.");

                var originalCurrentLimit =
                    before.MaxFazaaDebtLimit;

                /*
                 * أقل من المستهلك ولو بجزء بسيط.
                 */
                var invalidLimit =
                    consumed - 0.01m;

                Assert.True(
                    invalidLimit >= 0m,
                    "The prepared debt must be greater than 0.01.");

                Assert.True(
                    invalidLimit < consumed);

                Output.WriteLine(
                    $"📊 Current Limit: {originalCurrentLimit}");

                Output.WriteLine(
                    $"📊 Consumed / Confirmed Debt: {consumed}");

                Output.WriteLine(
                    $"❌ Requested Invalid Limit: {invalidLimit}");

                var exception =
                    await Assert.ThrowsAsync<ApiException>(() =>
                        FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                            Dashboard.UserKey,
                            SulfaBusiness.AccountIdGuid,
                            invalidLimit));

                Assert.False(
                    string.IsNullOrWhiteSpace(exception.Body));

                Assert.Contains(
                    "greater than current debt",
                    exception.Body,
                    StringComparison.OrdinalIgnoreCase);

                Output.WriteLine(
                    $"✅ Limit below consumed amount rejected.");

                Output.WriteLine(
                    $"📋 Status: {exception.ApiStatusCode}");

                Output.WriteLine(
                    $"📋 Response: {exception.Body}");

                var after = await GetSulfaAccountAsync(
                    SulfaBusiness);

                Assert.Equal(
                    originalCurrentLimit,
                    after.MaxFazaaDebtLimit);

                PrintResult(
                    nameof(SetAccountFazaaLimit_BelowConsumedAmount_ShouldFail),
                    true);
            }
            finally
            {
                await PrepareBusinessDebtFreeAsync();

                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    debtPreparation.OriginalBusinessLimit,
                    debtPreparation.PreparedBusinessLimit);

                await RestoreAccountLimitSafelyAsync(
                    SulfaOperator,
                    debtPreparation.OriginalOperatorLimit,
                    debtPreparation.PreparedOperatorLimit);

                await RestoreRegionLimitSafelyAsync(
                    debtPreparation.RegionPreparation);
            }
        }

        // 10. Decimal value
        [Fact]
        public async Task SetAccountFazaaLimit_DecimalValue_ShouldSucceed()
        {
            Output.WriteLine(
                "\n═══════════════════════════════════════════════════════════");
            Output.WriteLine(
                "🧪 اختبار: تعيين سقف حساب بقيمة عشرية");
            Output.WriteLine(
                "═══════════════════════════════════════════════════════════");

            await PrepareBusinessDebtFreeAsync();

            var before = await GetSulfaAccountAsync(SulfaBusiness);

            var originalAccountLimit = before.MaxFazaaDebtLimit;

            /*
             * نضمن أن الزيادة العشرية تقع داخل المتاح.
             */
            var regionPreparation = await EnsureRegionAdditionalCapacityAsync(1000.55m);

            decimal valueSetByThisTest = originalAccountLimit;

            try
            {
                var account = await GetSulfaAccountAsync(
                    SulfaBusiness);

                var region = await GetBusinessRegionAsync();

                var regionAvailable =
                    region.FazaaMaxLimit -
                    region.TotalAllocatedFazaaAmount;

                const decimal additionalAmount =
                    500.55m;

                Assert.True(
                    regionAvailable >= additionalAmount,
                    $"Not enough region capacity for decimal test. " +
                    $"Available: {regionAvailable}");

                var newLimit =
                    account.MaxFazaaDebtLimit +
                    additionalAmount;

                Output.WriteLine(
                    $"📊 Current Limit: {account.MaxFazaaDebtLimit}");

                Output.WriteLine(
                    $"📊 Region Available: {regionAvailable}");

                Output.WriteLine(
                    $"🎯 Decimal Limit: {newLimit}");

                var exception =
                    await Record.ExceptionAsync(() =>
                        ExecuteWithRetryAsync(
                            Dashboard,
                            () =>
                                FazzaTopUp
                                    .SetAccountFazzaDeptMaxLimitAsync(
                                        Dashboard.UserKey,
                                        SulfaBusiness.AccountIdGuid,
                                        newLimit)));

                Assert.Null(exception);

                var after =
                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        newLimit);

                valueSetByThisTest = newLimit;

                Assert.Equal(newLimit, after.MaxFazaaDebtLimit);

                /*
                 * تأكيد أن الجزء العشري لم يحذف.
                 */
                Assert.NotEqual(
                    decimal.Truncate(newLimit),
                    after.MaxFazaaDebtLimit);

                Output.WriteLine(
                    $"✅ Decimal account limit stored correctly: " +
                    $"{after.MaxFazaaDebtLimit}");

                PrintResult(
                    nameof(SetAccountFazaaLimit_DecimalValue_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    originalAccountLimit,
                    valueSetByThisTest);

                await RestoreRegionLimitSafelyAsync(regionPreparation);
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>
        /// تجهيز حساب الأعمال ليصبح خاليًا من الدين.
        ///
        /// يستخدم AccountReadiness الموجود في المشروع:
        /// Dashboard -> قراءة التقرير
        /// SulfaOperator -> الحساب الدافع
        /// SulfaBusiness -> الحساب المدين
        /// </summary>
        private async Task PrepareBusinessDebtFreeAsync()
        {
            var readiness =
                Flow<AccountReadinessFlow>();

            var result =
                await readiness.PrepareAsync(
                    Dashboard,
                    SulfaOperator,
                    SulfaBusiness,
                    new AccountReadinessOptions
                    {
                        SettleDueDebt = true,
                        RequireZeroTotalDue = true,
                        ValidatePayerWallet = true,
                        ValidateDebtorWallet = true
                    },
                    message => Output.WriteLine(message));

            Assert.True(
                result.TotalDueAfterSettlement <= DebtTolerance,
                $"AccountReadiness completed but TotalDue is still " +
                $"{result.TotalDueAfterSettlement}.");

            /*
             * AccountLimit يعتمد على ConfirmedDebt،
             * لذلك لا نكتفي بـTotalDue.
             */
            var account =
                await WaitForConfirmedDebtAtMostAsync(
                    SulfaBusiness,
                    DebtTolerance);

            Assert.True(
                account.ConfirmedDebt <= DebtTolerance,
                $"Business account is not debt-free. " +
                $"ConfirmedDebt: {account.ConfirmedDebt}");
        }

        /// <summary>
        /// يجلب حساب Sulfa الصحيح للجلسة.
        ///
        /// المطابقة أولًا بـAccountId ثم بالهاتف.
        /// </summary>
        private async Task<SulfaAccount> GetSulfaAccountAsync(
            TestSession session)
        {
            var accounts =
                await ExecuteWithRetryAsync(
                    Dashboard,
                    () =>
                        FazzaTopUp.GetSulfaAccountsAsync(
                            Dashboard.UserKey,
                            session.PhoneNumber));

            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);

            SulfaAccount? account = null;

            if (session.AccountIdGuid != Guid.Empty)
            {
                account =
                    accounts.FirstOrDefault(item =>
                        Guid.TryParse(
                            item.Id,
                            out var itemAccountId) &&
                        itemAccountId ==
                        session.AccountIdGuid);
            }

            account ??=
                accounts.FirstOrDefault(item =>
                    NormalizePhone(item.Phone) ==
                    NormalizePhone(session.PhoneNumber));

            if (account == null)
            {
                throw new InvalidOperationException(
                    $"Sulfa account was not found for " +
                    $"'{session.PhoneNumber}' / " +
                    $"AccountId '{session.AccountId}'.");
            }

            return account;
        }

        /// <summary>
        /// جلب Region الخاصة بحساب الأعمال.
        /// </summary>
        private async Task<RegionSulfaFullData> GetBusinessRegionAsync()
        {
            Assert.False(
                string.IsNullOrWhiteSpace(SulfaBusiness.RegionId),
                "SulfaBusiness.RegionId is missing.");

            var region =
                await ExecuteWithRetryAsync(
                    Dashboard,
                    () =>
                        FazzaTopUp.GetRegionFullDataAsync(
                            Dashboard.Token,
                            SulfaBusiness.RegionId));

            Assert.NotNull(region);

            return region;
        }

        /// <summary>
        /// انتظار ظهور سقف الحساب الجديد في GET.
        /// </summary>
        private async Task<SulfaAccount> WaitForAccountLimitAsync(
            TestSession session,
            decimal expectedLimit,
            int attempts = 10)
        {
            SulfaAccount? lastAccount = null;

            for (var attempt = 1;
                 attempt <= attempts;
                 attempt++)
            {
                lastAccount =
                    await GetSulfaAccountAsync(session);

                if (lastAccount.MaxFazaaDebtLimit ==
                    expectedLimit)
                {
                    return lastAccount;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(500));
                }
            }

            throw new InvalidOperationException(
                $"Account Fazaa limit was not updated. " +
                $"Expected: {expectedLimit}, " +
                $"Actual: {lastAccount?.MaxFazaaDebtLimit}");
        }

        /// <summary>
        /// انتظار أن يصبح ConfirmedDebt أقل من أو يساوي القيمة المطلوبة.
        /// </summary>
        private async Task<SulfaAccount>
            WaitForConfirmedDebtAtMostAsync(
                TestSession session,
                decimal maximumDebt,
                int attempts = 12)
        {
            SulfaAccount? lastAccount = null;

            for (var attempt = 1;
                 attempt <= attempts;
                 attempt++)
            {
                lastAccount =
                    await GetSulfaAccountAsync(session);

                if (lastAccount.ConfirmedDebt <=
                    maximumDebt)
                {
                    return lastAccount;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(750));
                }
            }

            throw new InvalidOperationException(
                $"ConfirmedDebt did not reach the required value. " +
                $"Required <= {maximumDebt}, " +
                $"Actual: {lastAccount?.ConfirmedDebt}");
        }

        /// <summary>
        /// انتظار ظهور دين فعلي على الحساب.
        /// </summary>
        private async Task<SulfaAccount>
            WaitForConfirmedDebtGreaterThanZeroAsync(
                int attempts = 12)
        {
            SulfaAccount? lastAccount = null;

            for (var attempt = 1;
                 attempt <= attempts;
                 attempt++)
            {
                lastAccount =
                    await GetSulfaAccountAsync(
                        SulfaBusiness);

                if (lastAccount.ConfirmedDebt >
                    DebtTolerance)
                {
                    return lastAccount;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(750));
                }
            }

            throw new InvalidOperationException(
                $"The debt creation operation completed, " +
                $"but ConfirmedDebt did not become greater than zero. " +
                $"Current ConfirmedDebt: " +
                $"{lastAccount?.ConfirmedDebt}");
        }

        /// <summary>
        /// يضمن فقط وجود مساحة إضافية في المنطقة.
        ///
        /// لا يغير سقف الحساب؛ لأن سقف الحساب هو الشيء
        /// الذي تقوم التستات نفسها باختباره.
        /// </summary>
        private async Task<RegionPreparation>
            EnsureRegionAdditionalCapacityAsync(
                decimal requiredAdditionalCapacity)
        {
            if (requiredAdditionalCapacity < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredAdditionalCapacity));
            }

            var region =
                await GetBusinessRegionAsync();

            var originalLimit =
                region.FazaaMaxLimit;

            var currentAvailable =
                region.FazaaMaxLimit -
                region.TotalAllocatedFazaaAmount;

            Output.WriteLine(
                $"📊 Region capacity before preparation: " +
                $"{currentAvailable}");

            /*
             * لا نغير المنطقة إذا كانت المساحة موجودة أصلًا.
             */
            if (currentAvailable >=
                requiredAdditionalCapacity)
            {
                return new RegionPreparation(
                    OriginalLimit: originalLimit,
                    ValueSetByThisTest: originalLimit,
                    WasChanged: false);
            }

            var shortage =
                requiredAdditionalCapacity -
                currentAvailable;

            /*
             * نضيف 100 كهامش صغير حتى لا نقف بالضبط
             * على boundary أثناء تجهيز التست.
             */
            var newRegionLimit =
                region.FazaaMaxLimit +
                shortage +
                100m;

            var exception =
                await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        SulfaBusiness.RegionId,
                        newRegionLimit));

            Assert.Null(exception);

            await WaitForRegionLimitAsync(
                newRegionLimit);

            Output.WriteLine(
                $"✅ Region temporarily prepared. " +
                $"New Region Limit: {newRegionLimit}");

            return new RegionPreparation(
                OriginalLimit: originalLimit,
                ValueSetByThisTest: newRegionLimit,
                WasChanged: true);
        }

        /// <summary>
        /// سياق Region لم تتغير.
        /// يستخدم عندما نحتاج متغير cleanup موحدًا.
        /// </summary>
        private async Task<RegionPreparation>
            GetUnchangedRegionPreparationAsync()
        {
            var region =
                await GetBusinessRegionAsync();

            return new RegionPreparation(
                OriginalLimit: region.FazaaMaxLimit,
                ValueSetByThisTest: region.FazaaMaxLimit,
                WasChanged: false);
        }

        /// <summary>
        /// انتظار تغير سقف المنطقة.
        /// </summary>
        private async Task<RegionSulfaFullData>
            WaitForRegionLimitAsync(
                decimal expectedLimit,
                int attempts = 10)
        {
            RegionSulfaFullData? lastRegion = null;

            for (var attempt = 1;
                 attempt <= attempts;
                 attempt++)
            {
                lastRegion =
                    await GetBusinessRegionAsync();

                if (lastRegion.FazaaMaxLimit ==
                    expectedLimit)
                {
                    return lastRegion;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(500));
                }
            }

            throw new InvalidOperationException(
                $"Region limit did not reach expected value. " +
                $"Expected: {expectedLimit}, " +
                $"Actual: {lastRegion?.FazaaMaxLimit}");
        }

        /// <summary>
        /// انتظار القيمة المتاحة في Region.
        /// </summary>
        private async Task<RegionSulfaFullData>
            WaitForRegionAvailableAmountAsync(
                decimal expectedAvailable,
                int attempts = 10)
        {
            RegionSulfaFullData? lastRegion = null;

            for (var attempt = 1;
                 attempt <= attempts;
                 attempt++)
            {
                lastRegion =
                    await GetBusinessRegionAsync();

                var currentAvailable =
                    lastRegion.FazaaMaxLimit -
                    lastRegion.TotalAllocatedFazaaAmount;

                if (Math.Abs(
                        currentAvailable -
                        expectedAvailable) <= 0.01m)
                {
                    return lastRegion;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(500));
                }
            }

            var lastAvailable =
                lastRegion == null
                    ? (decimal?)null
                    : lastRegion.FazaaMaxLimit -
                      lastRegion.TotalAllocatedFazaaAmount;

            throw new InvalidOperationException(
                $"Region available amount did not reach " +
                $"the expected value. " +
                $"Expected: {expectedAvailable}, " +
                $"Actual: {lastAvailable}");
        }

        /// <summary>
        /// إعادة سقف حساب بأمان.
        ///
        /// لا نكتب فوق أي تغيير قام به Test آخر
        /// أو مستخدم Dashboard أثناء تنفيذ التست.
        /// </summary>
        private async Task RestoreAccountLimitSafelyAsync(
            TestSession session,
            decimal originalLimit,
            decimal valueSetByThisTest)
        {
            var current =
                await GetSulfaAccountAsync(session);

            /*
             * تغيرت القيمة بواسطة جهة أخرى.
             */
            if (current.MaxFazaaDebtLimit !=
                valueSetByThisTest)
            {
                Output.WriteLine(
                    $"⚠️ Account limit will not be restored because " +
                    $"it changed outside this test.");

                Output.WriteLine(
                    $"   Test value: {valueSetByThisTest}");

                Output.WriteLine(
                    $"   Current value: {current.MaxFazaaDebtLimit}");

                return;
            }

            /*
             * لا نعيد سقفًا أصبح أقل من الدين الحالي.
             */
            if (originalLimit <
                current.ConfirmedDebt)
            {
                Output.WriteLine(
                    $"⚠️ Original account limit cannot be restored " +
                    $"because it is now lower than ConfirmedDebt.");

                Output.WriteLine(
                    $"   Original Limit: {originalLimit}");

                Output.WriteLine(
                    $"   ConfirmedDebt: {current.ConfirmedDebt}");

                return;
            }

            if (current.MaxFazaaDebtLimit ==
                originalLimit)
            {
                return;
            }

            var exception =
                await Record.ExceptionAsync(() =>
                    ExecuteWithRetryAsync(
                        Dashboard,
                        () =>
                            FazzaTopUp
                                .SetAccountFazzaDeptMaxLimitAsync(
                                    Dashboard.UserKey,
                                    session.AccountIdGuid,
                                    originalLimit)));

            if (exception != null)
            {
                throw new InvalidOperationException(
                    $"Failed to restore Fazaa limit for account " +
                    $"'{session.PhoneNumber}'.",
                    exception);
            }

            await WaitForAccountLimitAsync(
                session,
                originalLimit);

            Output.WriteLine(
                $"✅ Account limit restored safely: " +
                $"{originalLimit}");
        }

        /// <summary>
        /// إعادة سقف المنطقة بأمان.
        ///
        /// يجب استدعاؤها بعد RestoreAccountLimitSafelyAsync.
        /// </summary>
        private async Task RestoreRegionLimitSafelyAsync(
            RegionPreparation preparation)
        {
            if (!preparation.WasChanged)
            {
                return;
            }

            var current =
                await GetBusinessRegionAsync();

            /*
             * لو تغير سقف المنطقة بعد تجهيزنا له،
             * لا نكتب فوق تغيير جهة أخرى.
             */
            if (current.FazaaMaxLimit !=
                preparation.ValueSetByThisTest)
            {
                Output.WriteLine(
                    "⚠️ Region limit will not be restored because " +
                    "it changed outside this test.");

                Output.WriteLine(
                    $"   Test value: " +
                    $"{preparation.ValueSetByThisTest}");

                Output.WriteLine(
                    $"   Current value: " +
                    $"{current.FazaaMaxLimit}");

                return;
            }

            /*
             * لا نعيد سقف المنطقة إذا أصبح أقل
             * من إجمالي المخصص الحالي.
             */
            if (preparation.OriginalLimit <
                current.TotalAllocatedFazaaAmount)
            {
                Output.WriteLine(
                    "⚠️ Original region limit cannot be restored " +
                    "because it is now lower than TotalAllocated.");

                Output.WriteLine(
                    $"   Original Region Limit: " +
                    $"{preparation.OriginalLimit}");

                Output.WriteLine(
                    $"   Total Allocated: " +
                    $"{current.TotalAllocatedFazaaAmount}");

                return;
            }

            var exception =
                await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        SulfaBusiness.RegionId,
                        preparation.OriginalLimit));

            if (exception != null)
            {
                throw new InvalidOperationException(
                    "Failed to restore original region limit.",
                    exception);
            }

            await WaitForRegionLimitAsync(
                preparation.OriginalLimit);

            Output.WriteLine(
                $"✅ Region limit restored safely: " +
                $"{preparation.OriginalLimit}");
        }

        /// <summary>
        /// تجهيز دين حقيقي لاستخدامه في:
        ///
        /// ZeroWithExistingDebt
        /// BelowConsumedAmount
        ///
        /// يبدأ بحساب Business خالٍ من الدين،
        /// ثم يضمن أن Business وOperator لديهما سقف كافٍ،
        /// وبعدها ينشئ Fazaa بقيمة صغيرة.
        /// </summary>
        private async Task<DebtPreparation>
            PrepareExistingBusinessDebtAsync(
                decimal requestedDebt)
        {
            if (requestedDebt <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedDebt));
            }

            /*
             * نقطة البداية يجب أن تكون معروفة:
             * Business بدون دين.
             */
            await PrepareBusinessDebtFreeAsync();

            var business =
                await GetSulfaAccountAsync(
                    SulfaBusiness);

            var operatorAccount =
                await GetSulfaAccountAsync(
                    SulfaOperator);

            /*
             * الجلسات الحالية بالمشروع معدة على Tripoli.
             * نتحقق من ذلك بدل الافتراض الصامت.
             */
            Assert.Equal(
                SulfaBusiness.RegionId,
                SulfaOperator.RegionId);

            var originalBusinessLimit =
                business.MaxFazaaDebtLimit;

            var originalOperatorLimit =
                operatorAccount.MaxFazaaDebtLimit;

            /*
             * نترك 100 كمساحة إضافية فوق الدين الذي سننشئه.
             */
            var requiredAvailable =
                requestedDebt + 100m;

            var requiredBusinessLimit =
                business.ConfirmedDebt +
                requiredAvailable;

            var requiredOperatorLimit =
                operatorAccount.ConfirmedDebt +
                requiredAvailable;

            var businessAdditionalRequired =
                Math.Max(
                    0m,
                    requiredBusinessLimit -
                    business.MaxFazaaDebtLimit);

            var operatorAdditionalRequired =
                Math.Max(
                    0m,
                    requiredOperatorLimit -
                    operatorAccount.MaxFazaaDebtLimit);

            /*
             * نضمن مساحة المنطقة لكل الزيادات المطلوبة.
             */
            var regionPreparation =
                await EnsureRegionAdditionalCapacityAsync(
                    businessAdditionalRequired +
                    operatorAdditionalRequired);

            var preparedBusinessLimit =
                originalBusinessLimit;

            var preparedOperatorLimit =
                originalOperatorLimit;

            try
            {
                if (businessAdditionalRequired > 0m)
                {
                    var exception =
                        await Record.ExceptionAsync(() =>
                            ExecuteWithRetryAsync(
                                Dashboard,
                                () =>
                                    FazzaTopUp
                                        .SetAccountFazzaDeptMaxLimitAsync(
                                            Dashboard.UserKey,
                                            SulfaBusiness.AccountIdGuid,
                                            requiredBusinessLimit)));

                    Assert.Null(exception);

                    await WaitForAccountLimitAsync(
                        SulfaBusiness,
                        requiredBusinessLimit);

                    preparedBusinessLimit =
                        requiredBusinessLimit;
                }

                if (operatorAdditionalRequired > 0m)
                {
                    var exception =
                        await Record.ExceptionAsync(() =>
                            ExecuteWithRetryAsync(
                                Dashboard,
                                () =>
                                    FazzaTopUp
                                        .SetAccountFazzaDeptMaxLimitAsync(
                                            Dashboard.UserKey,
                                            SulfaOperator.AccountIdGuid,
                                            requiredOperatorLimit)));

                    Assert.Null(exception);

                    await WaitForAccountLimitAsync(
                        SulfaOperator,
                        requiredOperatorLimit);

                    preparedOperatorLimit =
                        requiredOperatorLimit;
                }

                Output.WriteLine(
                    $"💳 Creating Fazaa debt: {requestedDebt}");

                var requestResult =
                    await ExecuteWithRetryAsync(
                        SulfaOperator,
                        () =>
                            FazzaTopUp.RequestSulfaAsync(
                                SulfaOperator.UserKey,
                                requestedDebt,
                                SulfaBusiness.SubscriptionId,
                                SulfaOperator.WalletId));

                Assert.False(
                    string.IsNullOrWhiteSpace(requestResult),
                    "Fazaa request returned an empty response.");

                var accountWithDebt =
                    await WaitForConfirmedDebtGreaterThanZeroAsync();

                Output.WriteLine(
                    $"✅ ConfirmedDebt created: " +
                    $"{accountWithDebt.ConfirmedDebt}");

                return new DebtPreparation(
                    OriginalBusinessLimit:
                        originalBusinessLimit,

                    PreparedBusinessLimit:
                        preparedBusinessLimit,

                    OriginalOperatorLimit:
                        originalOperatorLimit,

                    PreparedOperatorLimit:
                        preparedOperatorLimit,

                    RegionPreparation:
                        regionPreparation,

                    ConfirmedDebt:
                        accountWithDebt.ConfirmedDebt);
            }
            catch
            {
                /*
                 * إذا فشل التجهيز في منتصف الطريق،
                 * لا نترك البيئة معدلة.
                 */
                try
                {
                    await PrepareBusinessDebtFreeAsync();
                }
                catch
                {
                    // نواصل محاولة إعادة السقوف.
                }

                await RestoreAccountLimitSafelyAsync(
                    SulfaBusiness,
                    originalBusinessLimit,
                    preparedBusinessLimit);

                await RestoreAccountLimitSafelyAsync(
                    SulfaOperator,
                    originalOperatorLimit,
                    preparedOperatorLimit);

                await RestoreRegionLimitSafelyAsync(
                    regionPreparation);

                throw;
            }
        }

        private static string NormalizePhone(
            string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            return new string(
                phone
                    .Where(char.IsDigit)
                    .ToArray());
        }

        private sealed record RegionPreparation(
            decimal OriginalLimit,
            decimal ValueSetByThisTest,
            bool WasChanged);

        private sealed record DebtPreparation(
            decimal OriginalBusinessLimit,
            decimal PreparedBusinessLimit,
            decimal OriginalOperatorLimit,
            decimal PreparedOperatorLimit,
            RegionPreparation RegionPreparation,
            decimal ConfirmedDebt);
    }
}