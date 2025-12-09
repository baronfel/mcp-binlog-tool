using Binlog.MCP.Features.EvaluationAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the EvaluationAnalysis feature.
/// </summary>
public static class EvaluationAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing project evaluations.
    /// </summary>
    public static IMcpServerBuilder AddEvaluationAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<ListEvaluationsTool>(BinlogJsonOptions.Options);
        builder.WithTools<GetEvaluationPropertiesTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
