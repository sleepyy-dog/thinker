namespace Thinker.Services;

public interface IPowerCfgRunner
{
    Task<string> RunAsync(string arguments, CancellationToken cancellationToken = default);
}
