using Automation.Framework.Core.Http;
using Automation.Framework.Services.Region.Model;
using Automation.Framework.Configuration;
using Automation.Framework.Services.Region.Endpoint;
namespace Automation.Framework.Services.Region.Client

{
    public class RegionClient
    {
        private readonly ApiClient _api;

        public RegionClient(ApiClient api)
        {
            _api = api;
        }



        public async Task<List<RegionData>> GetAllRegionsAsync(string token)
        {
            var response = await _api.GetAsync<RegionsResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                RegionEndpoint.GetAllRegions,
                token);

            return response.Data.Data;
        }
    }
}
