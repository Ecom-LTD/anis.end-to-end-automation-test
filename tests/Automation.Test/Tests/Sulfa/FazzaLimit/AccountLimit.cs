using Automation.Test.Fixtures.Fazza;
using Automation.Test.Tests.Sulfa.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Sulfa.AccountLimit
{
    public class AccountLimit : BaseSulfaTest
    {

        public AccountLimit(ITestOutputHelper output, SulfaFixture fixture) : base(output, fixture) { }




        [Fact]
        public void Sessions_ShouldBePrewarmed()
        {
            Assert.True(Dashboard.IsAuthenticated);
            Assert.True(string.IsNullOrEmpty(Dashboard.AccountId));
            Assert.True(SulfaOperator.HasWallet);
            Assert.False(string.IsNullOrEmpty(SulfaBusiness.SubscriptionId));

        }



        [Fact]
        public async Task GetSulfaAccounts_ForBusinessAccount_ShouldSucceed()
        {
            Output.WriteLine("\n═══════════════════════════════════════════════════════════");
            Output.WriteLine("📊 جلب بيانات السلفة (Sulfa) لحساب الأعمال");
            Output.WriteLine("═══════════════════════════════════════════════════════════");

            // ✅ إضافة التحقق من وجود Dashboard
            if (Dashboard == null)
            {
                Output.WriteLine("❌ Dashboard is NULL! Please check SulfaFixture initialization.");
                throw new Exception("Dashboard session is not initialized");
            }

            if (string.IsNullOrEmpty(Dashboard.UserKey))
            {
                Output.WriteLine("❌ Dashboard.UserKey is NULL or empty!");
                throw new Exception("Dashboard.UserKey is not set");
            }

            if (SulfaBusiness == null)
            {
                Output.WriteLine("❌ SulfaBusiness is NULL!");
                throw new Exception("SulfaBusiness session is not initialized");
            }

            if (string.IsNullOrEmpty(SulfaBusiness.PhoneNumber))
            {
                Output.WriteLine("❌ SulfaBusiness.PhoneNumber is NULL or empty!");
                throw new Exception("SulfaBusiness.PhoneNumber is not set");
            }
            // ✅ 1. جلب بيانات السلفة باستخدام Dashboard (Admin) ورقم هاتف حساب الأعمال
            var sulfaAccounts = await FazzaTopUp.GetSulfaAccountsAsync(
                Dashboard.UserKey,           // المستخدم Admin (Dashboard)
                SulfaBusiness.PhoneNumber);  // رقم هاتف حساب الأعمال

            // ✅ 2. التحقق من وجود بيانات
            Assert.NotNull(sulfaAccounts);
            Assert.True(sulfaAccounts.Count > 0, "لا توجد بيانات سلفة لهذا الحساب");

            // ✅ 3. عرض بيانات الحساب الأول
            var account = sulfaAccounts.First();

            Output.WriteLine("\n📋 بيانات حساب السلفة:");
            Output.WriteLine($"   🆔 Account ID: {account.Id}");
            Output.WriteLine($"   📞 Phone: {account.Phone}");
            Output.WriteLine($"   👤 Owner Name: {account.OwnerName}");
            Output.WriteLine($"   💰 Current Fazaa Debt: {account.CurrentFazaaDebt}");
            Output.WriteLine($"   💰 Current Sulfa Debt: {account.CurrentSulfaDebt}");
            Output.WriteLine($"   💰 Confirmed Debt: {account.ConfirmedDebt}");
            Output.WriteLine($"   📈 Max Fazaa Limit: {account.MaxFazaaDebtLimit}");
            Output.WriteLine($"   🔄 Extra Requests: {account.ExtraSulfaRequestCount}");
            Output.WriteLine($"   ⏰ Grace Period Hours: {account.ExtraSulfaGracePeriodHours}");

            // ✅ 4. التحقق من صحة البيانات
            Assert.False(string.IsNullOrEmpty(account.Id), "Account ID should not be empty");
            Assert.False(string.IsNullOrEmpty(account.Phone), "Phone should not be empty");

            PrintResult(nameof(GetSulfaAccounts_ForBusinessAccount_ShouldSucceed), true);
        }





        [Fact]
        public async Task SetFazzaLimit_WithZeroLimit_ShouldFail_WhenConfirmedDebtExists()
        {
            Output.WriteLine("\n❌ اختبار: تعيين حد الفزعة بقيمة 0 (يجب أن يفشل إذا كان confirmedDebt > 0)");

            // 1. جلب بيانات الحساب
            var sulfaAccounts = await FazzaTopUp.GetSulfaAccountsAsync(
                Dashboard.UserKey,
                SulfaBusiness.PhoneNumber);

            var businessAccount = sulfaAccounts.FirstOrDefault();

            if (businessAccount == null)
                throw new Exception("Business account not found");

            // ✅ استخدام ConfirmedDebt
            var confirmedDebt = businessAccount.ConfirmedDebt;
            Output.WriteLine($"  📊 Confirmed Debt: {confirmedDebt}");

            if (confirmedDebt > 0)
            {
                Output.WriteLine($"  📝 محاولة تعيين الحد إلى 0 رغم وجود confirmedDebt = {confirmedDebt}");

                var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                        Dashboard.UserKey,
                        SulfaBusiness.AccountIdGuid,
                        0));

                var errorMessage = exception.Message;
                Output.WriteLine($"  📋 رسالة الخطأ: {errorMessage}");

                Assert.Contains("greater than current debt", errorMessage);
                PrintResult(nameof(SetFazzaLimit_WithZeroLimit_ShouldFail_WhenConfirmedDebtExists), true);
            }
            else
            {
                Output.WriteLine($"  ℹ️ confirmedDebt = 0، يمكن تعيين الحد إلى 0");

                var result = await ExecuteWithRetryAsync(Dashboard, async () =>
                {
                    return await FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                        Dashboard.UserKey,
                        SulfaBusiness.AccountIdGuid,
                        0);
                });

                Assert.Equal("Operation successfully completed", result);
                PrintResult(nameof(SetFazzaLimit_WithZeroLimit_ShouldFail_WhenConfirmedDebtExists), true);
            }
        }

        [Fact]
        public async Task SetFazzaLimit_ForSulfaBusinessAccount_ReturnsSuccess()
        {
            Output.WriteLine("\n✅ اختبار: تعيين حد الفزعة لحساب الأعمال");

            // 1. جلب بيانات حساب الأعمال الحالية
            var sulfaAccounts = await FazzaTopUp.GetSulfaAccountsAsync(
                Dashboard.UserKey,
                SulfaBusiness.PhoneNumber);

            var businessAccount = sulfaAccounts.FirstOrDefault();

            if (businessAccount == null)
                throw new Exception("Business account not found");

            // ✅ استخدام ConfirmedDebt بدلاً من CurrentFazaaDebt
            var confirmedDebt = businessAccount.ConfirmedDebt;
            var currentMaxLimit = businessAccount.MaxFazaaDebtLimit;

            Output.WriteLine($"  📊 Confirmed Debt: {confirmedDebt}");
            Output.WriteLine($"  📊 Current Max Limit: {currentMaxLimit}");

            // ✅ 2. حساب القيمة المتاحة = الحد الأقصى - الدين المؤكد
            var availableAmount = currentMaxLimit - confirmedDebt;
            Output.WriteLine($"  📊 Available Amount (Max - Confirmed): {availableAmount}");

            // ✅ 3. القيمة المطلوبة = نصف القيمة المتاحة
            decimal newLimit;
            if (availableAmount <= 0)
            {
                // إذا كانت القيمة المتاحة صفر أو أقل، نستخدم قيمة افتراضية
                newLimit = confirmedDebt + 10000;
                Output.WriteLine($"  ⚠️ Available amount is {availableAmount}, using default: {newLimit}");
            }
            else
            {
                newLimit = confirmedDebt + (availableAmount / 2);
                Output.WriteLine($"  📝 تعيين الحد إلى: {newLimit} = ConfirmedDebt + (AvailableAmount / 2)");
                Output.WriteLine($"     (ConfirmedDebt: {confirmedDebt} + {availableAmount / 2})");
            }

            // 4. تنفيذ الطلب
            var result = await ExecuteWithRetryAsync(Dashboard, async () =>
            {
                return await FazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                    Dashboard.UserKey,
                    SulfaBusiness.AccountIdGuid,
                    newLimit);
            });

            Assert.Equal("Operation successfully completed", result);
            PrintResult(nameof(SetFazzaLimit_ForSulfaBusinessAccount_ReturnsSuccess), true);
        }
    }
}