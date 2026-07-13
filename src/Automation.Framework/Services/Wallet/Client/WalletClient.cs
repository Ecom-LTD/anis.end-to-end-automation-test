using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Wallet.Endpoints;
using Automation.Framework.Services.Wallet.Models;

namespace Automation.Framework.Services.Wallet.Client
{
    public class WalletClient
    {
        private readonly ApiClient _api;
        public WalletClient(ApiClient api) => _api = api;

        public async Task<ProfileResponse> GetProfileAsync(string token)
            => (await _api.GetAsync<ProfileResponse>(
                    ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                    WalletEndpoints.Profile, token)).Data;

        public async Task<BalanceResponse> GetBalanceAsync(System.Guid walletId, string token)
            => (await _api.GetAsync<BalanceResponse>(
                    ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                    WalletEndpoints.Balance(walletId), token)).Data;


        public async Task<string> UpdateDefaultWalletAsync(string token, string walletId)
        {
            var request = new UpdateDefaultWalletRequest { WalletId = walletId };

            var response = await _api.PatchAsync<UpdateDefaultWalletRequest, object>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                WalletEndpoints.UpdateDefaultWallet,
                request,
                token);

            return "Wallet updated successfully";
        }
    }

}
