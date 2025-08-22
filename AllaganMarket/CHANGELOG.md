# Changelog

All notable changes to this project will be documented in this file.

The log versioning the plugin versioning will not match as 1.0.0.0 technically does not match semantic versioning but the headache of trying to change this would be too much.
Instead the changelog reader and automation surrounding plugin PRs will add the 1. back in 

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.html).

## [1.2.0.1] - 2025-08-22

### Added
- Added debug windows available for end-users when debugging specific issues
- Added a /amconfig command for opening the configuration window
- If undercutting has to fall back to the NQ price a small tooltip will be displayed making it more obvious to the end user why the price was used

### Fixed
- Fixed a bug if the current sales CSV fails to parse correctly
- Fixed a bug where a retainer that you owned's listings would be ignored
- Fixed how the arrow buttons were rendered
- Changing the date in the sales summary should update the list instantly instead of needing a reorder

## [1.2.0.0] - 2025-08-09

### Fixed
- API13 support
- The arrow buttons inside the main UI will be weirdly sized until ArrowButton returns in a new dalamud release

