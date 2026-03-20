# baronfel.binlog.cli

A `dotnet` global/local tool for analyzing and investigating MSBuild binary log files (`.binlog`) from the command line.

## Installation

```bash
dotnet tool install -g baronfel.binlog.cli
```

## Usage

```bash
binlog <command> <binlog-path> [options]
```

## Commands

| Command | Description |
|---|---|
| `load` | Load and cache a binlog file |
| `diagnostics` | Extract errors and warnings |
| `list-files` | List source files referenced in the build |
| `get-file` | Get the content of a specific source file |
| `list-projects` | List all projects |
| `expensive-projects` | Top N most expensive projects |
| `project-build-time` | Build time for a specific project |
| `project-target-list` | Targets executed in a project |
| `project-target-times` | Timing for all targets in a project |
| `expensive-targets` | Top N most expensive targets |
| `search-targets` | Find all executions of a named target |
| `target-info` | Details about a specific target execution |
| `expensive-tasks` | Top N most expensive MSBuild tasks |
| `list-tasks` | Tasks within a specific target |
| `task-info` | Detailed info about a specific task invocation |
| `search-tasks` | Find all invocations of a named task |
| `expensive-analyzers` | Top N most expensive Roslyn analyzers |
| `task-analyzers` | Analyzer data from a specific Csc task |
| `list-evaluations` | All evaluations for a project |
| `eval-global-props` | Global properties for an evaluation |
| `eval-properties` | Specific properties from an evaluation |
| `eval-items` | Specific items from an evaluation |
| `timeline` | Build timeline and node utilization |
| `search` | Freetext search with full MSBuild Structured Log Viewer query syntax |

All output is compact JSON, suitable for agent and scripting workflows.

## Example

```bash
# Find the top 5 most expensive projects
binlog expensive-projects /path/to/build.binlog --top 5

# Search for CoreCompile targets
binlog search-targets /path/to/build.binlog CoreCompile

# Extract errors and warnings
binlog diagnostics /path/to/build.binlog --severity Error
```
