using Automation.Framework.Core.Session;
using Automation.Framework.Helpers;
using Automation.Framework.Services.AccountReadiness.Models;
using Automation.Framework.Services.CashFlowReport.Flow;
using Automation.Framework.Services.CashFlowReport.Models;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Shared;

using System.Text;

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
      Action<string>? log = null,
      Func<Task>? onAccountNotFound = null)
    {
        options ??= new AccountReadinessOptions();
        ValidateOptions(options);
        ValidateSessions(reportSession, payerSession, debtorSession);

        log?.Invoke($"[AccountReadiness] Preparing debtor account {debtorSession.PhoneNumber}.");

        var payerBalance = options.ValidatePayerWallet
            ? await GetWalletBalanceAsync(payerSession)
            : 0m;

        var debtorBalance = options.ValidateDebtorWallet
            ? await GetWalletBalanceAsync(debtorSession)
            : 0m;

        // ✅ الاستدعاء الأول (يمرر onAccountNotFound)
        var debtReport = await GetDebtReportAsync(
            reportSession,
            debtorSession,
            onAccountNotFound,
            log);

        var totalDueBefore = NormalizeDebt(debtReport.TotalDue, options.DebtTolerance);

        log?.Invoke($"[AccountReadiness] TotalDue before settlement: {totalDueBefore}.");

        var transferredAmount = 0m;
        var totalDueAfter = totalDueBefore;

        if (options.SettleDueDebt && totalDueBefore > options.DebtTolerance)
        {

            await SettleDebtAsync(payerSession, debtorSession, payerBalance, totalDueBefore, log);

            transferredAmount = totalDueBefore;

            // آخر تقرير تمت قراءته داخل حلقة التحقق هو التقرير النهائي؛
            // لا حاجة لاستدعاء Cashflow إضافي بعد الانتظار.
            (totalDueAfter, debtReport) = await WaitForDebtSettlementAsync(
                reportSession,
                debtorSession,
                options,
                log);
        }

        if (options.RequireZeroTotalDue && totalDueAfter > options.DebtTolerance)
        {
            throw new InvalidOperationException(
                $"Account '{debtorSession.PhoneNumber}' is not ready. TotalDue is still {totalDueAfter}.");
        }

        log?.Invoke(
            totalDueBefore <= options.DebtTolerance
                ? "[AccountReadiness] No due debt was found."
                : $"[AccountReadiness] Settlement completed. TotalDue after settlement: {totalDueAfter}.");

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

        Guard.RequiredValue(
            reportSession.UserKey,
            nameof(reportSession.UserKey),
            "report session");

        Guard.RequiredValue(
            payerSession.UserKey,
            nameof(payerSession.UserKey),
            "payer session");

        Guard.RequiredValue(
            payerSession.WalletId,
            nameof(payerSession.WalletId),
            "payer session");

        Guard.RequiredGuid(
            payerSession.WalletId,
            nameof(payerSession.WalletId),
            "payer session");

        Guard.RequiredValue(
            debtorSession.PhoneNumber,
            nameof(debtorSession.PhoneNumber),
            "debtor session");

        Guard.RequiredValue(
            debtorSession.SubscriptionId,
            nameof(debtorSession.SubscriptionId),
            "debtor session");

        Guard.RequiredGuid(
            debtorSession.SubscriptionId,
            nameof(debtorSession.SubscriptionId),
            "debtor session");

        Guard.RequiredValue(
            debtorSession.RegionId,
            nameof(debtorSession.RegionId),
            "debtor session");

        Guard.RequiredGuid(
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
            Guard.RequiredGuid(
                debtorSession.WalletId,
                nameof(debtorSession.WalletId),
                "debtor session");
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
        TestSession debtorSession,
        Func<Task>? onAccountNotFound,
        Action<string>? log)
    {
        var response = await _resilience.ExecuteAsync(
            reportSession,
            () => _cashFlow.GetCashflowReportAsync(
                reportSession.UserKey,
                debtorSession.PhoneNumber));

        // ✅ التعديل هنا: تنفيذ الـ Callback بدلاً من رمي الاستثناء مباشرة
        if (response?.Results is null || response.Results.Count == 0)
        {
            if (onAccountNotFound != null)
            {
                log?.Invoke($"[AccountReadiness] Account '{debtorSession.PhoneNumber}' not found. Invoking activation callback...");
                await onAccountNotFound();

                // إعادة القراءة بعد التفعيل: الحساب قد يتأخر ظهوره في Cashflow، لذا نعيد المحاولة بدل قراءة واحدة.
                for (var attempt = 1; attempt <= 6; attempt++)
                {
                    response = await _resilience.ExecuteAsync(
                        reportSession,
                        () => _cashFlow.GetCashflowReportAsync(
                            reportSession.UserKey,
                            debtorSession.PhoneNumber));

                    if (response?.Results is { Count: > 0 })
                        break;

                    await Task.Delay(500);
                }
            }

            // إذا لم تنجح القراءة بعد التفعيل أو لم يتم تمرير Callback
            if (response?.Results is null || response.Results.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cashflow report did not return any result for " +
                    $"'{debtorSession.PhoneNumber}'.");
            }
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
        // استخدام الكلاس المساعد بدلاً من المنطق المعقد
        return AccountMatcher.IdsMatch(item.AccountId, debtorSession.AccountId);
    }

    /// <summary>
    /// مطابقة رقم الهاتف بعد إزالة المسافات والرموز.
    /// </summary>
    private static bool IsPhoneNumberMatch(
       CashflowReportItem item,
       TestSession debtorSession)
    {
        // استخدام الكلاس المساعد (يقوم بتطبيع الهاتف والمقارنة)
        return AccountMatcher.PhonesMatch(item.PhoneNumber, debtorSession.PhoneNumber);
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
    private async Task<(decimal TotalDue, CashflowReportItem Report)> WaitForDebtSettlementAsync(
     TestSession reportSession,
     TestSession debtorSession,
     AccountReadinessOptions options,
     Action<string>? log)
    {
        decimal lastTotalDue = decimal.MaxValue;
        CashflowReportItem? lastReport = null;

        for (var attempt = 1;
             attempt <= options.DebtVerificationAttempts;
             attempt++)
        {
            // ✅ التعديل: تمرير null و log
            var report = await GetDebtReportAsync(
                reportSession,
                debtorSession,
                null,
                log);

            lastReport = report;
            lastTotalDue = NormalizeDebt(
                report.TotalDue,
                options.DebtTolerance);

            log?.Invoke(
                $"[AccountReadiness] Debt verification attempt " +
                $"{attempt}/{options.DebtVerificationAttempts}: " +
                $"TotalDue={lastTotalDue}.");

            if (lastTotalDue <= options.DebtTolerance)
                return (0m, report);

            if (attempt < options.DebtVerificationAttempts)
            {
                await Task.Delay(
                    options.DebtVerificationDelay);
            }
        }

        return (lastTotalDue, lastReport!);
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


}