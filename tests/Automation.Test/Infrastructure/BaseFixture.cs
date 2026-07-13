using Automation.Framework.Context;
using Automation.Framework.Core.Session;
using Automation.Framework.Core.UserPool;
using Automation.Test.Infrastructure;

namespace Automation.Test.Fixtures
{
    /// <summary>كل شيء يُحَلّ من حاوية DI المشتركة عبر TestHost.</summary>
    public abstract class BaseFixture
    {
        public StateManager State => TestHost.Resolve<StateManager>();
        public SessionCache Sessions => TestHost.Resolve<SessionCache>();
        public ResilientSession Resilience => TestHost.Resolve<ResilientSession>();
        public UserPoolRegistry Pools => TestHost.Resolve<UserPoolRegistry>();

        public T Flow<T>() where T : notnull => TestHost.Resolve<T>();
    }
}
