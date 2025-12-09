using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TaskAnalysis;

/// <summary>
/// MCP tool for getting detailed information about a specific task invocation.
/// </summary>
public class GetTaskInfoTool
{
    [McpServerTool(Name = "get_task_info", Title = "Get Task Information", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get detailed information about a specific MSBuild task invocation, including parameters and messages.")]
    public static TaskDetails? GetTaskInfo(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the task")] int projectId,
        [Description("The ID of the target containing the task")] int targetId,
        [Description("The ID of the task to get information for")] int taskId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null ||
            !projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            return null;
        }

        // Find the task by searching through all targets
        Microsoft.Build.Logging.StructuredLogger.Task? foundTask = null;
        Target? parentTarget = project.GetTargetById(targetId);

        if (parentTarget == null)
        {
            return null;
        }

        foundTask = parentTarget.GetTaskById(taskId);

        if (foundTask == null)
        {
            return null;
        }

        return Util.CreateTaskDetails(foundTask);
    }
}
