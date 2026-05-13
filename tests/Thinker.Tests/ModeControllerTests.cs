using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class ModeControllerTests
{
    [Fact]
    public async Task ToggleAsync_FromNormal_UsesDefaultThirtyMinuteLockedMode()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService
        {
            Current = new PowerSchemeState("scheme", LidAction.Sleep, LidAction.Hibernate)
        };
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        var state = await store.LoadAsync();

        Assert.True(state.Active);
        Assert.Equal(RunMode.Timed30Minutes, state.ActiveMode);
        Assert.Equal(RunMode.Timed30Minutes, state.LockedMode);
        Assert.Equal(LidAction.Sleep, state.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, state.PreviousDcAction);
        Assert.Equal(DateTimeOffset.Parse("2026-05-13T01:30:00Z"), state.ExpiresAt);
        Assert.Equal((LidAction.DoNothing, LidAction.DoNothing), power.SetCalls.Single());
    }

    [Fact]
    public async Task SelectModeAsync_WhileActive_RefreshesCurrentModeAndKeepsOriginalActions()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService
        {
            Current = new PowerSchemeState("scheme", LidAction.Sleep, LidAction.Hibernate)
        };
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        clock.Advance(TimeSpan.FromMinutes(5));
        await controller.SelectModeAsync(RunMode.Timed2Hours);
        var state = await store.LoadAsync();

        Assert.True(state.Active);
        Assert.Equal(RunMode.Timed2Hours, state.ActiveMode);
        Assert.Equal(RunMode.Timed2Hours, state.LockedMode);
        Assert.Equal(LidAction.Sleep, state.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, state.PreviousDcAction);
        Assert.Equal(DateTimeOffset.Parse("2026-05-13T03:05:00Z"), state.ExpiresAt);
    }

    [Fact]
    public async Task CheckExpiryAsync_RestoresWhenTimedModeExpired()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var power = new FakePowerSettingsService();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-13T01:00:00Z"));
        var controller = new ModeController(power, store, clock);

        await controller.ToggleAsync();
        clock.Advance(TimeSpan.FromMinutes(31));
        await controller.CheckExpiryAsync();
        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal((LidAction.Sleep, LidAction.Sleep), power.SetCalls.Last());
    }
}
