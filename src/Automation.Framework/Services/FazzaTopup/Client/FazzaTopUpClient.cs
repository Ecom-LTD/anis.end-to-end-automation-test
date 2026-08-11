using Automation.Framework.Core.Http;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Services.FazzaTopup.Endpoint;
using Automation.Framework.Configuration;
namespace Automation.Framework.Services.FazzaTopup.Client
{
    public class FazzaTopUpClient
    {
        private readonly ApiClient _api;

        public FazzaTopUpClient(ApiClient api)
        {
            _api = api;
        }

        public async Task<string> SetFazzaDeptMaxLimitAsync(
            string token, SetAccountFazzaDeptMaxLimitRequest request)
        {
            var response =
                await _api.PostAsync<SetAccountFazzaDeptMaxLimitRequest, SetFazzaDeptMaxLimitResponse>(
                    ConfigurationManager
                        .Settings
                        .ApiSettings
                        .FinOpsManagementUrl,
                        FazzatopupEndpoint.SetFazzaDeptMaxLimit(),
                        request,
                        token);

            return response.Data.Message;
        }

        // ✅ دالة جديدة: طلب سلفة (Sulfa Request)
        public async Task<string> RequestSulfaAsync(
            string token,
            SulfaRequest request)
        {
            // ✅ إضافة PinNumber في Headers
            var headers = new Dictionary<string, string> { { "Pin-Number", request.PinNumber ?? "001100" } };

            // ✅ استخدام PostWithHeadersAsync بدلاً من PostAsync
            var response = await _api.PostWithHeadersAsync<SulfaRequest, SulfaResponse>(
                ConfigurationManager.Settings.ApiSettings.FinOpsConsumersUrl,
                FazzatopupEndpoint.RequestSulfa(),
                request,
                token,
                headers);

            return response.Data.Message;
        }

        // ✅ دالة جديدة: جلب حسابات السلفة
        public async Task<List<SulfaAccount>> GetSulfaAccountsAsync(
            string token, string phone)
        {
            var response =
                await _api.GetAsync<SulfaAccountResponse>(
                    ConfigurationManager
                        .Settings
                        .ApiSettings
                        .FinOpsManagementUrl,
                        FazzatopupEndpoint.FilterSulfaAccounts(phone),
                        token);


            // ✅ التحقق من صحة الاستجابة
            if (response?.Data == null)
                return new List<SulfaAccount>();

            return response.Data.Results ?? new List<SulfaAccount>();
        }

        // ✅ تعديل عدد طلبات السلف الإضافية (زيادة/تخفيض)
        public async Task<string> ChangeSulfaExtraRequestCountAsync(
            string token, ChangeSulfaExtraRequestCountRequest request)
        {
            var response =
                await _api.PostAsync<ChangeSulfaExtraRequestCountRequest, SulfaLimitOperationResponse>(
                    ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
                    FazzatopupEndpoint.ChangeSulfaExtraRequestCount(),
                    request,
                    token);

            return response.Data.Message;
        }

        // ✅ التمديد الدائم لوقت السلف (زيادة/تخفيض)
        public async Task<string> SetSulfaExtraGracePeriodAsync(
            string token, SetSulfaExtraGracePeriodRequest request)
        {
            var response =
                await _api.PostAsync<SetSulfaExtraGracePeriodRequest, SulfaLimitOperationResponse>(
                    ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
                    FazzatopupEndpoint.SetSulfaExtraGracePeriod(),
                    request,
                    token);

            return response.Data.Message;
        }

        // ✅ التمديد المؤقت لوقت السلف (إضافة فقط)
        public async Task<string> AddSulfaProvisionalExtraGracePeriodAsync(
            string token, AddSulfaProvisionalExtraGracePeriodRequest request)
        {
            var response =
                await _api.PostAsync<AddSulfaProvisionalExtraGracePeriodRequest, SulfaLimitOperationResponse>(
                    ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
                    FazzatopupEndpoint.AddSulfaProvisionalExtraGracePeriod(),
                    request,
                    token);

            return response.Data.Message;
        }

        // ========== ✅ دوال المنطقة (Region) الجديدة ==========

        /// <summary>
        /// جلب بيانات المنطقة الكاملة
        /// </summary>
        //public async Task<RegionSulfaFullData?> GetRegionSulfaDataAsync(
        //    string token,
        //    string regionId)
        //{
        //    var response = await _api.GetAsync<RegionSulfaFullData>(
        //  ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
        //  FazzatopupEndpoint.GetRegionSulfaData(regionId),
        //  token);

        //    return response?.Data;
        //}
        public async Task<List<RegionSulfaFullData>?> GetRegionSulfaDataAsync(
                 string token,
                 string regionId)
        {
            var response = await _api.GetAsync<List<RegionSulfaFullData>>(
                ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
                FazzatopupEndpoint.GetRegionSulfaData(regionId),
                token);

            return response?.Data;
        }

        /// <summary>
        /// تعيين سقف الفزعة للمنطقة باستخدام الموديلات
        /// </summary>
        public async Task<RegionSulfaResponse> SetRegionMaxFazaaLimitAsync(
            string token,
            string regionId,
            decimal maxLimit)
        {
            var request = new RegionSulfaRequest
            {
                RegionId = regionId,
                MaxFazaaLimit = maxLimit
            };

            var response = await _api.PostAsync<RegionSulfaRequest, RegionSulfaResponse>(
                ConfigurationManager.Settings.ApiSettings.FinOpsManagementUrl,
                FazzatopupEndpoint.SetRegionMaxFazaaLimit(),
                request,
                token);

            return response.Data;
        }
    }
}