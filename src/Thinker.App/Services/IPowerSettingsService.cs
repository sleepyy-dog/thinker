using Thinker.Models;

namespace Thinker.Services;

public interface IPowerSettingsService
{
    Task<PowerSchemeState> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task SetLidActionsAsync(LidAction acAction, LidAction dcAction, CancellationToken cancellationToken = default);
    Task ApplyCurrentSchemeAsync(CancellationToken cancellationToken = default);
}
