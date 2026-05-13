using System.Text.Json.Serialization;

namespace Thinker.Models;

public sealed class AppState
{
    public bool Active { get; set; }
    public RunMode ActiveMode { get; set; } = RunMode.Timed30Minutes;
    public RunMode LockedMode { get; set; } = RunMode.Timed30Minutes;
    public string SchemeGuid { get; set; } = "SCHEME_CURRENT";
    public LidAction? PreviousAcAction { get; set; }
    public LidAction? PreviousDcAction { get; set; }
    public DateTimeOffset? EnabledAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? LastError { get; set; }

    [JsonIgnore]
    public ModeStatus Status =>
        !string.IsNullOrWhiteSpace(LastError)
            ? ModeStatus.Error
            : ActiveMode == RunMode.Permanent && Active
                ? ModeStatus.ActivePermanent
                : Active
                    ? ModeStatus.ActiveTimed
                    : ModeStatus.Normal;

    public static AppState Default() => new()
    {
        Active = false,
        ActiveMode = RunMode.Timed30Minutes,
        LockedMode = RunMode.Timed30Minutes,
        SchemeGuid = "SCHEME_CURRENT"
    };
}
