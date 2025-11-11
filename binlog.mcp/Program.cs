using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Serilog;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Concurrent;
using ModelContextProtocol;
using System.Collections.Frozen;
using Microsoft.Build.Framework;
using Microsoft.VisualBasic;

namespace Binlog.MCP;

public readonly record struct BinlogPath(string filePath)
{
    public string FullPath => new FileInfo(filePath).FullName;
}

public readonly record struct ProjectFilePath(string path);

public readonly record struct EvalId(int id);
public readonly record struct ProjectId(int id);

public class Timeline
{
    public ConcurrentDictionary<int, List<TimedNode>> NodesByNodeId = new();
    public Timeline(Build build)
    {
        Populate(build);
    }

    private void Populate(Build build)
    {
        build.ParallelVisitAllChildren<TimedNode>(node =>
            {
                if (node is not TimedNode timedNode)
                {
                    return;
                }

                if (timedNode is Build)
                {
                    return;
                }

                if (timedNode is Microsoft.Build.Logging.StructuredLogger.Task task &&
                    (string.Equals(task.Name, "MSBuild", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(task.Name, "CallTarget", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var nodeId = timedNode.NodeId;
                var lane = NodesByNodeId.GetOrAdd(nodeId, (_) => new());
                lane.Add(timedNode);
            });
    }
}

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

    private static readonly ConcurrentDictionary<BinlogPath, Timeline> timeLinesByPath = new();

    public record struct InterestingBuildData(long totalDurationMs, int nodeCount);

    [McpServerTool(Name = "load_binlog", UseStructuredContent = true, Idempotent = true, ReadOnly = true)]
    [Description("Load a binary log file from a given absolute path")]
    public static InterestingBuildData Load(
        [Description("The absolute path to a MSBuild binlog file to load and analyze")] string path,
        IProgress<ProgressNotificationValue> mcpProgress)
    {
        BinlogPath binlog = new(path);
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Loading {binlog.FullPath}" });
        var thisBuild = builds.GetOrAdd(binlog, binlog =>
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
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Computing node timelines mapping" });
        var timeline = timeLinesByPath.GetOrAdd(binlog, binlog =>
        {
            var build = builds[binlog];
            var timeline = new Timeline(build);
            return timeline;
        });
        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Done loading data for {binlog.FullPath}" });

        return new InterestingBuildData(totalDurationMs: (long)thisBuild.Duration.TotalMilliseconds, nodeCount: timeline.NodesByNodeId.Keys.Count);
    }

    public record struct TargetExecutionData(
        [Description("The number of times the target was actually run.")] int executionCount,
        [Description("The number of times the target was requested but not run due to incrementality. Generally the higher this number the better.")] int skippedCount,
        [Description("The total inclusive duration of the target execution in milliseconds. This time includes 'child' Target calls, so it may not be representative of the work actually done _in_ this Target.")] long inclusiveDurationMs,
        [Description("The total exclusive duration of the target execution in milliseconds. This is the work done _in_ this Target.")] long exclusiveDurationMs);

    [McpServerTool(Name = "get_expensive_targets", Title = "Get Expensive Targets", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the N most expensive targets in the loaded binary log file")]
    public static Dictionary<string, TargetExecutionData> GetExpensiveTargets(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top targets to return")] int top_number
    )
    {
        if (!builds.TryGetValue(new(binlog_file), out var build)) return [];
        // the same target can be executed multiple times, so we need to group by name and sum the durations.
        // we can't use LINQ's GroupBy because the set of targets in the binlog could be huge, so we will use a dictionary to group them.
        // we should also track the _number_ of times each target was executed.
        var targetInclusiveDurations = new Dictionary<string, TimeSpan>();
        var targetExclusiveDurations = new Dictionary<string, TimeSpan>();
        var targetExecutions = new Dictionary<string, int>();
        var targetSkips = new Dictionary<string, int>();
        foreach (var target in build.FindChildrenRecursive<Target>())
        {
            if (target == null || target.Duration == TimeSpan.Zero) continue;
            if (targetInclusiveDurations.TryGetValue(target.Name, out var existingDuration))
            {
                targetInclusiveDurations[target.Name] = existingDuration + target.Duration;
            }
            else
            {
                targetInclusiveDurations[target.Name] = target.Duration;
            }

            var innerCallsDuration = target.FindChildrenRecursive<Microsoft.Build.Logging.StructuredLogger.Task>().Aggregate(TimeSpan.Zero, (counter, task) => counter + task.Duration);
            TimeSpan exclusiveDuration;
            if (innerCallsDuration != TimeSpan.Zero)
            {
                // if we spent any time in inner target calls, we should subtract that from the inclusive duration to get the exclusive duration.
                exclusiveDuration = target.Duration - innerCallsDuration;
            }
            else
            {
                // if there are no inner target calls, the exclusive duration is the same as the inclusive duration.
                exclusiveDuration = target.Duration;
            }

            if (targetExclusiveDurations.TryGetValue(target.Name, out var existingExclusiveDuration))
            {
                targetExclusiveDurations[target.Name] = existingExclusiveDuration + exclusiveDuration;
            }
            else
            {
                targetExclusiveDurations[target.Name] = exclusiveDuration;
            }

            if (target.Skipped)
            {
                if (targetSkips.TryGetValue(target.Name, out var existingSkipCount))
                {
                    targetSkips[target.Name] = existingSkipCount + 1;
                }
                else
                {
                    targetSkips[target.Name] = 1;
                }
            }
            else
            {

                if (targetExecutions.TryGetValue(target.Name, out var existingCount))
                {
                    targetExecutions[target.Name] = existingCount + 1;
                }
                else
                {
                    targetExecutions[target.Name] = 1;
                }
            }
        }

        // Get the top N most expensive targets by exclusive duration
        var expensiveTargets =
            targetExclusiveDurations
                .OrderByDescending(kvp => kvp.Value)
                .Take(top_number)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new TargetExecutionData(
                        executionCount: targetExecutions.TryGetValue(kvp.Key, out var execCount) ? execCount : 0,
                        skippedCount: targetSkips.TryGetValue(kvp.Key, out var skipCount) ? skipCount : 0,
                        inclusiveDurationMs: (long)targetInclusiveDurations[kvp.Key].TotalMilliseconds,
                        exclusiveDurationMs: (long)kvp.Value.TotalMilliseconds));
        return expensiveTargets;
    }

    public record struct ProjectData(string projectFile, int id, Dictionary<int, EntryTargetData>? entryTargets);
    public record struct EntryTargetData(string targetName, int id, long durationMs);

    [McpServerTool(Name = "list_projects", Title = "List Projects", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("List all projects in the loaded binary log file")]
    public static Dictionary<int, ProjectData> ListProjects(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file
        )
    {
        if (!builds.TryGetValue(new(binlog_file), out var build)) return [];
        return projectsById[new(binlog_file)].Values.ToDictionary(p => p.Id, MakeProjectSummary);
    }

    public static ProjectData MakeProjectSummary(Project p)
    {
        var targetInfo = p.EntryTargets?.Select(p.FindTarget)?.Where(t => t != null);
        return new(p.ProjectFile, p.Id, targetInfo?.ToDictionary(t => t.Id, t => new EntryTargetData(t.Name, t.Id, (long)t.Duration.TotalMilliseconds)));
    }

    public record struct EvaluationData(int id, string projectFile, long durationMs);

    [McpServerTool(Name = "list_evaluations", Title = "Get Project Evaluations", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("List all evaluations for a specific project in the loaded binary log file. You can use the `list_projects` command to find the project file paths.")]
    public static Dictionary<int, EvaluationData> GetEvaluationsForProject(
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
            var evalData = evalIds.Select(e => build.FindEvaluation(e.id)).Select(e => new EvaluationData(e.Id, e.ProjectFile, (long)e.Duration.TotalMilliseconds));
            return evalData.ToDictionary(e => e.id);
        }
        return [];
    }

    [McpServerTool(Name = "get_evaluation_global_properties", Title = "Get Properties for Evaluation", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the global properties for a specific evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. Global properties are what make evaluations distinct from one another within the same project.")]
    public static Dictionary<string, string> GetGlobalPropertiesForEvaluation(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the evaluation to get properties for")] int evaluationId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (builds.TryGetValue(binlog, out var build)
            && build.FindEvaluation(evaluationId) is var eval
            && eval.FindChild<Folder>("Properties") is var propertiesFolder
            && propertiesFolder.FindChild<Folder>("Global") is var globalProperties)
        {
            return globalProperties.Children.OfType<Property>().ToDictionary(p => p.Name, p => p.Value);
        }

        return [];
    }

    public abstract record TargetBuildReason;
    public record DependsOnReason(string targetThatDependsOnCurrentTarget) : TargetBuildReason;
    public record BeforeTargetsReason(string targetThatThisTargetMustRunBefore) : TargetBuildReason;
    public record AfterTargetsReason(string targetThatThisTargetIsRunningAfter) : TargetBuildReason;
    public record struct TargetInfo(int id, string name, long durationMs, bool succeeded, bool skipped, TargetBuildReason? builtReason, string[] targetMessages);


    [McpServerTool(Name = "get_target_info_by_name", Title = "Get Target Information", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc.")]
    public static TargetInfo? GetTargetInfoByName(
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
                return new(target.Id, target.Name, (long)target.Duration.TotalMilliseconds, target.Succeeded, target.Skipped, BuildReason(target), [.. target.Children.OfType<Message>().Select(m => m.Text)]);
            }
        }

        return null;
    }

    [McpServerTool(Name = "get_target_info_by_id", Title = "Get Target Information", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc. This is more efficient than `get_target_info_by_name` if you already know the target ID, as it avoids searching by name.")]
    public static TargetInfo? GetTargetInfoById(
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
                return new(target.Id, target.Name, (long)target.Duration.TotalMilliseconds, target.Succeeded, target.Skipped, BuildReason(target), [.. target.Children.OfType<Message>().Select(m => m.Text)]);
            }
        }

        return null;
    }

    private static TargetBuildReason? BuildReason(Target target)
    {
        return target.TargetBuiltReason switch
        {
            TargetBuiltReason.AfterTargets => new AfterTargetsReason(target.ParentTarget),
            TargetBuiltReason.BeforeTargets => new BeforeTargetsReason(target.ParentTarget),
            TargetBuiltReason.DependsOn => new DependsOnReason(target.ParentTarget),
            _ => null
        };
    }

    public record struct ProjectTargetListData(int id, string name, long durationMs);

    [McpServerTool(Name = "get_project_target_list", Title = "Get Project Target List", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get a list of targets for a specific project in the loaded binary log file. This includes the target's name, ID, and duration.")]
    public static IEnumerable<ProjectTargetListData> GetProjectTargetList(
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
                return targets.Select(t => new ProjectTargetListData(t.Id, t.Name, (long)t.Duration.TotalMilliseconds));
            }
        }
        return [];
    }

    [McpServerTool(Name = "list_files_from_binlog", Title = "List Files from Binlog", Idempotent = true, ReadOnly = true)]
    [Description("List all source files from the loaded binary log file, optionally filtering by a path pattern.")]
    public static IEnumerable<string> ListFilesFromBinlog(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlogPath,
        [Description("An optional path pattern to filter the files inside the binlog")] string? pathPattern)
    {
        if (!builds.TryGetValue(new(binlogPath), out var build)) throw new InvalidOperationException($"Binlog {binlogPath} has not been loaded. Please load it using the `load_binlog` command first.");
        if (pathPattern != null)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pathPattern);
            return build.SourceFiles.Where(f => matcher.Match(f.FullPath).HasMatches).Select(f => f.FullPath);
        }
        else
        {
            return build.SourceFiles.Select(f => f.FullPath);
        }
    }

    [McpServerTool(Name = "get_file_from_binlog", Title = "Get File from Binlog", Idempotent = true, ReadOnly = true)]
    [Description("Get a specific source file from the loaded binary log file.")]
    public static string? GetFileFromBinlog(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlogPath,
        [Description("An absolute path of a file inside the binlog")] string filePathInsideBinlog)
    {
        if (!builds.TryGetValue(new(binlogPath), out var build)) throw new InvalidOperationException($"Binlog {binlogPath} has not been loaded. Please load it using the `load_binlog` command first.");
        return build.SourceFiles.FirstOrDefault(f => f.FullPath == filePathInsideBinlog)?.Text;
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
           .WriteTo.Debug()
           .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
           .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<BinlogTool>(BinlogJsonOptions.Options)
            .WithPrompts<BinlogTool>(BinlogJsonOptions.Options);
        await builder.Build().RunAsync();
    }
}
