using Newtonsoft.Json;
using Automation.Framework.Services.Catalog.Models;
namespace Automation.Framework.Services.Cart.Models
{
    /// <summary>
    /// طلب إتمام عملية الشراء
    /// </summary>
    public class CheckoutDTO
    {
        [JsonProperty("walletId")]
        public string WalletId { get; set; } = string.Empty;

        [JsonProperty("cartItems")]
        public List<string> CartItems { get; set; } = new();

        [JsonProperty("totalValue")]
        public decimal TotalValue { get; set; }

        [JsonProperty("pinNumber")]
        public string PinNumber { get; set; } = "001100";

        [JsonProperty("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonProperty("useAllowedDebt")]
        public bool UseAllowedDebt { get; set; } = true;
    }

    /// <summary>
    /// استجابة إتمام عملية الشراء
    /// </summary>
    public class CheckoutResponse
    {
        [JsonProperty("data")]
        public CheckoutData Data { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        public bool Success => Message?.Equals("success", StringComparison.OrdinalIgnoreCase) == true;

    }

    /// <summary>
    /// بيانات عملية الشراء بعد الإتمام
    /// </summary>
    public class CheckoutData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; } = string.Empty;

        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonProperty("walletIdentifier")]
        public string WalletIdentifier { get; set; } = string.Empty;

        [JsonProperty("currencyType")]
        public int CurrencyType { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; } = string.Empty;

        [JsonProperty("regionName")]
        public string RegionName { get; set; } = string.Empty;

        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonProperty("advertisement")]
        public string Advertisement { get; set; } = string.Empty;

        [JsonProperty("cards")]
        public List<CheckoutCard> Cards { get; set; } = new();
    }
    public class CheckoutCard
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("card")]
        public CardDetails Card { get; set; } = new();

        [JsonProperty("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonProperty("virtualCost")]
        public decimal VirtualCost { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("cost")]
        public decimal Cost { get; set; }

        [JsonProperty("secretNumber")]
        public string SecretNumber { get; set; } = string.Empty;

        [JsonProperty("printNote")]
        public string PrintNote { get; set; } = string.Empty;

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        [JsonProperty("soldAt")]
        public DateTime SoldAt { get; set; }
    }
}