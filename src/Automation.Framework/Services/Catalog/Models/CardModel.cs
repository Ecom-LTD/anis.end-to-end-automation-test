
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Automation.Framework.Services.Catalog.Models
{
    public class CardDetailsResponse
    {
        [JsonPropertyName("data")]
        public CardDetails Data { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// تفاصيل البطاقة الكاملة
    /// </summary>
    public class CardDetails
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arabicName")]
        public string ArabicName { get; set; } = string.Empty;

        [JsonPropertyName("englishName")]
        public string EnglishName { get; set; } = string.Empty;

        // ✅ تغيير إلى decimal? (يقبل null)
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }

        [JsonPropertyName("specialOfferPrice")]
        public decimal? SpecialOfferPrice { get; set; }

        [JsonPropertyName("businessPrice")]
        public decimal? BusinessPrice { get; set; }

        [JsonPropertyName("personalPrice")]
        public decimal? PersonalPrice { get; set; }

        [JsonPropertyName("currencyType")]
        public int CurrencyType { get; set; }

        [JsonPropertyName("faceValue")]
        public string FaceValue { get; set; } = string.Empty;

        [JsonPropertyName("logo")]
        public string Logo { get; set; } = string.Empty;

        [JsonPropertyName("printLogoPath")]
        public string PrintLogoPath { get; set; } = string.Empty;

        [JsonPropertyName("printLogoName")]
        public string PrintLogoName { get; set; } = string.Empty;

        [JsonPropertyName("inStock")]
        public bool InStock { get; set; }

        [JsonPropertyName("hasSpecialOffer")]
        public bool HasSpecialOffer { get; set; }
    }
}
