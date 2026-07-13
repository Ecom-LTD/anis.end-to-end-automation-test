using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.FazzaTopup.Models
{
    public class RegionSulfaFullData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("code")]
        public string Code { get; set; } = "";

        [JsonProperty("fazaaMaxLimit")]
        public decimal FazaaMaxLimit { get; set; }

        [JsonProperty("totalAllocatedFazaaAmount")]
        public decimal TotalAllocatedFazaaAmount { get; set; }

        [JsonProperty("currentFazaaDebt")]
        public decimal CurrentFazaaDebt { get; set; }

        [JsonProperty("currentSulfaDebt")]
        public decimal CurrentSulfaDebt { get; set; }

        [JsonProperty("currentDebt")]
        public decimal CurrentDebt { get; set; }
    }
    public class RegionSulfaRequest
    {
        [JsonProperty("regionId")]
        public string RegionId { get; set; } = string.Empty;

        [JsonProperty("maxFazaaLimit")]
        public decimal MaxFazaaLimit { get; set; }
    }

    public class RegionSulfaResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
