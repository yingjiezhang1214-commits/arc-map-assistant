# Arc Raiders AI Map Assistant

Independent Windows desktop map assistant prototype for ARC Raiders.

This first MVP only renders local JSON map markers in a transparent WPF overlay. It does not read game memory, inject into the game, hook graphics APIs, capture drivers, simulate input, or automate gameplay.

## Current Scope

- Windows 10/11 desktop app.
- Designed for 2560x1440, borderless windowed game mode, 100% UI scale, single monitor.
- Transparent topmost overlay.
- Mouse click-through overlay.
- `F7` hides the overlay if visible; if hidden, it captures the current screen once and shows the overlay only when the Buried Ruins map is detected above the confidence threshold.
- `F9` reloads `config/points.json` and `config/settings.json`.
- `F10` records the current mouse screen coordinate to `config/points.json` when debug mode is enabled, using the selected group/type in the main window.
- `F11` captures the current primary screen to `debug/screenshots` with a timestamped PNG filename.
- `F12` removes the most recent `F10` debug point.
- Local JSON marker and settings files.
- Point list search, filtering, sorting, duplicate, batch edit, and backup tools.

## Safety Notice

Third-party overlays may still carry account risk in online games. This project reduces risk by staying outside the game process, but it does not guarantee zero ban or enforcement risk.

See [docs/SAFETY_BOUNDARY.md](docs/SAFETY_BOUNDARY.md).

## Run

This repository is an SDK-style .NET 8 WPF project.

```powershell
dotnet build .\src\ArcMapAssistant.App\ArcMapAssistant.App.csproj
dotnet run --project .\src\ArcMapAssistant.App\ArcMapAssistant.App.csproj
```

If this machine only has the .NET Runtime, install the .NET 8 SDK first or open the project on a machine with Visual Studio 2022 and the .NET desktop workload.

## Configure Points

Edit `config/points.json`.

By default, `mapX` and `mapY` are full-view map coordinates for the configured map in `config/maps.json`.

Set `pointCoordinateMode` to `screen` in `config/settings.json` to use the older fixed 2560x1440 `x/y` screen-coordinate fallback.

```json
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
  "note": "Manually placed test marker"
}
```

Press `F9` while the app is running to reload.

## Configure Hotkeys

Edit `config/settings.json`.

```json
{
  "hotkeyToggleOverlay": "F7",
  "hotkeyReloadConfig": "F9",
  "hotkeyRecordPoint": "F10",
  "hotkeyCaptureScreenshot": "F11",
  "hotkeyUndoDebugPoint": "F12"
}
```

Only simple function-key hotkeys such as `F1` through `F24` are supported in this MVP. The default detect/toggle key is `F7`, but you can change it back to `F8`, `F10`, `F11`, or another function key in `config/settings.json`.

## Debug Point Capture

Set `enableDebugMode` to `true` in `config/settings.json`.

- Show the overlay with the configured detect/toggle hotkey. The default is `F7`.
- Choose the current F10 marker group/type from the main window.
- Move the mouse to the target screen position.
- Read the current 2560x1440 coordinate from the top-left debug panel.
- Press `F10` to append a marker with the selected `group`, `type`, and `displayName` to `config/points.json`.
- Press `F12` to undo the newest point recorded by `F10`.
- Press `F9` to reload manually if needed. The app also reloads automatically after recording a point.

F10 debug points include:

- `source`: `debug_f10`
- `createdAt`: local ISO timestamp
- `updatedAt`: local ISO timestamp

## Screenshot Samples

Press `F11` to save the current primary screen as a PNG under `debug/screenshots`.

The filename format is:

```text
screenshot_yyyyMMdd_HHmmss_fff.png
```

This is intended only for manual sample collection before later image-recognition work. It does not perform recognition.

## Map Open Detection

The current detector only answers one question: is the Buried Ruins map open?

- Uses OpenCvSharp template matching.
- Uses `assets/maps/buried_ruins/templates/map_tab_template.png`.
- Searches a fixed top navigation region for the selected `地图` tab.
- Uses `config/settings.json` `confidenceThreshold`.
- If confidence is below the threshold, overlay stays hidden.
- It does not handle zoomed/dragged map alignment yet.
- It projects `mapX/mapY` points only for the configured full-view state.

## Category Filters

Use the category checkboxes in the main window to show or hide marker groups and types. Changes are saved to `config/settings.json` under `enabledGroups` and `enabledTypes`, then applied to the overlay immediately.

Marker filters now use a two-level structure:

- `enabledGroups`: large ARC Raiders Hub-style groups such as `containers`, `extracts`, and `special_events`.
- `enabledTypes`: fine marker types such as `weapon_case`, `hatch_extract`, `loose_loot`, `key`, and `info`.

Older point files that only have `type` are normalized on load. For example, legacy `high_value_loot` becomes `special_events / loose_loot`.

The F10 debug capture type is also saved in `config/settings.json`:

```json
{
  "debugPointGroup": "special_events",
  "debugPointType": "info",
  "debugPointDisplayName": "Info"
}
```

## Point Management

The main point table supports:

- Search by `name` and `note`.
- Filter by `group`, `type`, `enabled`, and `priority`.
- Sort by `priority` ascending or descending.
- Duplicate selected points.
- Delete selected points.
- Batch change selected points to a chosen `group/type`.
- Batch enable or disable selected points.
- Highlight selected table rows in the overlay.
- `Reload All` to reload settings, maps, and points together.

Saving points automatically exports the previous `config/points.json` to `debug/backups`.

Use `Export` to manually copy the current `points.json` into `debug/backups`, and `Import` to restore a JSON backup file.

The app keeps the newest 50 `points_*.json` backups and prunes older backup files after each new backup.

## Coordinate Calibration

Buried Ruins currently uses a manual full-view calibration:

- In-game, open the map.
- Manually zoom out to minimum so the whole map is visible.
- Keep the map in that standard full-view state.
- The app uses `fullViewScreenRect` to convert between screen `x/y` and point `mapX/mapY`.

```json
{
  "mapId": "buried_ruins",
  "baseMapWidth": 2432,
  "baseMapHeight": 1140,
  "fullViewScreenRect": {
    "left": 64,
    "top": 149,
    "width": 2432,
    "height": 1140
  }
}
```

This means a point at map coordinate `(0, 0)` renders at screen coordinate `(64, 149)` in the standard full-view state. This phase does not handle arbitrary zoomed or dragged map states.

## Close Overlay

- Press the configured detect/toggle hotkey to hide the overlay. The default is `F7`.
- Use the control window's Exit button to close the app.
- Closing the control window exits the app.
