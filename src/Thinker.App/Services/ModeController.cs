using Thinker.Models;

namespace Thinker.Services;

public sealed class ModeController(
    IPowerSettingsService powerSettings,
    StateStore stateStore,
    IClock clock,
    IModeStatusSink? statusSink = null)
{
    public async Task<AppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        if (state.Active && state.ExpiresAt is not null && state.ExpiresAt <= clock.UtcNow)
        {
            return await RestoreAsync(cancellationToken);
        }

        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> ToggleAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        return state.Active
            ? await RestoreAsync(cancellationToken)
            : await EnableAsync(state.LockedMode, cancellationToken);
    }

    public async Task<AppState> SelectModeAsync(RunMode mode, CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        state.LockedMode = mode;
        if (state.Active)
        {
            state.ActiveMode = mode;
            state.ExpiresAt = CalculateExpiry(mode);
        }

        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> EnableAsync(RunMode mode, CancellationToken cancellationToken = default)
    {
        var existing = await stateStore.LoadAsync(cancellationToken);
        var current = await powerSettings.GetCurrentAsync(cancellationToken);

        var state = existing.Active
            ? existing
            : new AppState
            {
                PreviousAcAction = current.AcAction,
                PreviousDcAction = current.DcAction,
                SchemeGuid = current.SchemeGuid
            };

        state.Active = true;
        state.ActiveMode = mode;
        state.LockedMode = mode;
        state.EnabledAt = clock.UtcNow;
        state.ExpiresAt = CalculateExpiry(mode);
        state.LastError = null;

        await powerSettings.SetLidActionsAsync(LidAction.DoNothing, LidAction.DoNothing, cancellationToken);
        await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        var targetAc = state.PreviousAcAction ?? LidAction.Sleep;
        var targetDc = state.PreviousDcAction ?? LidAction.Sleep;

        try
        {
            await powerSettings.SetLidActionsAsync(targetAc, targetDc, cancellationToken);
            await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
            MarkRestored(state);
        }
        catch (Exception originalRestoreError)
        {
            try
            {
                await powerSettings.SetLidActionsAsync(LidAction.Sleep, LidAction.Sleep, cancellationToken);
                await powerSettings.ApplyCurrentSchemeAsync(cancellationToken);
                MarkRestored(state);
            }
            catch (Exception fallbackError)
            {
                state.LastError = originalRestoreError.Message + " | " + fallbackError.Message;
            }
        }

        await stateStore.SaveAsync(state, cancellationToken);
        await NotifyAsync(state, cancellationToken);
        return state;
    }

    public async Task<AppState> CheckExpiryAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        if (state.Active && state.ExpiresAt is not null && state.ExpiresAt <= clock.UtcNow)
        {
            return await RestoreAsync(cancellationToken);
        }

        return state;
    }

    private DateTimeOffset? CalculateExpiry(RunMode mode) => mode switch
    {
        RunMode.Timed30Minutes => clock.UtcNow.AddMinutes(30),
        RunMode.Timed2Hours => clock.UtcNow.AddHours(2),
        RunMode.Permanent => null,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    private static void MarkRestored(AppState state)
    {
        state.Active = false;
        state.ActiveMode = state.LockedMode;
        state.PreviousAcAction = null;
        state.PreviousDcAction = null;
        state.EnabledAt = null;
        state.ExpiresAt = null;
        state.LastError = null;
    }

    private Task NotifyAsync(AppState state, CancellationToken cancellationToken)
    {
        return statusSink?.OnStateChangedAsync(state, cancellationToken) ?? Task.CompletedTask;
    }
}
