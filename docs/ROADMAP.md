# Roadmap

## Phase 1: Fixed Screen Overlay MVP

- WPF app shell.
- Transparent topmost overlay.
- Mouse click-through.
- Configurable function-key show/hide, default `F7`.
- `F9` reload config.
- Load `points.json`.
- Load `settings.json`.
- Render fixed 2560x1440 screen-coordinate markers.

## Phase 2: Manual Screenshot Map Detection

- Screenshot current primary screen on hotkey.
- Template-match fixed map UI elements.
- Show overlay only when map UI is detected.
- Save debug screenshots only when enabled.

## Phase 3: Fixed Map Coordinate Calibration

- Add map configs.
- Convert base-map coordinates to screen coordinates.
- Add coordinate transformer tests.

## Phase 4: Zoom/Pan Recognition

- Match visible map region against base map.
- Estimate Homography.
- Project visible markers only when confidence is high enough.
