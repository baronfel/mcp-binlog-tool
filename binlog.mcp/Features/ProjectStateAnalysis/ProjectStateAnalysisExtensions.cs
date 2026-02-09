using Binlog.MCP.Features.ProjectStateAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the ProjectStateAnalysis feature.
/// </summary>
public static class ProjectStateAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for project state inspection.
    /// </summary>
    public static IMcpServerBuilder AddProjectStateAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<GetProjectStateTool>(BinlogJsonOptions.Options);
        builder.WithTools<ListTargetExecutionOrderTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
