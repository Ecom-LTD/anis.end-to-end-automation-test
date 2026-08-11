using System.Text;
using Automation.Framework.Core.Session;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;

namespace Automation.Framework.Services.FazzaTopup.Flow;

/// <summary>
/// يجهّز سقف Fazza للحسابات قبل تشغيل التست.
///
/// مسؤولياته:
/// 1. جلب حساب السلفة الصحيح.
/// 2. حساب سقف Fazza المتاح.
/// 3. التحقق من كفاية السقف.
/// 4. رفع سقف المنطقة عند الحاجة.
/// 5. رفع سقف الحساب عند الحاجة.
/// 6. إعادة القراءة للتأكد من تطبيق التعديلات.
///
/// لا يحتوي هذا الـFlow على:
/// - فحص TotalDue.
/// - سداد الديون.
/// - تحويل الأموال.
///
/// هذه المسؤوليات موجودة داخل AccountReadinessFlow.
/// </summary>
public sealed class FazzaLimitReadinessFlow
{
    private readonly FazzaTopUpFlow _fazzaTopUp;
    private readonly ResilientSession _resilience;

    public FazzaLimitReadinessFlow(
        FazzaTopUpFlow fazzaTopUp,
        ResilientSession resilience)
    {
        _fazzaTopUp = fazzaTopUp;
        _resilience = resilience;
    }

    /// <summary>
    /// نقطة الدخول الرئيسية لتجهيز سقف المندوب وحساب الأعمال.
    ///
    /// يجب استدعاء AccountReadinessFlow قبل هذه الدالة؛
    /// لأن سداد الدين قد يغيّر ConfirmedDebt والسقف المتاح.
    /// </summary>
    public async Task<FazzaLimitReadinessResult> EnsureLimitsAsync(
        TestSession dashboardSession,
        TestSession operatorSession,
        TestSession businessSession,
        FazzaLimitReadinessOptions? options = null,
        Action<string>? log = null)
    {
        options ??= new FazzaLimitReadinessOptions();

        ValidateOptions(options);

        ValidateSessions(
            dashboardSession,
            operatorSession,
            businessSession);

        log?.Invoke(
            "[FazzaLimitReadiness] Starting Fazza limit preparation.");

        var operatorResult = await EnsureAccountLimitAsync(
            dashboardSession,
            operatorSession,
            options.RequiredOperatorAvailableLimit,
            options,
            log);

        var businessResult = await EnsureAccountLimitAsync(
            dashboardSession,
            businessSession,
            options.RequiredBusinessAvailableLimit,
            options,
            log);

        log?.Invoke(
            "[FazzaLimitReadiness] Fazza limit preparation completed.");

        return new FazzaLimitReadinessResult
        {
            OperatorAvailableLimit =
                operatorResult.AvailableLimit,

            BusinessAvailableLimit =
                businessResult.AvailableLimit,

            OperatorLimitWasUpdated =
                operatorResult.WasUpdated,

            BusinessLimitWasUpdated =
                businessResult.WasUpdated
        };
    }

    /// <summary>
    /// يجهّز سقف حساب واحد.
    ///
    /// إذا كان السقف المتاح كافيًا، لا يعدّل شيئًا.
    /// إذا كان غير كافٍ:
    /// 1. يحسب الحد الأقصى الجديد المطلوب.
    /// 2. يتأكد أن المنطقة تستطيع استيعاب الزيادة.
    /// 3. يرفع سقف المنطقة عند الحاجة.
    /// 4. يرفع سقف الحساب.
    /// 5. يعيد القراءة حتى يظهر التعديل.
    /// </summary>
    private async Task<AccountLimitPreparationResult>
        EnsureAccountLimitAsync(
            TestSession dashboardSession,
            TestSession accountSession,
            decimal requiredAvailableLimit,
            FazzaLimitReadinessOptions options,
            Action<string>? log)
    {
        var account = await GetAccountAsync(
            dashboardSession,
            accountSession);

        var availableLimit = CalculateAvailableLimit(account);

        log?.Invoke(
            $"[FazzaLimitReadiness] Account: " +
            $"{accountSession.PhoneNumber}, " +
            $"MaxLimit: {account.MaxFazaaDebtLimit}, " +
            $"ConfirmedDebt: {account.ConfirmedDebt}, " +
            $"AvailableLimit: {availableLimit}, " +
            $"RequiredAvailableLimit: {requiredAvailableLimit}.");

        /*
         * لا يوجد داعٍ لأي تعديل عندما يكون السقف الحالي كافيًا.
         */
        if (availableLimit >= requiredAvailableLimit)
        {
            log?.Invoke(
                $"[FazzaLimitReadiness] Account " +
                $"{accountSession.PhoneNumber} already has " +
                $"sufficient available limit.");

            return new AccountLimitPreparationResult(
                availableLimit,
                WasUpdated: false);
        }

        /*
         * عندما يكون الرفع التلقائي غير مسموح،
         * نفشل برسالة واضحة بدل تعديل البيانات.
         */
        if (!options.AutoIncreaseInsufficientLimits)
        {
            throw new InvalidOperationException(
                $"Account '{accountSession.PhoneNumber}' does not have " +
                $"enough available Fazza limit. " +
                $"Available: {availableLimit}, " +
                $"Required: {requiredAvailableLimit}.");
        }

        /*
         * الحد الأقصى المطلوب يجب أن يغطي:
         *
         * ConfirmedDebt الحالي
         * + السقف المتاح المطلوب للتست
         * + هامش أمان.
         */
        var requiredMaximumLimit =
            account.ConfirmedDebt +
            requiredAvailableLimit +
            options.LimitSafetyMargin;

        log?.Invoke(
            $"[FazzaLimitReadiness] Account limit must be increased " +
            $"from {account.MaxFazaaDebtLimit} " +
            $"to {requiredMaximumLimit}.");

        /*
         * قبل رفع سقف الحساب، يجب التأكد من أن سقف المنطقة
         * يستطيع استيعاب الزيادة الإضافية في تخصيص الحساب.
         */
        await EnsureRegionCapacityAsync(
            dashboardSession,
            accountSession,
            account,
            requiredMaximumLimit,
            options,
            log);

        /*
         * بعد ضمان سعة المنطقة، نرفع سقف الحساب.
         */
        await UpdateAccountLimitAsync(
            dashboardSession,
            accountSession,
            requiredMaximumLimit,
            log);

        /*
         * لا نعتبر نجاح طلب التعديل كافيًا.
         * نعيد قراءة الحساب حتى يظهر السقف المتاح الجديد.
         */
        var updatedAccount = await WaitForAccountAvailableLimitAsync(
            dashboardSession,
            accountSession,
            requiredAvailableLimit,
            options,
            log);

        var updatedAvailableLimit =
            CalculateAvailableLimit(updatedAccount);

        return new AccountLimitPreparationResult(
            updatedAvailableLimit,
            WasUpdated: true);
    }

    /// <summary>
    /// يجلب حساب السلفة المطابق للجلسة.
    ///
    /// لا يستخدم First() أو FirstOrDefault() عشوائيًا.
    /// تتم المطابقة أولًا بـAccountId ثم برقم الهاتف.
    /// </summary>
    private async Task<SulfaAccount> GetAccountAsync(
        TestSession dashboardSession,
        TestSession accountSession)
    {
        var accounts = await _resilience.ExecuteAsync(
            dashboardSession,
            () => _fazzaTopUp.GetSulfaAccountsAsync(
                dashboardSession.UserKey,
                accountSession.PhoneNumber));

        if (accounts is null || accounts.Count == 0)
        {
            throw new InvalidOperationException(
                $"No Sulfa account was returned for phone " +
                $"'{accountSession.PhoneNumber}'.");
        }

        /*
         * المطابقة الأقوى تكون باستخدام AccountId.
         */
        var matchedAccount = accounts.FirstOrDefault(
            account => IsAccountIdMatch(
                account,
                accountSession));

        /*
         * الهاتف يستخدم كخيار احتياطي إذا لم تنجح
         * المطابقة باستخدام AccountId.
         */
        matchedAccount ??= accounts.FirstOrDefault(
            account => IsPhoneNumberMatch(
                account,
                accountSession));

        if (matchedAccount is null)
        {
            var returnedAccounts = string.Join(
                ", ",
                accounts.Select(
                    account => $"{account.Phone}/{account.Id}"));

            throw new InvalidOperationException(
                $"Sulfa accounts were returned, but none matched " +
                $"account '{accountSession.PhoneNumber}' with " +
                $"AccountId '{accountSession.AccountId}'. " +
                $"Returned accounts: {returnedAccounts}.");
        }

        ValidateAccountValues(
            matchedAccount,
            accountSession);

        return matchedAccount;
    }

    /// <summary>
    /// حساب سقف Fazza المتاح فعليًا.
    ///
    /// MaxFazaaDebtLimit وحده لا يمثل القيمة المتاحة؛
    /// لأن ConfirmedDebt يستهلك جزءًا من السقف.
    /// </summary>
    private static decimal CalculateAvailableLimit(
        SulfaAccount account)
    {
        return account.MaxFazaaDebtLimit -
               account.ConfirmedDebt;
    }

    /// <summary>
    /// يتأكد أن المنطقة تستطيع استيعاب الزيادة الجديدة
    /// التي سنضيفها إلى سقف الحساب.
    ///
    /// إذا كانت السعة غير كافية، يرفع سقف المنطقة أولًا.
    /// </summary>
    private async Task EnsureRegionCapacityAsync(
        TestSession dashboardSession,
        TestSession accountSession,
        SulfaAccount currentAccount,
        decimal requiredAccountMaximumLimit,
        FazzaLimitReadinessOptions options,
        Action<string>? log)
    {
        var region = await GetRegionAsync(
            dashboardSession,
            accountSession.RegionId);

        /*
         * القيمة المتاحة حاليًا في المنطقة.
         */
        var regionAvailableLimit =
            region.FazaaMaxLimit -
            region.TotalAllocatedFazaaAmount;

        /*
         * لا تحتاج المنطقة إلى استيعاب سقف الحساب كاملًا؛
         * لأن السقف الحالي للحساب محسوب أصلًا ضمن المخصص.
         *
         * المطلوب فقط هو استيعاب الفرق بين:
         * السقف الجديد - السقف الحالي.
         */
        var additionalAccountAllocation =
            Math.Max(
                0m,
                requiredAccountMaximumLimit -
                currentAccount.MaxFazaaDebtLimit);

        log?.Invoke(
            $"[FazzaLimitReadiness] Region: {region.Name}, " +
            $"RegionMaxLimit: {region.FazaaMaxLimit}, " +
            $"TotalAllocated: {region.TotalAllocatedFazaaAmount}, " +
            $"RegionAvailable: {regionAvailableLimit}, " +
            $"RequiredAdditionalAllocation: " +
            $"{additionalAccountAllocation}.");

        /*
         * المنطقة لديها مساحة كافية، لذلك لا نعدل سقفها.
         */
        if (regionAvailableLimit >= additionalAccountAllocation)
        {
            log?.Invoke(
                $"[FazzaLimitReadiness] Region '{region.Name}' " +
                $"already has sufficient capacity.");

            return;
        }

        /*
         * مقدار العجز في المنطقة.
         */
        var shortage =
            additionalAccountAllocation -
            regionAvailableLimit;

        /*
         * نرفع الحد الحالي بمقدار العجز، ثم نضيف هامش الأمان.
         */
        var requiredRegionMaximumLimit =
            region.FazaaMaxLimit +
            shortage +
            options.LimitSafetyMargin;

        await UpdateRegionLimitAsync(
            dashboardSession,
            accountSession.RegionId,
            requiredRegionMaximumLimit,
            log);

        /*
         * إعادة القراءة للتأكد من تطبيق حد المنطقة الجديد.
         */
        await WaitForRegionLimitAsync(
            dashboardSession,
            accountSession.RegionId,
            requiredRegionMaximumLimit,
            options,
            log);
    }

    /// <summary>
    /// يجلب بيانات المنطقة ويتحقق من أن النتيجة
    /// تخص RegionId المطلوب فعلًا.
    /// </summary>
    private async Task<RegionSulfaFullData> GetRegionAsync(
        TestSession dashboardSession,
        string regionId)
    {
        var region = await _resilience.ExecuteAsync(
            dashboardSession,
            () => _fazzaTopUp.GetRegionFullDataAsync(
                dashboardSession.Token,
                regionId));

        if (region is null)
        {
            throw new InvalidOperationException(
                $"Region data was not found for RegionId '{regionId}'.");
        }

        /*
         * هذه الحماية مهمة لأن GetRegionFullDataAsync في المشروع
         * يأخذ أول عنصر من القائمة التي يعيدها الـAPI.
         */
        if (!AreSameIdentifiers(region.Id, regionId))
        {
            throw new InvalidOperationException(
                $"The region API returned RegionId '{region.Id}' " +
                $"while the requested RegionId was '{regionId}'. " +
                $"Do not use the first region without matching its Id.");
        }

        if (region.FazaaMaxLimit < 0m)
        {
            throw new InvalidOperationException(
                $"Region '{region.Id}' returned a negative " +
                $"FazaaMaxLimit: {region.FazaaMaxLimit}.");
        }

        if (region.TotalAllocatedFazaaAmount < 0m)
        {
            throw new InvalidOperationException(
                $"Region '{region.Id}' returned a negative " +
                $"TotalAllocatedFazaaAmount: " +
                $"{region.TotalAllocatedFazaaAmount}.");
        }

        return region;
    }

    /// <summary>
    /// يرسل طلب تعديل سقف المنطقة مرة واحدة فقط.
    ///
    /// التحقق بعد ذلك يتم باستخدام GET وليس بإعادة طلب التعديل.
    /// </summary>
    private async Task UpdateRegionLimitAsync(
        TestSession dashboardSession,
        string regionId,
        decimal newMaximumLimit,
        Action<string>? log)
    {
        log?.Invoke(
            $"[FazzaLimitReadiness] Increasing region '{regionId}' " +
            $"maximum limit to {newMaximumLimit}.");

        var response = await _resilience.ExecuteAsync(
            dashboardSession,
            () => _fazzaTopUp.SetRegionMaxFazaaLimitAsync(
                dashboardSession.Token,
                regionId,
                newMaximumLimit));

        if (response is null)
        {
            throw new InvalidOperationException(
                $"Updating region '{regionId}' returned no response.");
        }

        /*
         * بعض الـAPIs قد تعيد Success=false أو Status مختلفًا.
         * لا نعتمد على Message وحدها.
         */
        if (!response.Success)
        {
            throw new InvalidOperationException(
                $"Failed to update region '{regionId}'. " +
                $"Status: {response.Status}, " +
                $"Message: {response.Message}.");
        }

        log?.Invoke(
            $"[FazzaLimitReadiness] Region update response: " +
            $"{response.Message}.");
    }

    /// <summary>
    /// يرفع سقف حساب Fazza.
    /// </summary>
    private async Task UpdateAccountLimitAsync(
    TestSession dashboardSession,
    TestSession accountSession,
    decimal newMaximumLimit,
    Action<string>? log)
    {
        if (accountSession.AccountIdGuid == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"AccountId is missing or invalid for account " +
                $"'{accountSession.PhoneNumber}'.");
        }

        if (newMaximumLimit < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newMaximumLimit),
                newMaximumLimit,
                "The new account Fazza limit cannot be negative.");
        }

        log?.Invoke(
            $"[FazzaLimitReadiness] Increasing account " +
            $"'{accountSession.PhoneNumber}' maximum limit " +
            $"to {newMaximumLimit}.");

        var response = await _resilience.ExecuteAsync(
            dashboardSession,
            () => _fazzaTopUp.SetAccountFazzaDeptMaxLimitAsync(
                dashboardSession.UserKey,
                accountSession.AccountIdGuid,
                newMaximumLimit));

        /*
         * الدالة الحالية ترجع string، وقد ترجع رسالة فشل
         * بدل أن ترمي Exception.
         */
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException(
                $"Updating Fazza limit for account " +
                $"'{accountSession.PhoneNumber}' returned an empty response.");
        }

        if (IsFailureResponse(response))
        {
            throw new InvalidOperationException(
                $"Failed to update Fazza limit for account " +
                $"'{accountSession.PhoneNumber}'. " +
                $"Requested limit: {newMaximumLimit}. " +
                $"Response: {response}");
        }

        log?.Invoke(
            $"[FazzaLimitReadiness] Account update response: " +
            $"{response}.");
    }

    private static bool IsFailureResponse(
    string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return true;

        var failureIndicators = new[]
        {
        "failed",
        "failure",
        "error",
        "invalid",
        "not found",
        "unauthorized",
        "forbidden",
        "conflict",
        "غير ناجح",
        "فشل",
        "خطأ"
    };

        return failureIndicators.Any(indicator =>
            response.Contains(
                indicator,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// يعيد قراءة المنطقة عدة مرات حتى يظهر السقف الجديد.
    ///
    /// هذا يعالج التأخير المحتمل بين نجاح طلب التعديل
    /// وظهور القيمة الجديدة في طلب القراءة.
    /// </summary>
    private async Task<RegionSulfaFullData> WaitForRegionLimitAsync(
        TestSession dashboardSession,
        string regionId,
        decimal requiredMaximumLimit,
        FazzaLimitReadinessOptions options,
        Action<string>? log)
    {
        RegionSulfaFullData? lastRegion = null;

        for (var attempt = 1;
             attempt <= options.VerificationAttempts;
             attempt++)
        {
            lastRegion = await GetRegionAsync(
                dashboardSession,
                regionId);

            log?.Invoke(
                $"[FazzaLimitReadiness] Region verification " +
                $"{attempt}/{options.VerificationAttempts}: " +
                $"CurrentMaxLimit={lastRegion.FazaaMaxLimit}, " +
                $"RequiredMaxLimit={requiredMaximumLimit}.");

            if (lastRegion.FazaaMaxLimit >= requiredMaximumLimit)
                return lastRegion;

            if (attempt < options.VerificationAttempts)
            {
                await Task.Delay(
                    options.VerificationDelay);
            }
        }

        throw new InvalidOperationException(
            $"Region '{regionId}' limit was updated, but the new value " +
            $"was not observed. " +
            $"Current: {lastRegion?.FazaaMaxLimit}, " +
            $"Required: {requiredMaximumLimit}.");
    }

    /// <summary>
    /// يعيد قراءة الحساب عدة مرات حتى يصبح السقف المتاح
    /// مساويًا أو أكبر من القيمة التي يحتاجها التست.
    /// </summary>
    private async Task<SulfaAccount>
        WaitForAccountAvailableLimitAsync(
            TestSession dashboardSession,
            TestSession accountSession,
            decimal requiredAvailableLimit,
            FazzaLimitReadinessOptions options,
            Action<string>? log)
    {
        SulfaAccount? lastAccount = null;
        decimal lastAvailableLimit = 0m;

        for (var attempt = 1;
             attempt <= options.VerificationAttempts;
             attempt++)
        {
            lastAccount = await GetAccountAsync(
                dashboardSession,
                accountSession);

            lastAvailableLimit =
                CalculateAvailableLimit(lastAccount);

            log?.Invoke(
                $"[FazzaLimitReadiness] Account verification " +
                $"{attempt}/{options.VerificationAttempts}: " +
                $"Phone={accountSession.PhoneNumber}, " +
                $"AvailableLimit={lastAvailableLimit}, " +
                $"RequiredAvailableLimit={requiredAvailableLimit}.");

            if (lastAvailableLimit >= requiredAvailableLimit)
                return lastAccount;

            if (attempt < options.VerificationAttempts)
            {
                await Task.Delay(
                    options.VerificationDelay);
            }
        }

        throw new InvalidOperationException(
            $"Fazza limit for account '{accountSession.PhoneNumber}' " +
            $"was updated, but the required available limit was not " +
            $"observed. Current available limit: {lastAvailableLimit}, " +
            $"Required: {requiredAvailableLimit}.");
    }

    /// <summary>
    /// التحقق من خيارات تجهيز السقف.
    /// </summary>
    private static void ValidateOptions(
        FazzaLimitReadinessOptions options)
    {
        if (options.RequiredOperatorAvailableLimit < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.RequiredOperatorAvailableLimit),
                "RequiredOperatorAvailableLimit cannot be negative.");
        }

        if (options.RequiredBusinessAvailableLimit < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.RequiredBusinessAvailableLimit),
                "RequiredBusinessAvailableLimit cannot be negative.");
        }

        if (options.LimitSafetyMargin < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.LimitSafetyMargin),
                "LimitSafetyMargin cannot be negative.");
        }

        if (options.VerificationAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.VerificationAttempts),
                "VerificationAttempts must be greater than zero.");
        }

        if (options.VerificationDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.VerificationDelay),
                "VerificationDelay cannot be negative.");
        }
    }

    /// <summary>
    /// التحقق من بيانات الجلسات المطلوبة.
    /// </summary>
    private static void ValidateSessions(
        TestSession dashboardSession,
        TestSession operatorSession,
        TestSession businessSession)
    {
        ArgumentNullException.ThrowIfNull(dashboardSession);
        ArgumentNullException.ThrowIfNull(operatorSession);
        ArgumentNullException.ThrowIfNull(businessSession);

        ValidateRequiredValue(
            dashboardSession.UserKey,
            nameof(dashboardSession.UserKey),
            "dashboard session");

        ValidateRequiredValue(
            dashboardSession.Token,
            nameof(dashboardSession.Token),
            "dashboard session");

        ValidateAccountSession(
            operatorSession,
            "operator session");

        ValidateAccountSession(
            businessSession,
            "business session");
    }

    /// <summary>
    /// التحقق من أن جلسة الحساب تحتوي على:
    /// PhoneNumber وAccountId وRegionId.
    /// </summary>
    private static void ValidateAccountSession(
        TestSession session,
        string sessionName)
    {
        ValidateRequiredValue(
            session.PhoneNumber,
            nameof(session.PhoneNumber),
            sessionName);

        ValidateRequiredValue(
            session.AccountId,
            nameof(session.AccountId),
            sessionName);

        ValidateRequiredGuid(
            session.AccountId,
            nameof(session.AccountId),
            sessionName);

        ValidateRequiredValue(
            session.RegionId,
            nameof(session.RegionId),
            sessionName);

        ValidateRequiredGuid(
            session.RegionId,
            nameof(session.RegionId),
            sessionName);
    }

    /// <summary>
    /// التحقق من القيم القادمة من API.
    /// </summary>
    private static void ValidateAccountValues(
        SulfaAccount account,
        TestSession accountSession)
    {
        if (account.MaxFazaaDebtLimit < 0m)
        {
            throw new InvalidOperationException(
                $"Account '{accountSession.PhoneNumber}' returned a " +
                $"negative MaxFazaaDebtLimit: " +
                $"{account.MaxFazaaDebtLimit}.");
        }

        if (account.ConfirmedDebt < 0m)
        {
            throw new InvalidOperationException(
                $"Account '{accountSession.PhoneNumber}' returned a " +
                $"negative ConfirmedDebt: {account.ConfirmedDebt}.");
        }

        /*
         * وجود ConfirmedDebt أكبر من MaxLimit يعني أن بيانات الحساب
         * غير منطقية أو أن الحساب تجاوز سقفه.
         *
         * لا نوقف التنفيذ هنا؛ لأن وظيفة هذا الـFlow هي إصلاح
         * السقف غير الكافي. سيحسب AvailableLimit كقيمة سالبة،
         * ثم يرفع السقف إلى القيمة المطلوبة.
         */
    }

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

    private static void ValidateRequiredGuid(
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
    /// مطابقة الحساب باستخدام AccountId.
    /// </summary>
    private static bool IsAccountIdMatch(
        SulfaAccount account,
        TestSession accountSession)
    {
        if (string.IsNullOrWhiteSpace(account.Id) ||
            string.IsNullOrWhiteSpace(accountSession.AccountId))
        {
            return false;
        }

        return AreSameIdentifiers(
            account.Id,
            accountSession.AccountId);
    }

    /// <summary>
    /// مطابقة الحساب باستخدام رقم الهاتف كخيار احتياطي.
    /// </summary>
    private static bool IsPhoneNumberMatch(
        SulfaAccount account,
        TestSession accountSession)
    {
        var accountPhone =
            NormalizePhoneNumber(account.Phone);

        var sessionPhone =
            NormalizePhoneNumber(accountSession.PhoneNumber);

        return !string.IsNullOrWhiteSpace(accountPhone) &&
               accountPhone == sessionPhone;
    }

    /// <summary>
    /// مقارنة معرفين سواء كانا Guid أو نصًا.
    /// </summary>
    private static bool AreSameIdentifiers(
        string? first,
        string? second)
    {
        if (string.IsNullOrWhiteSpace(first) ||
            string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        if (Guid.TryParse(first, out var firstGuid) &&
            Guid.TryParse(second, out var secondGuid))
        {
            return firstGuid == secondGuid;
        }

        return string.Equals(
            first.Trim(),
            second.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// توحيد رقم الهاتف بإزالة جميع الرموز وإبقاء الأرقام فقط.
    /// </summary>
    private static string NormalizePhoneNumber(
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var result = new StringBuilder();

        foreach (var character in phoneNumber)
        {
            if (char.IsDigit(character))
                result.Append(character);
        }

        return result.ToString();
    }

    /// <summary>
    /// نتيجة داخلية لتجهيز حساب واحد.
    /// لا تحتاج إلى وضعها في ملف Models العام.
    /// </summary>
    private sealed record AccountLimitPreparationResult(
        decimal AvailableLimit,
        bool WasUpdated);
}