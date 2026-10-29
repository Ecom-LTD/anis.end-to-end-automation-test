using System.Collections.Concurrent;
using Automation.Framework.Builders;
using Automation.Framework.Shared;

namespace Automation.Framework.Core.Session
{
    /// <summary>
    /// كاش جلسات آمن للتوازي: بناء مرة واحدة، Pre-warming، وتحديث عند انتهاء الصلاحية.
    /// المفتاح يشمل الخيارات (وليس UserKey فقط) حتى لا تتشارك Collections مختلفة جلسة
    /// مبنية بخيارات غير خياراتها (مثل Dashboard بـ LoadAccount مختلف).
    /// </summary>
    public sealed class SessionCache
    {
        private readonly SessionBuilder _builder;
        private readonly ConcurrentDictionary<string, Entry> _cache = new();

        public SessionCache(SessionBuilder builder) => _builder = builder;

        public async Task<TestSession> GetOrBuildAsync(SessionOptions options)
        {
            var entry = _cache.GetOrAdd(KeyOf(options), _ => new Entry(options.UserKey));
            await entry.Gate.WaitAsync();
            try
            {
                if (entry.Session is { } cached && !entry.IsExpired) return cached;
                entry.Set(await _builder.BuildAsync(options), options);
                return entry.Session!;
            }
            finally { entry.Gate.Release(); }
        }

        /// <summary>
        /// يعيد بناء الجلسة المعطاة (تحديث في المكان). إن كان أحد المستدعين المتزامنين قد حدّثها
        /// بالفعل (التوكن تغيّر عن <paramref name="staleToken"/>) لا يُعاد تسجيل الدخول مرة ثانية.
        /// </summary>
        public async Task<TestSession> RefreshAsync(TestSession session, string? staleToken = null)
        {
            var entry = _cache.Values.FirstOrDefault(e => ReferenceEquals(e.Session, session))
                ?? _cache.Values.FirstOrDefault(e => e.UserKey == session.UserKey && e.Options is not null)
                ?? throw new InvalidOperationException($"No cached session to refresh for '{session.UserKey}'.");

            await entry.Gate.WaitAsync();
            try
            {
                if (staleToken is not null && entry.Session is { } current && current.Token != staleToken)
                    return current;

                entry.Set(await _builder.BuildAsync(entry.Options!), entry.Options!);
                return entry.Session!;
            }
            finally { entry.Gate.Release(); }
        }

        public async Task<TestSession> RefreshAsync(string userKey)
        {
            var entry = _cache.Values.FirstOrDefault(e => e.UserKey == userKey && e.Session is not null)
                ?? throw new InvalidOperationException($"No cached session to refresh for '{userKey}'.");
            return await RefreshAsync(entry.Session!);
        }

        public Task PrewarmAsync(IEnumerable<SessionOptions> options)
            => Task.WhenAll(options.Select(GetOrBuildAsync));

        public void Invalidate(string userKey)
        {
            foreach (var kv in _cache.Where(kv => kv.Value.UserKey == userKey))
                _cache.TryRemove(kv.Key, out _);
        }

        private static string KeyOf(SessionOptions o) =>
            string.Join('|', o.UserKey, o.LoadAccount, o.LoadWallet, o.CurrencyType, o.RegionName,
                o.HolderName, o.SubscriptionType, o.SubscriptionName, o.AutoCreateWalletIfNotFound);

        private sealed class Entry
        {
            public Entry(string userKey) => UserKey = userKey;

            public string UserKey { get; }
            public SemaphoreSlim Gate { get; } = new(1, 1);
            public TestSession? Session { get; private set; }
            public SessionOptions? Options { get; private set; }
            private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
            public bool IsExpired => DateTimeOffset.UtcNow >= _expiresAt;

            /// <summary>أول بناء يحفظ الـ instance؛ التحديثات اللاحقة تنسخ فيه كي ترى كل الجهات الحاملة له التوكن الجديد.</summary>
            public void Set(TestSession built, SessionOptions options)
            {
                if (Session is null) Session = built;
                else Session.CopyFrom(built);
                Options = options;
                _expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.TokenLifetimeMinutes);
            }
        }
    }
}
