using Automation.Framework.Shared;
using Automation.Test.Sessions;
using Automation.Test.Fixtures;
using Xunit;

namespace Automation.Test.Fixtures
{
    public class AlmuhserFixture : BaseFixture, IAsyncLifetime
    {
        // ================================================================
        // ✅ الجلسات الأساسية
        // ================================================================

        public TestSession AnisPay { get; private set; } = null!;
        public TestSession AnisCard { get; private set; } = null!;
        public TestSession Hreysh { get; private set; } = null!;
        public TestSession Profit { get; private set; } = null!;
        public TestSession Commission { get; private set; } = null!;
        public TestSession Dashboard { get; private set; } = null!;

        // ================================================================
        // ✅ محافظ Anis Card (LYD و USD)
        // ================================================================

        public TestSession AnisCardLyd { get; private set; } = null!;
        public TestSession AnisCardUsd { get; private set; } = null!;

        // ================================================================
        // ✅ معرفات المحافظ (Properties)
        // ================================================================

        /// <summary>
        /// معرف محفظة Anis Card بالـ LYD
        /// </summary>
        public string AnisCardLydWalletId => AnisCardLyd?.WalletId ?? string.Empty;

        /// <summary>
        /// معرف محفظة Anis Card بالـ USD
        /// </summary>
        public string AnisCardUsdWalletId => AnisCardUsd?.WalletId ?? string.Empty;

        // ================================================================
        // ✅ IAsyncLifetime
        // ================================================================

        public async Task InitializeAsync()
        {
            // ✅ 1. Dashboard أولاً
            Dashboard = await Sessions.GetOrBuildAsync(AlmusherSession.dashboard);

            // ✅ 2. بناء جميع الجلسات الأساسية بالتوازي
            await Sessions.PrewarmAsync(AlmusherSession.NonDashboard);

            // ✅ 3. بناء محافظ Anis Card (LYD و USD) بالتوازي
            await Sessions.PrewarmAsync(AlmusherSession.AnisCardSessions);

            // ✅ 4. استرجاع الجلسات من Cache
            AnisPay = await Sessions.GetOrBuildAsync(AlmusherSession.anispay);
            AnisCard = await Sessions.GetOrBuildAsync(AlmusherSession.aniscard);
            Hreysh = await Sessions.GetOrBuildAsync(AlmusherSession.hreysh);
            Profit = await Sessions.GetOrBuildAsync(AlmusherSession.profit);
            Commission = await Sessions.GetOrBuildAsync(AlmusherSession.commission);

            // ✅ 5. استرجاع محافظ Anis Card
            AnisCardLyd = await Sessions.GetOrBuildAsync(AlmusherSession.AnisCardLyd);
            AnisCardUsd = await Sessions.GetOrBuildAsync(AlmusherSession.AnisCardUsd);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}