using Automation.Framework.Context;
using Automation.Framework.Services.OperatorCashback.Client;
using Automation.Framework.Services.OperatorCashback.Models;

namespace Automation.Framework.Services.OperatorCashback.Flow
{
    public class OperatorCashbackFlow
    {
        private readonly OperatorCashbackReportClient _reportClient;
        private readonly StateManager _state;


        public OperatorCashbackFlow(OperatorCashbackReportClient operatorCashbackReportClient, StateManager state)
        {
            _reportClient = operatorCashbackReportClient;
            _state = state;
        }

        /// <summary>
        /// جلب تقرير الكاش باك الأسبوعي للمشغل
        /// </summary>
        public async Task<WeeklyOperatorCashbackReportResponse> GetWeeklyReportAsync(
            string userKey,
            string phone,
            DateTime fromDate,
            DateTime toDate,
            string? locationId = null,
            int currentPage = 1,
            int pageSize = 25)
        {
            var token = _state.GetToken(userKey);

            var request = new OperatorCashbackReportRequest
            {
                LocationId = locationId,
                FromDate = fromDate.ToString("yyyy-MM-dd"),
                ToDate = toDate.ToString("yyyy-MM-dd"),
                CurrentPage = currentPage,
                PageSize = pageSize,
                Phone = phone
            };

            return await _reportClient.GetWeeklyReportAsync(token, request);
        }
        /// <summary>
        /// جلب تقرير الكاش باك الشهري للمشغل
        /// </summary>
        public async Task<MonthlyOperatorCashbackReportResponse> GetMonthlyReportAsync(
            string userKey,
            string phone,
            DateTime fromDate,
            DateTime toDate,
            string? locationId = null,
            int currentPage = 1,
            int pageSize = 25)
        {
            var token = _state.GetToken(userKey);

            var request = new OperatorCashbackReportRequest
            {
                LocationId = locationId,
                FromDate = fromDate.ToString("yyyy-MM-dd"),
                ToDate = toDate.ToString("yyyy-MM-dd"),
                CurrentPage = currentPage,
                PageSize = pageSize,
                Phone = phone
            };

            return await _reportClient.GetMonthlyReportAsync(token, request);
        }

        /// <summary>
        /// جلب قيمة الكاش باك لآخر تقرير
        /// </summary>
        public async Task<decimal> GetLatestCashbackValueAsync(string userKey, string phone)
        {
            var toDate = DateTime.Now;
            var fromDate = toDate.AddDays(-7);

            var response = await GetWeeklyReportAsync(userKey, phone, fromDate, toDate);

            if (response.Data?.Results == null || response.Data.Results.Count == 0)
                return 0;

            return response.Data.Results.First().CashbackValue;
        }

        /// <summary>
        /// جلب إجمالي قيمة الخصم (Debit) لآخر تقرير
        /// </summary>
        public async Task<decimal> GetLatestDebitValueAsync(string userKey, string phone)
        {
            var toDate = DateTime.Now;
            var fromDate = toDate.AddDays(-7);

            var response = await GetWeeklyReportAsync(userKey, phone, fromDate, toDate);

            if (response.Data?.Results == null || response.Data.Results.Count == 0)
                return 0;

            return response.Data.Results.First().DebitValue;
        }
    }

}

