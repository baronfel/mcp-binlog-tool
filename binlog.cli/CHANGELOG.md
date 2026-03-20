# Changelog

## [Unreleased]

## [0.0.1] - 2026-03-20

### Added

- Initial release of `binlog` CLI tool
- All analysis commands available as CLI subcommands, backed by the same implementation as the MCP server:
  - `load` — load and cache a binlog file
  - `diagnostics` — extract errors and warnings
  - `list-files`, `get-file` — source file access
  - `list-projects`, `expensive-projects`, `project-build-time`, `project-target-list`, `project-target-times` — project analysis
  - `expensive-targets`, `search-targets`, `target-info` — target analysis
  - `expensive-tasks`, `list-tasks`, `task-info`, `search-tasks` — task analysis
  - `expensive-analyzers`, `task-analyzers` — Roslyn analyzer analysis
  - `list-evaluations`, `eval-global-props`, `eval-properties`, `eval-items` — evaluation analysis
  - `timeline` — build timeline and parallelization analysis
  - `search` — full MSBuild Structured Log Viewer query syntax
