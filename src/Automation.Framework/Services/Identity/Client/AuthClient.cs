using Automation.Framework.Configuration;
using Automation.Framework.Core.Http;
using Automation.Framework.Services.Identity.Endpoints;
using Automation.Framework.Services.Identity.Models;

namespace Automation.Framework.Services.Identity.Client
{
    public class AuthClient
    {
        private readonly ApiClient _api;
        public AuthClient(ApiClient api) => _api = api;

        public async Task<LoginResponse> LoginAsync(string phoneNumber)
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "phone_v2",
                ["client_id"] = "anis-app-v3",
                ["client_secret"] = "CGCHCKHCghchgEKMKL878ghjyZSJNL45CFXRZfd",
                ["number"] = phoneNumber,
                ["code"] = "321654"
            };

            var resp = await _api.PostFormAsync<LoginResponse>(
                ConfigurationManager.Settings.ApiSettings.IdentityUrl + IdentityEndpoints.Token, form);

            return resp.Data;
        }
    }
}
