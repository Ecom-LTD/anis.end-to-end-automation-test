using System.Collections.Concurrent;

namespace Automation.Test.Support
{
    /// <summary>
    /// قفل عالمي (process-wide) على سقف منطقة معينة.
    /// اختبارات الأسقف من أي مشروع (Sulfa, Fazza, ...) تحجزه قبل التعديل.
    /// يمنع تصادم الاختبارات على نفس المنطقة أثناء التوازي.
    /// </summary>
    public static class RegionLock
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public static async Task<IDisposable> AcquireAsync(
            string regionId,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(regionId))
                throw new ArgumentException("RegionId cannot be null or empty.", nameof(regionId));

            var gate = _locks.GetOrAdd(regionId, _ => new SemaphoreSlim(1, 1));
            var acquired = await gate.WaitAsync(timeout ?? TimeSpan.FromMinutes(3), ct);

            if (!acquired)
                throw new TimeoutException(
                    $"Could not acquire region lock for '{regionId}' within timeout.");

            return new Releaser(gate);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _gate;
            private int _released;

            public Releaser(SemaphoreSlim gate) => _gate = gate;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                    _gate.Release();
            }
        }
    }
}