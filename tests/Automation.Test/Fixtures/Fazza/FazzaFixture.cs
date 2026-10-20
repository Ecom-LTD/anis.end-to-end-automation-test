using Automation.Framework.Shared;
using Automation.Test.Fixtures;
using Automation.Test.Sessions;
using Xunit;
using Xunit.Abstractions;

public sealed class FazzaFixture : BaseFixture, IAsyncLifetime
{
    public ITestOutputHelper? Output { get; set; }

    public TestSession Dashboard { get; private set; } = null!;
    public TestSession FazzaUser1 { get; private set; } = null!; // 👈 التعديل هنا
    public TestSession FazzaUser2 { get; private set; } = null!; // 👈 التعديل هنا

    public async Task InitializeAsync()
    {
        Log("FazzaFixture: بدء التهيئة...");

        Dashboard = await Sessions.GetOrBuildAsync(FazzaSessions.Dashboard);
        await Sessions.PrewarmAsync(FazzaSessions.NonDashboard);

        FazzaUser1 = await Sessions.GetOrBuildAsync(FazzaSessions.FazzaUser1); // 👈 التعديل هنا
        FazzaUser2 = await Sessions.GetOrBuildAsync(FazzaSessions.FazzaUser2); // 👈 التعديل هنا

        EnsureSessionsAreValid();
        Log("FazzaFixture: ✅ التهيئة اكتملت بنجاح.");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void EnsureSessionsAreValid()
    {
        if (string.IsNullOrWhiteSpace(Dashboard.Token))
            throw new InvalidOperationException("FazzaFixture: Dashboard has no token.");

        if (string.IsNullOrWhiteSpace(FazzaUser1.Token))
            throw new InvalidOperationException($"FazzaFixture: '{FazzaUser1.UserKey}' has no token.");

        if (string.IsNullOrWhiteSpace(FazzaUser2.Token))
            throw new InvalidOperationException($"FazzaFixture: '{FazzaUser2.UserKey}' has no token.");

        // يمكنك إضافة فحص الـ WalletId إذا كنت تريد ضمان وجود محفظة للطرفين
        if (string.IsNullOrWhiteSpace(FazzaUser1.WalletId))
            throw new InvalidOperationException($"FazzaFixture: '{FazzaUser1.UserKey}' has no WalletId.");

        if (string.IsNullOrWhiteSpace(FazzaUser2.WalletId))
            throw new InvalidOperationException($"FazzaFixture: '{FazzaUser2.UserKey}' has no WalletId.");
    }

    private void Log(string message) => Output?.WriteLine(message);
}