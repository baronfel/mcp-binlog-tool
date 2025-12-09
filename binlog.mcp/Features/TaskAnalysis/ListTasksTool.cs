using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TaskAnalysis;

/// <summary>
/// MCP tool for listing all task invocations in a target.
/// </summary>
public class ListTasksTool
{
    [McpServerTool(Name = "list_tasks_in_target", Title = "List Tasks in Target", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("List all MSBuild task invocations within a specific target, ordered by duration.")]
    public static Dictionary<int, TaskDetails> ListTasksInTarget(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the target")] int projectId,
        [Description("The ID of the target to list tasks for")] int targetId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null ||
            !projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            return [];
        }

        var target = project.GetTargetById(targetId);
        if (target == null)
        {
            return [];
        }

        return target.Children.OfType<Microsoft.Build.Logging.StructuredLogger.Task>()
            .OrderBy(t => t.Id)
            .ToDictionary(
                t => t.Id,
                Util.CreateTaskDetails);
    }
}
