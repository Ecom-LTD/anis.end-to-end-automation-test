using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Transfer.Endpoints;
using Automation.Framework.Services.Transfer.Models;

namespace Automation.Framework.Services.Transfer.Client
{
    public class TransferClient
    {
        private readonly ApiClient _api;
        public TransferClient(ApiClient api) => _api = api;

        public async Task<TransferResponse> TransferAsync(string token, TransferRequest request)
            => (await _api.PostAsync<TransferRequest, TransferResponse>(
                    ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                    TransferEndpoints.Transfer, request, token)).Data;
    }
}
