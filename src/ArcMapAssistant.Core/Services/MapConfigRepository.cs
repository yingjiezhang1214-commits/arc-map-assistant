using System.Text.Json;
using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class MapConfigRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
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
}

