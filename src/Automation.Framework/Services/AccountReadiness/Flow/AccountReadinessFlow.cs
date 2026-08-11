using System.Text;
using Automation.Framework.Core.Session;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.CashFlowReport.Flow;
using Automation.Framework.Services.CashFlowReport.Models;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;

namespace Automation.Framework.Services.AccountReadiness.Flow;

/// <summary>
/// مسؤول عن تجهيز الحسابات قبل بدء التست.
///
/// مسؤولياته:
/// 1. التحقق من بيانات الجلسات المطلوبة.
/// 2. التحقق من إمكانية الوصول إلى المحافظ.
/// 3. قراءة TotalDue من تقرير Cashflow.
/// 4. سداد الدين كاملًا من الحساب الدافع إلى الحساب المدين.
/// 5. إعادة قراءة TotalDue والتأكد من وصوله إلى صفر.
///
/// هذا الـFlow عام ولا يحتوي أي منطق متعلق بسقف Fazza.
/// </summary>
public sealed class AccountReadinessFlow
{
    private readonly CashflowReportFlow _cashFlow;
    private readonly TransferFlow _transfer;
    private readonly WalletFlow _wallet;
    private readonly ResilientSession _resilience;

    /// <summary>
    /// حقن الخدمات التي يحتاجها تجهيز الحساب.
    /// </summary>
    public AccountReadinessFlow(
        CashflowReportFlow cashFlow,
        TransferFlow transfer,
        WalletFlow wallet,
        ResilientSession resilience)
    {
        _cashFlow = cashFlow;
        _transfer = transfer;
        _wallet = wallet;
        _resilience = resilience;
    }

    /// <summary>
    /// الدالة الرئيسية لتجهيز الحسابات قبل التست.
    ///
    /// reportSession:
    /// الحساب الذي يملك صلاحية قراءة تقرير Cashflow، وغالبًا Dashboard.
    ///
    /// payerSession:
    /// الحساب الذي سيدفع قيمة الدين. في السيناريو الحالي هو المندوب.
    ///
    /// debtorSession:
    /// الحساب الذي عليه TotalDue. في السيناريو الحالي هو حساب الأعمال.
    /// </summary>
    public async Task<AccountReadinessResult> PrepareAsync(
        TestSession reportSession,
        TestSession payerSession,
        TestSession debtorSession,
        AccountReadinessOptions? options = null,
        Action<string>? log = null)
    {
        options ??= new AccountReadinessOptions();

        ValidateOptions(options);

        ValidateSessions(reportSession, payerSession, debtorSession);

        log?.Invoke($"[AccountReadiness] Preparing debtor account " + $"{debtorSession.PhoneNumber}.");

        /*
         * نقرأ رصيد الحساب الدافع قبل قراءة الدين.
         * هذا يثبت أن المحفظة موجودة وقابلة للوصول،
         * كما سنستخدم الرصيد لاحقًا للتحقق من إمكانية سداد TotalDue.
         */
        var payerBalance = options.ValidatePayerWallet
            ? await GetWalletBalanceAsync(payerSession)
            : 0m;

        /*
         * التحقق من محفظة الحساب المدين ليس ضروريًا لتنفيذ التحويل،
         * لكنه مفيد للتأكد من أن بيانات الحساب كاملة وصحيحة.
         */
        var debtorBalance = options.ValidateDebtorWallet
            ? await GetWalletBalanceAsync(debtorSession)
            : 0m;

        /*
         * قراءة تقرير Cashflow ومطابقة العنصر مع الحساب المدين.
         * لا نستخدم FirstOrDefault بصورة عشوائية.
         */
        var debtReport = await GetDebtReportAsync(
            reportSession,
            debtorSession);

        var totalDueBefore = NormalizeDebt(
            debtReport.TotalDue,
            options.DebtTolerance);

        log?.Invoke(
            $"[AccountReadiness] TotalDue before settlement: " +
            $"{totalDueBefore}.");

        var transferredAmount = 0m;
        var totalDueAfter = totalDueBefore;

        /*
         * يتم تنفيذ التحويل فقط عندما:
         * 1. خيار السداد التلقائي مفعّل.
         * 2. TotalDue أكبر من هامش الصفر.
         */
        if (options.SettleDueDebt &&
            totalDueBefore > options.DebtTolerance)
        {
            await SettleDebtAsync(
                payerSession,
                debtorSession,
                payerBalance,
                totalDueBefore,
                log);

            transferredAmount = totalDueBefore;

            /*
             * نجاح TransferAsync لا يعني بالضرورة أن Cashflow
             * تم تحديثه فورًا، لذلك نعيد القراءة عدة مرات.
             */
            totalDueAfter = await WaitForDebtSettlementAsync(
                reportSession,
                debtorSession,
                options,
                log);

            /*
             * نجلب أحدث تقرير حتى تكون النتيجة المعادة
             * محتوية على أحدث حالة للحساب.
             */
            debtReport = await GetDebtReportAsync(
                reportSession,
                debtorSession);
        }
        
        /*
         * إذا كان السداد التلقائي معطلًا، نعيد القيمة الموجودة كما هي.
         * وإذا كان RequireZeroTotalDue مفعّلًا، نفشل بوضوح.
         */
        if (options.RequireZeroTotalDue &&
            totalDueAfter > options.DebtTolerance)
        {
            throw new InvalidOperationException(
                $"Account '{debtorSession.PhoneNumber}' is not ready. " +
                $"TotalDue is still {totalDueAfter}.");
        }

        log?.Invoke(
            totalDueBefore <= options.DebtTolerance
                ? "[AccountReadiness] No due debt was found."
                : $"[AccountReadiness] Settlement completed. " +
                  $"TotalDue after settlement: {totalDueAfter}.");

        return new AccountReadinessResult
        {
            DebtReport = debtReport,
            PayerBalanceBeforeSettlement = payerBalance,
            DebtorBalanceBeforeSettlement = debtorBalance,
            TotalDueBeforeSettlement = totalDueBefore,
            TotalDueAfterSettlement = totalDueAfter,
            TransferredAmount = transferredAmount
        };
    }

    /// <summary>
    /// التحقق من صحة الخيارات قبل بدء أي API calls.
    /// يمنع إعدادات خاطئة مثل عدد محاولات يساوي صفرًا.
    /// </summary>
    private static void ValidateOptions(
        AccountReadinessOptions options)
    {
        if (options.DebtVerificationAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DebtVerificationAttempts),
                "DebtVerificationAttempts must be greater than zero.");
        }

        if (options.DebtVerificationDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DebtVerificationDelay),
                "DebtVerificationDelay cannot be negative.");
        }

        if (options.DebtTolerance < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DebtTolerance),
                "DebtTolerance cannot be negative.");
        }
    }

    /// <summary>
    /// التحقق من وجود البيانات الأساسية في الجلسات.
    ///
    /// لا يقوم هذا الجزء بالاتصال بالـAPI، بل يكتشف
    /// أخطاء البيانات مبكرًا قبل بدء التجهيز.
    /// </summary>
    private static void ValidateSessions(TestSession reportSession, TestSession payerSession, TestSession debtorSession)
    {
        ArgumentNullException.ThrowIfNull(reportSession);
        ArgumentNullException.ThrowIfNull(payerSession);
        ArgumentNullException.ThrowIfNull(debtorSession);

        ValidateRequiredValue(
            reportSession.UserKey,
            nameof(reportSession.UserKey),
            "report session");

        ValidateRequiredValue(
            payerSession.UserKey,
            nameof(payerSession.UserKey),
            "payer session");

        ValidateRequiredValue(
            payerSession.WalletId,
            nameof(payerSession.WalletId),
            "payer session");

        ValidateGuid(
            payerSession.WalletId,
            nameof(payerSession.WalletId),
            "payer session");

        ValidateRequiredValue(
            debtorSession.PhoneNumber,
            nameof(debtorSession.PhoneNumber),
            "debtor session");

        ValidateRequiredValue(
            debtorSession.SubscriptionId,
            nameof(debtorSession.SubscriptionId),
            "debtor session");

        ValidateGuid(
            debtorSession.SubscriptionId,
            nameof(debtorSession.SubscriptionId),
            "debtor session");

        ValidateRequiredValue(
            debtorSession.RegionId,
            nameof(debtorSession.RegionId),
            "debtor session");

        ValidateGuid(
            debtorSession.RegionId,
            nameof(debtorSession.RegionId),
            "debtor session");

        /*
         * محفظة الحساب المدين مطلوبة عندما يكون
         * ValidateDebtorWallet مفعّلًا.
         *
         * التحقق النهائي منها يتم داخل GetWalletBalanceAsync.
         */
        if (!string.IsNullOrWhiteSpace(debtorSession.WalletId))
        {
            ValidateGuid(
                debtorSession.WalletId,
                nameof(debtorSession.WalletId),
                "debtor session");
        }
    }

    /// <summary>
    /// التحقق من أن النص المطلوب غير فارغ.
    /// </summary>
    private static void ValidateRequiredValue(
        string? value,
        string propertyName,
        string sessionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{propertyName} is missing from {sessionName}.");
        }
    }

    /// <summary>
    /// التحقق من أن المعرف النصي Guid صحيح.
    /// </summary>
    private static void ValidateGuid(
        string value,
        string propertyName,
        string sessionName)
    {
        if (!Guid.TryParse(value, out var parsedValue) ||
            parsedValue == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"{propertyName} in {sessionName} is not a valid Guid. " +
                $"Current value: '{value}'.");
        }
    }

    /// <summary>
    /// قراءة رصيد محفظة حساب.
    ///
    /// تستخدم ResilientSession حتى تتم إعادة المصادقة
    /// وإعادة المحاولة تلقائيًا عند استجابة 401.
    /// </summary>
    private Task<decimal> GetWalletBalanceAsync(
        TestSession session)
    {
        if (session.WalletIdGuid == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"WalletId is missing or invalid for account " +
                $"'{session.PhoneNumber}'.");
        }

        return _resilience.ExecuteAsync(
            session,
            () => _wallet.GetBalanceAsync(
                session.UserKey,
                session.WalletIdGuid));
    }

    /// <summary>
    /// قراءة تقرير Cashflow ثم اختيار العنصر المطابق للحساب المدين.
    ///
    /// يتم البحث باستخدام رقم هاتف الحساب المدين، ثم نتحقق
    /// من أن العنصر المعاد يطابق AccountId أو PhoneNumber.
    /// </summary>
    private async Task<CashflowReportItem> GetDebtReportAsync(
        TestSession reportSession,
        TestSession debtorSession)
    {
        var response = await _resilience.ExecuteAsync(
            reportSession,
            () => _cashFlow.GetCashflowReportAsync(
                reportSession.UserKey,
                debtorSession.PhoneNumber));

        if (response?.Results is null ||
            response.Results.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cashflow report did not return any result for " +
                $"'{debtorSession.PhoneNumber}'.");
        }

        /*
         * المطابقة الأولى والأقوى تكون باستخدام AccountId.
         */
        var matchedItem = response.Results.FirstOrDefault(
            item => IsAccountIdMatch(item, debtorSession));

        /*
         * إذا لم يكن AccountId موجودًا أو لم يطابق،
         * نستخدم الهاتف كطريقة احتياطية.
         */
        matchedItem ??= response.Results.FirstOrDefault(
            item => IsPhoneNumberMatch(item, debtorSession));

        if (matchedItem is null)
        {
            var returnedAccounts = string.Join(
                ", ",
                response.Results.Select(
                    item => $"{item.PhoneNumber}/{item.AccountId}"));

            throw new InvalidOperationException(
                $"Cashflow report returned results, but none matched " +
                $"debtor '{debtorSession.PhoneNumber}' with AccountId " +
                $"'{debtorSession.AccountId}'. " +
                $"Returned accounts: {returnedAccounts}.");
        }

        if (matchedItem.TotalDue < 0m)
        {
            throw new InvalidOperationException(
                $"Cashflow returned a negative TotalDue " +
                $"({matchedItem.TotalDue}) for account " +
                $"'{debtorSession.PhoneNumber}'.");
        }

        return matchedItem;
    }

    /// <summary>
    /// مطابقة AccountId بين التقرير والجلسة.
    /// </summary>
    private static bool IsAccountIdMatch(
        CashflowReportItem item,
        TestSession debtorSession)
    {
        if (string.IsNullOrWhiteSpace(item.AccountId) ||
            string.IsNullOrWhiteSpace(debtorSession.AccountId))
        {
            return false;
        }

        if (!Guid.TryParse(item.AccountId, out var reportAccountId) ||
            !Guid.TryParse(
                debtorSession.AccountId,
                out var sessionAccountId))
        {
            return string.Equals(
                item.AccountId.Trim(),
                debtorSession.AccountId.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        return reportAccountId == sessionAccountId;
    }

    /// <summary>
    /// مطابقة رقم الهاتف بعد إزالة المسافات والرموز.
    /// </summary>
    private static bool IsPhoneNumberMatch(
        CashflowReportItem item,
        TestSession debtorSession)
    {
        var reportPhone = NormalizePhoneNumber(
            item.PhoneNumber);

        var sessionPhone = NormalizePhoneNumber(
            debtorSession.PhoneNumber);

        return !string.IsNullOrWhiteSpace(reportPhone) &&
               reportPhone == sessionPhone;
    }

    /// <summary>
    /// تنفيذ السداد كاملًا من الحساب الدافع إلى الحساب المدين.
    ///
    /// لا يسمح بالسداد الجزئي؛ لأن بدء التست مع دين متبقٍ
    /// يجعل حالة التست غير معروفة وغير مستقرة.
    /// </summary>
    private async Task SettleDebtAsync(
        TestSession payerSession,
        TestSession debtorSession,
        decimal payerBalance,
        decimal totalDue,
        Action<string>? log)
    {
        if (totalDue <= 0m)
            return;

        if (payerBalance < totalDue)
        {
            throw new InvalidOperationException(
                $"Payer account '{payerSession.PhoneNumber}' does not " +
                $"have enough wallet balance to settle the debt. " +
                $"Balance: {payerBalance}, TotalDue: {totalDue}, " +
                $"Missing: {totalDue - payerBalance}.");
        }

        log?.Invoke(
            $"[AccountReadiness] Transferring {totalDue} from " +
            $"{payerSession.PhoneNumber} to " +
            $"{debtorSession.PhoneNumber}.");

        await _resilience.ExecuteAsync(
            payerSession,
            () => _transfer.TransferAsync(
                payerSession.UserKey,
                payerSession.WalletId,
                debtorSession.SubscriptionId,
                totalDue,
                debtorSession.RegionId));
    }

    /// <summary>
    /// إعادة قراءة TotalDue بعد التحويل.
    ///
    /// بعض الخدمات لا تحدث Cashflow مباشرة، لذلك نمنح النظام
    /// عدة محاولات قبل اعتبار السداد فاشلًا.
    /// </summary>
    private async Task<decimal> WaitForDebtSettlementAsync(
        TestSession reportSession,
        TestSession debtorSession,
        AccountReadinessOptions options,
        Action<string>? log)
    {
        decimal lastTotalDue = decimal.MaxValue;

        for (var attempt = 1;
             attempt <= options.DebtVerificationAttempts;
             attempt++)
        {
            var report = await GetDebtReportAsync(
                reportSession,
                debtorSession);

            lastTotalDue = NormalizeDebt(
                report.TotalDue,
                options.DebtTolerance);

            log?.Invoke(
                $"[AccountReadiness] Debt verification attempt " +
                $"{attempt}/{options.DebtVerificationAttempts}: " +
                $"TotalDue={lastTotalDue}.");

            if (lastTotalDue <= options.DebtTolerance)
                return 0m;

            if (attempt < options.DebtVerificationAttempts)
            {
                await Task.Delay(
                    options.DebtVerificationDelay);
            }
        }

        return lastTotalDue;
    }

    /// <summary>
    /// تحويل القيم الصغيرة جدًا إلى صفر.
    /// </summary>
    private static decimal NormalizeDebt(
        decimal totalDue,
        decimal tolerance)
    {
        return Math.Abs(totalDue) <= tolerance
            ? 0m
            : totalDue;
    }

    /// <summary>
    /// توحيد رقم الهاتف بإبقاء الأرقام فقط.
    ///
    /// مثال:
    /// +218 91-123-4567
    /// يصبح:
    /// 218911234567
    /// </summary>
    private static string NormalizePhoneNumber(
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var normalized = new StringBuilder();

        foreach (var character in phoneNumber)
        {
            if (char.IsDigit(character))
                normalized.Append(character);
        }

        return normalized.ToString();
    }
}