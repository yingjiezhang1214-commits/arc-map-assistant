using System.Text.Json;
using System.Text.Json.Serialization;
using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class SettingsService
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

    public SettingsService(string path)
    {
        _path = path;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(_path);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
            ?? new AppSettings();

        Normalize(settings);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Normalize(settings);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }

    private static void Normalize(AppSettings settings)
    {
        if (settings.EnabledGroups.Count == 0)
        {
            settings.EnabledGroups = MarkerTaxonomy.DefaultEnabledGroups.ToList();
        }

        settings.EnabledTypes = MarkerTaxonomy.NormalizeEnabledTypes(settings.EnabledTypes).ToList();

        var debugType = MarkerTaxonomy.NormalizeDebugSelection(settings.DebugPointGroup, settings.DebugPointType);
        settings.DebugPointGroup = debugType.Group;
        settings.DebugPointType = debugType.Type;
        settings.DebugPointDisplayName = debugType.DisplayName;
    }
}
