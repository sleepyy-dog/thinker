namespace Thinker.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
