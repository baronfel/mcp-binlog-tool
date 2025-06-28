using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Serilog;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using ModelContextProtocol;
using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

namespace Binlog.MCP;

public readonly record struct BinlogPath(string filePath)
{
    public string FullPath => new FileInfo(filePath).FullName;
}

public readonly record struct ProjectFilePath(string path);

public readonly record struct EvalId(int id);
public readonly record struct ProjectId(int id);


public class BinlogTool
{
    private static readonly ConcurrentDictionary<BinlogPath, Build> builds = new();

    /// <summary>
    /// Tracks evaluations by project file path, within a specific binlog file.
    /// </summary>
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectFilePath, EvalId[]>> evaluationsByPath = new();

    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectFilePath, ProjectId[]>> projectsByPath = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectId, Project>> projectsById = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<EvalId, FrozenSet<ProjectId>>> projectsByEvaluation = new();

    [McpServerTool(Name = "load_binlog")]
    [Description("Load a binary log file")]
    public static void Load(
        [Description("The path to a MSBuild binlog file to load and analyze")] string path,
        IProgress<ProgressNotificationValue> mcpProgress)
    {
        BinlogPath binlog = new(path);
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Loading {binlog.FullPath}" });
        builds.GetOrAdd(binlog, binlog =>
        {

            Progress progress = new Progress();
            int? messageCount = null;
            progress.Updated += update =>
            {
                messageCount ??= update.BufferLength;
                mcpProgress.Report(new ProgressNotificationValue
                {
                    Progress = (float)update.Ratio,
                    Total = messageCount,
                    Message = $"Loading all messages",
                });
            };
            return BinaryLog.ReadBuild(binlog.FullPath, progress);
        });
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Reading evaluation mapping" });
        evaluationsByPath.GetOrAdd(binlog, binlog =>
        {
            var build = builds[binlog];
            FrozenDictionary<ProjectFilePath, EvalId[]> evaluations =
                build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>()
                .GroupBy(e => e.ProjectFile)
                .ToFrozenDictionary(g => new ProjectFilePath(g.Key), g => g.Select(e => new EvalId(e.Id)).ToArray());
            return evaluations;
        });
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Reading project mapping" });
        projectsById.GetOrAdd(binlog, binlog =>
        {
            var build = builds[binlog];
            FrozenDictionary<ProjectId, Project> projects =
                build.FindChildrenRecursive<Project>()
                .ToFrozenDictionary(p => new ProjectId(p.Id), p => p);
            return projects;
        });
        projectsByPath.GetOrAdd(binlog, binlog =>
        {
            var build = builds[binlog];
            FrozenDictionary<ProjectFilePath, ProjectId[]> projects =
                build.FindChildrenRecursive<Project>()
                .GroupBy(p => p.ProjectFile)
                .ToFrozenDictionary(g => new ProjectFilePath(g.Key), g => g.Select(p => new ProjectId(p.Id)).ToArray());
            return projects;
        });
        projectsByEvaluation.GetOrAdd(binlog, binlog =>
        {
            var build = builds[binlog];
            var evalIds = build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>().Select(e => e.Id);
            var projects = projectsById[binlog].Values;
            var evalsAndProjects = evalIds
                .ToFrozenDictionary(e => new EvalId(e), e => projects.Where(p => p.EvaluationId == e).Select(p => new ProjectId(p.Id)).ToFrozenSet());
            return evalsAndProjects;
        });
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Done loading data for {binlog.FullPath}" });
    }

    [McpServerTool(Name = "get_expensive_targets", Title = "Get Expensive Targets", Idempotent = true), Description("Get the N most expensive targets in the loaded binary log file")]
    public static string[] GetExpensiveTargets(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top targets to return")] int top_number
    )
    {
        if (!builds.TryGetValue(new(binlog_file), out var build)) return [];
        // the same target can be executed multiple times, so we need to group by name and sum the durations.
        // we can't use LINQ's GroupBy because the set of targets in the binlog could be huge, so we will use a dictionary to group them.
        // we should also track the _number_ of times each target was executed.
        var targetDurations = new Dictionary<string, TimeSpan>();
        var targetExecutions = new Dictionary<string, int>();
        foreach (var target in build.FindChildrenRecursive<Target>())
        {
            if (target == null || target.Duration == TimeSpan.Zero) continue;
            if (targetDurations.TryGetValue(target.Name, out var existingDuration))
            {
                targetDurations[target.Name] = existingDuration + target.Duration;
            }
            else
            {
                targetDurations[target.Name] = target.Duration;
            }
            if (targetExecutions.TryGetValue(target.Name, out var existingCount))
            {
                targetExecutions[target.Name] = existingCount + 1;
            }
            else
            {
                targetExecutions[target.Name] = 1;
            }
        }

        // Get the top N most expensive targets
        var expensiveTargets = targetDurations.OrderByDescending(kvp => kvp.Value).Take(top_number);
        return [.. expensiveTargets.Select(kvp => $"{kvp.Key} was called {targetExecutions[kvp.Key]} times ({kvp.Value.Milliseconds} ms)")];
    }

    [McpServerTool(Name = "list_projects", Title = "List Projects", Idempotent = true), Description("List all projects in the loaded binary log file")]
    public static string[] ListProjects(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file
        )
    {
        if (!builds.TryGetValue(new(binlog_file), out var build)) return [];
        return [.. build.FindChildrenRecursive<Project>().Select(MakeProjectSummary)];
    }

    public static string MakeProjectSummary(Project p)
    {
        var targetInfo = p.EntryTargets?.Select(p.FindChild<Target>)?.Where(t => t != null);
        var part = $"{p.ProjectFile} Id={p.Id}";
        if (targetInfo is not null)
        {
            var targets = string.Join(", ", targetInfo.Select(t => $"{t.Name} (Id={t.Id}) (Duration={t.Duration.TotalMilliseconds}ms)"));
            part += $" EntryTargets=[{targets}]";
        }
        return part;
    }

    [McpServerTool(Name = "list_evaluations", Title = "Get Project Evaluations", Idempotent = true), Description("List all evaluations for a specific project in the loaded binary log file. You can use the `list_projects` command to find the project file paths.")]
    public static string[] GetEvaluationsForProject(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The path to the project file to get evaluations for")] string projectFilePath)
    {
        BinlogPath binlog = new(binlog_file);
        ProjectFilePath projectFile = new(projectFilePath);
        if (evaluationsByPath.TryGetValue(binlog, out var evaluations)
            && evaluations.TryGetValue(projectFile, out var evalIds)
            && evalIds.Length > 0
        )
        {
            var build = builds[binlog];
            var evalData = evalIds.Select(e => build.FindEvaluation(e.id)).Select(e => $"{e.Id} - {e.ProjectFile} ({e.Duration.TotalMilliseconds}ms)");
            return [.. evalData];
        }
        return [];
    }

    [McpServerTool(Name = "get_evaluation_global_properties", Title = "Get Properties for Evaluation", Idempotent = true), Description("Get the global properties for a specific evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. Global properties are what make evaluations distinct from one another within the same project.")]
    public static string[] GetGlobalPropertiesForEvaluation(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the evaluation to get properties for")] int evaluationId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (builds.TryGetValue(binlog, out var build)
            && build.FindEvaluation(evaluationId) is var eval
            && eval.FindChild<Folder>("Properties") is var propertiesFolder
            && propertiesFolder.FindChild<Folder>("Global") is var globalProperties)
        {
            return [..globalProperties.Children.OfType<Property>()
                .Select(p => $"{p.Name} = {p.Value}")];
        }

        return [];
    }

    [McpServerTool(Name = "get_target_info_by_name", Title = "Get Target Information", Idempotent = true), Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc.")]
    public static string[] GetTargetInfoByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the target")] int projectId,
        [Description("The name of the target to get dependencies for")] string targetName)
    {
        var binlog = new BinlogPath(binlog_file);
        if (projectsById.TryGetValue(binlog, out var projects) &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var target = project.FindTarget(targetName);
            if (target != null)
            {
                return [
                    $"Target: {target.Name} (Id={target.Id})",
                    $"Duration: {target.Duration.TotalMilliseconds} ms",
                    $"Why was this target built? {BuildReason(target)}",
                    $"Succeeded? {target.Succeeded}",
                    $"Messages:",
                    ..target.Children.OfType<Message>().Select(m => $"\t{m.Text}"),
                ];
            }
        }

        return [];
    }

    [McpServerTool(Name = "get_target_info_by_id", Title = "Get Target Information", Idempotent = true), Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc. This is more efficient than `get_target_info_by_name` if you already know the target ID, as it avoids searching by name.")]
    public static string[] GetTargetInfoById(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the target")] int projectId,
        [Description("The ID of the target to get dependencies for")] int targetId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (projectsById.TryGetValue(binlog, out var projects) &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var target = project.GetTargetById(targetId);
            if (target != null)
            {
                return [
                    $"Target: {target.Name} (Id={target.Id})",
                    $"Was target skipped? { target.Skipped }",
                    $"Duration: {target.Duration.TotalMilliseconds} ms",
                    $"Why was this target built? {BuildReason(target)}",
                    $"Succeeded? {target.Succeeded}",
                    $"Messages:",
                    ..target.Children.OfType<Message>().Select(m => $"\t{m.Text}"),
                ];
            }
        }

        return [];
    }

    public static string BuildReason(Target target)
    {
        return target.TargetBuiltReason switch
        {
            TargetBuiltReason.AfterTargets => $"It had AfterTargets='{target.ParentTarget}' directly or indirectly",
            TargetBuiltReason.BeforeTargets => $"Target '{target.Name}' had BeforeTargets='{target.ParentTarget}'",
            TargetBuiltReason.DependsOn => $"The parent target '{target.ParentTarget}' had DependsOnTargets on this target",
            _ => "No specific reason provided for why this target was built."
        };
    }

    [McpServerTool(Name = "get_project_target_list", Title = "Get Project Target List", Idempotent = true), Description("Get a list of targets for a specific project in the loaded binary log file. This includes the target's name, ID, and duration.")]
    public static string GetProjectTargetList(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to get targets for")] int projectId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (projectsById.TryGetValue(binlog, out var projects) &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var targets = project.Children.OfType<Target>();
            if (targets is not null)
            {
                return string.Join(", ", targets.Select(t => $"{t.Name} (Id={t.Id}) (Duration={t.Duration.TotalMilliseconds}ms)"));
            }
        }
        return "No targets found for this project.";
    }

    [McpServerPrompt(Name = "initial_build_analysis", Title = "Analyze Binary Log"), Description("Perform a build of the current workspace and profile it using the binary logger.")]
    public static IEnumerable<ChatMessage> InitialBuildAnalysis() => [
        new ChatMessage(ChatRole.User, """
            Please perform a build of the current workspace using dotnet build with the binary logger enabled.
            You can use the `--binaryLogger` option to specify the log file name. For example: `dotnet build --binaryLogger:binlog.binlog`.
            Create a binlog file using a name that is randomly generated, then remember it for later use.
            """),
        new ChatMessage(ChatRole.Assistant, """
            Once the build is complete, you can use the `load_binlog` command to load the newly-generated binary log file.
            Then use `get_expensive_targets` to list the most expensive targets,
            and check how many evaluations a project has using the `list_evaluations` command with the project file path.
            Multiple evaluations can sometimes be a cause of overbuilding, so it's worth checking.
            """),
        new ChatMessage(ChatRole.User, """
            Now that you have a binlog, show me the top 5 targets that took the longest time to execute in the build.
            Also, note if any projects had multiple evaluations. You can check evaluations using the `list_evaluations` command with the project file path.
            """),
    ];

    [McpServerPrompt(Name = "compare_binlogs", Title = "Compare Binary Logs"), Description("Compare two binary logs.")]
    public static IEnumerable<ChatMessage> CompareBinlogs() => [
        new ChatMessage(ChatRole.System, """
            Get paths to two binary log files from the user.
            Then load both binlogs using the `load_binlog` command.
            After loading the binlogs, list all of the projects, get the evaluations for each project, and check if any projects have multiple evaluations.
            Finally, compare the timings of the evaluations and display the comparisons in a table that compares the same evaluation across both binlogs.
            """),
    ];
}

public static class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Verbose() // Capture all log levels
           .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "TestServer_.log"),
               rollingInterval: RollingInterval.Day,
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .WriteTo.Debug()
           .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
           .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog();
        var jsonConfig = new System.Text.Json.JsonSerializerOptions();
        jsonConfig.TypeInfoResolverChain.Add(SourceGenerationContext.Default);
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<BinlogTool>(jsonConfig)
            .WithPrompts<BinlogTool>(jsonConfig);
        await builder.Build().RunAsync();
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(ProgressNotificationValue))]
[JsonSerializable(typeof(IEnumerable<ChatMessage>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
