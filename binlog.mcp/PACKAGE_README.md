# Baronfel.Binlog.MCP


This package provides a tool for reading and analyzing Microsoft Build Engine (MSBuild) binary log files (.binlog). It is designed to work with the Model Context Protocol (MCP) to facilitate structured logging and analysis of build processes, helping developers understand build performance, target execution, and project dependencies.

## Tools

The binlog.mcp tool provides the following MCP tools for analyzing MSBuild binary log files:

### `load_binlog`
Load a binary log file from a given absolute path. This must be called before using any other analysis tools.
- **Parameters**:
  - `path` (string): The absolute path to a MSBuild binlog file to load and analyze
- **Returns**: `InterestingBuildData` containing total duration in milliseconds and node count
- **Description**: Loads the binlog file and builds internal mappings for projects, evaluations, and targets for efficient querying.

### `get_expensive_targets`
Get the N most expensive targets in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int): The number of top targets to return
- **Returns**: Dictionary of target names mapped to `TargetExecutionData` containing execution count, skipped count, inclusive duration, and exclusive duration in milliseconds

### `list_projects`
List all projects in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
- **Returns**: Dictionary of project IDs mapped to `ProjectData` containing project file path, ID, and entry target information

### `list_evaluations`
List all evaluations for a specific project in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectFilePath` (string): The path to the project file to get evaluations for
- **Returns**: Dictionary of evaluation IDs mapped to `EvaluationData` containing ID, project file, and duration in milliseconds
- **Note**: Use `list_projects` to find project file paths first

### `get_evaluation_global_properties`
Get the global properties for a specific evaluation in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `evaluationId` (int): The ID of the evaluation to get properties for
- **Returns**: Dictionary of property names mapped to their values
- **Note**: Global properties are what make evaluations distinct from one another within the same project

### `get_target_info_by_name`
Get detailed information about a specific target by name.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetName` (string): The name of the target to get information for
- **Returns**: `TargetInfo` containing ID, name, duration, success status, skipped status, build reason, and target messages

### `get_target_info_by_id`
Get detailed information about a specific target by ID (more efficient than by name).
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetId` (int): The ID of the target to get information for
- **Returns**: `TargetInfo` containing ID, name, duration, success status, skipped status, build reason, and target messages

### `get_project_target_list`
Get a list of all targets for a specific project.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get targets for
- **Returns**: Collection of `ProjectTargetListData` containing target ID, name, and duration in milliseconds

### `list_files_from_binlog`
List all source files from the loaded binary log file, optionally filtering by a path pattern.
- **Parameters**:
  - `binlogPath` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `pathPattern` (string, optional): An optional path pattern to filter the files inside the binlog
- **Returns**: Collection of file paths from the binlog

### `get_file_from_binlog`
Get a specific source file from the loaded binary log file.
- **Parameters**:
  - `binlogPath` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `filePathInsideBinlog` (string): An absolute path of a file inside the binlog
- **Returns**: The text content of the file, or null if not found

### `get_project_build_time`
Get the total build time for a specific project, calculating exclusive time across all its targets with optional filtering.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get build time for
  - `excludeTargets` (string[], optional): Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])
- **Returns**: `ProjectBuildTimeData` containing exclusive duration, inclusive duration, and target count
- **Note**: Results are cached for performance; first call populates data for all projects

### `get_expensive_projects`
Get the N most expensive projects in the loaded binary log file, aggregated at the project level.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int): The number of top projects to return
  - `excludeTargets` (string[], optional): Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])
  - `sortByExclusive` (bool, default true): Whether to sort by exclusive time (true) or inclusive time (false)
- **Returns**: Dictionary of project IDs mapped to `ExpensiveProjectData` containing project file, ID, exclusive/inclusive durations, and target count
- **Note**: Results are cached for performance; uses same cache as `get_project_build_time`

### `get_project_target_times`
Get all target execution times for a specific project in one call.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get target times for
- **Returns**: Dictionary of target IDs mapped to `TargetTimeData` containing ID, name, inclusive/exclusive durations, and skip status
- **Note**: More efficient than querying each target individually

### `search_targets_by_name`
Find all executions of a specific target across all projects and return their timing information.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `targetName` (string): The name of the target to search for (case-insensitive)
- **Returns**: Dictionary of unique keys mapped to `TargetExecutionInfo` containing project ID, project file, target ID, inclusive/exclusive durations, and skip status
- **Example**: Search for "CoreCompile" to see all compilation executions across the build

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
