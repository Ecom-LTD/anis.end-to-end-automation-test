using Automation.Framework.Constants;
using Automation.Framework.Context;
using Automation.Framework.Core.Enums;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Wallet.Client;
using Automation.Framework.Services.Wallet.Models;
using Automation.Framework.Services.Region.Flow;

namespace Automation.Framework.Services.Wallet.Flow;

public class WalletFlow
{
    private readonly WalletClient _walletClient;
    private readonly StateManager _state;
    private readonly RegionFlow _regionFlow;

    public WalletFlow(WalletClient walletClient, StateManager state)
    {
        _walletClient = walletClient;
        _state = state;
    }

    public async Task<(Guid WalletId, Guid SubscriptionId, Guid RegionId)> GetAllIdsAsync(
      string userKey,
      CurrencyType currencyType,
      string regionName,
      string holderName,
      SubscriptionType subscriptionType,
      string subscriptionName,
      bool autoCreateIfNotFound = false)  // ✅ افتراضياً = false (لا يؤثر على المشاريع الأخرى)
    {
        var token = _state.GetToken(userKey);
        var profile = await _walletClient.GetProfileAsync(token);

        var subscription = profile.Data.Subscriptions.FirstOrDefault(s =>
            s.SubscriptionType == subscriptionType && s.SubscriptionName == subscriptionName)
            ?? throw new Exception($"Subscription not found: {subscriptionName}");

        var wallet = subscription.Wallets.FirstOrDefault(w =>
            w.CurrencyType == currencyType && w.RegionName == regionName && w.HolderName == holderName);

        // ✅ إذا لم تكن المحفظة موجودة و autoCreateIfNotFound = true → قم بإنشائها
        if (wallet == null && autoCreateIfNotFound)
        {
            var Dashboard = TestUsers.Dashboard;
            // 1. جلب RegionId
            var regionId = await _regionFlow.GetRegionIdByNameAsync(Dashboard, regionName);

            // 2. إنشاء المحفظة
            var createResponse = await CreateWalletAsync(
                userKey,
                subscription.SubscriptionId,
                Guid.Parse(regionId),
                currencyType);

            if (createResponse.StatusCode == System.Net.HttpStatusCode.OK && createResponse.Data.Success)
            {
                // 3. إعادة جلب الـ Profile بعد الإنشاء
                profile = await _walletClient.GetProfileAsync(token);

                // 4. البحث عن المحفظة الجديدة
                subscription = profile.Data.Subscriptions.FirstOrDefault(s =>
                    s.SubscriptionType == subscriptionType && s.SubscriptionName == subscriptionName);

                wallet = subscription?.Wallets.FirstOrDefault(w =>
                    w.CurrencyType == currencyType && w.RegionName == regionName && w.HolderName == holderName);

                if (wallet != null)
                {
                    // ✅ تخزين في State
                    _state.SetValue(StateKeys.Wallet(userKey, currencyType), wallet.WalletId);
                    _state.SetValue(StateKeys.RegionId(userKey, currencyType), wallet.RegionId);
                    _state.SetValue(StateKeys.SubscriptionId(userKey, subscriptionType), subscription.SubscriptionId);

                    return (wallet.WalletId, subscription.SubscriptionId, wallet.RegionId);
                }
            }

            throw new Exception($"Failed to create wallet for Currency={currencyType}, Region={regionName}");
        }

        if (wallet == null)
            throw new Exception($"Wallet not found for Currency={currencyType}, Region={regionName}");

        _state.SetValue(StateKeys.Wallet(userKey, currencyType), wallet.WalletId);
        _state.SetValue(StateKeys.RegionId(userKey, currencyType), wallet.RegionId);
        _state.SetValue(StateKeys.SubscriptionId(userKey, subscriptionType), subscription.SubscriptionId);

        return (wallet.WalletId, subscription.SubscriptionId, wallet.RegionId);
    }

    public async Task<decimal> GetBalanceAsync(string userKey, Guid walletId)
    {
        await UpdateDefaultWalletAsync(userKey, walletId.ToString());
        var token = _state.GetToken(userKey);
        var resp = await _walletClient.GetBalanceAsync(walletId, token);
        return resp.Data;
    }

    public async Task<string> UpdateDefaultWalletAsync(string userKey, string walletId)
    {
        var token = _state.GetToken(userKey);
        return await _walletClient.UpdateDefaultWalletAsync(token, walletId);
    }
    // ================================================================
    // ✅ دوال جديدة لجلب المحافظ حسب العملة
    // ================================================================

    /// <summary>
    /// جلب معرف محفظة المستخدم بعملة محددة
    /// </summary>
    public async Task<Guid> GetWalletIdByCurrencyAsync(
        string userKey,
        CurrencyType currencyType,
        string regionName = "Tripoli",
        string holderName = "Cash")
    {
        var token = _state.GetToken(userKey);
        var profile = await _walletClient.GetProfileAsync(token);

        foreach (var subscription in profile.Data.Subscriptions)
        {
            var wallet = subscription.Wallets.FirstOrDefault(w =>
                w.CurrencyType == currencyType &&
                w.RegionName == regionName &&
                w.HolderName == holderName);

            if (wallet != null)
                return wallet.WalletId;
        }

        throw new Exception($"No wallet found for user {userKey} with Currency={currencyType}, Region={regionName}, Holder={holderName}");
    }

    /// <summary>
    /// جلب معرف محفظة المستخدم بعملة محددة وتخزينها في State
    /// </summary>
    public async Task<Guid> GetAndStoreWalletIdByCurrencyAsync(
        string userKey,
        CurrencyType currencyType,
        string regionName = "Tripoli",
        string holderName = "Cash")
    {
        var walletId = await GetWalletIdByCurrencyAsync(userKey, currencyType, regionName, holderName);

        // ✅ تخزين المحفظة في State مع العملة كمفتاح
        _state.SetValue(StateKeys.Wallet(userKey, currencyType), walletId);

        return walletId;
    }

    /// <summary>
    /// جلب جميع محافظ المستخدم (كل العملات)
    /// </summary>
    public async Task<Dictionary<CurrencyType, Guid>> GetAllWalletsAsync(
        string userKey,
        string regionName = "Tripoli",
        string holderName = "Cash")
    {
        var token = _state.GetToken(userKey);
        var profile = await _walletClient.GetProfileAsync(token);
        var result = new Dictionary<CurrencyType, Guid>();

        foreach (var subscription in profile.Data.Subscriptions)
        {
            foreach (var wallet in subscription.Wallets)
            {
                if (wallet.RegionName == regionName && wallet.HolderName == holderName)
                {
                    result[wallet.CurrencyType] = wallet.WalletId;

                    // ✅ تخزين كل محفظة في State
                    _state.SetValue(StateKeys.Wallet(userKey, wallet.CurrencyType), wallet.WalletId);
                }
            }
        }

        if (result.Count == 0)
            throw new Exception($"No wallets found for user {userKey} with Region={regionName}, Holder={holderName}");

        return result;
    }

    /// <summary>
    /// جلب محفظة من State (إذا كانت موجودة) أو جلبها من API
    /// </summary>
    public async Task<Guid> GetOrLoadWalletIdAsync(
        string userKey,
        CurrencyType currencyType,
        string regionName = "Tripoli",
        string holderName = "Cash")
    {
        // ✅ محاولة القراءة من State أولاً
        var key = StateKeys.Wallet(userKey, currencyType);
        if (_state.TryGetValue<Guid>(key, out var cachedWalletId))
        {
            return cachedWalletId;
        }

        // ✅ غير موجودة في State → نجلبها من API
        return await GetAndStoreWalletIdByCurrencyAsync(userKey, currencyType, regionName, holderName);
    }

    // ================================================================
    // ✅ دوال مساعدة للأرصدة
    // ================================================================

    /// <summary>
    /// جلب رصيد محفظة باستخدام العملة (بدون معرف المحفظة)
    /// </summary>
    public async Task<decimal> GetBalanceByCurrencyAsync(
        string userKey,
        CurrencyType currencyType,
        string regionName = "Tripoli",
        string holderName = "Cash")
    {
        var walletId = await GetOrLoadWalletIdAsync(userKey, currencyType, regionName, holderName);
        return await GetBalanceAsync(userKey, walletId);
    }

    /// <summary>
    /// إنشاء محفظة جديدة لمشترك معين
    /// </summary>
    public async Task<ApiResponse<CreateWalletResponse>> CreateWalletAsync(
        string userKey,
        Guid subscriptionId,
        Guid regionId,
        CurrencyType currencyType,
        Guid? holderId = null)
    {
        // 1. جلب التوكن
        var token = _state.GetToken(userKey);

        // 2. بناء الطلب
        var request = new CreateWalletRequest
        {
            SubscriptionId = subscriptionId,
            RegionId = regionId,
            HolderId = holderId,
            CurrencyType = (int)currencyType
        };

        // 3. استدعاء الـ Client (يرجع ApiResponse كاملاً)
        var response = await _walletClient.CreateWalletAsync(token, request);

        // 4. إرجاع ApiResponse كاملاً
        return response;  // ✅ صحيح
    }
    public async Task<ProfileResponse> GetProfileAsync(string userKey)
    {
        var token = _state.GetToken(userKey);
        return await _walletClient.GetProfileAsync(token);
    }

}
