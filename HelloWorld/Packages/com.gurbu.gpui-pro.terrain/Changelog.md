# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.10.4] - 2025-06-20

### New
- Improved performance for vegetation transform matrix calculations.

### Fixed
- Fixed an issue where the Terrain module attempted to load the Foliage shader before it was imported.

## [0.9.18] - 2025-02-17

### Fixed
- Resolved the terrain alignment issue for detail instances.

## [0.9.17] - 2025-02-11

### New
- Added a warning message and a fix button for prefab terrain references in the Terrains list of Detail Manager and Tree Manager.

### Fixed
- Fixed a null reference error that occurred when terrain tree prototypes could not be found.

## [0.9.16] - 2025-01-23

### Changed
- Optimized the Auto-Find processes in the Tree and Detail Managers for terrains, enhancing terrain streaming performance.

## [0.9.14] - 2024-12-13

### Fixed
- Fixed an issue where modifying the mesh or materials of a texture-type detail prototype unintentionally impacted other texture-type detail prototypes.
- Resolved an issue with detail instance positioning at the (0,0) index on the terrain.

## [0.9.11] - 2024-11-04

### Changed
- Detail density calculation improvements for Coverage Mode.
- Refined terrain module design to support better extensibility.
- Optimized data loading performance for the Tree Manager.

### Fixed
- Resolved an issue where the incorrect tree prototype was rendered when multiple tree prototypes shared the same prefab.

## [0.9.5] - 2024-09-09

### Added
- In edit mode, the Tree and Detail Managers can now render terrain details and trees from other scenes.
- The Tree and Detail Managers now include an option to automatically add terrains from scenes loaded at runtime.
- Map Magic 2 integration component for runtime generated terrains.

### Fixed
- 'Add Active Terrains' button on Detail and Tree Managers would add references to terrains in other scenes when multiple scenes with terrains were loaded in edit mode, causing a 'Scene mismatch' error.

## [0.9.4] - 2024-08-10

### Added
- New RequireUpdate API methods for Tree and Detail Managers to handle runtime terrain modifications.

### Fixed
- Detail Manager IndexOutOfRangeException when using Coverage mode with multiple prototypes that have the same prefab or texture.

## [0.9.0] - 2024-07-22

### Added
- Initial release.