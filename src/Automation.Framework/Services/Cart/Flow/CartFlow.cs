using Automation.Framework.Context;
using Automation.Framework.Services.Cart.Client;
using Automation.Framework.Services.Cart.Models;
using Automation.Framework.Services.Catalog.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Cart.Flow
{
    public class CartFlow
    {
        private readonly CartClient _cartClient;
        private readonly CatalogFlow _catalogFlow;
 
        private readonly StateManager _state;

        public CartFlow(CartClient cartClient, CatalogFlow catalogFlow, StateManager state)
        {
            _cartClient = cartClient;
            _catalogFlow = catalogFlow;
           
            _state = state;
        }

        /// <summary>
        /// ✅ دالة متكاملة: إضافة بطاقة إلى السلة (بدون البحث المسبق)
        /// </summary>
        public async Task<(string ItemId, decimal Price, int Quantity)> AddCardToCartDirectAsync(
            string token,
            string cardId)
        {
            // 1. إضافة البطاقة إلى السلة
            var addResponse = await _cartClient.AddCardToCartAsync(token, cardId);

            var itemId = addResponse.Data.Id;
            var price = addResponse.Data.Card.Price ?? 0;
            var quantity = addResponse.Data.Quantity;

            Console.WriteLine($"✅ Item added to cart. ItemId: {itemId}, Price: {price}, Quantity: {quantity}");

            return (itemId, price, quantity);
        }


        /// <summary>
        /// ✅ دالة متكاملة: إضافة بطاقة وتحديث الكمية
        /// </summary>
        public async Task<(string ItemId, decimal TotalValue, int Quantity)> AddAndUpdateQuantityAsync(
            string token,
            string cardId,
            int quantity = 1)
        {
            // 1. إضافة البطاقة إلى السلة
            var (itemId, price, _) = await AddCardToCartDirectAsync(token, cardId);

            // 2. تحديث الكمية (إذا كانت الكمية المطلوبة أكبر من 1)
            if (quantity > 1)
            {
                var updateResponse = await _cartClient.UpdateCartItemQuantityAsync(token, itemId, quantity);
                price = updateResponse.Data.Card.Price ?? 0;
                quantity = updateResponse.Data.Quantity;
                Console.WriteLine($"✅ Quantity updated to: {quantity}");
            }

            var totalValue = price * quantity;
            Console.WriteLine($"💰 Total value: {totalValue}");

            return (itemId, totalValue, quantity);
        }


        // ========== ✅ دوال السلة (Business Logic) ==========

        /// <summary>
        /// حذف جميع منتجات السلة
        /// </summary>
        public async Task<bool> DeleteAllCartItemsAsync(string userKey)
        {
            var token = _state.GetToken(userKey);
            return await _cartClient.DeleteAllCartItemsAsync(token);
        }

        /// <summary>
        /// إضافة بطاقة إلى السلة (مع البحث عن cardId تلقائياً)
        /// </summary>
        public async Task<(string ItemId, decimal TotalValue)> AddCardToCartAsync(
            string userKey,
            string subCategoryName,
            string cardName,
            int quantity = 1)
        {
            var token = _state.GetToken(userKey);

            // 1. البحث عن معرف الفئة الفرعية
            var subCategoryId = await _catalogFlow.GetSubCategoryIdByNameAsync(token, subCategoryName);
            if (subCategoryId == null)
                throw new Exception($"SubCategory '{subCategoryName}' not found");

            // 2. البحث عن معرف البطاقة
            var cardId = await _catalogFlow.GetCardIdByNameAsync(userKey, subCategoryId, cardName);
            if (cardId == null)
                throw new Exception($"Card '{cardName}' not found");

            // 3. إضافة البطاقة إلى السلة
            var addResponse = await _cartClient.AddCardToCartAsync(token, cardId);
            var itemId = addResponse.Data.Id;
            var price = addResponse.Data.Card.Price ?? 0;
            var currentQuantity = addResponse.Data.Quantity;
            var totalValue = price * currentQuantity;

            // 4. إذا كانت الكمية المطلوبة مختلفة، قم بتحديثها
            if (quantity != currentQuantity)
            {
                var updateResponse = await _cartClient.UpdateCartItemQuantityAsync(token, itemId, quantity);
                totalValue = (updateResponse.Data.Card.Price ?? 0) * updateResponse.Data.Quantity;
            }

            return (itemId, totalValue);
        }

        /// <summary>
        /// إضافة بطاقة إلى السلة (باستخدام cardId مباشرة)
        /// </summary>
        public async Task<(string ItemId, decimal TotalValue)> AddCardToCartByCardIdAsync(
            string userKey,
            string cardId,
            int quantity = 1)
        {
            var token = _state.GetToken(userKey);

            // 1. إضافة البطاقة إلى السلة
            var addResponse = await _cartClient.AddCardToCartAsync(token, cardId);
            var itemId = addResponse.Data.Id;
            var price = addResponse.Data.Card.Price ?? 0;
            var currentQuantity = addResponse.Data.Quantity;
            var totalValue = price * currentQuantity;

            // 2. إذا كانت الكمية المطلوبة مختلفة، قم بتحديثها
            if (quantity != currentQuantity)
            {
                var updateResponse = await _cartClient.UpdateCartItemQuantityAsync(token, itemId, quantity);
                totalValue = (updateResponse.Data.Card.Price ?? 0)  * updateResponse.Data.Quantity;
            }

            return (itemId, totalValue);
        }

        /// <summary>
        /// شراء بطاقة (عملية كاملة - إضافة إلى السلة ثم الدفع)
        /// </summary>
        public async Task<CheckoutResponse> PurchaseCardAsync(
            string userKey,
            string walletId,
            string subCategoryName,
            string cardName,
            int quantity = 1,
            string pinNumber = "001100")
        {
            var token = _state.GetToken(userKey);

            // 1. إضافة البطاقة إلى السلة والحصول على itemId و totalValue
            var (itemId, totalValue) = await AddCardToCartAsync(userKey, subCategoryName, cardName, quantity);

            // 2. إنشاء GUID فريد للطلب
            var orderId = Guid.NewGuid().ToString();

            // 3. بناء طلب الدفع
            var request = new CheckoutDTO
            {
                WalletId = walletId,
                CartItems = new List<string> { itemId },
                TotalValue = totalValue,
                PinNumber = pinNumber,
                OrderId = orderId,
                UseAllowedDebt = true
            };

            // 4. تنفيذ الدفع
            return await _cartClient.CheckoutAsync(token, request);
        }

        /// <summary>
        /// شراء بطاقة (باستخدام cardId مباشرة)
        /// </summary>
        public async Task<CheckoutResponse> PurchaseCardByCardIdAsync(
            string userKey,
            string walletId,
            string cardId,
            int quantity = 1,
            string pinNumber = "001100")
        {
            var token = _state.GetToken(userKey);

            // 1. إضافة البطاقة إلى السلة
            var (itemId, totalValue) = await AddCardToCartByCardIdAsync(userKey, cardId, quantity);

            // 2. إنشاء GUID للطلب
            var orderId = Guid.NewGuid().ToString();

            // 3. بناء طلب الدفع
            var request = new CheckoutDTO
            {
                WalletId = walletId,
                CartItems = new List<string> { itemId },
                TotalValue = totalValue,
                PinNumber = pinNumber,
                OrderId = orderId,
                UseAllowedDebt = true
            };

            // 4. تنفيذ الدفع
            return await _cartClient.CheckoutAsync(token, request);
        }
    }
}
