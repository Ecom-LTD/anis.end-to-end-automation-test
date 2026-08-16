using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
//using Newtonsoft.Json;
namespace Automation.Framework.Core.Http
{
    /// <summary>
    /// غلاف HttpClient. يقبل HttpMessageHandler اختياريًا (يُحقن عبر DI) بحيث:
    /// - في الإنتاج: يستخدم HttpClientHandler الافتراضي (شبكة حقيقية).
    /// - في الاختبار: يستخدم FakeBackendHandler (بدون شبكة).
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpMessageHandler? handler = null)
        {
            _http = handler is null
                ? new HttpClient()
                : new HttpClient(handler);
            _http.Timeout = TimeSpan.FromSeconds(100);
        }

        public Task<ApiResponse<T>> GetAsync<T>(string baseUrl, string path, string? token = null)
            => ExecuteAsync<T>(CreateRequest(HttpMethod.Get, baseUrl + path, token));

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string baseUrl, string path, TRequest body, string? token = null)
        {
            var req = CreateRequest(HttpMethod.Post, baseUrl + path, token);
            req.Content = CreateJsonContent(body);
            return ExecuteAsync<TResponse>(req);
        }

        public Task<ApiResponse<TResponse>> PostWithHeadersAsync<TRequest, TResponse>(
            string baseUrl, string path, TRequest body, string? token = null,
            Dictionary<string, string>? customHeaders = null)
        {
            var req = CreateRequest(HttpMethod.Post, baseUrl + path, token);
            if (customHeaders != null)
                foreach (var h in customHeaders)
                    req.Headers.TryAddWithoutValidation(h.Key, h.Value);
            req.Content = CreateJsonContent(body);
            return ExecuteAsync<TResponse>(req);
        }

        public Task<ApiResponse<T>> PostFormAsync<T>(string url, Dictionary<string, string> formData)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(formData) };
            return ExecuteAsync<T>(req);
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
            string baseUrl, string path, TRequest body, string? token = null)
        {
            var req = CreateRequest(HttpMethod.Put, baseUrl + path, token);
            req.Content = CreateJsonContent(body);
            return ExecuteAsync<TResponse>(req);
        }

        public Task<ApiResponse<T>> DeleteAsync<T>(string baseUrl, string path, string? token = null)
            => ExecuteAsync<T>(CreateRequest(HttpMethod.Delete, baseUrl + path, token));

        private async Task<ApiResponse<T>> ExecuteAsync<T>(HttpRequestMessage request)
        {
            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new ApiException(response.StatusCode, body);

            var data = string.IsNullOrWhiteSpace(body)
                ? default!
                : JsonSerializer.Deserialize<T>(body, JsonOptions)!;

            return new ApiResponse<T>
            {
                Data = data,
                StatusCode = response.StatusCode,
                RawBody = body
            };
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token)
        {
            var req = new HttpRequestMessage(method, url);
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return req;
        }

        private static StringContent CreateJsonContent<T>(T body)
            => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        public async Task<ApiResponse<TResponse>>
    PatchAsync<TRequest, TResponse>(
        string baseUrl,
        string path,
        TRequest body,
        string? token = null)
        {
            var method =
                new HttpMethod("PATCH");

            var request =
                CreateRequest(
                    method,
                    baseUrl + path,
                    token);

            request.Content =
                CreateJsonContent(body);

            return await ExecuteAsync<TResponse>(
                request);
        }


        public async Task<ApiResponse<string>>
    PostWithHeadersForStringAsync<TRequest>(
        string baseUrl,
        string path,
        TRequest body,
        string? token = null,
        Dictionary<string, string>? customHeaders = null)
        {
            var request =
                CreateRequest(
                    HttpMethod.Post,
                    baseUrl + path,
                    token);

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    request.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }
            }

            request.Content =
                CreateJsonContent(body);

            using var response =
                await _http.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(
                    response.StatusCode,
                    responseBody);
            }

            return new ApiResponse<string>
            {
                Data = responseBody,
                StatusCode = response.StatusCode,
                RawBody = responseBody
            };
        }
    }
}