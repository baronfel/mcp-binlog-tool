using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the TaskAnalysis feature.
/// </summary>
public static class TaskAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing MSBuild task invocations.
    /// </summary>
    public static IMcpServerBuilder AddTaskAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<Features.TaskAnalysis.GetTaskInfoTool>(BinlogJsonOptions.Options);
        builder.WithTools<Features.TaskAnalysis.ListTasksTool>(BinlogJsonOptions.Options);
        builder.WithTools<Features.TaskAnalysis.SearchTasksTool>(BinlogJsonOptions.Options);
        builder.WithTools<Features.TaskAnalysis.GetExpensiveTasksTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
