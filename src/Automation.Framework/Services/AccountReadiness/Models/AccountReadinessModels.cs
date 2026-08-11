using Automation.Framework.Services.CashFlowReport.Models;

namespace Automation.Framework.Services.AccountReadiness.Models
{
    /// <summary>
    /// الإعدادات التي تتحكم في طريقة تجهيز الحساب قبل تشغيل التست.
    /// </summary>
    public sealed class AccountReadinessOptions
    {
        /// <summary>
        /// عند وجود TotalDue أكبر من صفر، هل يتم سداده تلقائيًا؟
        /// </summary>
        public bool SettleDueDebt { get; init; } = true;

        /// <summary>
        /// هل يجب أن ينتهي التجهيز وTotalDue يساوي صفرًا؟
        /// إذا بقي دين بعد السداد، يفشل التجهيز.
        /// </summary>
        public bool RequireZeroTotalDue { get; init; } = true;

        /// <summary>
        /// هل يتم التحقق من إمكانية الوصول إلى محفظة الحساب الدافع؟
        /// </summary>
        public bool ValidatePayerWallet { get; init; } = true;

        /// <summary>
        /// هل يتم التحقق من إمكانية الوصول إلى محفظة الحساب المدين؟
        /// </summary>
        public bool ValidateDebtorWallet { get; init; } = true;

        /// <summary>
        /// عدد مرات إعادة قراءة تقرير Cashflow بعد التحويل.
        /// </summary>
        public int DebtVerificationAttempts { get; init; } = 8;

        /// <summary>
        /// مدة الانتظار بين كل قراءة والأخرى بعد التحويل.
        /// </summary>
        public TimeSpan DebtVerificationDelay { get; init; }
            = TimeSpan.FromMilliseconds(750);

        /// <summary>
        /// هامش صغير لمعالجة اختلافات decimal.
        /// أي قيمة أقل منه تعامل كأنها صفر.
        /// </summary>
        public decimal DebtTolerance { get; init; } = 0.0001m;
    }

    /// <summary>
    /// نتيجة تجهيز الحسابات، ويمكن للتست استخدامها في Assertions أو التسجيل.
    /// </summary>
    public sealed class AccountReadinessResult
    {
        /// <summary>
        /// عنصر Cashflow الذي تمت مطابقته مع الحساب المدين.
        /// </summary>
        public required CashflowReportItem DebtReport { get; init; }

        /// <summary>
        /// رصيد الحساب الدافع قبل السداد.
        /// </summary>
        public decimal PayerBalanceBeforeSettlement { get; init; }

        /// <summary>
        /// رصيد الحساب المدين قبل السداد.
        /// </summary>
        public decimal DebtorBalanceBeforeSettlement { get; init; }

        /// <summary>
        /// قيمة TotalDue قبل تنفيذ التحويل.
        /// </summary>
        public decimal TotalDueBeforeSettlement { get; init; }

        /// <summary>
        /// قيمة TotalDue بعد تنفيذ التحويل وإعادة التحقق.
        /// </summary>
        public decimal TotalDueAfterSettlement { get; init; }

        /// <summary>
        /// المبلغ الذي تم تحويله فعليًا.
        /// </summary>
        public decimal TransferredAmount { get; init; }

        /// <summary>
        /// هل كان هناك دين وتم تنفيذ تحويل لسداده؟
        /// </summary>
        public bool DebtWasSettled => TransferredAmount > 0m;

        /// <summary>
        /// هل الحساب أصبح خاليًا من الدين المستحق؟
        /// </summary>
        public bool IsDebtFree => TotalDueAfterSettlement <= 0m;
    }
}