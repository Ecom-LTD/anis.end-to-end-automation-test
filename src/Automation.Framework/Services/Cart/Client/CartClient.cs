using Automation.Framework.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automation.Framework.Configuration;
using Automation.Framework.Services.Cart.Endpoint;
using Automation.Framework.Services.Cart.Models;

namespace Automation.Framework.Services.Cart.Client
{
    public class CartClient
    {
        private readonly ApiClient _api;
        public CartClient(ApiClient api) => _api = api;

        public async Task<bool> DeleteAllCartItemsAsync(string token)
        {
            try
            {
                await _api.DeleteAsync<object>(
                    ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                    CartEndpoint.DeleteAllCartItems,
                    token);
                return true;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                Console.WriteLine("⚠️ Cart not found (already empty) - treating as success");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to delete cart items: {ex.Message}");
                return false;
            }
        }

        // ========== ✅ دوال السلة ==========

        /// <summary>
        /// إضافة بطاقة إلى السلة (POST /cart)
        /// </summary>
        public async Task<AddToCartResponse> AddCardToCartAsync(string token, string cardId)
        {
            var request = new AddToCartRequest { CardId = cardId };

            var response = await _api.PostAsync<AddToCartRequest, AddToCartResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CartEndpoint.AddToCart,
                request,
                token);

            return response.Data;
        }

        /// <summary>
        /// تحديث كمية عنصر في السلة (PUT /cart/{itemId})
        /// </summary>
        public async Task<UpdateQuantityResponse> UpdateCartItemQuantityAsync(
            string token,
            string itemId,
            int quantity)
        {
            var request = new UpdateQuantityRequest { Quantity = quantity };

            var response = await _api.PutAsync<UpdateQuantityRequest, UpdateQuantityResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CartEndpoint.UpdateCartItem(itemId),
                request,
                token);

            return response.Data;
        }

        /// <summary>
        /// إتمام عملية الشراء (Checkout)
        /// </summary>
        public async Task<CheckoutResponse> CheckoutAsync(string token, CheckoutDTO request)
        {
            var response = await _api.PostAsync<CheckoutDTO, CheckoutResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CartEndpoint.Checkout,
                request,
                token);

            return response.Data;
        }


        /// <summary>
        /// إضافة بطاقة إلى السلة وإرجاع itemId والسعر
        /// </summary>
        public async Task<(string ItemId, decimal Price, int Quantity)> AddCardToCartSimpleAsync(
            string token,
            string cardId)
        {
            // 1. إضافة البطاقة إلى السلة (POST /cart)
            var request = new AddToCartRequest { CardId = cardId };

            var response = await _api.PostAsync<AddToCartRequest, AddToCartResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CartEndpoint.AddToCart,
                request,
                token);

            // 2. استخراج البيانات من الاستجابة
            var itemId = response.Data.Data.Id;
            var price = response.Data.Data.Card.Price ?? 0;
            var quantity = response.Data.Data.Quantity;

            Console.WriteLine($"✅ Item added - ID: {itemId}, Price: {price}, Quantity: {quantity}");

            return (itemId, price, quantity);
        }

        /// <summary>
        /// تحديث كمية عنصر في السلة
        /// </summary>
        public async Task<(decimal Price, int Quantity)> UpdateCartItemQuantitySimpleAsync(
            string token,
            string itemId,
            int quantity)
        {
            var updateRequest = new UpdateQuantityRequest { Quantity = quantity };

            var response = await _api.PutAsync<UpdateQuantityRequest, UpdateQuantityResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CartEndpoint.UpdateCartItem(itemId),
                updateRequest,
                token);

            var price = response.Data.Data.Card.Price ?? 0;
            var newQuantity = response.Data.Data.Quantity;

            Console.WriteLine($"✅ Quantity updated - Item: {itemId}, New Quantity: {newQuantity}, Price: {price}");

            return (price, newQuantity);
        }
    }
}