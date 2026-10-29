using Automation.Framework.Context;
using Automation.Framework.Helpers;
using Automation.Framework.Services.FazzaTopup.Client;
using Automation.Framework.Services.FazzaTopup.Models;

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

        public async Task<string> SetAccountFazzaDebtMaxLimitAsync(   // ← Debt بدل Dept
            string userKey,
            Guid accountId,
            decimal maxLimit)
        {
            var token = _state.GetToken(userKey);

            var request = new SetAccountFazzaDeptMaxLimitRequest   // ← اسم الموديل لم يُغيَّر (اختياري)
            {
                AccountId = accountId,
                MaxFazaaLimit = maxLimit
            };

            var response = await _fazzaTopUpClient.SetFazzaDeptMaxLimitAsync(token, request);

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidOperationException(
                    "Failed to set Fazza account maximum debt limit. " +
                    "The API returned an empty response.");
            }

            return response;
        }

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

        public async Task<List<SulfaAccount>> GetSulfaAccountsAsync(
            string userKey,
            string phone)
        {
            if (string.IsNullOrEmpty(userKey))
                throw new ArgumentException("UserKey cannot be null or empty");

            if (string.IsNullOrEmpty(phone))
                throw new ArgumentException("Phone cannot be null or empty");

            var token = _state.GetToken(userKey);

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(
                    $"Token not found for user: {userKey}. Please ensure user is logged in.");
            }

            return await _fazzaTopUpClient.GetSulfaAccountsAsync(token, phone);
        }

        public async Task<string> ChangeSulfaExtraRequestCountAsync(
            string userKey,
            Guid accountId,
            int number,
            bool isReducingExtraCount)
        {
            var token = _state.GetToken(userKey);

            var request = new ChangeSulfaExtraRequestCountRequest
            {
                AccountId = accountId,
                Number = number,
                IsReducingExtraCount = isReducingExtraCount
            };

            var response = await _fazzaTopUpClient.ChangeSulfaExtraRequestCountAsync(token, request);
            if (string.IsNullOrWhiteSpace(response))   // ← إصلاح
                throw new InvalidOperationException(
                    "ChangeSulfaExtraRequestCount returned an empty response.");
            return response;
        }

        public async Task<string> SetSulfaExtraGracePeriodAsync(
            string userKey,
            Guid accountId,
            int hours,
            bool isReducingExtraGracePeriod)
        {
            var token = _state.GetToken(userKey);

            var request = new SetSulfaExtraGracePeriodRequest
            {
                AccountId = accountId,
                Hours = hours,
                IsReducingExtraGracePeriod = isReducingExtraGracePeriod
            };

            var response = await _fazzaTopUpClient.SetSulfaExtraGracePeriodAsync(token, request);
            if (string.IsNullOrWhiteSpace(response))   // ← إصلاح
                throw new InvalidOperationException(
                    "SetSulfaExtraGracePeriod returned an empty response.");
            return response;
        }

        public async Task<string> AddSulfaProvisionalExtraGracePeriodAsync(
            string userKey,
            Guid accountId,
            int hours)
        {
            var token = _state.GetToken(userKey);

            var request = new AddSulfaProvisionalExtraGracePeriodRequest
            {
                AccountId = accountId,
                Hours = hours
            };

            var response = await _fazzaTopUpClient.AddSulfaProvisionalExtraGracePeriodAsync(token, request);
            if (string.IsNullOrWhiteSpace(response))   // ← إصلاح
                throw new InvalidOperationException(
                    "AddSulfaProvisionalExtraGracePeriod returned an empty response.");
            return response;
        }

        /// <summary>
        /// يجلب حساب السلفة المطابق (AccountId أولًا ثم الهاتف). المصدر الوحيد لهذه المطابقة،
        /// يعيد null عند عدم وجود تطابق ليقرر المستدعي كيف يفشل.
        /// </summary>
        public async Task<SulfaAccount?> FindAccountAsync(string userKey, string phone, string? accountId)
        {
            var accounts = await GetSulfaAccountsAsync(userKey, phone);
            if (accounts is null || accounts.Count == 0) return null;

            return accounts.FirstOrDefault(a => AccountMatcher.IdsMatch(a.Id, accountId))
                ?? accounts.FirstOrDefault(a => PhoneNormalizer.AreEqual(a.Phone, phone));
        }

        public async Task<RegionSulfaFullData?> GetRegionFullDataAsync(
            string userKey,      // ← userKey بدل token
            string regionId)
        {
            var token = _state.GetToken(userKey);
            var response = await _fazzaTopUpClient.GetRegionSulfaDataAsync(token, regionId);

            if (response is null || response.Count == 0)
                return null;

            return response.FirstOrDefault(region =>
                string.Equals(region.Id, regionId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<RegionSulfaResponse> SetRegionMaxFazaaLimitAsync(
            string userKey,      // ← userKey بدل token
            string regionId,
            decimal maxLimit)
        {
            var token = _state.GetToken(userKey);
            return await _fazzaTopUpClient.SetRegionMaxFazaaLimitAsync(token, regionId, maxLimit);
        }

        public async Task<decimal> GetRegionMaxFazaaLimitAsync(string userKey, string regionId)
        {
            var data = await GetRegionFullDataAsync(userKey, regionId);
            return data?.FazaaMaxLimit ?? 0;
        }

        public async Task<decimal> GetTotalAllocatedFazaaAmountAsync(string userKey, string regionId)
        {
            var data = await GetRegionFullDataAsync(userKey, regionId);
            return data?.TotalAllocatedFazaaAmount ?? 0;
        }

        public async Task<decimal> GetRegionAvailableLimitAsync(string userKey, string regionId)
        {
            var maxLimit = await GetRegionMaxFazaaLimitAsync(userKey, regionId);
            var allocated = await GetTotalAllocatedFazaaAmountAsync(userKey, regionId);
            return maxLimit - allocated;
        }
    }
}