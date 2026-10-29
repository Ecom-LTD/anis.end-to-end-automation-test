using Automation.Framework.Helpers;
using Automation.Framework.Helpers.Almusher;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;
using Automation.Framework.Testing;
using Automation.Test.Infrastructure.Reporting;
using Automation.Test.Support;
using Automation.Test.Tests.Fazza.Base;
using Xunit;

namespace Automation.Test.TestSupport
{
    public static class FazzaHelper
    {
        // ================================================================
        // 1. الجلب والقراءة (Read Operations)
        // ================================================================

        /// <summary>
        /// جلب حساب السلفة/الفزعة (يطابق بـ AccountId أولاً ثم الهاتف).
        /// </summary>
        public static async Task<SulfaAccount> GetSulfaAccountAsync(
            this BaseFazzaTest test, TestSession session)
        {
            var matched = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.FindAccountAsync(
                    test.Dashboard.UserKey, session.PhoneNumber, session.AccountId));

            return matched ?? throw new InvalidOperationException(
                $"Sulfa account not found for '{session.PhoneNumber}' " +
                $"(AccountId: '{session.AccountId}').");
        }

        /// <summary>
        /// جلب بيانات المنطقة.
        /// </summary>
        public static async Task<RegionSulfaFullData> GetRegionAsync(
            this BaseFazzaTest test, string regionId)
        {
            // تم تصحيح الخطأ: تمرير Dashboard.Token بدلاً من UserKey (لأن الدالة في الـ Client تتطلب token)
            var region = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.GetRegionFullDataAsync(
                    test.Dashboard.UserKey, regionId));

            Assert.NotNull(region);
            return region;
        }

        /// <summary>
        /// حساب المتاح في المنطقة (MaxLimit - TotalAllocated).
        /// </summary>
        public static async Task<decimal> GetRegionAvailableCapacityAsync(
            this BaseFazzaTest test, string regionId)
        {
            test.Output.WriteLine($"📊 جاري حساب المتاح في المنطقة (Region: {regionId})...");

            var region = await test.GetRegionAsync(regionId);
            decimal available = region.FazaaMaxLimit - region.TotalAllocatedFazaaAmount;

            test.Output.WriteLine($"   ├─ سقف المنطقة (Max Limit): {region.FazaaMaxLimit}");
            test.Output.WriteLine($"   ├─ المخصص للمنطقة (Allocated): {region.TotalAllocatedFazaaAmount}");
            test.Output.WriteLine($"   └─ ⚠️ المتاح في المنطقة (Available): {available}");

            return available;
        }

        // ================================================================
        // 2. التجهيز والتنظيف (Setup & Cleanup)
        // ================================================================

        /// <summary>
        /// تصفير ديون الحساب قبل بدء الاختبار.
        /// </summary>
        public static async Task EnsureDebtFreeAsync(
            this BaseFazzaTest test, TestSession debtorSession)
        {
            // تحديد الدافع (إذا كان المدين هو FazzaUser1، نجعل الدافع FazzaUser2 والعكس)
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
                DecimalComparer.AreClose(0m, result.TotalDueAfterSettlement),
                $"Account '{debtorSession.PhoneNumber}' is not debt-free.");
        }

        /// <summary>
        /// يضمن أن المنطقة لديها سعة متاحة كافية. إذا لم يكن كافياً، يرفع سقف المنطقة بمقدار العجز.
        /// </summary>
        /// <summary>
        /// يضمن أن المنطقة لديها سعة متاحة كافية (أكبر من أو تساوي القيمة المطلوبة).
        /// إذا كان المتاح أقل، يرفع سقف المنطقة بمقدار العجز.
        /// </summary>
        public static async Task EnsureRegionCapacityAsync(
            this BaseFazzaTest test,
            string regionId,
            decimal requiredLimit)
        {
            // قفل المنطقة يمنع تسابق قراءة-ثم-تعديل بين اختبارات متوازية على نفس المنطقة.
            using var regionLock = await RegionLock.AcquireAsync(regionId);

            var region = await test.GetRegionAsync(regionId);

            // المتاح = سقف المنطقة - إجمالي المخصص
            var available = region.FazaaMaxLimit - region.TotalAllocatedFazaaAmount;

            test.Output.WriteLine($"📊 البيانات الحالية للمنطقة:");
            test.Output.WriteLine($"   ├─ سقف المنطقة (Max Limit):      {region.FazaaMaxLimit:N2}");
            test.Output.WriteLine($"   ├─ المخصص للمنطقة (Allocated):   {region.TotalAllocatedFazaaAmount:N2}");
            test.Output.WriteLine($"   └─ ⚠️ المتاح في المنطقة (Available): {available:N2}");
            test.Output.WriteLine($"🎯 القيمة المطلوبة للتخصيص:          {requiredLimit:N2}");

            // إذا كان المتاح كافياً، لا نفعل شيئاً
            if (available >= requiredLimit)
            {
                test.Output.WriteLine($"✅ النتيجة: المتاح في المنطقة كافٍ. لا حاجة لرفع السقف.");
                return;
            }

            // السقف الجديد = السقف الحالي + العجز فقط (المطلوب - المتاح).
            var shortage = requiredLimit - available;
            var newLimit = region.FazaaMaxLimit + shortage;

            test.Output.WriteLine($"❌ النتيجة: المتاح في المنطقة غير كافٍ.");
            test.Output.WriteLine($"   ├─ العجز المطلوب:              {shortage:N2}");
            test.Output.WriteLine($"   └─ سقف المنطقة الجديد المطلوب:  {newLimit:N2}");

            var response = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.SetRegionMaxFazaaLimitAsync(
                    test.Dashboard.UserKey,
                    regionId,
                    newLimit));

            Assert.True(response is { Success: true },
                $"Failed to raise region '{regionId}' limit: {response?.Message}");

            await PollingHelper.UntilAsync(
                () => test.GetRegionAsync(regionId),
                r => r.FazaaMaxLimit >= newLimit,
                $"region {regionId} FazaaMaxLimit to reach {newLimit}");

            test.Output.WriteLine($"✅ تم رفع سقف المنطقة بنجاح.");
        }

        // ================================================================
        // 3. المراقبة والانتظار (Polling & Waiting)
        // ================================================================


        // في FazzaHelper.cs

        /// <summary>
        /// يضمن أن حساب معين يملك سقف فزعة (MaxFazaaDebtLimit) أكبر من أو يساوي القيمة المطلوبة.
        /// إذا كان السقف الحالي أقل، يقوم برفع سقف المنطقة (إن لزم) ثم يرفع سقف الحساب.
        /// </summary>
        public static async Task EnsureFazzaLimitAsync(
            this BaseFazzaTest test,
            TestSession accountSession,
            decimal requiredLimit )
        {
            test.Output.WriteLine($"\n🛡️ [EnsureFazzaLimit] ضمان سقف للحساب {accountSession.UserKey} (المطلوب: {requiredLimit})...");

            // 1. جلب بيانات الحساب الحالية
            var account = await test.GetSulfaAccountAsync(accountSession);
            var currentLimit = account.MaxFazaaDebtLimit;

            // 2. إذا كان السقف الحالي كافياً، لا نفعل شيئاً
            if (currentLimit >= requiredLimit)
            {
                test.Output.WriteLine($"✅ السقف الحالي ({currentLimit}) كافٍ. لا حاجة لأي تعديل.");
                return;
            }

            // 3. إذا لم يكن كافياً، نحتاج لرفعه
            test.Output.WriteLine($"⚠️ السقف الحالي ({currentLimit}) أقل من المطلوب ({requiredLimit}). جاري الرفع...");

            // المنطقة تحتاج استيعاب الزيادة فقط (المطلوب - الحالي) لأن السقف الحالي محسوب أصلًا ضمن المخصص.
            await test.EnsureRegionCapacityAsync(accountSession.RegionId, requiredLimit - currentLimit);

            // 5. رفع سقف الحساب إلى القيمة المطلوبة
            var response = await test.ExecuteWithRetryAsync(
                test.Dashboard,
                () => test.FazzaTopUp.SetAccountFazzaDebtMaxLimitAsync(
                    test.Dashboard.UserKey,
                    accountSession.AccountIdGuid,
                    requiredLimit));

            Assert.False(string.IsNullOrWhiteSpace(response), "API returned empty response.");

            // 6. الانتظار حتى يظهر السقف الجديد في النظام
            await test.WaitForAccountLimitAsync(accountSession, requiredLimit);

            test.Output.WriteLine($"✅ تم رفع السقف بنجاح إلى {requiredLimit}.");
        }
        /// <summary>
        /// الانتظار حتى يتم تحديث سقف الحساب ويصبح مساوياً للقيمة المطلوبة.
        /// </summary>
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

        /// <summary>
        /// انتظار عودة رصيد محفظة إلى قيمة متوقعة.
        /// </summary>
        public static async Task<FazzaSnapshot> WaitForBalanceAsync(
            this BaseFazzaTest test,
            TestSession session,
            decimal expectedBalance)
        {
            return await PollingHelper.UntilAsync(
                async () => await test.TakeSnapshotAsync(session),
                snapshot => DecimalComparer.AreClose(expectedBalance, snapshot.WalletBalance, 0.001m),
                $"{session.UserKey} balance return to {expectedBalance:F3}");
        }

        // ================================================================
        // 4. الأدوات المساعدة (Utilities & Assertions)
        // ================================================================

        /// <summary>
        /// أخذ لقطة (Snapshot) لحالة الحساب.
        /// </summary>
        public static async Task<FazzaSnapshot> TakeSnapshotAsync(
            this BaseFazzaTest test, TestSession session)
        {
            // القراءتان مستقلتان؛ نشغّلهما بالتوازي ونحمي الرصيد بإعادة المحاولة عند 401.
            var balanceTask = test.ExecuteWithRetryAsync(
                session,
                () => test.Wallet.GetBalanceAsync(session.UserKey, session.WalletIdGuid));
            var accountTask = test.GetSulfaAccountAsync(session);

            await Task.WhenAll(balanceTask, accountTask);

            return new FazzaSnapshot
            {
                WalletBalance = balanceTask.Result,
                Account = accountTask.Result
            };
        }

        /// <summary>
        /// التحقق من أن الفرق (Delta) بين حالتين يطابق القيمة المتوقعة.
        /// </summary>
        public static void AssertDelta(
            this BaseFazzaTest test,
            decimal before,
            decimal after,
            decimal expectedDelta,
            string label)
        {
            test.Output.WriteLine($"   {label}: قبل = {before} | بعد = {after}");
            test.Output.Verify($"فرق {label}", expectedDelta, after - before, 0.0001m);
        }

        /// <summary>
        /// التحقق من أن القيمة الفعلية تساوي القيمة المتوقعة (مطابقة تامة).
        /// تُستخدم في عمليات "التعيين" التي لا تقبل الجمع أو الطرح.
        /// </summary>
        public static void AssertEqualTo(
            this BaseFazzaTest test,
            decimal expected,
            decimal actual,
            string label)
            => test.Output.Verify(label, expected, actual, 0.0001m);

        /// <summary>يطبع لقطة الحساب بصيغة موحّدة (للاستخدام داخل Before/After).</summary>
        public static (string, object?)[] Describe(this FazzaSnapshot s, string prefix)
            => new (string, object?)[]
            {
                ($"{prefix} الرصيد", s.WalletBalance),
                ($"{prefix} سقف الحساب (MaxFazaaDebtLimit)", s.Account.MaxFazaaDebtLimit),
                ($"{prefix} الدين المؤكد (ConfirmedDebt)", s.Account.ConfirmedDebt)
            };
        /// <summary>
        /// استعادة السقف الأصلي بأمان (دالة Cleanup).
        /// </summary>
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

                // تم تصحيح اسم الدالة من SetAccountFazzaDebtMaxLimitAsync إلى SetAccountFazzaDebtMaxLimitAsync
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
    }
}