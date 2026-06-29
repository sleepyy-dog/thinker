using Thinker.Models;
using Thinker.Services;
using Xunit;

namespace Thinker.Tests;

public sealed class PowerSettingsIntegrationTests
{
    [Fact]
    [Trait("Category", "Hardware")]
    public async Task ToggleAsync_UsesRealPowerCfgToChangeAndRestoreLidActions()
    {
        using var dir = TempDir.Create();
        var powerSettings = new PowerSettingsService(new PowerCfgRunner());
        var original = await powerSettings.GetCurrentAsync();
        var store = new StateStore(Path.Combine(dir.Path, "state.json"));
        var controller = new ModeController(
            powerSettings,
            store,
            new FakeClock(DateTimeOffset.UtcNow));

        try
        {
            var enabledState = await controller.ToggleAsync();
            var enabledPowerState = await powerSettings.GetCurrentAsync();

            Assert.True(enabledState.Active);
            Assert.Equal(LidAction.DoNothing, enabledPowerState.AcAction);
            Assert.Equal(LidAction.DoNothing, enabledPowerState.DcAction);

            await controller.ToggleAsync();
            var restoredPowerState = await powerSettings.GetCurrentAsync();

            Assert.Equal(original.AcAction, restoredPowerState.AcAction);
            Assert.Equal(original.DcAction, restoredPowerState.DcAction);
        }
        finally
        {
            await powerSettings.SetLidActionsAsync(original.AcAction, original.DcAction);
            await powerSettings.ApplyCurrentSchemeAsync();
        }
    }
}
