using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Binlog.MCP.Features.TargetAnalysis;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectAnalysis;

public class GetProjectTargetTimesTool
{
    [McpServerTool(Name = "get_project_target_times", Title = "Get Project Target Times", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get all target execution times for a specific project in one call, including both inclusive and exclusive durations.")]
    public static Dictionary<int, TargetTimeData> GetProjectTargetTimes(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the project to get target times for")] int projectId)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null ||
            !projects.TryGetValue(new ProjectId(projectId), out var project))
        {
            return [];
        }

        var targets = project.Children.OfType<Target>();
        var result = new Dictionary<int, TargetTimeData>();

        foreach (var target in targets)
        {
            var inclusiveMs = (long)target.Duration.TotalMilliseconds;
            var exclusiveDuration = TargetTimeCalculator.CalculateExclusiveDuration(target);
            var exclusiveMs = (long)exclusiveDuration.TotalMilliseconds;

            result[target.Id] = new TargetTimeData(target.Id, target.Name, inclusiveMs, exclusiveMs, target.Skipped);
        }

        return result;
    }
}
