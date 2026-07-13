using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Almusher.Endpoint;
using Automation.Framework.Services.Almusher.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;
namespace Automation.Framework.Services.Almusher.Client
{
    public class AlMusheerClient
    {
        private readonly ApiClient _api;

        public AlMusheerClient(ApiClient api)
        {
            _api = api;
        }

        public async Task<CreatePaymentChainResponse> CreatePaymentChainAsync(string token, Guid id, string reference)
        {
            var body = new
            {
                id,
                reference,
                statement = "Automated E2E Test"
            };

            var response = await _api.PostAsync<object, CreatePaymentChainResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.InitiatePaymentChain,
                body,
                token);

            return response.Data;
        }

        /// <summary>
        /// تنفيذ صرف العملات العادي في سلسلة الدفع
        /// </summary>
        public async Task<RegularCurrencyExchangeResponse> RegularCurrencyExchangeAsync(
            string token,
            string chainId,
            RegularCurrencyExchangeRequest request)
        {
            var response = await _api.PostAsync<RegularCurrencyExchangeRequest, RegularCurrencyExchangeResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                AlmusherEndpoint.RegularCurrencyExchange(Guid.Parse(chainId)),
                request,
                token);

            return response.Data;
        }
    }
}
