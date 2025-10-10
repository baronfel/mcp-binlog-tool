# Baronfel.Binlog.MCP


This package provides a tool for reading and analyzing Microsoft Build Engine (MSBuild) binary log files (.binlog). It is designed to work with the Model Context Protocol (MCP) to facilitate structured logging and analysis of build processes, helping developers understand build performance, target execution, and project dependencies.

## Configuration

### VSCode

```json
{
  "mcp": {
    "servers": {
      "msbuild": {
        "command": "dnx",
        "args": [ "baronfel.binlog.mcp", "-y" ]
      }
    }
  }
}
```

## Tools

The binlog.mcp tool provides the following MCP tools for analyzing MSBuild binary log files:

### `load_binlog`
Load a binary log file for analysis. This must be called before using any other analysis tools.
- **Parameters**:
  - `path` (string): The path to a MSBuild binlog file to load and analyze
- **Description**: Loads the binlog file and builds internal mappings for projects, evaluations, and targets for efficient querying.

### `get_expensive_targets`
Get the N most expensive targets in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int): The number of top targets to return
- **Returns**: Array of strings describing target names, execution counts, and total duration

### `list_projects`
List all projects in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
- **Returns**: Array of strings with project file paths, IDs, and entry target information

### `list_evaluations`
List all evaluations for a specific project in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectFilePath` (string): The path to the project file to get evaluations for
- **Returns**: Array of strings with evaluation IDs, project files, and durations
- **Note**: Use `list_projects` to find project file paths first

### `get_evaluation_global_properties`
Get the global properties for a specific evaluation in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `evaluationId` (int): The ID of the evaluation to get properties for
- **Returns**: Array of strings with property names and values
- **Note**: Global properties are what make evaluations distinct from one another within the same project

### `get_target_info_by_name`
Get detailed information about a specific target by name.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetName` (string): The name of the target to get information for
- **Returns**: Array of strings with target details including duration, build reason, success status, and messages

### `get_target_info_by_id`
Get detailed information about a specific target by ID (more efficient than by name).
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetId` (int): The ID of the target to get information for
- **Returns**: Array of strings with target details including duration, build reason, success status, and messages

### `get_project_target_list`
Get a list of all targets for a specific project.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get targets for
- **Returns**: Comma-separated string of target names, IDs, and durations

## Prompts

The binlog.mcp tool provides the following MCP prompts for common analysis workflows:

### `initial_build_analysis`
**Title**: Analyze Binary Log
**Description**: Perform a build of the current workspace and profile it using the binary logger.

This prompt guides you through:
1. Building the current workspace with `dotnet build --binaryLogger`
2. Loading the generated binlog file
3. Analyzing the most expensive targets
4. Checking for projects with multiple evaluations (which can indicate overbuilding)

### `compare_binlogs`
**Title**: Compare Binary Logs
**Description**: Compare two binary logs.

This prompt helps you:
1. Load two different binlog files
2. Compare project evaluations between builds
3. Analyze timing differences in a tabular format
4. Identify performance regressions or improvements
