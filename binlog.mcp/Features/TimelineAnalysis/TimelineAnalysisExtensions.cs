using Binlog.MCP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to enable timeline analysis capabilities.
/// </summary>
public static class TimelineAnalysisExtensions
{
    /// <summary>
    /// Enables timeline computation and caching for binlog analysis.
    /// This adds a post-load hook to BinlogLoader to automatically compute timelines.
    /// </summary>
    public static IMcpServerBuilder AddTimelineAnalysis(this IMcpServerBuilder builder)
    {
        // Register a callback with BinlogLoader to compute timelines after loading
        BinlogLoader.RegisterPostLoadCallback((binlog, build) =>
        {
            var timeline = Features.TimelineAnalysis.TimelineCache.GetOrCompute(binlog, build);
            return timeline.NodesByNodeId.Keys.Count;
        });
        return builder;
    }
}
