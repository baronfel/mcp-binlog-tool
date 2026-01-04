# Changelog

## [0.0.11] - 2026-01-04

### Added

- New **Diagnostic Analysis** feature with `get_diagnostics` tool to extract warnings and errors from binlogs
- New **Search Analysis** feature with `search_binlog` with full MSBuild Structured Log Viewer query syntax support.

## [0.0.10] - 2025-12-17

### Changed

- Fixed the `search_tasks_by_name` tool to correctly report results.

## [0.0.9] - 2025-12-09

### Added

- New tool `get_task_info` to get detailed information about a specific MSBuild task invocation including parameters and messages
- New tool `list_tasks_in_target` to list all MSBuild task invocations within a specific target
- New tool `search_tasks_by_name` to find all invocations of a specific MSBuild task across all projects
- New tool `get_expensive_tasks` to get the N most expensive MSBuild tasks aggregated by task name with execution statistics
- New tool `get_task_analyzers` to extract Roslyn analyzer and source generator execution data from a specific Csc task invocation
- New tool `get_expensive_analyzers` to get the N most expensive Roslyn analyzers and source generators across the entire build
- New tool `get_node_timeline` to get timeline of active and inactive time for a specific build node

### Changed

- Made `top_number` parameter optional all tools - returns all results when not specified

## [0.0.8] - 2025-12-03

### Changed

- Updating MSBuild.StructuredLogger 2.3.71 to 2.3.109.
- Updating Microsoft.Build.Framework 17.14.8 to 18.0.2.
- Updating Microsoft.Extensions.Hosting 9.0.9 to 10.0.0.
- Updating ModelContextProtocol 0.4.0-preview.3 to 0.4.1-preview.1.
- Updating Serilog.Extensions.Hosting 9.0.0 to 10.0.0.
- Updating Serilog.Sinks.Console 6.0.0 to 6.1.1.

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
