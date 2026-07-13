using System.Collections.Concurrent;
using Automation.Framework.Configuration;

namespace Automation.Framework.Core.UserPool
{
    /// <summary>سجلّ مجموعات المستخدمين: مجموعة معزولة لكل مشروع.</summary>
    public sealed class UserPoolRegistry
    {
        private readonly ConcurrentDictionary<string, UserPoolManager> _pools =
            new(StringComparer.OrdinalIgnoreCase);

        public UserPoolRegistry(IEnumerable<UserCredentials> allUsers)
        {
            foreach (var group in allUsers.GroupBy(u =>
                         string.IsNullOrWhiteSpace(u.Project) ? "Shared" : u.Project))
            {
                _pools[group.Key] = new UserPoolManager(group.Key, group);
            }
        }

        public UserPoolManager For(string project) =>
            _pools.TryGetValue(project, out var pool)
                ? pool
                : throw new InvalidOperationException(
                    $"No user pool for project '{project}'. Add users with \"Project\": \"{project}\" in appsettings.json.");

        public IReadOnlyCollection<string> Projects => _pools.Keys.ToList();
    }
}
