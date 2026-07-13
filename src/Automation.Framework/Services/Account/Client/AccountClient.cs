using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Account.Endpoints;
using Automation.Framework.Services.Account.Models;

namespace Automation.Framework.Services.Account.Client
{
    public class AccountClient
    {
        private readonly ApiClient _api;
        public AccountClient(ApiClient api) => _api = api;

        public async Task<Guid> GetAccountByPhoneNumberAsync(string token, string phoneNumber)
        {
            if (string.IsNullOrEmpty(token)) throw new Exception("Token is null or empty");

            var resp = await _api.GetAsync<AccountByPhoneNumberResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                AccountEndpoints.ByPhone(phoneNumber), token);

            var results = resp.Data?.Data?.Results;
            if (results is null || results.Count == 0)
                throw new Exception($"No account found for phone number: {phoneNumber}");

            return results[0].Id;
        }
    }
}
