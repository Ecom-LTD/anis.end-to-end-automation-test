using System.Collections.Concurrent;

namespace Automation.Framework.Context
{
    /// <summary>مخزن حالة آمن للتوازي: توكنات وقيم عامة مفهرسة بالمفتاح.</summary>
    public class StateManager
    {
        private readonly ConcurrentDictionary<string, object> _store = new();

        public void SetToken(string userKey, string token) => _store[$"token_{userKey}"] = token;

        public string GetToken(string userKey)
            => _store.TryGetValue($"token_{userKey}", out var t)
                ? t.ToString()!
                : throw new InvalidOperationException($"Token not found for user: {userKey}");

        public bool HasToken(string userKey) => _store.ContainsKey($"token_{userKey}");

        public void SetValue<T>(string key, T value) => _store[key] = value!;

        public T GetValue<T>(string key)
            => _store.TryGetValue(key, out var v) ? (T)v : throw new KeyNotFoundException($"Key not found: {key}");

        public bool TryGetValue<T>(string key, out T value)
        {
            if (_store.TryGetValue(key, out var v)) { value = (T)v; return true; }
            value = default!; return false;
        }

        public void Remove(string key) => _store.TryRemove(key, out _);
        public void Clear() => _store.Clear();
    }
}
