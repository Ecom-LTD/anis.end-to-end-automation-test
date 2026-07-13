using Automation.Framework.Services.Catalog.Models;
using Newtonsoft.Json;

namespace Automation.Framework.Services.Cart.Models
{
   

    
    


        public class AddToCartRequest
        {
            [JsonProperty("cardId")]
            public string CardId { get; set; } = string.Empty;


        }
        /// <summary>
        /// استجابة سلة المشتريات
        /// </summary>
        public class CartDTO
        {
            [JsonProperty("data")]
            public CartData Data { get; set; } = new();

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("success")]
            public bool Success { get; set; }
        }

        /// <summary>
        /// بيانات السلة
        /// </summary>
        public class CartData
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("items")]
            public List<CartItem> Items { get; set; } = new();

            [JsonProperty("totalValue")]
            public decimal TotalValue { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; } = string.Empty;
        }

        /// <summary>
        /// منتج في السلة
        /// </summary>
        public class CartItem
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("cardId")]
            public string CardId { get; set; } = string.Empty;

            [JsonProperty("cardName")]
            public string CardName { get; set; } = string.Empty;

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("unitPrice")]
            public decimal UnitPrice { get; set; }

            [JsonProperty("totalPrice")]
            public decimal TotalPrice { get; set; }
        }



        /// <summary>
        /// استجابة إضافة بطاقة إلى السلة
        /// </summary>
        public class AddToCartResponse
        {
            [JsonProperty("data")]
            public CartItemData Data { get; set; } = new();

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// بيانات عنصر السلة
        /// </summary>
        public class CartItemData
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("card")]
            public CardDetails Card { get; set; } = new();

            [JsonProperty("subCategoryId")]
            public string SubCategoryId { get; set; } = string.Empty;

            [JsonProperty("subCategoryName")]
            public string SubCategoryName { get; set; } = string.Empty;
        }

        public class UpdateQuantityRequest
        {
            [JsonProperty("quantity")]
            public int Quantity { get; set; }
        }

        /// <summary>
        /// استجابة تحديث الكمية
        /// </summary>
        public class UpdateQuantityResponse
        {
            [JsonProperty("data")]
            public CartItemData Data { get; set; } = new();

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;
        }
    }
