using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class StateStoreTests
{
    [Fact]
    public async Task LoadAsync_ReturnsDefaultWhenFileMissing()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));

        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal(RunMode.Timed30Minutes, state.LockedMode);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsState()
    {
        using var dir = TempDir.Create();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var expected = new AppState
        {
            Active = true,
            ActiveMode = RunMode.Timed2Hours,
            LockedMode = RunMode.Permanent,
            SchemeGuid = "abc",
            PreviousAcAction = LidAction.Sleep,
            PreviousDcAction = LidAction.Hibernate,
            EnabledAt = DateTimeOffset.Parse("2026-05-13T09:00:00+08:00"),
            ExpiresAt = DateTimeOffset.Parse("2026-05-13T11:00:00+08:00")
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.True(actual.Active);
        Assert.Equal(RunMode.Timed2Hours, actual.ActiveMode);
        Assert.Equal(RunMode.Permanent, actual.LockedMode);
        Assert.Equal(LidAction.Sleep, actual.PreviousAcAction);
        Assert.Equal(LidAction.Hibernate, actual.PreviousDcAction);
    }

    [Fact]
    public async Task SaveAsync_WritesEnumsAsStrings()
    {
        using var dir = TempDir.Create();
        var statePath = Path.Combine(dir.Path, "state.json");
        var store = new StateStore(statePath);

        await store.SaveAsync(new AppState { LockedMode = RunMode.Timed2Hours });

        var json = await File.ReadAllTextAsync(statePath);
        Assert.Contains("\"lockedMode\": \"Timed2Hours\"", json);
    }

    [Fact]
    public async Task LoadAsync_RenamesCorruptFileAndReturnsErrorState()
    {
        using var dir = TempDir.Create();
        var statePath = Path.Combine(dir.Path, "state.json");
        await File.WriteAllTextAsync(statePath, "{ not json");
        var store = new StateStore(statePath);

        var state = await store.LoadAsync();

        Assert.False(state.Active);
        Assert.Equal(ModeStatus.Error, state.Status);
        Assert.True(Directory.GetFiles(dir.Path, "state.json.corrupt.*").Length == 1);
    }
}
