using Automation.Framework.Context;
using Automation.Framework.Services.CashFlowReport.Client;
using Automation.Framework.Services.CashFlowReport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.CashFlowReport.Flow
{

    public class CashflowReportFlow
    {
        private readonly CashflowReportClient _cashflowReportClient;
        private readonly StateManager _state;

        public CashflowReportFlow(CashflowReportClient cashflowReportClient, StateManager state)
        {
            _cashflowReportClient = cashflowReportClient;
            _state = state;
        }

        /// <summary>
        /// جلب تقرير التدفق النقدي للمستخدم الحالي
        /// </summary>
        public async Task<CashflowReportResponse> GetCashflowReportAsync(
            string userKey,
            string searchText,
            bool isAscending = false)
        {
            var token = _state.GetToken(userKey);
            return await _cashflowReportClient.GetCashflowReportAsync(token, searchText, isAscending);
        }

        /// <summary>
        /// جلب تقرير التدفق النقدي باستخدام رقم هاتف المستخدم المخزن في State
        /// </summary>
        public async Task<CashflowReportResponse> GetCashflowReportForUserAsync(string userKey)
        {
            var token = _state.GetToken(userKey);
            var phoneNumber = _state.GetValue<string>($"phone_{userKey}"); // أو من UserHelper

            return await _cashflowReportClient.GetCashflowReportByPhoneAsync(token, phoneNumber);
        }

        /// <summary>
        /// استخراج عنصر التقرير الأول من النتائج
        /// </summary>
        public CashflowReportItem? GetFirstResult(CashflowReportResponse response)
        {
            return response?.Results?.FirstOrDefault();
        }

        /// <summary>
        /// التحقق من وجود ديون فزعة أو سلفة مستحقة
        /// </summary>
        public bool HasDueDebt(CashflowReportItem report)
        {
            return report.CurrentFazaaDue > 0 || report.CurrentSulfaDue > 0;
        }
    }
}
