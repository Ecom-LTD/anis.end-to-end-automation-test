using System.Net;
using Automation.Framework.Core.Http;
using Automation.Framework.Shared;

namespace Automation.Framework.Core.Session
{
    /// <summary>سياسة إعادة المحاولة المركزية عند 401: تحديث الجلسة ثم إعادة التنفيذ.</summary>
    public sealed class ResilientSession
    {
        private readonly SessionCache _cache;
        public ResilientSession(SessionCache cache) => _cache = cache;

        public async Task<T> ExecuteAsync<T>(TestSession session, Func<Task<T>> action, int maxRetries = 1)
        {
            for (var attempt = 0; ; attempt++)
            {
                var tokenUsed = session.Token;
                try { return await action(); }
                catch (ApiException ex)
                    when (ex.ApiStatusCode == HttpStatusCode.Unauthorized && attempt < maxRetries)
                {
                    // التوكن المستخدم يمنع إعادة تسجيل الدخول إن حدّثه طلب متزامن آخر.
                    var refreshed = await _cache.RefreshAsync(session, tokenUsed);
                    if (!ReferenceEquals(refreshed, session))
                        session.CopyFrom(refreshed);
                }
            }
        }

        public Task ExecuteAsync(TestSession session, Func<Task> action, int maxRetries = 1)
            => ExecuteAsync<object?>(session, async () => { await action(); return null; }, maxRetries);
    }
}
