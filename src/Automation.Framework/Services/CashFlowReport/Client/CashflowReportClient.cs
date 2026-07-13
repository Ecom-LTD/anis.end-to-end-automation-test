using Automation.Framework.Core.Http;
using Automation.Framework.Configuration;
using Automation.Framework.Services.CashFlowReport.Endpoint;
using Automation.Framework.Services.CashFlowReport.Models;
namespace Automation.Framework.Services.CashFlowReport.Client
{
    public class CashflowReportClient
    {

        private readonly ApiClient _api;

        public CashflowReportClient(ApiClient api)
        {
            _api = api;
        }


        public async Task<CashflowReportResponse> GetCashflowReportAsync(
       string token,
       string searchText,
       bool isAscending = false,
       int page = 1,
       int pageSize = 25)
        {
            // استخدم FalconsUrl كما في الإعدادات
            var baseUrl = ConfigurationManager.Settings.ApiSettings.FalconsUrl;

            // طبق المسار الثابت
            var path = $"{CashflowReportEndpoint.CashflowReportIndex}?SearchText={Uri.EscapeDataString(searchText)}&IsAscending={isAscending}&Page={page}&PageSize={pageSize}";

            // أرسل الطلب
            var response = await _api.GetAsync<CashflowReportResponse>(baseUrl, path, token);
            return response.Data;
        }
        /// <summary>
        /// جلب تقرير التدفق النقدي لرقم هاتف محدد (طريقة مبسطة)
        /// </summary>
        public async Task<CashflowReportResponse> GetCashflowReportByPhoneAsync(
            string token,
            string phoneNumber)
        {
            return await GetCashflowReportAsync(token, phoneNumber);
        }
    }
}
