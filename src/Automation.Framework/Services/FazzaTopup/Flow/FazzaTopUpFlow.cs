using Automation.Framework.Context;
using Automation.Framework.Services.FazzaTopup.Client;
using Automation.Framework.Services.FazzaTopup.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Automation.Framework.Services.FazzaTopup.Flow
{
   public class FazzaTopUpFlow
    {
        private readonly FazzaTopUpClient _fazzaTopUpClient;
        private readonly StateManager _state;


        public FazzaTopUpFlow(
            FazzaTopUpClient fazzaTopUpClient,
            StateManager state)
        {
            _fazzaTopUpClient = fazzaTopUpClient;
            _state = state;

        }

        public async Task<string> SetAccountFazzaDeptMaxLimitAsync(
            string userKey,
            Guid accountId,
            decimal maxLimit)
        {
            // 1. قراءة token
            var token = _state.GetToken(userKey);

            // 2. استدعاء API

            var request = new SetAccountFazzaDeptMaxLimitRequest
            {
                AccountId = accountId,
                MaxFazaaLimit = maxLimit
            };

            var response =
                await _fazzaTopUpClient.SetFazzaDeptMaxLimitAsync(
                    token, request);

            return response ?? "Failed to set Fazza dept max limit.";
        }

        // ✅ دالة جديدة: طلب سلفة
        public async Task<string> RequestSulfaAsync(
            string userKey,
            decimal paidValue,
            string recipientSubscriptionId,
            string walletId)
        {
            var token = _state.GetToken(userKey);

            var request = new SulfaRequest
            {
                PaidValue = paidValue,
                SulfaRecipientSubscriptionId = recipientSubscriptionId,
                WalletId = walletId,
                PinNumber = "001100"
            };

            var response = await _fazzaTopUpClient.RequestSulfaAsync(token, request);
            return response;
        }

        // ✅ دالة جديدة: جلب حسابات السلفة
        public async Task<List<SulfaAccount>> GetSulfaAccountsAsync(
            string userKey,
            string phone)
        {
            // ✅ التحقق من صحة المدخلات
            if (string.IsNullOrEmpty(userKey))
                throw new ArgumentException("UserKey cannot be null or empty");

            if (string.IsNullOrEmpty(phone))
                throw new ArgumentException("Phone cannot be null or empty");
            var token = _state.GetToken(userKey);


            // ✅ إضافة التحقق من وجود التوكن
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception($"Token not found for user: {userKey}. Please ensure user is logged in.");
            }
            return await _fazzaTopUpClient.GetSulfaAccountsAsync(token, phone);
        }

        // ========== ✅ دوال المنطقة (Region) ==========

        /// <summary>
        /// جلب بيانات المنطقة الكاملة
        /// </summary>
        //public async Task<RegionSulfaFullData?> GetRegionFullDataAsync(
        //    string token,
        //    string regionId)
        //{
        //    var response = await _fazzaTopUpClient.GetRegionSulfaDataAsync(token, regionId);

        //    // 2. التحقق من وجود البيانات
        //    if (response == null)
        //        return null;

        //    // 3. التحقق من البيانات الأساسية
        //    if (string.IsNullOrEmpty(response.Id))
        //        return null;

        //    // 4. إرجاع البيانات (جاهزة للاستخدام)
        //    return response; 

        //}


        /// <summary>
        /// ✅ جلب بيانات المنطقة الكاملة (تأخذ العنصر الأول من الـ List)
        /// </summary>
        public async Task<RegionSulfaFullData?> GetRegionFullDataAsync(
            string token,
            string regionId)
        {
            var response = await _fazzaTopUpClient.GetRegionSulfaDataAsync(token, regionId);

            if (response == null || response.Count == 0)
                return null;

            // ✅ إرجاع العنصر الأول (لأن الـ API يعيد Array)
            return response[0];
        }

        /// <summary>
        /// تعيين سقف الفزعة للمنطقة
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

            return await _fazzaTopUpClient.SetRegionMaxFazaaLimitAsync(token, regionId, maxLimit);
        }

        /// <summary>
        /// جلب الحد الأقصى للمنطقة
        /// </summary>
        public async Task<decimal> GetRegionMaxFazaaLimitAsync(string token, string regionId)
        {
            var data = await GetRegionFullDataAsync(token, regionId);
            return data?.FazaaMaxLimit ?? 0;
        }

        /// <summary>
        /// جلب إجمالي المبالغ المخصصة للمنطقة
        /// </summary>
        public async Task<decimal> GetTotalAllocatedFazaaAmountAsync(string token, string regionId)
        {
            var data = await GetRegionFullDataAsync(token, regionId);
            return data?.TotalAllocatedFazaaAmount ?? 0;
        }

        /// <summary>
        /// حساب الحد المتاح للمنطقة
        /// </summary>
        public async Task<decimal> GetRegionAvailableLimitAsync(string token, string regionId)
        {
            var maxLimit = await GetRegionMaxFazaaLimitAsync(token, regionId);
            var allocated = await GetTotalAllocatedFazaaAmountAsync(token, regionId);
            return maxLimit - allocated;
        }
    }
}
