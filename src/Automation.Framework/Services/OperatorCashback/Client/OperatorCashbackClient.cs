using Automation.Framework.Core.Http;
using Automation.Framework.Services.OperatorCashback.Models;
using Automation.Framework.Services.OperatorCashback.Endpoint;
using Automation.Framework.Configuration;


namespace Automation.Framework.Services.OperatorCashback.Client
{
    public class OperatorCashbackReportClient
    {
        private readonly ApiClient _api;

        public OperatorCashbackReportClient(ApiClient api)
        {
            _api = api;
        }

        /// <summary>
        /// جلب تقرير الكاش باك الأسبوعي للمشغل
        /// </summary>
        public async Task<WeeklyOperatorCashbackReportResponse> GetWeeklyReportAsync(
            string token,
            OperatorCashbackReportRequest request)
        {
            // بناء Query String من الـ Request
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.LocationId))
                queryParams.Add($"LocationId={request.LocationId}");

            queryParams.Add($"FromDate={request.FromDate}");
            queryParams.Add($"ToDate={request.ToDate}");
            queryParams.Add($"CurrentPage={request.CurrentPage}");
            queryParams.Add($"PageSize={request.PageSize}");
            queryParams.Add($"Phone={request.Phone}");

            var queryString = string.Join("&", queryParams);
            var url = $"{OperatorCashbackEndpoint.GetWeeklyOperatorCashbackReport}?{queryString}";

            var response = await _api.GetAsync<WeeklyOperatorCashbackReportResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                url,
                token);

            return response.Data;
        }

        /// <summary>
        /// جلب تقرير الكاش باك الشهري للمشغل
        /// </summary>
        public async Task<MonthlyOperatorCashbackReportResponse> GetMonthlyReportAsync(
            string token,
            OperatorCashbackReportRequest request)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.LocationId))
                queryParams.Add($"LocationId={request.LocationId}");

            queryParams.Add($"FromDate={request.FromDate}");
            queryParams.Add($"ToDate={request.ToDate}");
            queryParams.Add($"CurrentPage={request.CurrentPage}");
            queryParams.Add($"PageSize={request.PageSize}");
            queryParams.Add($"Phone={request.Phone}");

            var queryString = string.Join("&", queryParams);
            var url = $"{OperatorCashbackEndpoint.GetMonthlyOperatorCashbackReport}?{queryString}";

            var response = await _api.GetAsync<MonthlyOperatorCashbackReportResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                url,
                token);

            return response.Data;
        }
    }
}
