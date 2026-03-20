using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectAnalysis;

public class GetProjectBuildTimeTool
{
    [McpServerTool(Name = "get_project_build_time", Title = "Get Project Build Time", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the total build time for a specific project, calculating exclusive time across all its targets with optional filtering to exclude specific targets.")]
    public static ProjectBuildTimeData GetProjectBuildTime(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to get build time for")] int projectId,
        [Description("Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])")] string[]? excludeTargets = null)
    {
        var binlog = new BinlogPath(binlog_file);
        var excludeSet = excludeTargets?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var buildTimeData = ProjectBuildTimeCache.GetOrCompute(binlog, excludeSet);

        return buildTimeData.TryGetValue(new ProjectId(projectId), out var data)
            ? data
            : new ProjectBuildTimeData(0, 0, 0);
    }
}
