using System.Text.Json.Serialization;

namespace Automation.Framework.Services.ForeignTransfer.Models
{
    public class ForeignTransferResponse
    {
        [JsonPropertyName("results")]
        public List<ForeignTransferItem> Results { get; set; } = new();

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("lastPage")]
        public int LastPage { get; set; }
    }

    public class ForeignTransferItem
    {
        [JsonPropertyName("transferId")]
        public string TransferId { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("categoryIconUrl")]
        public string CategoryIconUrl { get; set; } = string.Empty;

        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("currencyDisplayName")]
        public string CurrencyDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("amountDisplayName")]
        public string AmountDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("foreignDisplayName")]
        public string ForeignDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("transferType")]
        public string TransferType { get; set; } = string.Empty;

        [JsonPropertyName("rateDisplayMultiplier")]
        public decimal RateDisplayMultiplier { get; set; }

        [JsonPropertyName("prices")]
        public List<TransferPrice> Prices { get; set; } = new();

        [JsonPropertyName("cardExchangeAllowed")]
        public bool CardExchangeAllowed { get; set; }
    }

    public class TransferPrice
    {
        [JsonPropertyName("holderId")]
        public string? HolderId { get; set; }

        [JsonPropertyName("holderName")]
        public string? HolderName { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("businessPrice")]
        public decimal? BusinessPrice { get; set; }

        [JsonPropertyName("lydMinimum")]
        public decimal LydMinimum { get; set; }

        [JsonPropertyName("lydMaximum")]
        public decimal LydMaximum { get; set; }

        [JsonPropertyName("fxMinimum")]
        public decimal FxMinimum { get; set; }

        [JsonPropertyName("fxdMaximum")]
        public decimal FxdMaximum { get; set; }

        [JsonPropertyName("commission")]
        public decimal Commission { get; set; }
    }
}