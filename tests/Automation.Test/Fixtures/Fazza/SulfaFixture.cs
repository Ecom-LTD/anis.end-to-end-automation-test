using Automation.Framework.Shared;
using Automation.Test.Fixtures;
using Automation.Test.Sessions;
using Xunit;

namespace Automation.Test.Fixtures.Fazza
{
    /// <summary>يبني Dashboard أولًا (لجلب AccountId) ثم باقي جلسات Sulfa بالتوازي عبر الكاش.</summary>
    public class SulfaFixture : BaseFixture, IAsyncLifetime
    {
        public TestSession Dashboard { get; private set; } = null!;
        public TestSession SulfaBusiness { get; private set; } = null!;
        public TestSession SulfaOperator { get; private set; } = null!;
        public TestSession AdminSulfaOperator { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Dashboard = await Sessions.GetOrBuildAsync(SulfaSessions.Dashboard);
            await Sessions.PrewarmAsync(SulfaSessions.NonDashboard);

            SulfaBusiness = await Sessions.GetOrBuildAsync(SulfaSessions.Business);
            SulfaOperator = await Sessions.GetOrBuildAsync(SulfaSessions.Operator);
       
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
