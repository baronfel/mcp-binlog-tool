using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

public class ListTargetExecutionOrderTool
{
    [McpServerTool(Name = "list_target_execution_order", Title = "List Targets in Execution Order", UseStructuredContent = true, ReadOnly = true, Idempotent = true)]
    [Description("List all targets that executed for a project in the order they executed. This helps you understand the build flow and identify which targets to use with get_project_state.")]
    public static TargetExecutionSummary[] ListTargetExecutionOrder(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to inspect. Use `list_projects` to find project IDs.")] int projectId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projectsById))
        {
            throw new InvalidOperationException($"Binlog not loaded: {binlog_file}");
        }

        var projectIdKey = new ProjectId(projectId);
        if (!projectsById.TryGetValue(projectIdKey, out var project))
        {
            throw new ArgumentException($"Project ID {projectId} not found in binlog");
        }

        // Get all targets in execution order
        var targets = project.Children.OfType<Target>().ToList();
        
        // Get the build start time for relative timing
        var build = FindBuild(project);
        var buildStartTime = build?.StartTime ?? DateTime.MinValue;

        return targets.Select(t => new TargetExecutionSummary(
            t.Name,
            t.Id,
            (long)t.Duration.TotalMilliseconds,
            (long)(t.StartTime - buildStartTime).TotalMilliseconds
        )).ToArray();
    }

    private static Build? FindBuild(BaseNode node)
    {
        while (node != null)
        {
            if (node is Build build)
                return build;
            node = node.Parent;
        }
        return null;
    }
}
