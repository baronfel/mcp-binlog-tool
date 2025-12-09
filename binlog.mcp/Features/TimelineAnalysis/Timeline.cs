using System.Collections.Concurrent;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.TimelineAnalysis;

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
