using System.Text.Json;
using System.Text.Json.Serialization;
using Thinker.Models;

namespace Thinker.Services;

public sealed class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public StateStore(string? statePath = null)
    {
        StatePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Thinker",
            "state.json");
    }

    public string StatePath { get; }

    public async Task<AppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StatePath))
        {
            return AppState.Default();
        }

        try
        {
            await using var stream = File.OpenRead(StatePath);
            return await JsonSerializer.DeserializeAsync<AppState>(stream, JsonOptions, cancellationToken)
                   ?? AppState.Default();
        }
        catch (JsonException ex)
        {
            RenameCorruptFile();
            var state = AppState.Default();
            state.LastError = "State file is corrupt: " + ex.Message;
            return state;
        }
    }

    public async Task SaveAsync(AppState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(StatePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = StatePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        File.Copy(tempPath, StatePath, overwrite: true);
        File.Delete(tempPath);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(StatePath))
        {
            File.Delete(StatePath);
        }

        return Task.CompletedTask;
    }

    private void RenameCorruptFile()
    {
        var corruptPath = StatePath + ".corrupt." + DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
        File.Move(StatePath, corruptPath, overwrite: true);
    }
}
