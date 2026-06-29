using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class CoordinateTransformer
{
    public MapPoint ToScreenPoint(MapPoint mapPoint, MapConfig mapConfig)
    {
        if (mapConfig.BaseMapWidth <= 0 || mapConfig.BaseMapHeight <= 0 ||
            mapConfig.DefaultScreenMapRect.Width <= 0 || mapConfig.DefaultScreenMapRect.Height <= 0)
        {
            return ClonePoint(mapPoint);
        }

        var rect = mapConfig.DefaultScreenMapRect;
        var normalizedX = mapPoint.X / mapConfig.BaseMapWidth;
        var normalizedY = mapPoint.Y / mapConfig.BaseMapHeight;

        var screenX = rect.Left + mapConfig.DefaultOffsetX + normalizedX * rect.Width * mapConfig.DefaultScale;
        var screenY = rect.Top + mapConfig.DefaultOffsetY + normalizedY * rect.Height * mapConfig.DefaultScale;

        var clone = ClonePoint(mapPoint);
        clone.X = screenX;
        clone.Y = screenY;
        return clone;
    }

    private static MapPoint ClonePoint(MapPoint point)
    {
        return new MapPoint
        {
            Id = point.Id,
            Name = point.Name,
            Group = point.Group,
            Type = point.Type,
            DisplayName = point.DisplayName,
            MapId = point.MapId,
            X = point.X,
            Y = point.Y,
            Confidence = point.Confidence,
            Priority = point.Priority,
            Enabled = point.Enabled,
            Source = point.Source,
            CreatedAt = point.CreatedAt,
            UpdatedAt = point.UpdatedAt,
            Note = point.Note
        };
    }
}
