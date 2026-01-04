# Baronfel.Binlog.MCP


This package provides a tool for reading and analyzing Microsoft Build Engine (MSBuild) binary log files (.binlog). It is designed to work with the Model Context Protocol (MCP) to facilitate structured logging and analysis of build processes, helping developers understand build performance, target execution, project dependencies, diagnostics, and enabling powerful search capabilities.

## Tools

The binlog.mcp tool provides the following MCP tools for analyzing MSBuild binary log files, organized by feature area:

## Binlog Loading

### `load_binlog`
Load a binary log file from a given absolute path. This must be called before using any other analysis tools.
- **Parameters**:
  - `path` (string): The absolute path to a MSBuild binlog file to load and analyze
- **Returns**: `InterestingBuildData` containing total duration in milliseconds and node count
- **Description**: Loads the binlog file and builds internal mappings for projects, evaluations, and targets for efficient querying.

## Analyzer Analysis

### `get_expensive_analyzers`
Get the N most expensive Roslyn analyzers and source generators across the entire build.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int, optional): The number of top analyzers to return. If not specified, returns all analyzers
- **Returns**: Dictionary of analyzer/generator names mapped to `AggregatedAnalyzerData` containing name, execution count, total/average/min/max durations
- **Note**: Aggregates analyzer performance data from all Csc task invocations in the build; helps identify slow analyzers

### `get_task_analyzers`
Extract Roslyn analyzer and source generator execution data from a specific Csc task invocation.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the task
  - `targetId` (int): The ID of the target containing the task
  - `taskId` (int): The ID of the Csc task to analyze
- **Returns**: `CscAnalyzerData` containing dictionaries of analyzer and generator assemblies with individual analyzer timing data, or null if no analyzer data found
- **Note**: Only works with Csc (C# compiler) tasks that have analyzer performance data enabled

## Diagnostic Analysis

### `get_diagnostics`
Extract diagnostic information (errors, warnings) from a binlog file with optional filtering.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `includeErrors` (bool, default true): Include error diagnostics
  - `includeWarnings` (bool, default true): Include warning diagnostics
  - `includeDetails` (bool, default true): Include detailed diagnostic information like file paths, line numbers, etc.
  - `projectIds` (int[], optional): Filter by specific project IDs
  - `targetIds` (int[], optional): Filter by specific target IDs
  - `taskIds` (int[], optional): Filter by specific task IDs
  - `maxResults` (int, optional): Maximum number of diagnostics to return
- **Returns**: `DiagnosticAnalysisResult` containing filtered diagnostics with severity classification (Error, Warning, Info), source locations, and context information
- **Features**: Single-pass optimization for efficient processing, enum-based severity classification, comprehensive filtering options
- **Use Cases**: Identify build errors and warnings, filter diagnostics by project/target/task scope, analyze diagnostic patterns across the build

## Evaluation Analysis

### `get_evaluation_global_properties`
Get the global properties for a specific evaluation in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `evaluationId` (int): The ID of the evaluation to get properties for
- **Returns**: Dictionary of property names mapped to their values
- **Note**: Global properties are what make evaluations distinct from one another within the same project

### `list_evaluations`
List all evaluations for a specific project in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectFilePath` (string): The path to the project file to get evaluations for
- **Returns**: Dictionary of evaluation IDs mapped to `EvaluationData` containing ID, project file, and duration in milliseconds
- **Note**: Use `list_projects` to find project file paths first

## File Analysis

### `get_file_from_binlog`
Get a specific source file from the loaded binary log file.
- **Parameters**:
  - `binlogPath` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `filePathInsideBinlog` (string): An absolute path of a file inside the binlog
- **Returns**: The text content of the file, or null if not found

### `list_files_from_binlog`
List all source files from the loaded binary log file, optionally filtering by a path pattern.
- **Parameters**:
  - `binlogPath` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `pathPattern` (string, optional): An optional path pattern to filter the files inside the binlog
- **Returns**: Collection of file paths from the binlog

## Project Analysis

### `get_expensive_projects`
Get the N most expensive projects in the loaded binary log file, aggregated at the project level.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int): The number of top projects to return
  - `excludeTargets` (string[], optional): Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])
  - `sortByExclusive` (bool, default true): Whether to sort by exclusive time (true) or inclusive time (false)
- **Returns**: Dictionary of project IDs mapped to `ExpensiveProjectData` containing project file, ID, exclusive/inclusive durations, and target count
- **Note**: Results are cached for performance; uses same cache as `get_project_build_time`

### `get_project_build_time`
Get the total build time for a specific project, calculating exclusive time across all its targets with optional filtering.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get build time for
  - `excludeTargets` (string[], optional): Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])
- **Returns**: `ProjectBuildTimeData` containing exclusive duration, inclusive duration, and target count
- **Note**: Results are cached for performance; first call populates data for all projects

### `get_project_target_list`
Get a list of all targets for a specific project.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get targets for
- **Returns**: Collection of `ProjectTargetListData` containing target ID, name, and duration in milliseconds

### `get_project_target_times`
Get all target execution times for a specific project in one call.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project to get target times for
- **Returns**: Dictionary of target IDs mapped to `TargetTimeData` containing ID, name, inclusive/exclusive durations, and skip status
- **Note**: More efficient than querying each target individually

### `list_projects`
List all projects in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
- **Returns**: Dictionary of project IDs mapped to `ProjectData` containing project file path, ID, and entry target information

## Search Analysis

### `search_binlog`
Perform powerful freetext search within a binlog file using the same search capabilities as the MSBuild Structured Log Viewer.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `query` (string): The search query to execute using MSBuild Structured Log Viewer query syntax
  - `maxResults` (int, default 300): Maximum number of search results to return
  - `includeDuration` (bool, default true): Whether to include duration information for timed nodes
  - `includeStartTime` (bool, default false): Whether to include start time information for timed nodes
  - `includeEndTime` (bool, default false): Whether to include end time information for timed nodes
  - `includeContext` (bool, default true): Whether to include context information like project, target, task IDs
- **Returns**: `SearchAnalysisResult` containing matched nodes with context information, timing data, and matched field details
- **Query Language Features**:
  - **Basic Search**: Text search, exact match with quotes, multiple terms (AND logic)
  - **Node Type Filtering**: `$task`, `$project`, `$target`, shortcuts like `$csc`, `$rar`
  - **Property Matching**: `name=value`, `value=text` for precise field matching
  - **Hierarchical Search**: `under()`, `notunder()`, `project()`, `not()` for nested queries
  - **Time Filtering**: `start<"date"`, `start>"date"`, `end<"date"`, `end>"date"`
  - **Special Properties**: `skipped=true/false`, `height=number`, node index search `$123`
  - **Result Enhancement**: `$time`, `$start`, `$end` to include timing information
- **Examples**:
  - `"error CS1234"` - Find exact error message
  - `$task Copy` - Find all Copy tasks
  - `under($project MyProject)` - Find nodes under MyProject
  - `name=Configuration value=Debug` - Find Configuration=Debug nodes
- **Use Cases**: Find specific build events, analyze error messages, trace build flow, identify performance bottlenecks, debug complex build scenarios

## Target Analysis

### `get_expensive_targets`
Get the N most expensive targets in the loaded binary log file.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int): The number of top targets to return
- **Returns**: Dictionary of target names mapped to `TargetExecutionData` containing execution count, skipped count, inclusive duration, and exclusive duration in milliseconds

### `get_target_info_by_id`
Get detailed information about a specific target by ID (more efficient than by name).
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetId` (int): The ID of the target to get information for
- **Returns**: `TargetInfo` containing ID, name, duration, success status, skipped status, build reason, and target messages

### `get_target_info_by_name`
Get detailed information about a specific target by name.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetName` (string): The name of the target to get information for
- **Returns**: `TargetInfo` containing ID, name, duration, success status, skipped status, build reason, and target messages

### `search_targets_by_name`
Find all executions of a specific target across all projects and return their timing information.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `targetName` (string): The name of the target to search for (case-insensitive)
- **Returns**: Dictionary of unique keys mapped to `TargetExecutionInfo` containing project ID, project file, target ID, inclusive/exclusive durations, and skip status
- **Example**: Search for "CoreCompile" to see all compilation executions across the build

## Task Analysis

### `get_expensive_tasks`
Get the N most expensive MSBuild tasks across the entire build, aggregated by task name.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `top_number` (int, optional): The number of top tasks to return. If not specified, returns all tasks
- **Returns**: Dictionary of task names mapped to `TaskExecutionData` containing task name, assembly, execution count, total/average/min/max durations
- **Note**: Useful for identifying which tasks consume the most build time across all projects

### `get_task_info`
Get detailed information about a specific MSBuild task invocation.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the task
  - `targetId` (int): The ID of the target containing the task
  - `taskId` (int): The ID of the task to get information for
- **Returns**: `TaskDetails` containing task name, assembly, duration, parameters, and messages

### `list_tasks_in_target`
List all MSBuild task invocations within a specific target, ordered by ID.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `projectId` (int): The ID of the project containing the target
  - `targetId` (int): The ID of the target to list tasks for
- **Returns**: Dictionary of task IDs mapped to `TaskDetails` containing task name, assembly, duration, parameters, and messages

### `search_tasks_by_name`
Find all invocations of a specific MSBuild task across all projects.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `taskName` (string): The name of the task to search for (case-insensitive)
- **Returns**: Nested dictionary structure (project ID → target ID → task ID) mapped to `SimpleTaskInfo` containing task name and duration
- **Example**: Search for "Csc" to find all C# compilation tasks

## Timeline Analysis

### `get_node_timeline`
Get the timeline of active and inactive time for a specific build node.
- **Parameters**:
  - `binlog_file` (string): The path to a MSBuild binlog file that has been loaded via `load_binlog`
  - `nodeId` (int): The ID of the node to get timeline for
- **Returns**: `NodeStats` containing active and inactive time in milliseconds for the specified node
- **Note**: Helps understand node utilization and identify bottlenecks in parallel builds

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
