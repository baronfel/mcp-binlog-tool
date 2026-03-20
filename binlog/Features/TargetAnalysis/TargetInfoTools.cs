using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TargetAnalysis;

public class TargetInfoTools
{
    [McpServerTool(Name = "get_target_info_by_name", Title = "Get Target Information", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc.")]
    public static TargetInfo? GetTargetInfoByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the target")] int projectId,
        [Description("The name of the target to get dependencies for")] string targetName)
    {
        var binlog = new BinlogPath(binlog_file);
        if (BinlogLoader.TryGetProjectsById(binlog, out var projects) && projects != null &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var target = project.FindTarget(targetName);
            if (target != null)
            {
                return new TargetInfo(
                    target.Id,
                    target.Name,
                    (long)target.Duration.TotalMilliseconds,
                    target.Succeeded,
                    target.Skipped,
                    BuildReason(target),
                    [.. target.Children.OfType<Message>().Select(m => m.Text)]);
            }
        }

        return null;
    }

    [McpServerTool(Name = "get_target_info_by_id", Title = "Get Target Information", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get some details about a specific target called in a project within the loaded binary log file. This includes the target's duration, its ID, why it was built, etc. This is more efficient than `get_target_info_by_name` if you already know the target ID, as it avoids searching by name.")]
    public static TargetInfo? GetTargetInfoById(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project containing the target")] int projectId,
        [Description("The ID of the target to get dependencies for")] int targetId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (BinlogLoader.TryGetProjectsById(binlog, out var projects) && projects != null &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var target = project.GetTargetById(targetId);
            if (target != null)
            {
                return new TargetInfo(
                    target.Id,
                    target.Name,
                    (long)target.Duration.TotalMilliseconds,
                    target.Succeeded,
                    target.Skipped,
                    BuildReason(target),
                    [.. target.Children.OfType<Message>().Select(m => m.Text)]);
            }
        }

        return null;
    }

    private static TargetBuildReason? BuildReason(Target target)
    {
        return target.TargetBuiltReason switch
        {
            TargetBuiltReason.AfterTargets => new AfterTargetsReason(target.ParentTarget),
            TargetBuiltReason.BeforeTargets => new BeforeTargetsReason(target.ParentTarget),
            TargetBuiltReason.DependsOn => new DependsOnReason(target.ParentTarget),
            _ => null
        };
    }
}
