using Automation.Framework.Services.Wallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Almusher.Models
{
    public class CreatePaymentChainRequest
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
    }
    public class CreatePaymentChainResponse
    {
        public string Id { get; set; } = string.Empty;

    }


    /// <summary>
    ///  Avrage RAte
    /// </summary>
    public class AverageRateInfoResponse
    {
        public string WalletId { get; set; } = string.Empty;
        public decimal KnownRateAmount { get; set; }
        public decimal UnknownRateAmount { get; set; }
        public decimal AverageRate { get; set; }          // ✅ هذا هو LydRate المطلوب
        public decimal KnownRateBalanceEstimatedLydAmount { get; set; }
        public int SubscriptionType { get; set; }
        public decimal Balance { get; set; }
    }



    // ================================================================
    // ✅ تأكيد الصرف
    // ================================================================

    public class ConfirmExchangeResponse
    {
        public ConfirmExchangeData Data { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class ConfirmExchangeData
    {
        public string OperationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ConfirmedAt { get; set; }
    }


    // ================================================================
    // ✅ نسبة usd
    // ================================================================

    public class UsdRatioResponse
    {
        public UsdRatioData Data { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class UsdRatioData
    {
        public decimal UsdRatio { get; set; }
        public decimal LydAmount { get; set; }
        public decimal UsdAmount { get; set; }
    }


}
// ================================================================
// ✅ تفاصيل الصرف - الهيكل الصحيح
// ================================================================

//    public class ExchangeDetailsResponse
//    {
//        [JsonPropertyName("id")]
//        public string Id { get; set; } = string.Empty;

//        [JsonPropertyName("currencyExchangeBuy")]
//        public CurrencyExchangeSide CurrencyExchangeBuy { get; set; } = new();

//        [JsonPropertyName("currencyExchangeSell")]
//        public CurrencyExchangeSide CurrencyExchangeSell { get; set; } = new();

//        [JsonPropertyName("conversionRate")]
//        public decimal ConversionRate { get; set; }

//        [JsonPropertyName("commission")]
//        public object? Commission { get; set; }

//        [JsonPropertyName("detailedStatement")]
//        public string DetailedStatement { get; set; } = string.Empty;

//        [JsonPropertyName("referenceId")]
//        public string? ReferenceId { get; set; }

//        [JsonPropertyName("reason")]
//        public string? Reason { get; set; }

//        [JsonPropertyName("exchangeType")]
//        public int ExchangeType { get; set; }

//        [JsonPropertyName("kind")]
//        public int Kind { get; set; }

//        [JsonPropertyName("profit")]
//        public object? Profit { get; set; }

//        [JsonPropertyName("return")]
//        public object? Return { get; set; }

//        [JsonPropertyName("lydRate")]
//        public decimal LydRate { get; set; }

//        [JsonPropertyName("usesSellCurrencyAsBase")]
//        public bool UsesSellCurrencyAsBase { get; set; }

//        [JsonPropertyName("partialRefund")]
//        public object? PartialRefund { get; set; }
//    }

//    // ================================================================
//    // ✅ جانب الصرف (Buy / Sell) - تم تعديله ليتوافق مع الـ Response
//    // ================================================================

//    public class CurrencyExchangeSide
//    {
//        [JsonPropertyName("creditorWallet")]
//        public AlmusherWalletInfo CreditorWallet { get; set; } = new();

//        [JsonPropertyName("debitorWallet")]
//        public AlmusherWalletInfo DebitorWallet { get; set; } = new();

//        [JsonPropertyName("amount")]
//        public decimal Amount { get; set; }

//        [JsonPropertyName("finalAmount")]
//        public decimal FinalAmount { get; set; }

//        [JsonPropertyName("regionId")]
//        public Guid RegionId { get; set; }

//        [JsonPropertyName("holderId")]
//        public Guid? HolderId { get; set; }
//    }

//    // ================================================================
//    // ✅ معلومات المحفظة
//    // ================================================================

//    public class AlmusherWalletInfo
//    {
//        [JsonPropertyName("walletId")]
//        public string WalletId { get; set; } = string.Empty;

//        [JsonPropertyName("walletIdentifier")]
//        public string WalletIdentifier { get; set; } = string.Empty;

//        [JsonPropertyName("currencyId")]
//        public int CurrencyId { get; set; }

//        [JsonPropertyName("subscriptionId")]
//        public string SubscriptionId { get; set; } = string.Empty;
//    }

//    // ================================================================
//    // ✅ متوسط السعر (Average Rate)
//    // ================================================================






//}
