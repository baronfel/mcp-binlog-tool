using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectAnalysis;

public class ListProjectsTool
{
    [McpServerTool(Name = "list_projects", Title = "List Projects", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("List all projects in the loaded binary log file")]
    public static Dictionary<int, ProjectData> ListProjects(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file)
    {
        var binlog = new BinlogPath(binlog_file);
        var projects = BinlogLoader.GetProjectsById(binlog);

        if (projects == null) return [];

        return projects.Values.ToDictionary(p => p.Id, MakeProjectSummary);
    }

    private static ProjectData MakeProjectSummary(Project p)
    {
        var targetInfo = p.EntryTargets?.Select(p.FindTarget)?.Where(t => t != null);
        return new ProjectData(
            p.ProjectFile,
            p.Id,
            targetInfo?.ToDictionary(
                t => t.Id,
                t => new EntryTargetData(t.Name, t.Id, (long)t.Duration.TotalMilliseconds)));
    }
}
