using Binlog.MCP.Features.TargetAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the TargetAnalysis feature.
/// </summary>
public static class TargetAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing target performance and execution.
    /// </summary>
    public static IMcpServerBuilder AddTargetAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<ExpensiveTargetsTool>(BinlogJsonOptions.Options);
        builder.WithTools<TargetInfoTools>(BinlogJsonOptions.Options);
        builder.WithTools<SearchTargetsTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
