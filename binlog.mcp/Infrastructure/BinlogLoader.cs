using System.Collections.Concurrent;
using System.Collections.Frozen;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol;

namespace Binlog.MCP.Infrastructure;

/// <summary>
/// Central component responsible for loading binlog files and managing cached data.
/// All feature slices depend on this loader to access binlog data.
/// </summary>
public static class BinlogLoader
{
    private static readonly ConcurrentDictionary<BinlogPath, Build> builds = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectFilePath, EvalId[]>> evaluationsByPath = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectFilePath, ProjectId[]>> projectsByPath = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<ProjectId, Project>> projectsById = new();
    private static readonly ConcurrentDictionary<BinlogPath, FrozenDictionary<EvalId, FrozenSet<ProjectId>>> projectsByEvaluation = new();

    /// <summary>
    /// Callbacks that are invoked after a binlog is loaded.
    /// Features can register callbacks to compute their own caches.
    /// Returns an optional node count for reporting.
    /// </summary>
    private static readonly List<Func<BinlogPath, Build, int>> postLoadCallbacks = new();

    public record struct LoadResult(long totalDurationMs, int nodeCount);

    /// <summary>
    /// Register a callback to be invoked after a binlog is successfully loaded.
    /// The callback receives the binlog path and Build object, and can return an optional node count.
    /// </summary>
    public static void RegisterPostLoadCallback(Func<BinlogPath, Build, int> callback)
    {
        postLoadCallbacks.Add(callback);
    }

    /// <summary>
    /// Load a binlog file and populate all caches.
    /// </summary>
    public static LoadResult Load(BinlogPath binlog, IProgress<ProgressNotificationValue> mcpProgress)
    {
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

        // Invoke post-load callbacks for feature-specific processing
        int nodeCount = 0;
        foreach (var callback in postLoadCallbacks)
        {
            mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Processing post-load callbacks..." });
            nodeCount = Math.Max(nodeCount, callback(binlog, thisBuild));
        }

        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Done loading data for {binlog.FullPath}" });

        return new LoadResult(totalDurationMs: (long)thisBuild.Duration.TotalMilliseconds, nodeCount: nodeCount);
    }

    /// <summary>
    /// Get the Build object for a loaded binlog.
    /// </summary>
    public static Build? GetBuild(BinlogPath binlog)
    {
        return builds.TryGetValue(binlog, out var build) ? build : null;
    }

    /// <summary>
    /// Try to get the Build object for a loaded binlog.
    /// </summary>
    public static bool TryGetBuild(BinlogPath binlog, out Build? build)
    {
        return builds.TryGetValue(binlog, out build);
    }

    /// <summary>
    /// Get evaluations grouped by project file path.
    /// </summary>
    public static FrozenDictionary<ProjectFilePath, EvalId[]>? GetEvaluationsByPath(BinlogPath binlog)
    {
        return evaluationsByPath.TryGetValue(binlog, out var evals) ? evals : null;
    }

    /// <summary>
    /// Get projects by ID.
    /// </summary>
    public static FrozenDictionary<ProjectId, Project>? GetProjectsById(BinlogPath binlog)
    {
        return projectsById.TryGetValue(binlog, out var projects) ? projects : null;
    }

    /// <summary>
    /// Try to get projects by ID.
    /// </summary>
    public static bool TryGetProjectsById(BinlogPath binlog, out FrozenDictionary<ProjectId, Project>? projects)
    {
        return projectsById.TryGetValue(binlog, out projects);
    }

    /// <summary>
    /// Get projects grouped by file path.
    /// </summary>
    public static FrozenDictionary<ProjectFilePath, ProjectId[]>? GetProjectsByPath(BinlogPath binlog)
    {
        return projectsByPath.TryGetValue(binlog, out var projects) ? projects : null;
    }

    /// <summary>
    /// Get projects grouped by evaluation ID.
    /// </summary>
    public static FrozenDictionary<EvalId, FrozenSet<ProjectId>>? GetProjectsByEvaluation(BinlogPath binlog)
    {
        return projectsByEvaluation.TryGetValue(binlog, out var projects) ? projects : null;
    }
}
