using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Almusher.Endpoint;
using Automation.Framework.Services.Almusher.Models;
using Newtonsoft.Json.Linq;

namespace Automation.Framework.Services.Almusher.Client
{
    public class AlMusheerClient
    {
        private readonly ApiClient _api;

        public AlMusheerClient(ApiClient api)
        {
            _api = api;
        }

        // ================================================================
        // ✅ 1. إنشاء سلسلة دفع جديدة
        // ================================================================

        public async Task<ApiResponse<CreatePaymentChainResponse>> CreatePaymentChainAsync(
            string token,
            CreatePaymentChainRequest request)
        {
            var response = await _api.PostAsync<CreatePaymentChainRequest, CreatePaymentChainResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.InitiatePaymentChain,
                request,
                token);

            return response;
        }

        // ================================================================
        // ✅ 2. صرف عملات عادي (Regular Currency Exchange)
        // ================================================================

        public async Task<ApiResponse<RegularCurrencyExchangeResponse>> RegularCurrencyExchangeAsync(
            string token,
            string chainId,
            RegularCurrencyExchangeRequest request)
        {
            var response = await _api.PostAsync<RegularCurrencyExchangeRequest, RegularCurrencyExchangeResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.RegularCurrencyExchange(Guid.Parse(chainId)),
                request,
                token);

            return response;
        }

        // ================================================================
        // ✅ 3. تأكيد صرف العملات
        // ================================================================

        public async Task<ApiResponse<ConfirmCurrencyExchangeResponse>> ConfirmCurrencyExchangeAsync(
            string token,
            string chainId,
            string operationId)
        {
            var response = await _api.PatchAsync<object, ConfirmCurrencyExchangeResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.ConfirmCurrencyExchange(Guid.Parse(chainId), Guid.Parse(operationId)),
                null,
                token);

            return response;
        }

        // ================================================================
        // ✅ 4. جلب تفاصيل صرف العملات
        // ================================================================

        public async Task<ApiResponse<ExchangeDetailsResponse>> GetExchangeDetailsAsync(
            string token,
            string chainId,
            string operationId)
        {
            var response = await _api.GetAsync<ExchangeDetailsResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.GetExchangeDetails(Guid.Parse(chainId), Guid.Parse(operationId)),
                token);

            return response;
        }

        // ================================================================
        // ✅ 5. جلب معلومات متوسط السعر
        // ================================================================

        public async Task<ApiResponse<AverageRateInfoResponse>> GetAverageRateInfoAsync(
            string token,
            string walletId)
        {
            var response = await _api.GetAsync<AverageRateInfoResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.AverageRateInfo(Guid.Parse(walletId)),
                token);

            return response;
        }

        // ================================================================
        // ✅ 6. صرف عملات أجنبية إلى أجنبية (Foreign to Foreign)
        // ================================================================

        public async Task<ApiResponse<ForeignToForeignExchangeResponse>> ForeignToForeignExchangeAsync(
            string token,
            string chainId,
            ForeignToForeignExchangeRequest request)
        {
            var response = await _api.PostAsync<ForeignToForeignExchangeRequest, ForeignToForeignExchangeResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.ForeignToForeignExchange(Guid.Parse(chainId)),
                request,
                token);

            return response;
        }

        // ================================================================
        // ✅ 7. الحصول على نسبة USD
        // ================================================================

        public async Task<ApiResponse<UsdRatioResponse>> GetUsdRatioAsync(
            string token,
            string chainId,
            string operationId)
        {
            var response = await _api.GetAsync<UsdRatioResponse>(
                ConfigurationManager.Settings.ApiSettings.AnisPaymentsUrl,
                AlmusherEndpoint.GetUsdRatio(Guid.Parse(chainId), Guid.Parse(operationId)),
                token);

            return response;
        }
    }
}