using Thinker.Models;

namespace Thinker.Services;

public interface IModeStatusSink
{
    Task OnStateChangedAsync(AppState state, CancellationToken cancellationToken = default);
}
