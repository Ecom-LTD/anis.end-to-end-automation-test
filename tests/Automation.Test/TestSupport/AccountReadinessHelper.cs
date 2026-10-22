using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Shared;

namespace Automation.Test.TestSupport
{
    public static class AccountReadinessHelper
    {
        /// <summary>
        /// تفعيل حساب جديد في النظام عن طريق إجراء تحويل بسيط من حساب دافع.
        /// يُستخدم عندما لا يظهر الحساب في تقرير الـ Cashflow.
        /// </summary>
        public static async Task ActivateAccountAsync(
            TransferFlow transferFlow,
            TestSession payerSession,
            TestSession debtorSession,
            decimal activationAmount = 1m,
            Action<string>? log = null)
        {
            log?.Invoke($"🚀 تفعيل الحساب {debtorSession.PhoneNumber} عن طريق تحويل تجريبي ({activationAmount})...");

            var result = await transferFlow.TransferAsync(
                payerSession.UserKey,
                payerSession.WalletId,
                debtorSession.SubscriptionId,
                activationAmount,
                debtorSession.RegionId);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"فشل تفعيل الحساب {debtorSession.PhoneNumber}. Response: {result.Message}");
            }

            log?.Invoke($"✅ تم إرسال التحويل التجريبي بنجاح. في انتظار النظام...");

            // ننتظر النظام ليستوعب التحويل ويظهر في التقارير
            await Task.Delay(2000);
        }
    }
}