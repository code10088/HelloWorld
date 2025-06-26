# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.10.0] - 2025-05-09

### Changed
- Reduced the amount of data sent to GPU when using Transform Updates on Prefab Manager.

## [0.9.17] - 2025-02-11

### New
- Added edit mode rendering capability to the Prefab Manager when default renderers on the prefabs are disabled.

### Changed
- Optimized automatic Add/Remove operations for the Prefab Manager during initialization, improving performance.

### Fixed
- Fixed an issue where the material variation definition could not be created if the prefab's parent had no renderers.

## [0.9.14] - 2024-12-13

### Fixed
- Resolved a crash that occurred when adding nested prefabs to the Prefab Manager.

## [0.9.8] - 2024-10-22

### Fixed
- Material variation demo scene shader warning on Unity 6000.0.23f1.
- Material variation shader conversion error when using built-in shaders.

## [0.9.5] - 2024-09-09

### Added
- Prefab Manager Add/Remove instance performance improvements.

### Fixed
- Prototype could not be removed from the Prefab Manager if the GPUIPrefab component was manually deleted from a prefab.
- GPUIPrefab component was not automatically added to a prefab when using a variant of a model prefab as a prototype on the Prefab Manager.

## [0.9.0] - 2024-07-22

### Added
- Initial release.