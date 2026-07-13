using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Catalog.Models
{
    /// </summary>
    public class CategoriesDTO
    {
        [JsonProperty("data")]
        public List<Category> Data { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    /// <summary>
    /// فئة رئيسية (مثل: ألعاب، بطاقات)
    /// </summary>
    public class Category
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("subCategories")]
        public List<SubCategory> SubCategories { get; set; } = new();
    }

    /// <summary>
    /// فئة فرعية (مثل: Razer Gold Global)
    /// </summary>
    public class SubCategory
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;
    }
}
