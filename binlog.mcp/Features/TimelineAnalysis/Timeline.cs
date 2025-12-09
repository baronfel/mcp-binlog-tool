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
    /// Gets the collection of timed nodes grouped by their node ID.
    /// Each node ID maps to a set of interesting data about that node.
    /// </summary>
    public ConcurrentDictionary<int, NodeStats> NodesByNodeId = new();

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
            var nodeStats = NodesByNodeId.GetOrAdd(nodeId, (_) => new(buildDurationMs));
            nodeStats.Track(timedNode);
        });
    }
}
