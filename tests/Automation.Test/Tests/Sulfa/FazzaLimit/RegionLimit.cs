using Automation.Framework.Core.Http;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.FazzaLimit
{
    public class RegionLimit : BaseSulfaTest
    {
        public RegionLimit(ITestOutputHelper output, SulfaFixture fixture): base(output, fixture) {}

        [Fact]
        public async Task SetRegionFazaaLimit_HigherThanCurrent_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف أعلى من السقف الحالي");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            var newLimit = originalLimit + 1000m;

            Output.WriteLine($"📊 Original Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");
            Output.WriteLine($"🎯 New Limit: {newLimit}");

            try
            {
                var exception = await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        newLimit));

                Assert.Null(exception);

                var after = await WaitForRegionLimitAsync(
                    regionId,
                    newLimit);

                Assert.Equal(
                    newLimit,
                    after.FazaaMaxLimit);

                Output.WriteLine(
                    $"✅ Updated Limit: {after.FazaaMaxLimit}");

                PrintResult(
                    nameof(SetRegionFazaaLimit_HigherThanCurrent_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreRegionLimitSafelyAsync(
                    regionId,
                    originalLimit,
                    newLimit);
            }
        }

        [Fact]
        public async Task SetRegionFazaaLimit_LowerThanCurrentButAboveAllocated_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف أقل من الحالي وأعلى من المستهلك");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var initialData = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(initialData);

            var originalLimit = initialData.FazaaMaxLimit;
            var allocated = initialData.TotalAllocatedFazaaAmount;

            var workingCurrentLimit = originalLimit;

            /*
             * نتابع آخر قيمة نجح هذا التست نفسه في وضعها.
             * هذا مهم جدًا للـ cleanup.
             */
            var valueSetByThisTest = originalLimit;

            try
            {
                /*
                 * إذا لم توجد مساحة كافية بين المستهلك
                 * والسقف الحالي، نرفع السقف مؤقتًا.
                 */
                if (workingCurrentLimit <= allocated + 1m)
                {
                    workingCurrentLimit = allocated + 2000m;

                    var prepareException =
                        await Record.ExceptionAsync(() =>
                            FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                                Dashboard.Token,
                                regionId,
                                workingCurrentLimit));

                    Assert.Null(prepareException);

                    await WaitForRegionLimitAsync(
                        regionId,
                        workingCurrentLimit);

                    valueSetByThisTest = workingCurrentLimit;
                }

                /*
                 * اختيار قيمة داخل المجال:
                 *
                 * allocated < newLimit < currentLimit
                 */
                var newLimit =
                    allocated +
                    ((workingCurrentLimit - allocated) / 2m);

                Assert.True(
                    newLimit > allocated,
                    "New limit must be greater than allocated amount.");

                Assert.True(
                    newLimit < workingCurrentLimit,
                    "New limit must be lower than current limit.");

                Output.WriteLine($"📊 Original Limit: {originalLimit}");
                Output.WriteLine($"📊 Working Current Limit: {workingCurrentLimit}");
                Output.WriteLine($"📊 Total Allocated: {allocated}");
                Output.WriteLine($"🎯 New Limit: {newLimit}");

                var exception = await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        newLimit));

                Assert.Null(exception);

                var after = await WaitForRegionLimitAsync(
                    regionId,
                    newLimit);

                valueSetByThisTest = newLimit;

                Assert.Equal(
                    newLimit,
                    after.FazaaMaxLimit);

                Assert.True(
                    after.FazaaMaxLimit >
                    after.TotalAllocatedFazaaAmount,
                    "Updated region limit must remain above allocated amount.");

                Output.WriteLine(
                    $"✅ Limit successfully decreased from " +
                    $"{workingCurrentLimit} to {after.FazaaMaxLimit}");

                PrintResult(
                    nameof(SetRegionFazaaLimit_LowerThanCurrentButAboveAllocated_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreRegionLimitSafelyAsync(
                    regionId,
                    originalLimit,
                    valueSetByThisTest);
            }
        }

        [Fact]
        public async Task SetRegionFazaaLimit_BelowAllocated_ShouldFail()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف أقل من المستهلك");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            Assert.True(
                allocated > 0m,
                "This test requires a region with allocated Fazaa amount greater than zero.");

            /*
             * أقل من المستهلك ولو بقليل.
             */
            var invalidLimit = allocated - 0.01m;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");
            Output.WriteLine($"❌ Invalid Limit: {invalidLimit}");

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                    Dashboard.Token,
                    regionId,
                    invalidLimit));

            Assert.Equal(
                HttpStatusCode.Conflict,
                exception.ApiStatusCode);

            Assert.Contains(
                "should not be less than allocated amount",
                exception.Body,
                StringComparison.OrdinalIgnoreCase);

            Output.WriteLine(
                $"✅ Request rejected correctly: {exception.Message}");

            var after = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(after);

            Assert.Equal(
                originalLimit,
                after.FazaaMaxLimit);

            Output.WriteLine(
                $"✅ Region limit remained unchanged: {after.FazaaMaxLimit}");

            PrintResult(
                nameof(SetRegionFazaaLimit_BelowAllocated_ShouldFail),
                true);
        }

        [Fact]
        public async Task SetRegionFazaaLimit_EqualToAllocated_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين السقف مساويًا تمامًا للمستهلك");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            Assert.True(
                allocated > 0m,
                "This test requires allocated amount greater than zero.");

            Assert.True(
                originalLimit >= allocated,
                $"Current limit ({originalLimit}) must not be lower than allocated amount ({allocated}).");

            var newLimit = allocated;

            Output.WriteLine($"📊 Original Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");
            Output.WriteLine($"🎯 New Limit: {newLimit}");

            try
            {
                var exception = await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        newLimit));

                Assert.Null(exception);

                var after = await WaitForRegionLimitAsync(
                    regionId,
                    newLimit);

                Assert.Equal(
                    allocated,
                    after.FazaaMaxLimit);

                Output.WriteLine(
                    $"✅ Limit equal to allocated amount was accepted: " +
                    $"{after.FazaaMaxLimit}");

                PrintResult(
                    nameof(SetRegionFazaaLimit_EqualToAllocated_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreRegionLimitSafelyAsync(
                    regionId,
                    originalLimit,
                    newLimit);
            }
        }

        [Fact]
        public async Task SetRegionFazaaLimit_SameAsCurrent_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: إعادة تعيين نفس السقف الحالي");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var currentLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            Output.WriteLine($"📊 Current Limit: {currentLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");

            Assert.True(
                currentLimit >= allocated,
                $"Current region limit ({currentLimit}) is lower than " +
                $"allocated amount ({allocated}).");

            var exception = await Record.ExceptionAsync(() =>
                FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                    Dashboard.Token,
                    regionId,
                    currentLimit));

            Assert.Null(exception);

            var after = await WaitForRegionLimitAsync(
                regionId,
                currentLimit);

            Assert.Equal(
                currentLimit,
                after.FazaaMaxLimit);

            Output.WriteLine(
                $"✅ Same limit accepted successfully: {after.FazaaMaxLimit}");

            PrintResult(
                nameof(SetRegionFazaaLimit_SameAsCurrent_ShouldSucceed),
                true);
        }

        [Fact]
        public async Task SetRegionFazaaLimit_Zero_ShouldRespectAllocatedAmount()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف المنطقة إلى صفر");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");

            /*
             * إذا كان هناك مبلغ مستهلك:
             * 0 < allocated
             * وبالتالي يجب أن يرفضه النظام.
             */
            if (allocated > 0m)
            {
                var exception =
                    await Assert.ThrowsAsync<ApiException>(() =>
                        FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                            Dashboard.Token,
                            regionId,
                            0m));

                Assert.Equal(
                    HttpStatusCode.Conflict,
                    exception.ApiStatusCode);

                Assert.Contains(
                    "should not be less than allocated amount",
                    exception.Body,
                    StringComparison.OrdinalIgnoreCase);

                var after = await FazzaTopUp.GetRegionFullDataAsync(
                    Dashboard.Token,
                    regionId);

                Assert.NotNull(after);

                Assert.Equal(
                    originalLimit,
                    after.FazaaMaxLimit);

                Output.WriteLine(
                    "✅ Zero was rejected because allocated amount is greater than zero.");

                Output.WriteLine(
                    $"✅ Region limit remained unchanged: {after.FazaaMaxLimit}");
            }
            else
            {
                /*
                 * إذا كان المستهلك = 0،
                 * نختبر أن السقف 0 مسموح وفق قاعدة:
                 *
                 * limit >= allocated
                 */
                try
                {
                    var exception = await Record.ExceptionAsync(() =>
                        FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                            Dashboard.Token,
                            regionId,
                            0m));

                    Assert.Null(exception);

                    var after = await WaitForRegionLimitAsync(
                        regionId,
                        0m);

                    Assert.Equal(
                        0m,
                        after.FazaaMaxLimit);

                    Output.WriteLine(
                        "✅ Zero limit accepted because allocated amount is zero.");
                }
                finally
                {
                    await RestoreRegionLimitSafelyAsync(
                        regionId,
                        originalLimit,
                        0m);
                }
            }

            PrintResult(
                nameof(SetRegionFazaaLimit_Zero_ShouldRespectAllocatedAmount),
                true);
        }

        [Fact]
        public async Task SetRegionFazaaLimit_DecimalValue_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف بقيمة عشرية");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            /*
             * نستخدم قيمة أعلى من الحالي وفيها جزء عشري
             * حتى لا يتداخل التست مع شرط allocated.
             */
            var newLimit = originalLimit + 1000.55m;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");
            Output.WriteLine($"🎯 Decimal Limit: {newLimit}");

            try
            {
                var exception = await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        newLimit));

                Assert.Null(exception);

                var after = await WaitForRegionLimitAsync(
                    regionId,
                    newLimit);

                Assert.Equal(
                    newLimit,
                    after.FazaaMaxLimit);

                /*
                 * التأكد أن الجزء العشري لم تتم إزالته.
                 */
                Assert.NotEqual(
                    decimal.Truncate(newLimit),
                    after.FazaaMaxLimit);

                Output.WriteLine(
                    $"✅ Decimal value stored correctly: {after.FazaaMaxLimit}");

                PrintResult(
                    nameof(SetRegionFazaaLimit_DecimalValue_ShouldSucceed),
                    true);
            }
            finally
            {
                await RestoreRegionLimitSafelyAsync(
                    regionId,
                    originalLimit,
                    newLimit);
            }
        }

        [Fact]
        public async Task SetRegionFazaaLimit_NegativeValue_ShouldFail()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين سقف بقيمة سالبة");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;
            var allocated = before.TotalAllocatedFazaaAmount;

            const decimal invalidLimit = -1m;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"📊 Total Allocated: {allocated}");
            Output.WriteLine($"❌ Invalid Negative Limit: {invalidLimit}");

            var exception =
                await Assert.ThrowsAsync<ApiException>(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        invalidLimit));

            /*
             * بما أن القيمة السالبة أقل من allocated،
             * فالـ backend الحالي يرفضها بقاعدة:
             * new limit < allocated.
             */
            Assert.Equal(
                HttpStatusCode.BadRequest,
                exception.ApiStatusCode);

            Assert.Contains(
                "Fazaa limit must be equal to or greater than zero",
                exception.Body,
                StringComparison.OrdinalIgnoreCase);

            Output.WriteLine(
                $"✅ Negative value rejected: {exception.Message}");

            var after = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(after);

            Assert.Equal(
                originalLimit,
                after.FazaaMaxLimit);

            Output.WriteLine(
                $"✅ Region limit remains unchanged: {after.FazaaMaxLimit}");

            PrintResult(
                nameof(SetRegionFazaaLimit_NegativeValue_ShouldFail),
                true);
        }

        [Fact]
        public async Task SetRegionFazaaLimit_NonDashboardUser_ShouldFail()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: منع مستخدم غير Dashboard من تعديل سقف المنطقة");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            Assert.True(
                Dashboard.IsAuthenticated,
                "Dashboard session must be authenticated.");

            Assert.True(
                SulfaBusiness.IsAuthenticated,
                "SulfaBusiness session must be authenticated.");

            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,
                "Tripoli");

            Assert.False(
                string.IsNullOrWhiteSpace(regionId),
                "RegionId should not be null or empty.");

            var before = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(before);

            var originalLimit = before.FazaaMaxLimit;

            /*
             * نستخدم قيمة صحيحة من ناحية Business Rule.
             * وبالتالي إذا رفضت، يجب أن يكون السبب Authorization.
             */
            var newLimit = originalLimit + 1000m;

            Output.WriteLine($"📊 Current Limit: {originalLimit}");
            Output.WriteLine($"🎯 Requested Limit: {newLimit}");
            Output.WriteLine($"👤 Non-Dashboard User: {SulfaBusiness.UserKey}");

            var exception =
                await Assert.ThrowsAsync<ApiException>(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        SulfaBusiness.Token,
                        regionId,
                        newLimit));

            /*
             * الحساب مصادق عليه لكنه غير Dashboard.
             * النظام الصحيح يجب أن يرفضه بصلاحيات:
             * 401 أو 403.
             *
             * نسمح بالاثنين حاليًا لأننا لم نثبت بعد
             * أيهما يعيده Backend فعليًا.
             */
            Assert.True(
                exception.ApiStatusCode == HttpStatusCode.Unauthorized ||
                exception.ApiStatusCode == HttpStatusCode.Forbidden,
                $"Expected Unauthorized or Forbidden, but received " +
                $"{exception.ApiStatusCode}. " +
                $"Response: {exception.Body}");

            Output.WriteLine(
                $"✅ Non-Dashboard request rejected with: " +
                $"{exception.ApiStatusCode}");

            var after = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            Assert.NotNull(after);

            Assert.Equal(
                originalLimit,
                after.FazaaMaxLimit);

            Output.WriteLine(
                $"✅ Region limit remains unchanged: {after.FazaaMaxLimit}");

            PrintResult(
                nameof(SetRegionFazaaLimit_NonDashboardUser_ShouldFail),
                true);
        }


        // ============================================================
        // Helpers
        // ============================================================

        private async Task<
            Framework.Services.FazzaTopup.Models.RegionSulfaFullData>
            WaitForRegionLimitAsync(
                string regionId,
                decimal expectedLimit,
                int attempts = 10)
        {
            Framework.Services.FazzaTopup.Models.RegionSulfaFullData?
                lastData = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                lastData =
                    await FazzaTopUp.GetRegionFullDataAsync(
                        Dashboard.Token,
                        regionId);

                if (lastData != null &&
                    lastData.FazaaMaxLimit == expectedLimit)
                {
                    return lastData;
                }

                if (attempt < attempts)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(500));
                }
            }

            throw new InvalidOperationException(
                $"Region limit was not updated to the expected value. " +
                $"Expected: {expectedLimit}, " +
                $"Actual: {lastData?.FazaaMaxLimit}");
        }

        private async Task RestoreRegionLimitSafelyAsync(
            string regionId,
            decimal originalLimit,
            decimal valueSetByThisTest)
        {
            Output.WriteLine(
                "\n🔄 محاولة إعادة سقف المنطقة بأمان...");

            var currentData =
                await FazzaTopUp.GetRegionFullDataAsync(
                    Dashboard.Token,
                    regionId);

            if (currentData == null)
            {
                Output.WriteLine(
                    "⚠️ تعذر قراءة المنطقة أثناء Cleanup. " +
                    "لن يتم تعديل السقف.");

                return;
            }

            /*
             * إذا كانت القيمة الحالية ليست هي القيمة
             * التي وضعها هذا التست، فهذا يعني أن جهة أخرى
             * قامت بالتعديل.
             *
             * لا نكتب فوق التغيير الخارجي.
             */
            if (currentData.FazaaMaxLimit != valueSetByThisTest)
            {
                Output.WriteLine(
                    "⚠️ لن تتم الاستعادة لأن السقف تغير خارج هذا التست.");

                Output.WriteLine(
                    $"   Value Set By Test: {valueSetByThisTest}");

                Output.WriteLine(
                    $"   Current Value: {currentData.FazaaMaxLimit}");

                return;
            }

            /*
             * نأخذ المستهلك الحالي الآن.
             * قد يكون تغير أثناء تنفيذ التست.
             */
            var latestAllocated =
                currentData.TotalAllocatedFazaaAmount;

            /*
             * إذا أصبح originalLimit أقل من المستهلك،
             * فمن غير الآمن إرجاعه.
             */
            if (originalLimit < latestAllocated)
            {
                Output.WriteLine(
                    "⚠️ لن تتم إعادة السقف القديم لأنه أصبح " +
                    "أقل من المستهلك الحالي.");

                Output.WriteLine(
                    $"   Original Limit: {originalLimit}");

                Output.WriteLine(
                    $"   Current Allocated: {latestAllocated}");

                return;
            }

            /*
             * السقف بالفعل يساوي القيمة الأصلية.
             */
            if (currentData.FazaaMaxLimit == originalLimit)
            {
                Output.WriteLine(
                    "ℹ️ السقف موجود بالفعل على القيمة الأصلية.");

                return;
            }

            var restoreException =
                await Record.ExceptionAsync(() =>
                    FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                        Dashboard.Token,
                        regionId,
                        originalLimit));

            if (restoreException != null)
            {
                throw new InvalidOperationException(
                    $"Failed to restore original region limit. " +
                    $"Error: {restoreException.Message}",
                    restoreException);
            }

            var restored =
                await WaitForRegionLimitAsync(
                    regionId,
                    originalLimit);

            Assert.Equal(
                originalLimit,
                restored.FazaaMaxLimit);

            Output.WriteLine(
                $"✅ Region limit restored safely to: {originalLimit}");
        }
    }
}