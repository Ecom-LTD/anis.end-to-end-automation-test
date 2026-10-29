using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Shared;

namespace Automation.Test.TestSupport
{
    public static class AccountReadinessHelper
    {
        /// <summary>
        /// تفعيل حساب جديد في النظام عن طريق تحويل تجريبي صغير،
        /// ثم إرجاع المبلغ فورًا إلى الدافع لتفادي أي تأثير على التقارير.
        ///
        /// التحويلان (ذهابًا وعودة) لا يغيّران الرصيد الصافي.
        /// </summary>
        public static async Task ActivateAccountAsync(
            TransferFlow transferFlow,
            TestSession payerSession,
            TestSession debtorSession,
            decimal activationAmount = 1m,
            Action<string>? log = null)
        {
            log?.Invoke(
                $"🚀 تفعيل الحساب {debtorSession.PhoneNumber} " +
                $"بتحويل {activationAmount} من {payerSession.PhoneNumber}...");

            // 1. تحويل التفعيل (الدافع → المدين)
            var forward = await transferFlow.TransferAsync(
                payerSession.UserKey,
                payerSession.WalletId,
                debtorSession.SubscriptionId,
                activationAmount,
                debtorSession.RegionId);

            if (!forward.Success)
            {
                throw new InvalidOperationException(
                    $"فشل تفعيل الحساب {debtorSession.PhoneNumber}. " +
                    $"Response: {forward.Message}");
            }

            // ظهور الحساب في Cashflow تتحقق منه AccountReadinessFlow بإعادة القراءة بعد هذا الـ callback.
            log?.Invoke("✅ التحويل التجريبي نجح.");

            // 3. إعادة المبلغ (المدين → الدافع) لإلغاء الأثر
            log?.Invoke(
                $"🔄 إعادة {activationAmount} من {debtorSession.PhoneNumber} " +
                $"إلى {payerSession.PhoneNumber}...");

            var reverse = await transferFlow.TransferAsync(
                debtorSession.UserKey,
                debtorSession.WalletId,           // محفظة المدين
                payerSession.SubscriptionId,      // اشتراك الدافع
                activationAmount,
                payerSession.RegionId);

            if (!reverse.Success)
            {
                // لا نرمي استثناء — التفعيل نفسه نجح. نُسجّل فقط.
                log?.Invoke(
                    $"⚠️ إعادة المبلغ فشلت (لكن الحساب مُفعَّل). " +
                    $"Response: {reverse.Message}");
                return;
            }

            log?.Invoke(
                $"✅ تم إرجاع المبلغ. " +
                $"الرصيد الصافي للحسابين = 0. لا تأثير على التقارير.");
        }
    }
}