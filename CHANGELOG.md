# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2026-09-13

### Added

- Initial package layout: Kinetics runtime moved into its own package
  (`Runtime`, `Editor`, `Tests/Editor`) with dedicated assembly definitions.
- Kinetic manager, trajectory groups, and trajectory implementations (linear,
  constant velocity).
- Job System / Burst batch update path.
- Optional DOTS/Entities integration behind the `KINEMATICS_ENTITIES` version
  define.

Pre-release: API is unstable and expected to change without notice until 1.0.0.
