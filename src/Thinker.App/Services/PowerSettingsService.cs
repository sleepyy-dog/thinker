using Thinker.Models;

namespace Thinker.Services;

public sealed class PowerSettingsService(PowerCfgRunner runner) : IPowerSettingsService
{
    public async Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var schemeOutput = await runner.RunAsync("/getactivescheme", cancellationToken);
        var schemeGuid = PowerSettingsParser.ParseActiveSchemeGuid(schemeOutput);
        var queryOutput = await runner.RunAsync("/q SCHEME_CURRENT SUB_BUTTONS", cancellationToken);
        return PowerSettingsParser.ParseLidActions(schemeGuid, queryOutput);
    }

    public async Task SetLidActionsAsync(
        LidAction acAction,
        LidAction dcAction,
        CancellationToken cancellationToken = default)
    {
        await runner.RunAsync($"/setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION {(int)acAction}", cancellationToken);
        await runner.RunAsync($"/setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION {(int)dcAction}", cancellationToken);
    }

    public async Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default)
    {
        await runner.RunAsync("/setactive SCHEME_CURRENT", cancellationToken);
    }
}
