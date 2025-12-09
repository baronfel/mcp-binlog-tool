using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the AnalyzerAnalysis feature.
/// </summary>
public static class AnalyzerAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing Roslyn analyzer and source generator execution.
    /// </summary>
    public static IMcpServerBuilder AddAnalyzerAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<Features.AnalyzerAnalysis.GetTaskAnalyzersTool>(BinlogJsonOptions.Options);
        builder.WithTools<Features.AnalyzerAnalysis.GetExpensiveAnalyzersTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
