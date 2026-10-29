using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.ForeignTransfer.Endpoints;
using Automation.Framework.Services.ForeignTransfer.Models;

namespace Automation.Framework.Services.ForeignTransfer.Client
{
    public class ForeignTransferClient
    {
        private readonly ApiClient _api;

        public ForeignTransferClient(ApiClient api)
        {
            _api = api;
        }

        public async Task<ForeignTransferResponse> GetForeignTransfersAsync(string token)
        {
            // بما أن الـ Request لا يحتوي على Body، نستخدم GetAsync
            // لاحظ أننا نستخدم BaseUrl الخاص بـ Cards Store (الذي عادة يكون GatewayUrl أو URL مخصص)
            var response = await _api.GetAsync<ForeignTransferResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisCardUrl,
                ForeignTransferEndpoints.GetTransfers,
                token);

            return response.Data;
        }
    }
}