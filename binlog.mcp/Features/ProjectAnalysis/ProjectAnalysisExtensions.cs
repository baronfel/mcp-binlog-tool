using Binlog.MCP.Features.ProjectAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the ProjectAnalysis feature.
/// </summary>
public static class ProjectAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing project-level build performance.
    /// </summary>
    public static IMcpServerBuilder AddProjectAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<ListProjectsTool>(BinlogJsonOptions.Options);
        builder.WithTools<GetProjectTargetListTool>(BinlogJsonOptions.Options);
        builder.WithTools<GetProjectTargetTimesTool>(BinlogJsonOptions.Options);
        builder.WithTools<GetProjectBuildTimeTool>(BinlogJsonOptions.Options);
        builder.WithTools<ExpensiveProjectsTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
