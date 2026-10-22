using Automation.Framework.Core.Session; // لتعريف SessionCache و ResilientSession
using Automation.Framework.Core.UserPool; // لتعريف UserPoolRegistry
using Automation.Framework.Services.AccountReadiness.Flow;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;
using Automation.Test.Fazza.Base;
using Automation.Framework.Services.Transfer.Flow; // تأكد من إضافة هذا
using Automation.Test.TestSupport; // تأكد من إضافة هذا
using Automation.Test.Fixtures.Fazza;
using Xunit;
using Xunit.Abstractions;

namespace Automation.Test.Tests.Fazza.Base
{
    [Collection("Fazza Collection")]
    public abstract class BaseFazzaTest
    {
        protected readonly ITestOutputHelper Output;
        protected readonly FazzaFixture Fixture;

        protected BaseFazzaTest(ITestOutputHelper output, FazzaFixture fixture)
        {
            Output = output;
            Fixture = fixture;

            // نمرر الـ Output للـ Fixture لكي نرى رسائل التهيئة
            Fixture.Output = output;
        }

        // ================================================================
        // الجلسات
        // ================================================================
        protected TestSession Dashboard => Fixture.Dashboard;
        protected TestSession FazzaUser1 => Fixture.FazzaUser1;
        protected TestSession FazzaUser2 => Fixture.FazzaUser2;

        // ================================================================
        // الـ Flows
        // ================================================================
        protected FazzaTopUpFlow FazzaTopUp => Fixture.Flow<FazzaTopUpFlow>();
        protected WalletFlow Wallet => Fixture.Flow<WalletFlow>();
        protected AccountReadinessFlow AccountReadiness => Fixture.Flow<AccountReadinessFlow>();

        // ================================================================
        // Helpers الأساسية
        // ================================================================

        // دوال مساعدة للـ Retry
        protected Task<T> ExecuteWithRetryAsync<T>(TestSession session, Func<Task<T>> action)
            => Fixture.Resilience.ExecuteAsync(session, action);

        /// <summary>
        /// جلب حساب السلفة/الفزعة الخاص بالمستخدم (يطابق بـ AccountId أولاً ثم Phone).
        /// </summary>
        protected async Task<SulfaAccount> GetSulfaAccountAsync(TestSession session)
        {
            var accounts = await ExecuteWithRetryAsync(
                Dashboard,
                () => FazzaTopUp.GetSulfaAccountsAsync(Dashboard.UserKey, session.PhoneNumber));

            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);

            // 1) المطابقة بـ AccountId (الأدق)
            var matched = session.AccountIdGuid != Guid.Empty
                ? accounts.FirstOrDefault(a =>
                    Guid.TryParse(a.Id, out var id) && id == session.AccountIdGuid)
                : null;

            // 2) احتياطي: المطابقة بـ PhoneNumber
            matched ??= accounts.FirstOrDefault(a =>
                NormalizePhone(a.Phone) == NormalizePhone(session.PhoneNumber));

            if (matched is null)
            {
                throw new InvalidOperationException(
                    $"Sulfa account not found for '{session.PhoneNumber}' (AccountId: '{session.AccountId}').");
            }

            return matched;
        }

        /// <summary>
        /// ينتظر حتى يتم تحديث سقف الحساب في النظام ويصبح مساوياً للقيمة المطلوبة.
        /// </summary>
        protected async Task<SulfaAccount> WaitForAccountLimitAsync(
            TestSession session,
            decimal expectedLimit)
        {
            // هنا نستخدم الـ PollingHelper
            return await PollingHelper.UntilAsync(
                () => GetSulfaAccountAsync(session),
                account => account.MaxFazaaDebtLimit == expectedLimit,
                $"MaxFazaaDebtLimit for {session.UserKey} to become {expectedLimit}");
        }

        /// <summary>
        /// تصفير ديون الحساب (استخدام AccountReadinessFlow).
        /// </summary>
        protected async Task EnsureDebtFreeAsync(TestSession debtorSession)
        {
            var result = await AccountReadiness.PrepareAsync(
                Dashboard,
                FazzaUser1, // الدافع
                debtorSession,
                new AccountReadinessOptions
                {
                    SettleDueDebt = true,
                    RequireZeroTotalDue = true,
                    ValidatePayerWallet = true,
                    ValidateDebtorWallet = true
                },
                log: msg => Output.WriteLine(msg),
                // ✅ تمرير دالة التفعيل
                onAccountNotFound: async () =>
                {
                    await AccountReadinessHelper.ActivateAccountAsync(
                        Fixture.Flow<TransferFlow>(),
                        FazzaUser1,
                        debtorSession,
                        log: msg => Output.WriteLine(msg));
                });

            Assert.True(
                result.TotalDueAfterSettlement <= 0.0001m,
                $"Account '{debtorSession.PhoneNumber}' is not debt-free.");
        }

        // ================================================================
        // Helper: طباعة النتيجة
        // ================================================================
        protected void PrintResult(string testName, bool success, string? message = null)
        {
            var separator = new string('=', 60);
            Output.WriteLine($"\n{separator}");
            Output.WriteLine($"🧪 {testName}: {(success ? "✅ PASSED" : "❌ FAILED")}");
            if (!string.IsNullOrWhiteSpace(message))
                Output.WriteLine($"📝 {message}");
            Output.WriteLine(separator);
        }

        // ================================================================
        // Helper: توحيد الأرقام
        // ================================================================
        protected static string NormalizePhone(string? phone)
            => string.IsNullOrWhiteSpace(phone)
                ? string.Empty
                : new string(phone.Where(char.IsDigit).ToArray());
    }
}