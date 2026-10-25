using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Shared;
using Automation.Test.Fixtures;
using Automation.Test.Sessions;
using Automation.Test.Support;
using Xunit;

namespace Automation.Test.Fixtures.Fazza
{
    public sealed class FazzaFixture : BaseFixture, IAsyncLifetime
    {
        private FazzaBaseline? _baseline;

        /// <summary>
        /// دالة تسجيل اختيارية. تُعيَّن من BaseFazzaTest في Constructor
        /// عبر ITestOutputHelper. أثناء InitializeAsync تكون null (لا تسجيل).
        /// </summary>
        public Action<string>? Log { get; set; }

        public TestSession Dashboard { get; private set; } = null!;
        public TestSession FazzaUser1 { get; private set; } = null!;
        public TestSession FazzaUser2 { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Log?.Invoke("FazzaFixture: بدء التهيئة...");

            Dashboard = await Sessions.GetOrBuildAsync(FazzaSessions.Dashboard);
            await Sessions.PrewarmAsync(FazzaSessions.NonDashboard);

            FazzaUser1 = await Sessions.GetOrBuildAsync(FazzaSessions.FazzaUser1);
            FazzaUser2 = await Sessions.GetOrBuildAsync(FazzaSessions.FazzaUser2);

            EnsureSessionsAreValid();

            _baseline = await FazzaBaseline.CaptureAsync(
                Flow<FazzaTopUpFlow>(),
                Dashboard,
                new[] { FazzaUser1, FazzaUser2 });

            Log?.Invoke("FazzaFixture: ✅ التهيئة اكتملت.");
        }

        public async Task DisposeAsync()
        {
            if (_baseline is not null)
            {
                Log?.Invoke("FazzaFixture: استعادة الحالة الأصلية...");
                await _baseline.RestoreAsync(Log);
                Log?.Invoke("FazzaFixture: ✅ الاستعادة اكتملت.");
            }
        }

        private void EnsureSessionsAreValid()
        {
            void RequireToken(TestSession s, string label)
            {
                if (string.IsNullOrWhiteSpace(s.Token))
                    throw new InvalidOperationException($"{label} has no token.");
            }
            RequireToken(Dashboard, "Dashboard");
            RequireToken(FazzaUser1, FazzaUser1.UserKey);
            RequireToken(FazzaUser2, FazzaUser2.UserKey);

            if (string.IsNullOrWhiteSpace(FazzaUser1.WalletId))
                throw new InvalidOperationException($"{FazzaUser1.UserKey} has no WalletId.");
            if (string.IsNullOrWhiteSpace(FazzaUser2.WalletId))
                throw new InvalidOperationException($"{FazzaUser2.UserKey} has no WalletId.");
        }
    }
}