using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.FazzaTopup.Models
{
    public class SulfaRequest

    {
        [JsonProperty("key")]
        public string Key { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("paidValue")]
        public decimal PaidValue { get; set; }

        [JsonProperty("sulfaRecipientSubscriptionId")]
        public string SulfaRecipientSubscriptionId { get; set; } = string.Empty;

        [JsonProperty("walletId")]
        public string WalletId { get; set; } = string.Empty;

        [JsonProperty("pinNumber")]
        public string PinNumber { get; set; } = "001100";

    }
    public class SulfaResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
