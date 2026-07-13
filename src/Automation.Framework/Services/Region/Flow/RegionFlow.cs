using Automation.Framework.Context;
using Automation.Framework.Services.Region.Client;


namespace Automation.Framework.Services.Region.Flow
{
    public class RegionFlow
    {
        private readonly RegionClient _regionClient;
        private readonly StateManager _state;

        public RegionFlow(RegionClient regionClient, StateManager state)
        {
            _regionClient = regionClient;
            _state = state;
        }

        public async Task<string> GetRegionIdByNameAsync(string userKey, string regionName)
        {
            var token = _state.GetToken(userKey);

            var regions = await _regionClient.GetAllRegionsAsync(token);

            var region = regions.FirstOrDefault(r =>
                r.EnglishName == regionName ||
                r.ArabicName == regionName);

            if (region == null)
                throw new Exception($"لم يتم العثور على المنطقة: {regionName}");

            return region.Id;
        }
    }
}
