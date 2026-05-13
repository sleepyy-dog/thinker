using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class PetVisualStateTests
{
    [Fact]
    public void FromAppState_WhenNormal_ShowsSleepyState()
    {
        var state = AppState.Default();

        var visual = PetVisualState.FromAppState(state);

        Assert.Equal(PetMood.Sleepy, visual.Mood);
        Assert.Equal("Zz", visual.BadgeText);
        Assert.Equal("正常睡眠", visual.Caption);
    }

    [Fact]
    public void FromAppState_WhenTimedActive_ShowsLockedModeBadge()
    {
        var state = AppState.Default();
        state.Active = true;
        state.ActiveMode = RunMode.Timed2Hours;
        state.LockedMode = RunMode.Timed2Hours;

        var visual = PetVisualState.FromAppState(state);

        Assert.Equal(PetMood.Alert, visual.Mood);
        Assert.Equal("2h", visual.BadgeText);
        Assert.Equal("合盖继续", visual.Caption);
    }

    [Fact]
    public void FromAppState_WhenPermanentActive_ShowsInfinityBadge()
    {
        var state = AppState.Default();
        state.Active = true;
        state.ActiveMode = RunMode.Permanent;
        state.LockedMode = RunMode.Permanent;

        var visual = PetVisualState.FromAppState(state);

        Assert.Equal(PetMood.Steady, visual.Mood);
        Assert.Equal("∞", visual.BadgeText);
        Assert.Equal("永久运行", visual.Caption);
    }

    [Fact]
    public void FromAppState_WhenError_ShowsErrorBadge()
    {
        var state = AppState.Default();
        state.LastError = "failed";

        var visual = PetVisualState.FromAppState(state);

        Assert.Equal(PetMood.Error, visual.Mood);
        Assert.Equal("!", visual.BadgeText);
        Assert.Equal("需要处理", visual.Caption);
    }
}
