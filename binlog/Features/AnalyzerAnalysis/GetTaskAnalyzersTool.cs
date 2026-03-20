using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.AnalyzerAnalysis;

/// <summary>
/// MCP tool for extracting analyzer and generator data from a specific Csc task invocation.
/// </summary>
public class GetTaskAnalyzersTool
{
    [McpServerTool(Name = "get_task_analyzers", Title = "Get Task Analyzers", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Extract Roslyn analyzer and source generator execution data from a specific Csc task invocation.")]
    public static CscAnalyzerData? GetTaskAnalyzers(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the task")] int projectId,
        [Description("The ID of the target containing the task")] int targetId,
        [Description("The ID of the Csc task to analyze")] int taskId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null ||
            !projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            return null;
        }

        var target = project.GetTargetById(targetId);
        if (target == null)
        {
            return null;
        }

        var task = target.GetTaskById(taskId);
        if (task == null)
        {
            return null;
        }

        return AnalyzerParser.ParseCscTask(task);
    }
}
