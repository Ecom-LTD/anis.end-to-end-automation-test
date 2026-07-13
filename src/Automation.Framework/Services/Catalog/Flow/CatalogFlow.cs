using Automation.Framework.Context;
using Automation.Framework.Services.Cart.Client;
using Automation.Framework.Services.Catalog.Client;
using Automation.Framework.Services.Catalog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Catalog.Flow
{
      
    public class CatalogFlow
    {
        private readonly CatalogClient _catalogClient;

        private readonly StateManager _state;

        public CatalogFlow(CatalogClient catalogClient, StateManager state)
        {
            _catalogClient = catalogClient;
            _state = state;
        }

        ////////////////////////////////////////////////////////
        ///////
        /// --------- Category Method
        //////
        ///////////////////////////////////////////////////////

        public async Task<CategoriesDTO> GetAllCategoriesAsync(string token)
        {
            var categories = await _catalogClient.GetCategoriesAsync(token);
            return categories;
        }
        ////////////////////////////////////////////////////////
        ///////
        /// --------- SubCategory Method
        //////
        ///////////////////////////////////////////////////////


        public async Task<string?> GetSubCategoryIdByNameAsync(string token, string subCategoryName)
        {
            var categories = await GetAllCategoriesAsync(token);
            foreach (var category in categories.Data)
            {
                if (category.SubCategories == null) continue;
                var subCategory = category.SubCategories.FirstOrDefault(sc => sc.Name.Trim() == subCategoryName.Trim());
                if (subCategory != null)
                 return subCategory.Id;
            }
            return null;
        }

        public async Task<SubCategoryData> GetSubCategoryCardsAsync(string token, string subCategoryId)
        {
            var subcategory = await _catalogClient.GetSubCategoryCardsAsync(token, subCategoryId);
            return subcategory.Data;
        }



        ////////////////////////////////////////////////////////
        ///////
        /// --------- Cards Method
        //////
        ///////////////////////////////////////////////////////
        


        /// <summary>
        /// جلب معرف البطاقة بالاسم
        /// </summary>
        public async Task<string?> GetCardIdByNameAsync(string userKey, string subCategoryId, string cardName)
        {
            var token = _state.GetToken(userKey);
            var subcategory = await GetSubCategoryCardsAsync(token, subCategoryId);
            var card = subcategory.Cards.FirstOrDefault(c => c.Name.Trim() == cardName.Trim());
            return card?.Id;
        }

        // <summary>
        /// جلب تفاصيل بطاقة محددة بواسطة ID
        /// </summary>
        public async Task<CardDetails> GetCardDetailsAsync(string userKey, string cardId)
        {
            var token = _state.GetToken(userKey);
            return await _catalogClient.GetCardDetailsAsync(token, cardId);
        }

        /// <summary>
        /// جلب تفاصيل بطاقة بالاسم (خطوتين: البحث عن ID ثم الجلب)
        /// </summary>
        public async Task<CardDetails> GetCardDetailsByNameAsync(
            string userKey,
            string subCategoryName,
            string cardName)
        {
            var token = _state.GetToken(userKey);

            // 1. البحث عن معرف الفئة الفرعية
            var subCategoryId = await GetSubCategoryIdByNameAsync(token, subCategoryName);
            if (subCategoryId == null)
                throw new Exception($"SubCategory '{subCategoryName}' not found");

            // 2. البحث عن معرف البطاقة
            var cardId = await GetCardIdByNameAsync(userKey, subCategoryId, cardName);
            if (cardId == null)
                throw new Exception($"Card '{cardName}' not found");

            // 3. جلب تفاصيل البطاقة
            return await GetCardDetailsAsync(userKey, cardId);
        }
    }
}
