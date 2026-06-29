namespace ArcMapAssistant.Core.Models;

public sealed class MapPoint
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string MapId { get; set; } = string.Empty;
    public double? MapX { get; set; }
    public double? MapY { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Confidence { get; set; } = 1.0;
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string Note { get; set; } = string.Empty;
}
