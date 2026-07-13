using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Catalog.Endpoint
{
    public class CatalogEndpoint
    {

        public const string GetCategories = "/api/consumers/v1.0/categories";

        public static string GetSubCategoryCards(string subCategoryId) =>
            $"/api/consumers/v1.0/categories/{subCategoryId}";

        /// <summary>
        /// ✅ جلب تفاصيل بطاقة محددة بواسطة ID
        /// </summary>
        public static string GetCardDetails(string cardId) =>
            $"/api/consumers/v1.0/categories/cards/{cardId}";
    }
}
