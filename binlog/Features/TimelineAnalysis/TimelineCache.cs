using System.Collections.Concurrent;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.TimelineAnalysis;

/// <summary>
/// Manages caching of Timeline data for loaded binlogs.
/// This is a feature-specific cache that depends on BinlogLoader.
/// </summary>
public static class TimelineCache
{
    private static readonly ConcurrentDictionary<BinlogPath, Timeline> timeLinesByPath = new();

    /// <summary>
    /// Get or compute the timeline for a loaded binlog.
    /// </summary>
    /// <param name="binlog">The binlog path.</param>
    /// <param name="build">The build object to create a timeline from if not cached.</param>
    /// <returns>The timeline for the binlog.</returns>
    public static Timeline GetOrCompute(BinlogPath binlog, Build build)
    {
        return timeLinesByPath.GetOrAdd(binlog, _ => new Timeline(build));
    }

    /// <summary>
    /// Try to get a cached timeline for a binlog.
    /// </summary>
    /// <param name="binlog">The binlog path.</param>
    /// <returns>The cached timeline if found; otherwise, null.</returns>
    public static Timeline? Get(BinlogPath binlog)
    {
        return timeLinesByPath.TryGetValue(binlog, out var timeline) ? timeline : null;
    }
}
