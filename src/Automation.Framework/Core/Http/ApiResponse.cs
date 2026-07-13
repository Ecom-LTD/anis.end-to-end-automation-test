using System.Net;

namespace Automation.Framework.Core.Http
{
    public class ApiResponse<T>
    {
        public T Data { get; set; } = default!;
        public HttpStatusCode StatusCode { get; set; }
        public string RawBody { get; set; } = string.Empty;
    }
}
