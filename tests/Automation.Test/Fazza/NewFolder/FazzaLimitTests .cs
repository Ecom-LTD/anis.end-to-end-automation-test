using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Test.Tests.Fazza.Base;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Fazza._3_Limits
{
    public class FazzaLimitTests : BaseFazzaTest
    {
        public FazzaLimitTests(ITestOutputHelper output, FazzaFixture fixture)
            : base(output, fixture) { }

        // ================================================================
        // 1. اختبار: تعيين سقف فزعة جديد والتأكد من تطبيقه
        // ================================================================
        [Fact]
        public async Task SetAccountFazzaLimit_ShouldUpdateLimitSuccessfully()
        {
            Output.WriteLine("\n🛡️ اختبار: تعيين سقف فزعة جديد للحساب والتأكد من تحديثه");

            // 1. تجهيز البيئة (تصفير الديون لضمان قبول السقف الجديد)
            await EnsureDebtFreeAsync(FazzaUser2);

            // 2. جلب السقف القديم
            var initialAccount = await GetSulfaAccountAsync(FazzaUser2);
            var oldLimit = initialAccount.MaxFazaaDebtLimit;
            Output.WriteLine($"📊 السقف القديم: {oldLimit}");

            // 3. تحديد السقف الجديد (نضيف 500)
            var newLimit = oldLimit + 500m;
            Output.WriteLine($"🎯 السقف الجديد المطلوب: {newLimit}");

            // 4. تنفيذ الطلب (عبر صلاحيات الـ Dashboard)
            var response = await ExecuteWithRetryAsync(
                Dashboard,
                () => FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser1.AccountIdGuid,
                    newLimit));

            Assert.False(string.IsNullOrWhiteSpace(response), "API returned empty response.");
            Output.WriteLine($"📋 استجابة الـ API: {response}");

            // 5. التحقق (Polling حتى يتحدث السقف في النظام)
            Output.WriteLine("⏳ في انتظار تحديث النظام...");
            var updatedAccount = await WaitForAccountLimitAsync(FazzaUser1, newLimit);

            // 6. التأكيد النهائي
            Assert.Equal(newLimit, updatedAccount.MaxFazaaDebtLimit);
            Output.WriteLine($"✅ تم تحديث السقف بنجاح إلى: {updatedAccount.MaxFazaaDebtLimit}");

            PrintResult(nameof(SetAccountFazzaLimit_ShouldUpdateLimitSuccessfully), true);
        }

        // ================================================================
        // 2. اختبار سلبي: محاولة تعيين سقف بقيمة سالبة (يجب أن يفشل)
        // ================================================================
        [Fact]
        public async Task SetAccountFazzaLimit_WithNegativeValue_ShouldFail()
        {
            Output.WriteLine("\n❌ اختبار: محاولة تعيين سقف بقيمة سالبة");

            // نستخدم Assert.ThrowsAsync للتأكد من أن الـ API سيرفض الطلب
            var exception = await Assert.ThrowsAsync<Automation.Framework.Core.Http.ApiException>(() =>
                FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                    Dashboard.UserKey,
                    FazzaUser2.AccountIdGuid,
                    -1m)); // قيمة سالبة

            Output.WriteLine($"📋 Status Code: {exception.ApiStatusCode}");
            Output.WriteLine($"📋 Body: {exception.Body}");

            // التأكد من أن كود الخطأ هو 400 Bad Request
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.ApiStatusCode);

            PrintResult(nameof(SetAccountFazzaLimit_WithNegativeValue_ShouldFail), true);
        }
    }
}