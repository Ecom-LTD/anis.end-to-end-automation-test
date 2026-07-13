using Automation.Framework.Core.Http;

using Automation.Framework.Services.Catalog.Endpoint;
using Automation.Framework.Services.Catalog.Models;
using Automation.Framework.Configuration;


namespace Automation.Framework.Services.Catalog.Client
{
    public class CatalogClient
    {
        private readonly ApiClient _api;

        public CatalogClient(ApiClient api) => _api = api;

        ///////////////////
        /// Category Client
        ///////////////////

        public async Task<CategoriesDTO> GetCategoriesAsync(string token)
        {
            var response = await _api.GetAsync<CategoriesDTO>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CatalogEndpoint.GetCategories,
                token);
            return response.Data;
        }



        ///////////////////
        /// Subcategory Client
        ///////////////////
        public async Task<SubCategoryCardsDTO> GetSubCategoryCardsAsync(string token, string subCategoryId)
        {
            var response = await _api.GetAsync<SubCategoryCardsDTO>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CatalogEndpoint.GetSubCategoryCards(subCategoryId),
                token);
            return response.Data;
        }

        ///////////////////
        /// Card Client
        ///////////////////

        public async Task<CardDetails> GetCardDetailsAsync(string token, string cardId)
        {
            var response = await _api.GetAsync<CardDetailsResponse>(
                ConfigurationManager.Settings.ApiSettings.GatewayUrl,
                CatalogEndpoint.GetCardDetails(cardId),
                token);

            var card = response.Data.Data;

            if (card == null || string.IsNullOrEmpty(card.Id))
                throw new Exception($"Card with ID '{cardId}' not found");

            if (!card.InStock)
                Console.WriteLine($"⚠️ Warning: Card '{card.Name}' is out of stock!");

            return card;
        }
    }
}
