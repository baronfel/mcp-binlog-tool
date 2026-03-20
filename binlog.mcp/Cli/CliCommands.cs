using System.CommandLine;
using Binlog.MCP.Features.AnalyzerAnalysis;
using Binlog.MCP.Features.BinlogLoading;
using Binlog.MCP.Features.DiagnosticAnalysis;
using Binlog.MCP.Features.EvaluationAnalysis;
using Binlog.MCP.Features.ProjectAnalysis;
using Binlog.MCP.Features.SearchAnalysis;
using Binlog.MCP.Features.TargetAnalysis;
using Binlog.MCP.Features.TaskAnalysis;
using Binlog.MCP.Features.TimelineAnalysis;

namespace Binlog.MCP.Cli;

internal static class CliCommands
{
    public static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("MSBuild binlog analysis and investigation tool.");

        // Binlog loading
        root.AddCommand(BuildLoadCommand());
        root.AddCommand(BuildListFilesCommand());
        root.AddCommand(BuildGetFileCommand());

        // Diagnostics
        root.AddCommand(BuildDiagnosticsCommand());

        // Search
        root.AddCommand(BuildSearchCommand());

        // Projects
        root.AddCommand(BuildListProjectsCommand());
        root.AddCommand(BuildExpensiveProjectsCommand());
        root.AddCommand(BuildProjectBuildTimeCommand());
        root.AddCommand(BuildProjectTargetListCommand());
        root.AddCommand(BuildProjectTargetTimesCommand());

        // Targets
        root.AddCommand(BuildExpensiveTargetsCommand());
        root.AddCommand(BuildSearchTargetsCommand());
        root.AddCommand(BuildTargetInfoCommand());

        // Tasks
        root.AddCommand(BuildExpensiveTasksCommand());
        root.AddCommand(BuildTaskInfoCommand());
        root.AddCommand(BuildListTasksCommand());
        root.AddCommand(BuildSearchTasksCommand());

        // Analyzers
        root.AddCommand(BuildExpensiveAnalyzersCommand());
        root.AddCommand(BuildTaskAnalyzersCommand());

        // Evaluations
        root.AddCommand(BuildListEvaluationsCommand());
        root.AddCommand(BuildEvalGlobalPropsCommand());
        root.AddCommand(BuildEvalPropertiesCommand());
        root.AddCommand(BuildEvalItemsCommand());

        // Timeline
        root.AddCommand(BuildTimelineCommand());

        // MCP server (see Program.cs for dispatch)
        root.AddCommand(BuildMcpCommand());

        return root;
    }

    // ─── Binlog loading ──────────────────────────────────────────────────────

    static Command BuildLoadCommand()
    {
        var binlogArg = BinlogArg();
        var cmd = new Command("load", "Load a binlog file and display summary information.");
        cmd.AddArgument(binlogArg);
        cmd.SetHandler(binlog =>
        {
            var result = CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(result);
        }, binlogArg);
        return cmd;
    }

    static Command BuildListFilesCommand()
    {
        var binlogArg = BinlogArg();
        var patternOpt = new Option<string?>("--pattern", "Glob pattern to filter files (e.g. '**/*.cs')");
        var cmd = new Command("list-files", "List source files embedded in the binlog.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(patternOpt);
        cmd.SetHandler((binlog, pattern) =>
        {
            CliRunner.EnsureLoaded(binlog);
            var result = ListFilesTool.ListFilesFromBinlog(binlog, pattern);
            CliRunner.PrintJson(result);
        }, binlogArg, patternOpt);
        return cmd;
    }

    static Command BuildGetFileCommand()
    {
        var binlogArg = BinlogArg();
        var fileArg = new Argument<string>("file-path", "Absolute path of the file inside the binlog.");
        var cmd = new Command("get-file", "Get the contents of a source file embedded in the binlog.");
        cmd.AddArgument(binlogArg);
        cmd.AddArgument(fileArg);
        cmd.SetHandler((binlog, file) =>
        {
            CliRunner.EnsureLoaded(binlog);
            var result = GetFileTool.GetFileFromBinlog(binlog, file);
            if (result is null)
                Console.Error.WriteLine($"File not found: {file}");
            else
                Console.WriteLine(result);
        }, binlogArg, fileArg);
        return cmd;
    }

    // ─── Diagnostics ─────────────────────────────────────────────────────────

    static Command BuildDiagnosticsCommand()
    {
        var binlogArg = BinlogArg();
        var errorsOnlyOpt = new Option<bool>("--errors-only", "Include only errors (exclude warnings).");
        var warningsOnlyOpt = new Option<bool>("--warnings-only", "Include only warnings (exclude errors).");
        var maxOpt = new Option<int?>("--max", "Maximum number of diagnostics to return.");
        var cmd = new Command("diagnostics", "Extract errors and warnings from the build log.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(errorsOnlyOpt);
        cmd.AddOption(warningsOnlyOpt);
        cmd.AddOption(maxOpt);
        cmd.SetHandler((binlog, errorsOnly, warningsOnly, max) =>
        {
            CliRunner.EnsureLoaded(binlog);
            var result = GetDiagnosticsTool.GetDiagnostics(
                binlog,
                includeErrors: !warningsOnly,
                includeWarnings: !errorsOnly,
                includeDetails: true,
                maxResults: max);
            CliRunner.PrintJson(result);
        }, binlogArg, errorsOnlyOpt, warningsOnlyOpt, maxOpt);
        return cmd;
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    static Command BuildSearchCommand()
    {
        var binlogArg = BinlogArg();
        var queryArg = new Argument<string>("query",
            "Search query. Supports $task, $project, $target type filters; " +
            "name=value property matching; under(...) hierarchical filters; and more.");
        var maxOpt = new Option<int>("--max", () => 300, "Maximum number of results to return.");
        var cmd = new Command("search", "Search the binlog using the Structured Log Viewer query language.");
        cmd.AddArgument(binlogArg);
        cmd.AddArgument(queryArg);
        cmd.AddOption(maxOpt);
        cmd.SetHandler((binlog, query, max) =>
        {
            CliRunner.EnsureLoaded(binlog);
            var result = SearchBinlogTool.SearchBinlog(binlog, query, maxResults: max);
            CliRunner.PrintJson(result);
        }, binlogArg, queryArg, maxOpt);
        return cmd;
    }

    // ─── Projects ─────────────────────────────────────────────────────────────

    static Command BuildListProjectsCommand()
    {
        var binlogArg = BinlogArg();
        var cmd = new Command("list-projects", "List all projects in the build log with their entry targets.");
        cmd.AddArgument(binlogArg);
        cmd.SetHandler(binlog =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(ListProjectsTool.ListProjects(binlog));
        }, binlogArg);
        return cmd;
    }

    static Command BuildExpensiveProjectsCommand()
    {
        var binlogArg = BinlogArg();
        var topOpt = new Option<int?>("--top", "Number of projects to return (default: all).");
        var excludeOpt = new Option<string[]?>("--exclude-targets",
            "Target names to exclude from time calculations (e.g. Copy CopyFilesToOutputDirectory).")
        { AllowMultipleArgumentsPerToken = true };
        var inclusiveOpt = new Option<bool>("--sort-by-inclusive", "Sort by inclusive time instead of exclusive time.");
        var cmd = new Command("expensive-projects",
            "Show the most time-consuming projects, ordered by exclusive build time.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(topOpt);
        cmd.AddOption(excludeOpt);
        cmd.AddOption(inclusiveOpt);
        cmd.SetHandler((binlog, top, exclude, sortInclusive) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(ExpensiveProjectsTool.GetExpensiveProjects(
                binlog, top, exclude, sortByExclusive: !sortInclusive));
        }, binlogArg, topOpt, excludeOpt, inclusiveOpt);
        return cmd;
    }

    static Command BuildProjectBuildTimeCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project (from list-projects).");
        var excludeOpt = new Option<string[]?>("--exclude-targets",
            "Target names to exclude from time calculations.")
        { AllowMultipleArgumentsPerToken = true };
        var cmd = new Command("project-build-time",
            "Get inclusive and exclusive build time for a specific project.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.AddOption(excludeOpt);
        cmd.SetHandler((binlog, projectId, exclude) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetProjectBuildTimeTool.GetProjectBuildTime(binlog, projectId, exclude));
        }, binlogArg, projectIdOpt, excludeOpt);
        return cmd;
    }

    static Command BuildProjectTargetListCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project (from list-projects).");
        var cmd = new Command("project-target-list",
            "List all targets executed for a specific project.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.SetHandler((binlog, projectId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetProjectTargetListTool.GetProjectTargetList(binlog, projectId));
        }, binlogArg, projectIdOpt);
        return cmd;
    }

    static Command BuildProjectTargetTimesCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project (from list-projects).");
        var cmd = new Command("project-target-times",
            "Get inclusive and exclusive execution time for every target in a project.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.SetHandler((binlog, projectId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetProjectTargetTimesTool.GetProjectTargetTimes(binlog, projectId));
        }, binlogArg, projectIdOpt);
        return cmd;
    }

    // ─── Targets ──────────────────────────────────────────────────────────────

    static Command BuildExpensiveTargetsCommand()
    {
        var binlogArg = BinlogArg();
        var topOpt = new Option<int?>("--top", "Number of targets to return (default: all).");
        var cmd = new Command("expensive-targets",
            "Show the most time-consuming targets across the entire build, aggregated by name.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(topOpt);
        cmd.SetHandler((binlog, top) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(ExpensiveTargetsTool.GetExpensiveTargets(binlog, top));
        }, binlogArg, topOpt);
        return cmd;
    }

    static Command BuildSearchTargetsCommand()
    {
        var binlogArg = BinlogArg();
        var nameArg = new Argument<string>("target-name",
            "Target name to search for across all projects (case-insensitive).");
        var cmd = new Command("search-targets",
            "Find all executions of a target by name across all projects.");
        cmd.AddArgument(binlogArg);
        cmd.AddArgument(nameArg);
        cmd.SetHandler((binlog, name) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(SearchTargetsTool.SearchTargetsByName(binlog, name));
        }, binlogArg, nameArg);
        return cmd;
    }

    static Command BuildTargetInfoCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project containing the target.");
        var targetNameOpt = new Option<string?>("--target-name",
            "Name of the target to look up. Use either this or --target-id.");
        var targetIdOpt = new Option<int?>("--target-id",
            "ID of the target to look up. More efficient than --target-name.");
        var cmd = new Command("target-info",
            "Get details about a specific target: duration, success, skip status, and build reason.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.AddOption(targetNameOpt);
        cmd.AddOption(targetIdOpt);
        cmd.SetHandler((binlog, projectId, targetName, targetId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            object? result = (targetName, targetId) switch
            {
                (not null, _) => TargetInfoTools.GetTargetInfoByName(binlog, projectId, targetName),
                (_, not null) => TargetInfoTools.GetTargetInfoById(binlog, projectId, targetId.Value),
                _ => null
            };
            if (result is null)
            {
                Console.Error.WriteLine("Provide --target-name or --target-id.");
                return;
            }
            CliRunner.PrintJson(result);
        }, binlogArg, projectIdOpt, targetNameOpt, targetIdOpt);
        return cmd;
    }

    // ─── Tasks ────────────────────────────────────────────────────────────────

    static Command BuildExpensiveTasksCommand()
    {
        var binlogArg = BinlogArg();
        var topOpt = new Option<int?>("--top", "Number of tasks to return (default: all).");
        var cmd = new Command("expensive-tasks",
            "Show the most time-consuming MSBuild tasks across the build, aggregated by name.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(topOpt);
        cmd.SetHandler((binlog, top) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetExpensiveTasksTool.GetExpensiveTasks(binlog, top));
        }, binlogArg, topOpt);
        return cmd;
    }

    static Command BuildTaskInfoCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project containing the task.");
        var targetIdOpt = RequiredIntOption("--target-id", "ID of the target containing the task.");
        var taskIdOpt = RequiredIntOption("--task-id", "ID of the task.");
        var cmd = new Command("task-info",
            "Get detailed information about a specific task invocation: parameters and messages.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.AddOption(targetIdOpt);
        cmd.AddOption(taskIdOpt);
        cmd.SetHandler((binlog, projectId, targetId, taskId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetTaskInfoTool.GetTaskInfo(binlog, projectId, targetId, taskId));
        }, binlogArg, projectIdOpt, targetIdOpt, taskIdOpt);
        return cmd;
    }

    static Command BuildListTasksCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project.");
        var targetIdOpt = RequiredIntOption("--target-id", "ID of the target to list tasks for.");
        var cmd = new Command("list-tasks",
            "List all task invocations within a target, ordered by ID.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.AddOption(targetIdOpt);
        cmd.SetHandler((binlog, projectId, targetId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(ListTasksTool.ListTasksInTarget(binlog, projectId, targetId));
        }, binlogArg, projectIdOpt, targetIdOpt);
        return cmd;
    }

    static Command BuildSearchTasksCommand()
    {
        var binlogArg = BinlogArg();
        var nameArg = new Argument<string>("task-name",
            "Task name to search for across all projects (case-insensitive, e.g. 'Csc', 'Copy').");
        var cmd = new Command("search-tasks",
            "Find all invocations of a task by name across all projects.");
        cmd.AddArgument(binlogArg);
        cmd.AddArgument(nameArg);
        cmd.SetHandler((binlog, name) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(SearchTasksTool.SearchTasksByName(binlog, name));
        }, binlogArg, nameArg);
        return cmd;
    }

    // ─── Analyzers ────────────────────────────────────────────────────────────

    static Command BuildExpensiveAnalyzersCommand()
    {
        var binlogArg = BinlogArg();
        var topOpt = new Option<int?>("--top", "Number of analyzers to return (default: all).");
        var cmd = new Command("expensive-analyzers",
            "Show the most time-consuming Roslyn analyzers and source generators across the build.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(topOpt);
        cmd.SetHandler((binlog, top) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetExpensiveAnalyzersTool.GetExpensiveAnalyzers(binlog, top));
        }, binlogArg, topOpt);
        return cmd;
    }

    static Command BuildTaskAnalyzersCommand()
    {
        var binlogArg = BinlogArg();
        var projectIdOpt = RequiredIntOption("--project-id", "ID of the project containing the Csc task.");
        var targetIdOpt = RequiredIntOption("--target-id", "ID of the target containing the Csc task.");
        var taskIdOpt = RequiredIntOption("--task-id", "ID of the Csc task to inspect.");
        var cmd = new Command("task-analyzers",
            "Extract Roslyn analyzer and source generator timings from a specific Csc task.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectIdOpt);
        cmd.AddOption(targetIdOpt);
        cmd.AddOption(taskIdOpt);
        cmd.SetHandler((binlog, projectId, targetId, taskId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetTaskAnalyzersTool.GetTaskAnalyzers(binlog, projectId, targetId, taskId));
        }, binlogArg, projectIdOpt, targetIdOpt, taskIdOpt);
        return cmd;
    }

    // ─── Evaluations ─────────────────────────────────────────────────────────

    static Command BuildListEvaluationsCommand()
    {
        var binlogArg = BinlogArg();
        var projectFileOpt = new Option<string>("--project-file",
            "Path to the project file to list evaluations for (from list-projects).")
        { IsRequired = true };
        var cmd = new Command("list-evaluations",
            "List all evaluations for a project. Multiple evaluations may indicate overbuilding.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(projectFileOpt);
        cmd.SetHandler((binlog, projectFile) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(ListEvaluationsTool.GetEvaluationsForProject(binlog, projectFile));
        }, binlogArg, projectFileOpt);
        return cmd;
    }

    static Command BuildEvalGlobalPropsCommand()
    {
        var binlogArg = BinlogArg();
        var evalIdOpt = RequiredIntOption("--eval-id", "Evaluation ID (from list-evaluations).");
        var cmd = new Command("eval-global-props",
            "Show the global properties for an evaluation. Global properties distinguish evaluations of the same project.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(evalIdOpt);
        cmd.SetHandler((binlog, evalId) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetEvaluationPropertiesTool.GetGlobalPropertiesForEvaluation(binlog, evalId));
        }, binlogArg, evalIdOpt);
        return cmd;
    }

    static Command BuildEvalPropertiesCommand()
    {
        var binlogArg = BinlogArg();
        var evalIdOpt = RequiredIntOption("--eval-id", "Evaluation ID (from list-evaluations).");
        var namesOpt = new Option<string[]?>("--names",
            "Property names to retrieve. Returns all properties if omitted.")
        { AllowMultipleArgumentsPerToken = true };
        var cmd = new Command("eval-properties",
            "Get properties (global and non-global) for a project evaluation.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(evalIdOpt);
        cmd.AddOption(namesOpt);
        cmd.SetHandler((binlog, evalId, names) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(
                GetEvaluationPropertiesByNameTool.GetPropertiesByName(binlog, evalId, names));
        }, binlogArg, evalIdOpt, namesOpt);
        return cmd;
    }

    static Command BuildEvalItemsCommand()
    {
        var binlogArg = BinlogArg();
        var evalIdOpt = RequiredIntOption("--eval-id", "Evaluation ID (from list-evaluations).");
        var typesOpt = new Option<string[]?>("--types",
            "Item type names to retrieve (e.g. Compile PackageReference). Returns all if omitted.")
        { AllowMultipleArgumentsPerToken = true };
        var maxOpt = new Option<int?>("--max", "Maximum items to return per item type (default: 100).");
        var cmd = new Command("eval-items",
            "Get items (Compile, PackageReference, etc.) for a project evaluation.");
        cmd.AddArgument(binlogArg);
        cmd.AddOption(evalIdOpt);
        cmd.AddOption(typesOpt);
        cmd.AddOption(maxOpt);
        cmd.SetHandler((binlog, evalId, types, max) =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(
                GetEvaluationItemsByNameTool.GetItemsByName(binlog, evalId, types, max));
        }, binlogArg, evalIdOpt, typesOpt, maxOpt);
        return cmd;
    }

    // ─── Timeline ─────────────────────────────────────────────────────────────

    static Command BuildTimelineCommand()
    {
        var binlogArg = BinlogArg();
        var cmd = new Command("timeline",
            "Show how much work each MSBuild build node performed (active vs idle time).");
        cmd.AddArgument(binlogArg);
        cmd.SetHandler(binlog =>
        {
            CliRunner.EnsureLoaded(binlog);
            CliRunner.PrintJson(GetNodeTimelineTool.GetNodeTimelineInfo(binlog));
        }, binlogArg);
        return cmd;
    }

    // ─── MCP server placeholder (dispatched in Program.cs) ───────────────────

    static Command BuildMcpCommand()
    {
        return new Command("mcp",
            "Start the MCP server for use with MCP-compatible AI assistants (e.g. GitHub Copilot, Claude).");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static Argument<string> BinlogArg() =>
        new("binlog", "Path to the MSBuild binlog file (.binlog).");

    static Option<int> RequiredIntOption(string name, string description) =>
        new(name, description) { IsRequired = true };
}
