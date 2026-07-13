using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Region.Model
{
    public class RegionsResponse
    {
        public List<RegionData> Data { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class RegionData
    {
        public string Id { get; set; } = string.Empty;

        [JsonProperty("englishName")]
        public string EnglishName { get; set; } = string.Empty;

        [JsonProperty("arabicName")]
        public string ArabicName { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
