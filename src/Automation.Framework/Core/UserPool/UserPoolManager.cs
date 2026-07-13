using System.Collections.Concurrent;
using Automation.Framework.Configuration;

namespace Automation.Framework.Core.UserPool
{
    /// <summary>مجموعة مستخدمين لمشروع واحد. حجز/تحرير آمن للتوازي مع تصفية بالدور.</summary>
    public sealed class UserPoolManager
    {
        private readonly string _project;
        private readonly List<UserCredentials> _users;
        private readonly ConcurrentDictionary<string, byte> _inUse = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        public UserPoolManager(string project, IEnumerable<UserCredentials> users)
        {
            _project = project;
            _users = users.ToList();
        }

        public async Task<UserLease> ReserveAsync(string? role = null, CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await _gate.WaitAsync(ct);
                try
                {
                    var candidate = _users.FirstOrDefault(u =>
                        (role is null || string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)) &&
                        !_inUse.ContainsKey(u.Key));

                    if (candidate is not null)
                    {
                        _inUse[candidate.Key] = 1;
                        return new UserLease(candidate, () => _inUse.TryRemove(candidate.Key, out _));
                    }

                    var total = _users.Count(u =>
                        role is null || string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase));
                    if (total == 0)
                        throw new InvalidOperationException(
                            $"No users with role '{role}' configured in project '{_project}'.");
                }
                finally { _gate.Release(); }

                await Task.Delay(50, ct);
            }
        }

        public int Available(string? role = null) =>
            _users.Count(u =>
                (role is null || string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)) &&
                !_inUse.ContainsKey(u.Key));

        public int Total(string? role = null) =>
            _users.Count(u => role is null || string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase));
    }
}
