using Binlog.MCP.Features.BinlogLoading;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the BinlogLoading feature.
/// </summary>
public static class BinlogLoadingExtensions
{
    /// <summary>
    /// Registers all tools for loading and inspecting binlog files.
    /// </summary>
    public static IMcpServerBuilder AddBinlogLoading(this IMcpServerBuilder builder)
    {
        builder.WithTools<LoadBinlogTool>(BinlogJsonOptions.Options);
        builder.WithTools<ListFilesTool>(BinlogJsonOptions.Options);
        builder.WithTools<GetFileTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
