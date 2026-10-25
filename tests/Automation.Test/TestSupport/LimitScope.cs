using Automation.Framework.Helpers;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;

namespace Automation.Test.Support
{
    /// <summary>
    /// يُلتقط في بداية الاختبار ويُستعاد تلقائيًا في نهاية using.
    /// يعيد سقف الحساب أولًا ثم سقف المنطقة (بهذا الترتيب لتفادي الرفض).
    /// </summary>
    public sealed class LimitScope : IAsyncDisposable
    {
        private readonly FazzaTopUpFlow _fazza;
        private readonly TestSession _dashboard;
        private readonly List<(TestSession Session, decimal OriginalLimit)> _accounts = new();
        private readonly List<(string RegionId, decimal OriginalLimit)> _regions = new();
        private bool _disposed;

        private LimitScope(FazzaTopUpFlow fazza, TestSession dashboard)
        {
            _fazza = fazza;
            _dashboard = dashboard;
        }

        public static async Task<LimitScope> CaptureAsync(
            FazzaTopUpFlow fazza,
            TestSession dashboard,
            params TestSession[] accounts)
        {
            var scope = new LimitScope(fazza, dashboard);

            foreach (var account in accounts)
            {
                var sulfa = await GetAccountAsync(fazza, dashboard, account);
                scope._accounts.Add((account, sulfa.MaxFazaaDebtLimit));

                if (!scope._regions.Any(r => r.RegionId == account.RegionId))
                {
                    var region = await GetRegionAsync(fazza, dashboard, account.RegionId);
                    scope._regions.Add((account.RegionId, region.FazaaMaxLimit));
                }
            }

            return scope;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            // 1. الحسابات
            foreach (var (session, original) in _accounts)
            {
                try
                {
                    var current = await GetAccountAsync(_fazza, _dashboard, session);

                    if (original < current.ConfirmedDebt)
                        continue;

                    if (current.MaxFazaaDebtLimit == original)
                        continue;

                    await _fazza.SetAccountFazzaDebtMaxLimitAsync(
                        _dashboard.UserKey, session.AccountIdGuid, original);
                }
                catch { /* best effort */ }
            }

            // 2. المناطق
            foreach (var (regionId, original) in _regions)
            {
                try
                {
                    var current = await GetRegionAsync(_fazza, _dashboard, regionId);

                    if (original < current.TotalAllocatedFazaaAmount)
                        continue;

                    if (current.FazaaMaxLimit == original)
                        continue;

                    await _fazza.SetRegionMaxFazaaLimitAsync(
                        _dashboard.UserKey, regionId, original);
                }
                catch { /* best effort */ }
            }
        }

        private static async Task<SulfaAccount> GetAccountAsync(
            FazzaTopUpFlow fazza, TestSession dashboard, TestSession account)
        {
            var accounts = await fazza.GetSulfaAccountsAsync(
                dashboard.UserKey, account.PhoneNumber);

            var matched = accounts.FirstOrDefault(a =>
                    AccountMatcher.IdsMatch(a.Id, account.AccountId))
                ?? accounts.FirstOrDefault(a =>
                    PhoneNormalizer.AreEqual(a.Phone, account.PhoneNumber))
                ?? throw new InvalidOperationException(
                    $"Sulfa account not found for {account.PhoneNumber}.");

            return matched;
        }

        private static async Task<RegionSulfaFullData> GetRegionAsync(
            FazzaTopUpFlow fazza, TestSession dashboard, string regionId)
        {
            var region = await fazza.GetRegionFullDataAsync(dashboard.UserKey, regionId)
                ?? throw new InvalidOperationException($"Region {regionId} not found.");
            return region;
        }
    }
}