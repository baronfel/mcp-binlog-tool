using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.TimelineAnalysis;

/// <summary>
/// Represents a timeline of timed nodes organized by their node ID.
/// Excludes MSBuild and CallTarget tasks to focus on actual work.
/// </summary>
public class Timeline
{
    /// <summary>
    /// Represents statistics for a node, including the list of timed nodes, active time, and inactive time.
    /// </summary>
    /// <param name="buildDurationMs">the total time spent for this build - nodes tracked by this stats will decrease this value</param>
    public class NodeStats(int buildDurationMs)
    {

        [JsonPropertyName("activeMs")]
        [Description("The portion of the time of this build that this node spent working.")]
        public int ActiveMs { get; private set; } = 0;

        [JsonPropertyName("inactiveMs")]
        [Description("The portion of the time of this build that this node spent not working.")]
        public int InactiveMs => buildDurationMs - ActiveMs;

        public void Track(TimedNode node)
        {
            ActiveMs += (int)(node.EndTime - node.StartTime).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Gets the work statistics for each build node, keyed by MSBuild node ID.
    /// </summary>
    public Dictionary<int, NodeStats> NodesByNodeId { get; private set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Timeline"/> class.
    /// </summary>
    /// <param name="build">The build to create a timeline for.</param>
    public Timeline(Build build)
    {
        Populate(build);
    }

    private void Populate(Build build)
    {
        var buildDurationMs = (int)(build.EndTime - build.StartTime).TotalMilliseconds;
        // ConcurrentDictionary for thread-safe parallel population; converted to Dictionary after.
        var concurrent = new ConcurrentDictionary<int, NodeStats>();
        build.ParallelVisitAllChildren<TimedNode>(node =>
        {
            if (node is not TimedNode timedNode)
                return;

            if (timedNode is Build)
                return;

            if (timedNode is Microsoft.Build.Logging.StructuredLogger.Task task &&
                (string.Equals(task.Name, "MSBuild", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(task.Name, "CallTarget", StringComparison.OrdinalIgnoreCase)))
                return;

            var nodeStats = concurrent.GetOrAdd(timedNode.NodeId, _ => new(buildDurationMs));
            nodeStats.Track(timedNode);
        });
        NodesByNodeId = new Dictionary<int, NodeStats>(concurrent);
    }
}
