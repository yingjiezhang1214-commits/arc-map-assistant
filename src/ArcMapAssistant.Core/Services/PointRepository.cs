using System.Text.Json;
using System.Text.Json.Serialization;
using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class PointRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;

    public PointRepository(string path)
    {
        _path = path;
    }

    public async Task<IReadOnlyList<MapPoint>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<MapPoint>();
        }

        await using var stream = File.OpenRead(_path);
        var points = await JsonSerializer.DeserializeAsync<List<MapPoint>>(stream, JsonOptions, cancellationToken);
        return points?.Select(MarkerTaxonomy.NormalizePoint).ToList() ?? [];
    }

    public async Task AddAsync(MapPoint point, CancellationToken cancellationToken = default)
    {
        var points = (await LoadAsync(cancellationToken)).ToList();
        points.Add(point);
        await SaveAsync(points, cancellationToken);
    }

    public async Task SaveAsync(IEnumerable<MapPoint> points, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, points.Select(MarkerTaxonomy.NormalizePoint).ToList(), JsonOptions, cancellationToken);
    }
}
