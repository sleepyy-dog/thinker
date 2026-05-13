using Thinker.Models;
using Thinker.Services;

namespace Thinker.Tests;

public sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "Thinker.Tests",
        Guid.NewGuid().ToString("N"));

    private TempDir()
    {
        Directory.CreateDirectory(Path);
    }

    public static TempDir Create() => new();

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Advance(TimeSpan value) => UtcNow += value;
}

public sealed class FakePowerSettingsService : IPowerSettingsService
{
    public PowerSchemeState Current { get; set; } = new("scheme", LidAction.Sleep, LidAction.Sleep);
    public List<(LidAction Ac, LidAction Dc)> SetCalls { get; } = [];
    public bool FailRestoreOriginal { get; set; }
    public bool FailSleepFallback { get; set; }

    public Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Current);
    }

    public Task SetLidActionsAsync(LidAction acAction, LidAction dcAction, CancellationToken cancellationToken = default)
    {
        if (FailRestoreOriginal && (acAction, dcAction) != (LidAction.Sleep, LidAction.Sleep))
        {
            throw new InvalidOperationException("restore original failed");
        }

        if (FailSleepFallback && (acAction, dcAction) == (LidAction.Sleep, LidAction.Sleep))
        {
            throw new InvalidOperationException("sleep fallback failed");
        }

        SetCalls.Add((acAction, dcAction));
        Current = Current with { AcAction = acAction, DcAction = dcAction };
        return Task.CompletedTask;
    }

    public Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
