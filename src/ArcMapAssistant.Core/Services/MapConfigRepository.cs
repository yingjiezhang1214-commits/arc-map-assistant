using System.Text.Json;
using System.Text.Json.Serialization;
using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class MapConfigRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly string _path;

    public MapConfigRepository(string path)
    {
        _path = path;
    }

    public async Task<IReadOnlyList<MapConfig>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        await using var stream = File.OpenRead(_path);
        var configs = await JsonSerializer.DeserializeAsync<List<MapConfig>>(stream, JsonOptions, cancellationToken);
        return configs ?? [];
    }

    public async Task SaveAsync(IEnumerable<MapConfig> configs, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, configs.ToList(), JsonOptions, cancellationToken);
    }
}
