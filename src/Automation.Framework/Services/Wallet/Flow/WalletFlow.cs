using Automation.Framework.Constants;
using Automation.Framework.Context;
using Automation.Framework.Core.Enums;
using Automation.Framework.Services.Wallet.Client;

namespace Automation.Framework.Services.Wallet.Flow;

public class WalletFlow
{
    private readonly WalletClient _walletClient;
    private readonly StateManager _state;

    public WalletFlow(WalletClient walletClient, StateManager state)
    {
        _walletClient = walletClient;
        _state = state;
    }

    /// <summary>جلب (WalletId, SubscriptionId, RegionId) دفعة واحدة وتخزينها في State.</summary>
    public async Task<(Guid WalletId, Guid SubscriptionId, Guid RegionId)> GetAllIdsAsync(
        string userKey, CurrencyType currencyType, string regionName, string holderName,
        SubscriptionType subscriptionType, string subscriptionName)
    {
        var token = _state.GetToken(userKey);
        var profile = await _walletClient.GetProfileAsync(token);

        var subscription = profile.Data.Subscriptions.FirstOrDefault(s =>
            s.SubscriptionType == subscriptionType && s.SubscriptionName == subscriptionName)
            ?? throw new Exception($"Subscription not found: {subscriptionName}");

        var wallet = subscription.Wallets.FirstOrDefault(w =>
            w.CurrencyType == currencyType && w.RegionName == regionName && w.HolderName == holderName)
            ?? throw new Exception($"Wallet not found for Currency={currencyType}, Region={regionName}");

        _state.SetValue(StateKeys.Wallet(userKey, currencyType), wallet.WalletId);
        _state.SetValue(StateKeys.RegionId(userKey, currencyType), wallet.RegionId);
        _state.SetValue(StateKeys.SubscriptionId(userKey, subscriptionType), subscription.SubscriptionId);

        return (wallet.WalletId, subscription.SubscriptionId, wallet.RegionId);
    }

    public async Task<decimal> GetBalanceAsync(string userKey, Guid walletId)
    {
        var token = _state.GetToken(userKey);
        var resp = await _walletClient.GetBalanceAsync(walletId, token);
        return resp.Data;
    }

    public async Task<string> UpdateDefaultWalletAsync(string userKey, string walletId)
    {
        var token = _state.GetToken(userKey);
        return await _walletClient.UpdateDefaultWalletAsync(token, walletId);
    }
}
