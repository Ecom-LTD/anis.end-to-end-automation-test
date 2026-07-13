using Automation.Framework.Configuration;

namespace Automation.Framework.Core.UserPool
{
    /// <summary>مستخدم محجوز يُحرَّر تلقائيًا عند التخلّص منه (using / await using).</summary>
    public sealed class UserLease : IAsyncDisposable, IDisposable
    {
        private readonly Action _release;
        private bool _released;

        public UserCredentials User { get; }
        public string UserKey => User.Key;

        public UserLease(UserCredentials user, Action release)
        {
            User = user;
            _release = release;
        }

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _release();
        }

        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
