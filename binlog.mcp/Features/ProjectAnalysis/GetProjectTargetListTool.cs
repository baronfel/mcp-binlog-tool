using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectAnalysis;

public class GetProjectTargetListTool
{
    [McpServerTool(Name = "get_project_target_list", Title = "Get Project Target List", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get a list of targets for a specific project in the loaded binary log file. This includes the target's name, ID, and duration.")]
    public static IEnumerable<ProjectTargetListData> GetProjectTargetList(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to get targets for")] int projectId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (BinlogLoader.TryGetProjectsById(binlog, out var projects) && projects != null &&
            projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            var targets = project.Children.OfType<Target>();
            if (targets is not null)
            {
                return targets.Select(t => new ProjectTargetListData(t.Id, t.Name, (long)t.Duration.TotalMilliseconds));
            }
        }
        return [];
    }
}
