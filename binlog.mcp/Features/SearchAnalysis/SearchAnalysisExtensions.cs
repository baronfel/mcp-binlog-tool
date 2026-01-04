using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the SearchAnalysis feature.
/// </summary>
public static class SearchAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for performing freetext search within binlog files.
    /// </summary>
    public static IMcpServerBuilder AddSearchAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<Features.SearchAnalysis.SearchBinlogTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
