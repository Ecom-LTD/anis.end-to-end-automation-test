using System.Text.Json.Serialization;

namespace Automation.Framework.Services.Identity.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]   public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")]   public string TokenType { get; set; } = string.Empty;
    }
}
