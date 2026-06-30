# Buried City / Buried Ruins Assets

Manual screenshot samples and imported marker data for the `buried_ruins` map id.

Screenshot files are collected for map-open detection and later map-region matching work. Imported marker files are static local data used by the overlay.

## Imported Markers

ARC Raiders Hub Buried City data is imported with:

```powershell
node .\tools\import_arcraidershub_buried_city.js
```

The committed import snapshot lives in:

```text
assets/maps/buried_ruins/imports/
  arcraidershub_buried_city_points.json
  arcraidershub_buried_city_summary.json
```

Hub coordinates are converted from center-origin source coordinates to local full-map coordinates with:

```text
mapX = source x + 3072
mapY = source y + 3072
```

The app still expects the user to manually zoom the map out to the minimum full-view state before using these coordinates.

## Source

- Imported from: `C:\Users\ying\Videos\NVIDIA\Arc Raiders\tu.zip`
- Import date: 2026-06-29
- Original folders:
  - `打开地图默认缩放/`
  - `地图放大拖动后/`
  - `未打开地图/`

## Directory Layout

```text
assets/maps/buried_ruins/samples/
  map_default/
    buried_ruins_default_001.png
    ...
  map_zoom_dragged/
    buried_ruins_zoom_dragged_001.png
    ...
  not_map/
    buried_ruins_not_map_001.png
    ...
```

## Sample Groups

- `samples/map_default/`
  - Map is open at the default zoom and position.
  - Use later for baseline map UI detection and default-coordinate calibration.
  - Current count: 6 PNG files.

- `samples/map_zoom_dragged/`
  - Map is open after zooming and dragging.
  - Use later for feature matching, visible-region matching, and Homography experiments.
  - Current count: 5 PNG files.

- `samples/not_map/`
  - Game is not showing the map screen.
  - Use later as negative samples for map-open detection.
  - Current count: 10 PNG files.

## Current Template

```text
assets/maps/buried_ruins/templates/map_tab_template.png
```

This template is cropped from `samples/map_default/buried_ruins_default_001.png` and captures the selected top navigation `地图` tab. It is used only for fixed-region map-open detection.

Initial sample confidence check with threshold `0.75`:

- `map_default`: 0.953 to 1.000
- `not_map`: 0.143 to 0.298

## Fixed Default-View Calibration

Current `config/maps.json` calibration:

```text
screenMapRect.left = 64
screenMapRect.top = 149
screenMapRect.width = 2432
screenMapRect.height = 1140
baseMapWidth = 6144
baseMapHeight = 6144
```

This is a first-pass full-view mapping. It intentionally does not support arbitrary zoomed or dragged map views yet.

## Naming Rule

Sample filenames are normalized during import:

```text
buried_ruins_default_###.png
buried_ruins_zoom_dragged_###.png
buried_ruins_not_map_###.png
```

The sequence number is local to each sample group.

## Safety Boundary

These samples are ordinary screenshots. They are local files only. Importing them does not add memory reading, process injection, graphics API hooks, driver capture, input simulation, OpenCV, or automatic recognition.
