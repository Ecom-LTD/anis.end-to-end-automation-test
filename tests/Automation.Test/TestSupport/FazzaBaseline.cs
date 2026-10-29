using Automation.Framework.Helpers;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.FazzaTopup.Models;
using Automation.Framework.Shared;

namespace Automation.Test.Support
{
    /// <summary>
    /// يلتقط الحالة الأصلية للأسقف (حسابات + منطقة) عند بدء الـ Fixture،
    /// ويعيدها في DisposeAsync كشبكة أمان نهائية.
    /// </summary>
    public sealed class FazzaBaseline
    {
        private readonly FazzaTopUpFlow _fazza;
        private readonly TestSession _dashboard;
        private readonly List<AccountBaseline> _accounts = new();
        private readonly List<RegionBaseline> _regions = new();

        private FazzaBaseline(FazzaTopUpFlow fazza, TestSession dashboard)
        {
            _fazza = fazza;
            _dashboard = dashboard;
        }

        public static async Task<FazzaBaseline> CaptureAsync(
            FazzaTopUpFlow fazza,
            TestSession dashboard,
            IEnumerable<TestSession> accounts)
        {
            var baseline = new FazzaBaseline(fazza, dashboard);

            foreach (var account in accounts)
            {
                var sulfa = await GetAccountAsync(fazza, dashboard, account);
                baseline._accounts.Add(new AccountBaseline(
                    account,
                    sulfa.MaxFazaaDebtLimit,
                    sulfa.ConfirmedDebt));

                if (!baseline._regions.Any(r => r.RegionId == account.RegionId))
                {
                    var region = await GetRegionAsync(fazza, dashboard, account.RegionId);
                    baseline._regions.Add(new RegionBaseline(
                        account.RegionId,
                        region.FazaaMaxLimit,
                        region.TotalAllocatedFazaaAmount));
                }
            }

            return baseline;
        }

        public async Task RestoreAsync(Action<string>? log = null)
        {
            // 1. الحسابات
            foreach (var acc in _accounts)
            {
                try
                {
                    var current = await GetAccountAsync(_fazza, _dashboard, acc.Session);

                    if (acc.OriginalLimit < current.ConfirmedDebt)
                    {
                        log?.Invoke(
                            $"⚠️ Cannot restore {acc.Session.UserKey} limit " +
                            $"({acc.OriginalLimit}) below current debt ({current.ConfirmedDebt}).");
                        continue;
                    }

                    if (current.MaxFazaaDebtLimit == acc.OriginalLimit)
                        continue;

                    await _fazza.SetAccountFazzaDebtMaxLimitAsync(
                        _dashboard.UserKey,
                        acc.Session.AccountIdGuid,
                        acc.OriginalLimit);

                    log?.Invoke($"✅ Restored {acc.Session.UserKey} limit to {acc.OriginalLimit}.");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"⚠️ Failed to restore {acc.Session.UserKey}: {ex.Message}");
                }
            }

            // 2. المناطق
            foreach (var reg in _regions)
            {
                try
                {
                    var current = await GetRegionAsync(_fazza, _dashboard, reg.RegionId);

                    if (reg.OriginalLimit < current.TotalAllocatedFazaaAmount)
                    {
                        log?.Invoke(
                            $"⚠️ Cannot restore region {reg.RegionId} limit " +
                            $"({reg.OriginalLimit}) below allocated ({current.TotalAllocatedFazaaAmount}).");
                        continue;
                    }

                    if (current.FazaaMaxLimit == reg.OriginalLimit)
                        continue;

                    var response = await _fazza.SetRegionMaxFazaaLimitAsync(
                        _dashboard.UserKey,
                        reg.RegionId,
                        reg.OriginalLimit);

                    if (response is not { Success: true })
                    {
                        log?.Invoke($"⚠️ Region {reg.RegionId} restore rejected: {response?.Message}");
                        continue;
                    }

                    log?.Invoke($"✅ Restored region {reg.RegionId} limit to {reg.OriginalLimit}.");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"⚠️ Failed to restore region {reg.RegionId}: {ex.Message}");
                }
            }
        }

        private static async Task<SulfaAccount> GetAccountAsync(
            FazzaTopUpFlow fazza, TestSession dashboard, TestSession account)
        {
            return await fazza.FindAccountAsync(dashboard.UserKey, account.PhoneNumber, account.AccountId)
                ?? throw new InvalidOperationException(
                    $"Sulfa account not found for {account.PhoneNumber}.");
        }

        private static async Task<RegionSulfaFullData> GetRegionAsync(
            FazzaTopUpFlow fazza, TestSession dashboard, string regionId)
        {
            var region = await fazza.GetRegionFullDataAsync(dashboard.UserKey, regionId)
                ?? throw new InvalidOperationException($"Region {regionId} not found.");
            return region;
        }

        private sealed record AccountBaseline(
            TestSession Session, decimal OriginalLimit, decimal OriginalDebt);

        private sealed record RegionBaseline(
            string RegionId, decimal OriginalLimit, decimal OriginalAllocated);
    }
}