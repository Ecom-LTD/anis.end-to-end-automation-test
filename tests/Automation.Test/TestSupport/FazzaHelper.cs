using Automation.Framework.Helpers;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;
using Automation.Framework.Testing;
using Automation.Test.Tests.Fazza.Base;
using Xunit;

namespace Automation.Test.TestSupport
{
    public static class FazzaHelper
    {
        // 1. جلب حساب السلفة
        public static async Task<SulfaAccount> GetSulfaAccountAsync(
            this BaseFazzaTest test, TestSession session)
        {
            var accounts = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.GetSulfaAccountsAsync(
                    test.Dashboard.UserKey, session.PhoneNumber));

            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);

            var matched = session.AccountIdGuid != Guid.Empty
                ? accounts.FirstOrDefault(a =>
                    AccountMatcher.IdsMatch(a.Id, session.AccountId))
                : null;

            matched ??= accounts.FirstOrDefault(a =>
                PhoneNormalizer.AreEqual(a.Phone, session.PhoneNumber));

            if (matched is null)
            {
                throw new InvalidOperationException(
                    $"Sulfa account not found for '{session.PhoneNumber}' " +
                    $"(AccountId: '{session.AccountId}').");
            }

            return matched;
        }

        // 2. تصفير الديون
        public static async Task EnsureDebtFreeAsync(
            this BaseFazzaTest test, TestSession debtorSession)
        {
            var payer = debtorSession.UserKey == test.FazzaUser1.UserKey
                ? test.FazzaUser2
                : test.FazzaUser1;

            test.Output.WriteLine(
                $"💳 EnsureDebtFree: payer = {payer.UserKey}, debtor = {debtorSession.UserKey}");

            var result = await test.AccountReadiness.PrepareAsync(
                test.Dashboard,
                payer,
                debtorSession,
                new AccountReadinessOptions
                {
                    SettleDueDebt = true,
                    RequireZeroTotalDue = true,
                    ValidatePayerWallet = true,
                    ValidateDebtorWallet = true
                },
                log: msg => test.Output.WriteLine(msg),
                onAccountNotFound: async () =>
                {
                    await AccountReadinessHelper.ActivateAccountAsync(
                        test.Transfer,
                        payer,
                        debtorSession,
                        log: msg => test.Output.WriteLine(msg));
                });

            Assert.True(
                result.TotalDueAfterSettlement <= 0.0001m,
                $"Account '{debtorSession.PhoneNumber}' is not debt-free.");
        }

        // 3. الانتظار حتى يتم تحديث السقف
        public static async Task<SulfaAccount> WaitForAccountLimitAsync(
            this BaseFazzaTest test,
            TestSession session,
            decimal expectedLimit)
        {
            return await PollingHelper.UntilAsync(
                () => test.GetSulfaAccountAsync(session),
                account => account.MaxFazaaDebtLimit == expectedLimit,
                $"MaxFazaaDebtLimit for {session.UserKey} to become {expectedLimit}");
        }

        // 4. أخذ اللقطة (Snapshot)
        public static async Task<FazzaSnapshot> TakeSnapshotAsync(
            this BaseFazzaTest test, TestSession session)
        {
            var balance = await test.Wallet.GetBalanceAsync(
                session.UserKey, session.WalletIdGuid);
            var account = await test.GetSulfaAccountAsync(session);

            return new FazzaSnapshot
            {
                WalletBalance = balance,
                Account = account
            };
        }

        // 5. التحقق من الفرق
        public static void AssertDelta(
            this BaseFazzaTest test,
            decimal before,
            decimal after,
            decimal expectedDelta,
            string label)
        {
            var actualDelta = after - before;
            var isEqual = Math.Abs(actualDelta - expectedDelta) < 0.0001m;

            if (!isEqual)
            {
                throw new Xunit.Sdk.XunitException(
                    $"❌ {label}: Expected Delta={expectedDelta}, " +
                    $"Actual Delta={actualDelta} (Before={before}, After={after})");
            }

            test.Output.WriteLine($"✅ {label}: Delta = {actualDelta} (Correct)");
        }

        // 6. جلب المنطقة (Region)
        public static async Task<RegionSulfaFullData> GetRegionAsync(
            this BaseFazzaTest test, string regionId)
        {
            var region = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.GetRegionFullDataAsync(
                    test.Dashboard.UserKey, regionId));   // ← userKey

            Assert.NotNull(region);
            return region;
        }

        // 7. توحيد الأرقام — استُبدلت بـ PhoneNormalizer في الإطار
        // (احذف الدالة القديمة `NormalizePhone` إن كانت موجودة)

        // 8. استعادة السقف الأصلي بأمان
        public static async Task RestoreAccountLimitAsync(
            this BaseFazzaTest test,
            TestSession session,
            decimal originalLimit)
        {
            try
            {
                var current = await test.GetSulfaAccountAsync(session);
                if (current.MaxFazaaDebtLimit == originalLimit)
                    return;

                if (originalLimit < current.ConfirmedDebt)
                {
                    test.Output.WriteLine(
                        $"⚠️ Cannot restore limit ({originalLimit}) below current debt " +
                        $"({current.ConfirmedDebt}). Skipping.");
                    return;
                }

                await test.FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    test.Dashboard.UserKey,
                    session.AccountIdGuid,
                    originalLimit);

                await test.WaitForAccountLimitAsync(session, originalLimit);
                test.Output.WriteLine($"✅ Restored limit to {originalLimit}.");
            }
            catch (Exception ex)
            {
                test.Output.WriteLine($"⚠️ Restore failed: {ex.Message}");
            }
        }

        // 9. انتظار عودة رصيد محفظة إلى قيمة متوقعة
        public static async Task<FazzaSnapshot> WaitForBalanceAsync(
            this BaseFazzaTest test,
            TestSession session,
            decimal expectedBalance)
        {
            return await PollingHelper.UntilAsync(
                async () => await test.TakeSnapshotAsync(session),
                snapshot => Math.Abs(snapshot.WalletBalance - expectedBalance) < 0.001m,
                $"{session.UserKey} balance return to {expectedBalance:F3}",
                describe: snapshot =>
                    $"{session.UserKey}: {snapshot.WalletBalance:F3} " +
                    $"(expected {expectedBalance:F3})");
        }
    }
}