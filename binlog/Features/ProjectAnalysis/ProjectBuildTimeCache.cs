using System.Collections.Concurrent;
using System.Collections.Frozen;
using Binlog.MCP.Features.TargetAnalysis;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.ProjectAnalysis;

/// <summary>
/// Manages caching of computed project build time data.
/// </summary>
public static class ProjectBuildTimeCache
{
    /// <summary>
    /// Tracks computed project build time data by project ID, within a specific binlog file.
    /// Key: (BinlogPath, excludeTargets set as comma-separated sorted string)
    /// </summary>
    private static readonly ConcurrentDictionary<(BinlogPath, string), FrozenDictionary<ProjectId, ProjectBuildTimeData>> cache = new();

    /// <summary>
    /// Compute and cache project build time data for all projects in a binlog.
    /// </summary>
    public static FrozenDictionary<ProjectId, ProjectBuildTimeData> GetOrCompute(
        BinlogPath binlog,
        HashSet<string> excludeSet)
    {
        // Create a cache key from sorted exclude targets
        var cacheKey = string.Join(",", excludeSet.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));

        return cache.GetOrAdd((binlog, cacheKey), _ =>
        {
            if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null)
            {
                return FrozenDictionary<ProjectId, ProjectBuildTimeData>.Empty;
            }

            var result = new Dictionary<ProjectId, ProjectBuildTimeData>();

            foreach (var (projectId, project) in projects)
            {
                var targets = project.Children.OfType<Target>()
                    .Where(t => !excludeSet.Contains(t.Name) && !t.Skipped);

                long totalInclusive = 0;
                long totalExclusive = 0;
                int count = 0;

                foreach (var target in targets)
                {
                    totalInclusive += (long)target.Duration.TotalMilliseconds;
                    var exclusiveDuration = TargetTimeCalculator.CalculateExclusiveDuration(target);
                    totalExclusive += (long)exclusiveDuration.TotalMilliseconds;
                    count++;
                }

                result[projectId] = new ProjectBuildTimeData(totalExclusive, totalInclusive, count);
            }

            return result.ToFrozenDictionary();
        });
    }
}
