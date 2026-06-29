using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public sealed class CoordinateTransformer
{
    public MapPoint ToScreenPoint(MapPoint mapPoint, MapConfig mapConfig, string coordinateMode)
    {
        if (!TryGetScreenRect(mapConfig, coordinateMode, out var rect) ||
            mapConfig.BaseMapWidth <= 0 || mapConfig.BaseMapHeight <= 0 ||
            mapPoint.MapX is null || mapPoint.MapY is null)
        {
            return ClonePoint(mapPoint);
        }

        var normalizedX = mapPoint.MapX.Value / mapConfig.BaseMapWidth;
        var normalizedY = mapPoint.MapY.Value / mapConfig.BaseMapHeight;

        var screenX = rect.Left + normalizedX * rect.Width;
        var screenY = rect.Top + normalizedY * rect.Height;

        var clone = ClonePoint(mapPoint);
        clone.X = screenX;
        clone.Y = screenY;
        return clone;
    }

    public MapPoint ToMapPoint(MapPoint screenPoint, MapConfig mapConfig, string coordinateMode)
    {
        if (!TryGetScreenRect(mapConfig, coordinateMode, out var rect) ||
            mapConfig.BaseMapWidth <= 0 || mapConfig.BaseMapHeight <= 0 ||
            rect.Width <= 0 || rect.Height <= 0)
        {
            return ClonePoint(screenPoint);
        }

        var normalizedX = (screenPoint.X - rect.Left) / rect.Width;
        var normalizedY = (screenPoint.Y - rect.Top) / rect.Height;

        var clone = ClonePoint(screenPoint);
        clone.MapX = normalizedX * mapConfig.BaseMapWidth;
        clone.MapY = normalizedY * mapConfig.BaseMapHeight;
        return clone;
    }

    private static bool TryGetScreenRect(MapConfig mapConfig, string coordinateMode, out ScreenRect rect)
    {
        if (string.Equals(coordinateMode, "full_view", StringComparison.OrdinalIgnoreCase))
        {
            rect = mapConfig.FullViewScreenRect;
            return rect.Width > 0 && rect.Height > 0;
        }

        if (string.Equals(coordinateMode, "base_map", StringComparison.OrdinalIgnoreCase))
        {
            rect = mapConfig.DefaultScreenMapRect;
            return rect.Width > 0 && rect.Height > 0;
        }

        rect = new ScreenRect();
        return false;
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
            MapX = point.MapX,
            MapY = point.MapY,
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
