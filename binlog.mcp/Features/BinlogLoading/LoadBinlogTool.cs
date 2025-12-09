using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.BinlogLoading;

public class LoadBinlogTool
{
    public record struct InterestingBuildData(long totalDurationMs, int nodeCount);

    [McpServerTool(Name = "load_binlog", UseStructuredContent = true, Idempotent = true, ReadOnly = true)]
    [Description("Load a binary log file from a given absolute path")]
    public static InterestingBuildData Load(
        [Description("The absolute path to a MSBuild binlog file to load and analyze")] string path,
        IProgress<ProgressNotificationValue> mcpProgress)
    {
        BinlogPath binlog = new(path);
        var result = BinlogLoader.Load(binlog, mcpProgress);
        return new InterestingBuildData(result.totalDurationMs, result.nodeCount);
    }
}
