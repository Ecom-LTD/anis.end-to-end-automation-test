using Newtonsoft.Json;

namespace Automation.Framework.Services.Transfer.Models
{



        public class TransferRequest
        {
            public string WalletId { get; set; } = string.Empty;
            public string SubscriptionId { get; set; } = string.Empty;
            public decimal Value { get; set; }
            public string DestinationRegionId { get; set; } = string.Empty;
            public string PinNumber { get; set; } = "001100";
       

             public string Note { get; set; } = "string";


           public string DetailedStatement { get; set; } = "string";
             public int AnisCardOperationType { get; set; } = 0;

    }

        public class TransferResponse
        {
            public string Message { get; set; } = string.Empty;
        public bool Success =>
           !string.IsNullOrEmpty(Message) &&
           (Message.Contains("succeed", StringComparison.OrdinalIgnoreCase) ||
            Message.Contains("success", StringComparison.OrdinalIgnoreCase));

    }
    }
//public class TransferRequest
//{
//    [JsonProperty("walletId")]
//    public string WalletId { get; set; } = string.Empty;

//    [JsonProperty("subscriptionId")]
//    public string SubscriptionId { get; set; } = string.Empty;

//    [JsonProperty("value")]
//    public decimal Value { get; set; }

//    [JsonProperty("pinNumber")]
//    public string PinNumber { get; set; } = "001100";

//    [JsonProperty("note")]
//    public string Note { get; set; } = "string";

//    [JsonProperty("detailedStatement")]
//    public string DetailedStatement { get; set; } = "string";

//    [JsonProperty("anisCardOperationType")]
//    public int AnisCardOperationType { get; set; } = 0;

//    [JsonProperty("destinationRegionId")]
//    public string DestinationRegionId { get; set; } = string.Empty;
//}

//public class TransferResponse
//{
//    [JsonProperty("message")]
//    public string Message { get; set; } = string.Empty;

//    // ✅ هذه الخاصية هي المفتاح
//    public bool Success =>
//        !string.IsNullOrEmpty(Message) &&
//        Message.Contains("succeed", StringComparison.OrdinalIgnoreCase);
//}