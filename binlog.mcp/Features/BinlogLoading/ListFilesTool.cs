using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Extensions.FileSystemGlobbing;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.BinlogLoading;

public class ListFilesTool
{
    [McpServerTool(Name = "list_files_from_binlog", Title = "List Files from Binlog", Idempotent = true, ReadOnly = true)]
    [Description("List all source files from the loaded binary log file, optionally filtering by a path pattern.")]
    public static IEnumerable<string> ListFilesFromBinlog(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlogPath,
        [Description("An optional path pattern to filter the files inside the binlog")] string? pathPattern)
    {
        var binlog = new BinlogPath(binlogPath);
        var build = BinlogLoader.GetBuild(binlog);

        if (build == null)
        {
            throw new InvalidOperationException($"Binlog {binlogPath} has not been loaded. Please load it using the `load_binlog` command first.");
        }

        if (pathPattern != null)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pathPattern);
            return build.SourceFiles.Where(f => matcher.Match(f.FullPath).HasMatches).Select(f => f.FullPath);
        }
        else
        {
            return build.SourceFiles.Select(f => f.FullPath);
        }
    }
}
