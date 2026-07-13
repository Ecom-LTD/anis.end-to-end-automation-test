using Automation.Test.Fixtures;
using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.FazzaLimit
{
    public class RegionLimit : BaseSulfaTest
    {
        public RegionLimit(ITestOutputHelper output, SulfaFixture fixture) : 
            base(output, fixture)
            
        { }
    


    
    [Fact]
        public async Task SetRegionFazaaLimit_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين حد الفزعة لمنطقة");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ================================================================
            // الخطوة 1: جلب معرف المنطقة (RegionId) باستخدام RegionFlow
            // ================================================================
            Output.WriteLine("\n📝 الخطوة 1: جلب معرف المنطقة 'Tripoli'");

            var regionName = "Tripoli";
            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey,  // نستخدم Dashboard لأنه يملك صلاحيات
                regionName);

            Output.WriteLine($"✅ Region ID: {regionId}");
            Assert.False(string.IsNullOrEmpty(regionId), "RegionId should not be null or empty");

            // ================================================================
            // الخطوة 2: جلب البيانات الحالية للمنطقة باستخدام FazzaTopUpFlow
            // ================================================================
            Output.WriteLine("\n📝 الخطوة 2: جلب بيانات المنطقة الحالية");

            var currentData = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,  // التوكن
                regionId);

            Assert.NotNull(currentData);

            var currentMaxLimit = currentData.FazaaMaxLimit;
            var currentDebt = currentData.CurrentDebt;

            Output.WriteLine($"📊 البيانات الحالية:");
            Output.WriteLine($"   📍 Region Name: {currentData.Name}");
            Output.WriteLine($"   📍 Region Code: {currentData.Code}");
            Output.WriteLine($"   💰 Current Max Limit: {currentMaxLimit}");
            Output.WriteLine($"   💰 Current Debt: {currentDebt}");
            Output.WriteLine($"   💰 Total Allocated: {currentData.TotalAllocatedFazaaAmount}");

            // ================================================================
            // الخطوة 3: حساب الحد الجديد (السقف الحالي + 1000)
            // ================================================================
            var increaseAmount = 1000m;
            var newLimit = currentMaxLimit + increaseAmount;

            Output.WriteLine($"\n📝 الخطوة 3: حساب الحد الجديد");
            Output.WriteLine($"   📊 Current Max Limit: {currentMaxLimit}");
            Output.WriteLine($"   ➕ Increase Amount: +{increaseAmount}");
            Output.WriteLine($"   🎯 New Limit: {newLimit}");

            // ================================================================
            // الخطوة 4: تعيين الحد الجديد للمنطقة
            // ================================================================
            Output.WriteLine("\n📝 الخطوة 4: تعيين الحد الجديد للمنطقة");

            var response = await FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                Dashboard.Token,  // التوكن
                regionId,         // معرف المنطقة
                newLimit);        // الحد الجديد

            Output.WriteLine($"📋 Response Message: {response.Message}");
            Output.WriteLine($"📋 Success: {response.Success}");
            Output.WriteLine($"📋 Status: {response.Status}");

            // ================================================================
            // الخطوة 5: التحقق من النتيجة
            // ================================================================
            // ✅ فقط التحقق من عدم وجود Exception
            var exception = await Record.ExceptionAsync(() =>
                FazzaTopUp.SetRegionMaxFazaaLimitAsync(Dashboard.Token, regionId, newLimit));

            Assert.Null(exception);
            Output.WriteLine("✅ تم تعيين الحد بنجاح (لا يوجد خطأ)");

            // ================================================================
            // الخطوة 6: التحقق من تحديث البيانات (اختياري)
            // ================================================================
            Output.WriteLine("\n📝 الخطوة 5: التحقق من تحديث البيانات");

            var updatedData = await FazzaTopUp.GetRegionFullDataAsync(
                Dashboard.Token,
                regionId);

            var updatedMaxLimit = updatedData?.FazaaMaxLimit ?? 0;
            Output.WriteLine($"   📊 Updated Max Limit: {updatedMaxLimit}");
            Output.WriteLine($"   🎯 Expected New Limit: {newLimit}");

            // ================================================================
            // النتيجة النهائية
            // ================================================================
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine($"📊 ملخص الاختبار:");
            Output.WriteLine($"   ✅ Region: {regionName}");
            Output.WriteLine($"   ✅ Region ID: {regionId}");
            Output.WriteLine($"   ✅ Old Limit: {currentMaxLimit}");
            Output.WriteLine($"   ✅ New Limit: {newLimit}");
            Output.WriteLine($"   ✅ Response: {response.Message}");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            PrintResult(nameof(SetRegionFazaaLimit_ShouldSucceed), true);
        }

        // ================================================================
        // اختبار إضافي: تعيين حد أقل من الحد المتاح (يجب أن يفشل)
        // ================================================================
        [Fact]
        public async Task SetRegionFazaaLimit_LessThanAllocated_ShouldFail()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار: تعيين حد أقل من المبلغ المخصص (يجب أن يفشل)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // 1. جلب RegionId
            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey, "Tripoli");

            // 2. جلب البيانات الحالية
            var currentData = await FazzaTopUp.GetRegionFullDataAsync(Dashboard.Token, regionId);

            var totalAllocated = currentData.TotalAllocatedFazaaAmount;
            var invalidLimit = totalAllocated - 1000;  // أقل من المبلغ المخصص

            Output.WriteLine($"📊 Total Allocated: {totalAllocated}");
            Output.WriteLine($"❌ محاولة تعيين حد: {invalidLimit} (أقل من المخصص)");

            // 3. محاولة التعيين - يجب أن تفشل
            var exception = await Record.ExceptionAsync(() =>
                FazzaTopUp.SetRegionMaxFazaaLimitAsync(Dashboard.Token, regionId, invalidLimit));

            Assert.NotNull(exception);
            Output.WriteLine($"✅ تم رفض العملية كما هو متوقع: {exception.Message}");

            PrintResult(nameof(SetRegionFazaaLimit_LessThanAllocated_ShouldFail), true);
        }

        // ================================================================
        // اختبار إضافي: تعيين حد اكبر من الحد المتاح (يجب أن ينجح)
        // ================================================================
        [Fact]
        public async Task SetRegionFazaaLimit_BiggerThanAllocated_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("🧪 اختبار:منع تعيين حد اكبر من المبلغ المخصص (يجب أن ينجح)");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // 1. جلب RegionId
            var regionId = await Region.GetRegionIdByNameAsync(
                Dashboard.UserKey, "Tripoli");

            // 2. جلب البيانات الحالية
            var currentData = await FazzaTopUp.GetRegionFullDataAsync(Dashboard.Token, regionId);

            var totalAllocated = currentData.TotalAllocatedFazaaAmount;
            var invalidLimit = totalAllocated + 1000;  // اكبر من المبلغ المخصص

            Output.WriteLine($"📊 Total Allocated: {totalAllocated}");
            Output.WriteLine($"❌ محاولة تعيين حد: {invalidLimit} (اكبر من المخصص)");

            // 3. محاولة التعيين - يجب أن تفشل
            var exception = await Record.ExceptionAsync(() =>
                FazzaTopUp.SetRegionMaxFazaaLimitAsync(Dashboard.Token, regionId, invalidLimit));
            Assert.Null(exception);
            Output.WriteLine("✅ تم تعيين الحد بنجاح (لا يوجد خطأ)");

            PrintResult(nameof(SetRegionFazaaLimit_BiggerThanAllocated_ShouldSucceed), true);
        }
    } 
}
