using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Cart.Endpoint
{
    public class CartEndpoint
    {
        public static string GetCardDetails(string cardId) =>
       $"/api/consumers/v1.0/categories/cards/{cardId}";
        public static string GetCartById(string cartId) =>
      $"/api/consumers/v1.0/cart/{cartId}";
        public const string AddToCart = "/api/consumers/v1.0/cart";
        public static string UpdateCartItem(string itemId) =>
         $"/api/consumers/v1.0/cart/{itemId}";

        public const string Checkout = "/api/consumers/v1.0/cart/checkout";
        public const string DeleteAllCartItems = "/api/consumers/v1.0/cart/all";
    }
}
