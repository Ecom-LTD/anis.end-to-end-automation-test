using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Catalog.Models
{
    /// <summary>
    /// استجابة جلب بطاقات فئة فرعية
    /// </summary>
    public class SubCategoryCardsDTO
    {
        [JsonProperty("data")]
        public SubCategoryData Data { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    /// <summary>
    /// بيانات الفئة الفرعية مع البطاقات
    /// </summary>
    public class SubCategoryData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("categoryId")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonProperty("cards")]
        public List<Card> Cards { get; set; } = new();
    }

    /// <summary>
    /// بطاقة منتج (مثل: Razer Gold Global $5)
    /// </summary>
    public class Card
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;
    }
}
