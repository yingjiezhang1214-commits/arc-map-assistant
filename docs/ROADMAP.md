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
- Render markers from local map coordinates, with screen-coordinate fallback.
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

## Phase 3: Full-View Map Coordinate Calibration

Status: next.

Goal: complete Buried City / Buried Ruins full-map coordinate calibration for the standard manual full-view state.

- User manually zooms the in-game map out to minimum so the whole map is visible.
- The app only supports this standard `full_view` state in this phase.
- Store `fullViewScreenRect` in `maps.json`.
- Set `fullViewScreenRect` from two captured screen corners: top-left and bottom-right.
- Store marker coordinates as `mapX/mapY` in `points.json`.
- Convert F10 screen captures to `mapX/mapY`.
- Convert `mapX/mapY` back to screen coordinates for overlay rendering.
- Keep legacy screen-coordinate fallback.
- Do not handle arbitrary zoom, drag, or Homography in this phase.

## Phase 4: Zoom/Pan Recognition

Status: later.

- Match visible map region against base map.
- Estimate Homography.
- Project visible markers only when confidence is high enough.
