# Data Format

## points.json

By default, `mapX` and `mapY` are full-view map coordinates for the configured map. `x` and `y` remain available as the older fixed 2560x1440 screen-coordinate fallback.

```json
[
  {
    "id": "loot_dam_001",
    "name": "High Value Loot A",
    "group": "special_events",
    "type": "loose_loot",
    "displayName": "Loose Loot",
    "mapId": "dam_battlegrounds",
    "mapX": 1234,
    "mapY": 876,
    "x": 1234,
    "y": 876,
    "confidence": 0.8,
    "priority": 5,
    "enabled": true,
    "source": "manual",
    "createdAt": "2026-06-29T12:00:00+08:00",
    "updatedAt": "2026-06-29T12:30:00+08:00",
    "note": "Manually placed test marker"
  }
]
```

`group` is the large filter group. `type` is the fine marker type. `displayName` is the user-facing marker type label.

`mapX` and `mapY` are coordinates on the full base map. `x` and `y` are retained for screen-coordinate fallback and for compatibility with older point files.

`source`, `createdAt`, and `updatedAt` are optional metadata fields. F10 debug capture writes:

- `source`: `debug_f10`
- `createdAt`: the local timestamp when the point was created
- `updatedAt`: the local timestamp when the point was last changed by the tool

Legacy point entries without `group` and `displayName` are normalized on load:

- `high_value_loot` -> `special_events / loose_loot`
- `quest` -> `special_events / info`
- `extraction` -> `extracts / hatch_extract`
- `keycard` -> `special_events / key`
- `note` -> `special_events / info`

## settings.json

```json
{
  "screenWidth": 2560,
  "screenHeight": 1440,
  "overlayOpacity": 0.75,
  "markerSize": 18,
  "hotkeyToggleOverlay": "F7",
  "hotkeyReloadConfig": "F9",
  "hotkeyRecordPoint": "F10",
  "hotkeyCaptureScreenshot": "F11",
  "hotkeyUndoDebugPoint": "F12",
  "debugPointGroup": "special_events",
  "debugPointType": "info",
  "debugPointDisplayName": "Info",
  "currentMapId": "buried_ruins",
  "pointCoordinateMode": "full_view",
  "showLabels": true,
  "enableDebugMode": true,
  "saveDebugScreenshots": false,
  "detectionIntervalMs": 1000,
  "enabledGroups": [
    "containers",
    "electrical_tech",
    "arc_enemies",
    "plants_resources",
    "movement",
    "extracts",
    "spawns_locations",
    "special_events"
  ],
  "enabledTypes": [
    "weapon_case",
    "hatch_extract",
    "loose_loot",
    "key",
    "info"
  ],
  "confidenceThreshold": 0.75
}
```

`pointCoordinateMode` accepts:

- `full_view`: user manually zooms the in-game map out to minimum, then the app interprets `mapX/mapY` through `fullViewScreenRect`.
- `base_map`: legacy name for fixed map-coordinate projection through `defaultScreenMapRect`.
- `screen`: fallback mode; interpret `points.json` `x/y` directly as 2560x1440 screen coordinates.

## backups

Point backups are plain JSON copies of `points.json` stored in:

```text
debug/backups/
```

Automatic backups are created before the app overwrites `config/points.json`. Manual export uses the same folder. Import reads a selected JSON backup, normalizes marker taxonomy, backs up the current file, then writes the restored points.

Backup retention keeps only the newest 50 files matching:

```text
points_*.json
```

Older point backups are pruned after each new backup. `.gitkeep` and unrelated files in `debug/backups` are left alone.

## maps.json

```json
[
  {
    "mapId": "buried_ruins",
    "displayName": "Buried Ruins / 掩埋废墟",
    "baseMapImage": "assets/maps/buried_ruins/base_map.png",
    "baseMapWidth": 2432,
    "baseMapHeight": 1140,
    "fullViewScreenRect": {
      "left": 64,
      "top": 149,
      "width": 2432,
      "height": 1140
    },
    "defaultScreenMapRect": {
      "left": 64,
      "top": 149,
      "width": 2432,
      "height": 1140
    },
    "defaultScale": 1.0,
    "defaultOffsetX": 0,
    "defaultOffsetY": 0
  }
]
```

`fullViewScreenRect` is the fixed full-map viewport on a 2560x1440 screen after the user manually zooms out to minimum. Phase 3 supports only this standard full-view state.

The app can set `fullViewScreenRect` from two captured screen corners:

1. User manually zooms the in-game map out to minimum.
2. User clicks `Start Rect`.
3. User captures the visible map top-left corner.
4. User captures the visible map bottom-right corner.
5. The app writes the normalized rectangle into `maps.json`.

`defaultScreenMapRect` remains for legacy fixed-view experiments.
