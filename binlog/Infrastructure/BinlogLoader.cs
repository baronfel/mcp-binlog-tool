using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol;

namespace Binlog.MCP.Infrastructure;

public record BinlogDataCache(Build Build,
    FrozenDictionary<ProjectFilePath, EvalId[]> EvaluationsByPath,
    FrozenDictionary<ProjectFilePath, ProjectId[]> ProjectsByPath,
    FrozenDictionary<ProjectId, Project> ProjectsById,
    FrozenDictionary<EvalId, FrozenSet<ProjectId>> ProjectsByEvaluation);

/// <summary>
/// A simple cache for loaded binlog data that allows for access by file path, but transparently
/// checks the last write time to ensure data is fresh on lookup.
/// </summary>
public class BinlogCache
{
    private Dictionary<BinlogPath, DateTime> lastWriteTimes = new();
    private ConcurrentDictionary<BinlogPath, BinlogDataCache> builds = new();

    public bool TryGetValue(BinlogPath binlogPath, [NotNullWhen(true)] out BinlogDataCache? cache)
    {
        if (builds.TryGetValue(binlogPath, out var innerCache) &&
            lastWriteTimes.TryGetValue(binlogPath, out var lastWriteTime)
            && File.GetLastWriteTime(binlogPath.FullPath) == lastWriteTime)
        {
            cache = innerCache;
            return true;
        }
        cache = null;
        return false;
    }

    public void Add(BinlogPath binlogPath, BinlogDataCache cache)
    {
        builds.AddOrUpdate(binlogPath, cache, (key, oldValue) => cache);
        lastWriteTimes[binlogPath] = File.GetLastWriteTime(binlogPath.FullPath);
    }
}

/// <summary>
/// Central component responsible for loading binlog files and managing cached data.
/// All feature slices depend on this loader to access binlog data.
/// </summary>
public static class BinlogLoader
{
    private static readonly BinlogCache cache = new();

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
    public static LoadResult Load(BinlogPath binlogPath, IProgress<ProgressNotificationValue> mcpProgress)
    {
        BinlogDataCache entry;
        if (cache.TryGetValue(binlogPath, out var cached))
        {
            entry = cached;
        }
        else
        {
            mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Loading {binlogPath.FullPath}" });
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
            var build = BinaryLog.ReadBuild(binlogPath.FullPath, progress);

            mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Reading evaluation mapping" });
            var evaluationsByPath = build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>()
                .GroupBy(e => e.ProjectFile)
                .ToFrozenDictionary(g => new ProjectFilePath(g.Key), g => g.Select(e => new EvalId(e.Id)).ToArray());

            mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Reading project mapping" });
            var projectsById = build.FindChildrenRecursive<Project>()
                .ToFrozenDictionary(p => new ProjectId(p.Id), p => p);

            var projectsByPath = build.FindChildrenRecursive<Project>()
                .GroupBy(p => p.ProjectFile)
                .ToFrozenDictionary(g => new ProjectFilePath(g.Key), g => g.Select(p => new ProjectId(p.Id)).ToArray());

            var evalIds = build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>().Select(e => e.Id);
            var projects = projectsById.Values;
            var projectsByEvaluation = evalIds
                .ToFrozenDictionary(e => new EvalId(e), e => projects.Where(p => p.EvaluationId == e).Select(p => new ProjectId(p.Id)).ToFrozenSet());

            entry = new BinlogDataCache(build, evaluationsByPath, projectsByPath, projectsById, projectsByEvaluation);
            cache.Add(binlogPath, entry);
        }

        // Invoke post-load callbacks for feature-specific processing
        int nodeCount = 0;
        foreach (var callback in postLoadCallbacks)
        {
            mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Processing post-load callbacks..." });
            nodeCount = Math.Max(nodeCount, callback(binlogPath, entry.Build));
        }

        mcpProgress.Report(new() { Progress = 0, Total = null, Message = $"Done loading data for {binlogPath.FullPath}" });

        return new LoadResult(totalDurationMs: (long)entry.Build.Duration.TotalMilliseconds, nodeCount: nodeCount);
    }

    /// <summary>
    /// Get the Build object for a loaded binlog.
    /// </summary>
    public static Build? GetBuild(BinlogPath binlog)
    {
        return cache.TryGetValue(binlog, out var entry) ? entry.Build : null;
    }

    /// <summary>
    /// Try to get the Build object for a loaded binlog.
    /// </summary>
    public static bool TryGetBuild(BinlogPath binlog, [NotNullWhen(true)] out Build? build)
    {
        if (cache.TryGetValue(binlog, out var entry))
        {
            build = entry.Build;
            return true;
        }
        else
        {
            build = null;
            return false;
        }
    }

    /// <summary>
    /// Get evaluations grouped by project file path.
    /// </summary>
    public static FrozenDictionary<ProjectFilePath, EvalId[]>? GetEvaluationsByPath(BinlogPath binlog)
    {
        return cache.TryGetValue(binlog, out var entry) ? entry.EvaluationsByPath : null;
    }

    /// <summary>
    /// Get projects by ID.
    /// </summary>
    public static FrozenDictionary<ProjectId, Project>? GetProjectsById(BinlogPath binlog)
    {
        return cache.TryGetValue(binlog, out var entry) ? entry.ProjectsById : null;
    }

    /// <summary>
    /// Try to get projects by ID.
    /// </summary>
    public static bool TryGetProjectsById(BinlogPath binlog, [NotNullWhen(true)] out FrozenDictionary<ProjectId, Project>? projects)
    {
        if (cache.TryGetValue(binlog, out var entry))
        {
            projects = entry.ProjectsById;
            return true;
        }
        projects = null;
        return false;
    }

    /// <summary>
    /// Get projects grouped by file path.
    /// </summary>
    public static FrozenDictionary<ProjectFilePath, ProjectId[]>? GetProjectsByPath(BinlogPath binlog)
    {
        return cache.TryGetValue(binlog, out var entry) ? entry.ProjectsByPath : null;
    }

    /// <summary>
    /// Get projects grouped by evaluation ID.
    /// </summary>
    public static FrozenDictionary<EvalId, FrozenSet<ProjectId>>? GetProjectsByEvaluation(BinlogPath binlog)
    {
        return cache.TryGetValue(binlog, out var entry) ? entry.ProjectsByEvaluation : null;
    }
}
