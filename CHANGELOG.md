# Changelog

## 2026-06-30

### Sealed

- Sealed the first-stage desktop tooling baseline for the ARC Raiders map assistant.
- Confirmed the project builds as a .NET 8 WPF application.

### Added

- Transparent click-through WPF overlay with configurable function-key hotkeys.
- `F7` map detect/toggle, `F9` reload all, `F10` debug point capture, `F11` screenshot capture, and `F12` debug point undo.
- Local `points.json`, `settings.json`, and `maps.json` workflows.
- Two-level marker taxonomy with `group`, `type`, and `displayName`.
- Main-window point management: search, filters, priority sort, duplicate, batch delete, batch type update, batch enabled toggle, import/export backup, and overlay selection highlight.
- Debug point metadata: `source`, `createdAt`, and `updatedAt`.
- Fixed default-view base-map coordinate mode with screen-coordinate fallback.
- OpenCvSharp template check for the limited "is map open" gate.
- Sample directories for Buried Ruins map captures.

### Changed

- Default detect/toggle hotkey is `F7`, while remaining configurable through `settings.json`.
- Point saves now automatically back up the previous `points.json`.
- Backup retention now keeps only the newest 50 `points_*.json` files under `debug/backups`.

### Not Included

- No game memory reads.
- No injection, hooks, graphics API interception, or keyboard/mouse automation.
- No new screenshot recognition work beyond the existing fixed map-open template check.
- No Homography, zoom, or dragged-map recognition.
- No website crawling or external marker data import in this sealed baseline.
