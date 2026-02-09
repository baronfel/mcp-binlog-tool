using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

public class GetProjectStateTool
{
    [McpServerTool(Name = "get_project_state", Title = "Get Project State at Point in Build", UseStructuredContent = true, ReadOnly = true, Idempotent = true)]
    [Description("Get the logical model (properties and items) of a project at a specific point during the build. This starts with the evaluation state and can optionally show the state before or after a specific target executed. Use this to answer questions like 'what was the value of property X before target Y ran?' or 'what were the Compile items after PrepareForBuild?'")]
    public static ProjectStateSnapshot GetProjectState(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to inspect. Use `list_projects` to find project IDs.")] int projectId,
        [Description("Optional: The name of a target. If specified with 'beforeTarget: true', shows state before this target executed. If specified with 'afterTarget: true' or both false, shows state after this target executed.")] string? targetName = null,
        [Description("If true and targetName is specified, shows the state before the target executed. Default is false.")] bool beforeTarget = false,
        [Description("If true and targetName is specified, shows the state after the target executed. Default is false. If both beforeTarget and afterTarget are false and targetName is specified, defaults to after.")] bool afterTarget = false,
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

        // Build the state
        var stateBuilder = new ProjectStateBuilder(project);
        
        // Determine which state to return
        Target? targetNode = null;
        if (!string.IsNullOrEmpty(targetName))
        {
            targetNode = project.FindTarget(targetName);
            if (targetNode == null)
            {
                throw new ArgumentException($"Target '{targetName}' not found in project {project.ProjectFile}");
            }

            // Determine if we want before or after
            // If both are false, default to after
            bool wantAfter = afterTarget || (!beforeTarget && !afterTarget);
            
            if (beforeTarget && !wantAfter)
            {
                // State before target
                stateBuilder.BuildStateBeforeTarget(targetNode);
            }
            else
            {
                // State after target
                stateBuilder.BuildStateAfterTarget(targetNode);
            }
        }
        else
        {
            // Just evaluation state
            stateBuilder.BuildEvaluationState();
        }

        // Get the state
        var properties = includeProperties ? stateBuilder.GetProperties() : new Dictionary<string, string>();
        var items = includeItems ? stateBuilder.GetItems() : new Dictionary<string, ProjectStateItem[]>();

        string? beforeTargetName = (!string.IsNullOrEmpty(targetName) && beforeTarget && !afterTarget) ? targetName : null;
        string? afterTargetName = (!string.IsNullOrEmpty(targetName) && (afterTarget || (!beforeTarget && !afterTarget))) ? targetName : null;

        return new ProjectStateSnapshot(
            project.Id,
            project.ProjectFile,
            project.EvaluationId,
            beforeTargetName,
            afterTargetName,
            properties,
            items);
    }
}
