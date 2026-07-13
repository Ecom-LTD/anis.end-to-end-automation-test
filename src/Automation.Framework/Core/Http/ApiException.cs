using System.Net;

namespace Automation.Framework.Core.Http
{
    /// <summary>
    /// استثناء موحّد لأخطاء الـ API. يرث من HttpRequestException للتوافق مع أي catch/Assert موجود،
    /// ويوفّر StatusCode صراحةً لآلية إعادة المحاولة عند 401.
    /// </summary>
    public sealed class ApiException : HttpRequestException
    {
        public HttpStatusCode ApiStatusCode { get; }
        public string Body { get; }

        public ApiException(HttpStatusCode status, string body)
            : base($"HTTP {(int)status}\n{body}")
        {
            ApiStatusCode = status;
            Body = body;
        }
    }
}
