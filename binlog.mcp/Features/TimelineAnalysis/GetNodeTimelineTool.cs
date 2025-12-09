using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TimelineAnalysis;

public class GetNodeTimelineTool
{
    [McpServerTool(Name = "get_node_timeline", Title = "Get Node Timeline Data", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get data about how much work specific build nodes did in a build.")]
    public static Timeline? GetNodeTimelineInfo(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file)
    {
        BinlogPath binlog = new(binlog_file);
        return TimelineCache.Get(binlog);
    }
}
