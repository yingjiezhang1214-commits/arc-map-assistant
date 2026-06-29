# Roadmap

## Phase 1: Overlay And Marker Tooling MVP

Status: sealed on 2026-06-30.

- WPF app shell.
- Transparent topmost overlay.
- Mouse click-through.
- Configurable function-key show/hide, default `F7`.
- `F9` reload all local config.
- `F10` record debug points with selected `group/type/displayName`.
- `F12` undo the newest debug point.
- Load and save `points.json`, `settings.json`, and `maps.json`.
- Render fixed-view markers from `base_map` coordinates, with screen-coordinate fallback.
- Point table search, filters, priority sorting, duplicate, batch delete, batch type change, and batch enabled toggles.
- Manual export/import backups under `debug/backups`.
- Automatic backup before point saves, retaining the newest 50 backups.
- Overlay highlights selected table points.

## Phase 2: Manual Screenshot Map Detection

Status: sealed on 2026-06-30.

- Screenshot current primary screen on hotkey.
- Template-match fixed map UI elements.
- Show overlay only when map UI is detected.
- Save debug screenshots only when enabled.

## Phase 3: Fixed Map Coordinate Calibration

Status: next.

Goal: complete Buried City / Buried Ruins fixed default-view coordinate calibration before any zoom/drag recognition.

- Add map configs.
- Convert base-map coordinates to screen coordinates.
- Confirm `screenMapRect` against real screenshots.
- Add calibration workflow for buried_ruins points.
- Validate overlay alignment on default map view.
- Keep legacy screen-coordinate fallback.

## Phase 4: Zoom/Pan Recognition

Status: later.

- Match visible map region against base map.
- Estimate Homography.
- Project visible markers only when confidence is high enough.
