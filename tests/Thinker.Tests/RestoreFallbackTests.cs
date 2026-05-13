using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class RestoreFallbackTests
{
    [Fact]
    public async Task RestoreAsync_FallsBackToSleepWhenOriginalRestoreFails()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        await store.SaveAsync(new AppState
        {
            Active = true,
            ActiveMode = RunMode.Permanent,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "scheme",
            PreviousAcAction = LidAction.Hibernate,
            PreviousDcAction = LidAction.Hibernate
        });
        var power = new FakePowerSettingsService { FailRestoreOriginal = true };
        var controller = new ModeController(power, store, new FakeClock(DateTimeOffset.UtcNow));

        await controller.RestoreAsync();

        Assert.Contains(power.SetCalls, call => call == (LidAction.Sleep, LidAction.Sleep));
        Assert.False((await store.LoadAsync()).Active);
    }

    [Fact]
    public async Task RestoreAsync_RecordsErrorWhenSleepFallbackFails()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        await store.SaveAsync(new AppState
        {
            Active = true,
            ActiveMode = RunMode.Permanent,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "scheme",
            PreviousAcAction = LidAction.Hibernate,
            PreviousDcAction = LidAction.Hibernate
        });
        var power = new FakePowerSettingsService
        {
            FailRestoreOriginal = true,
            FailSleepFallback = true
        };
        var controller = new ModeController(power, store, new FakeClock(DateTimeOffset.UtcNow));

        await controller.RestoreAsync();

        var state = await store.LoadAsync();
        Assert.Equal(ModeStatus.Error, state.Status);
        Assert.Contains("sleep fallback failed", state.LastError);
    }
}
