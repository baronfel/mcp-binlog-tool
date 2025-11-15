# Changelog

## [0.0.7] - 2025-11-14

### Added

- New tool `get_project_build_time` to calculate total build time for a specific project with optional target filtering
- New tool `get_expensive_projects` to get top N most expensive projects sorted by exclusive or inclusive time
- New tool `get_project_target_times` to get all target execution times for a project in one call
- New tool `search_targets_by_name` to find all executions of a specific target across all projects
- Caching system for project build time data to improve performance on repeated queries

### Changed

- Unified exclusive/inclusive duration calculation logic across project-level analysis tools
- First call to project analysis tools now populates cached data for all projects in the binlog

## [0.0.6] - 2025-11-14

### Changed

- Fix serialization errors
- Bump to .NET 10 TFM

## [0.0.5] - 2025-11-02

### Changed

- Update packaging information to be valid for nuget.org's MCP server configuration examples

## [0.0.4] - 2025-11-02

### Changed

- Update packaging information to be valid for nuget.org's MCP server configuration examples

## [0.0.3] - 2025-10-10

### Added

- New structured outputs for all tools to improve performance
- Two new tools - one to list matching files in a binlog, and one to get contents of a specific file

### Changed

- Update package dependencies

## [0.0.2] - 2025-10-10

### Added

- .mcp/server.json configuration file to make it easier to get started

## [0.0.1] - 2025-10-10

### Added

- Initial set of MCP tools focused on loading binlogs and investigating evaluations
- A few starter prompts for common workflows
