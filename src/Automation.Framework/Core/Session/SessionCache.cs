using System.Collections.Concurrent;
using Automation.Framework.Builders;
using Automation.Framework.Shared;

namespace Automation.Framework.Core.Session
{
    /// <summary>كاش جلسات آمن للتوازي: بناء مرة واحدة، Pre-warming، وتحديث عند انتهاء الصلاحية.</summary>
    public sealed class SessionCache
    {
        private readonly SessionBuilder _builder;
        private readonly ConcurrentDictionary<string, Entry> _cache = new();

        public SessionCache(SessionBuilder builder) => _builder = builder;

        public async Task<TestSession> GetOrBuildAsync(SessionOptions options)
        {
            var entry = _cache.GetOrAdd(options.UserKey, _ => new Entry());
            await entry.Gate.WaitAsync();
            try
            {
                if (entry.Session is { } cached && !entry.IsExpired) return cached;
                var session = await _builder.BuildAsync(options);
                entry.Set(session, options);
                return session;
            }
            finally { entry.Gate.Release(); }
        }

        public async Task<TestSession> RefreshAsync(string userKey)
        {
            if (!_cache.TryGetValue(userKey, out var entry) || entry.Options is null)
                throw new InvalidOperationException($"No cached session to refresh for '{userKey}'.");

            await entry.Gate.WaitAsync();
            try
            {
                var session = await _builder.BuildAsync(entry.Options);
                entry.Set(session, entry.Options);
                return session;
            }
            finally { entry.Gate.Release(); }
        }

        public Task PrewarmAsync(IEnumerable<SessionOptions> options)
            => Task.WhenAll(options.Select(GetOrBuildAsync));

        public void Invalidate(string userKey) => _cache.TryRemove(userKey, out _);

        private sealed class Entry
        {
            public SemaphoreSlim Gate { get; } = new(1, 1);
            public TestSession? Session { get; private set; }
            public SessionOptions? Options { get; private set; }
            private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
            public bool IsExpired => DateTimeOffset.UtcNow >= _expiresAt;

            public void Set(TestSession session, SessionOptions options)
            {
                Session = session;
                Options = options;
                _expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.TokenLifetimeMinutes);
            }
        }
    }
}
