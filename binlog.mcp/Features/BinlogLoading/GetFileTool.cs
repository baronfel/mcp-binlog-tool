using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.BinlogLoading;

public class GetFileTool
{
    [McpServerTool(Name = "get_file_from_binlog", Title = "Get File from Binlog", ReadOnly = true)]
    [Description("Get a specific source file from the loaded binary log file.")]
    public static string? GetFileFromBinlog(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlogPath,
        [Description("An absolute path of a file inside the binlog")] string filePathInsideBinlog)
    {
        var binlog = new BinlogPath(binlogPath);
        var build = BinlogLoader.GetBuild(binlog);

        if (build == null)
        {
            throw new InvalidOperationException($"Binlog {binlogPath} has not been loaded. Please load it using the `load_binlog` command first.");
        }

        return build.SourceFiles.FirstOrDefault(f => f.FullPath == filePathInsideBinlog)?.Text;
    }
}
