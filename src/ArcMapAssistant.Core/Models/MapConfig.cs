namespace ArcMapAssistant.Core.Models;

public sealed class MapConfig
{
    public string MapId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BaseMapImage { get; set; } = string.Empty;
    public double BaseMapWidth { get; set; }
    public double BaseMapHeight { get; set; }
    public ScreenRect DefaultScreenMapRect { get; set; } = new();
    public double DefaultScale { get; set; } = 1.0;
    public double DefaultOffsetX { get; set; }
    public double DefaultOffsetY { get; set; }
}

