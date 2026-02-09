using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

public class GetProjectStateTool
{
    [McpServerTool(Name = "get_project_state", Title = "Get Project State at Point in Build", UseStructuredContent = true, ReadOnly = true, Idempotent = true)]
    [Description("Get the logical model (properties and items) of a project at a specific point during the build. This returns the evaluation state (the state after project evaluation but before any targets execute). Note: MSBuild binlogs record evaluation state but do not record incremental state changes during target execution, so this tool shows the baseline state. Use list_target_execution_order to see what targets executed, and use get_task_info to see task parameters which may include output properties/items.")]
    public static ProjectStateSnapshot GetProjectState(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to inspect. Use `list_projects` to find project IDs.")] int projectId,
        [Description("If true, includes all properties. If false, only includes a summary count. Default is true.")] bool includeProperties = true,
        [Description("If true, includes all items. If false, only includes a summary count. Default is true.")] bool includeItems = true)
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

        // Build the evaluation state
        var stateBuilder = new ProjectStateBuilder(project);
        stateBuilder.BuildEvaluationState();

        // Get the state
        var properties = includeProperties ? stateBuilder.GetProperties() : new Dictionary<string, string>();
        var items = includeItems ? stateBuilder.GetItems() : new Dictionary<string, ProjectStateItem[]>();

        return new ProjectStateSnapshot(
            project.Id,
            project.ProjectFile,
            project.EvaluationId,
            properties,
            items);
    }
}
