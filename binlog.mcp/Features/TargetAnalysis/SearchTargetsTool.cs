using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TargetAnalysis;

public class SearchTargetsTool
{
    [McpServerTool(Name = "search_targets_by_name", Title = "Search Targets by Name", UseStructuredContent = true, ReadOnly = true)]
    [Description("Find all executions of a specific target across all projects (e.g., 'CoreCompile') and return their timing information.")]
    public static Dictionary<string, TargetExecutionInfo> SearchTargetsByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The name of the target to search for (case-insensitive)")] string targetName)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null)
        {
            return [];
        }

        var results = new Dictionary<string, TargetExecutionInfo>();
        int counter = 0;

        foreach (var project in projects.Values)
        {
            var matchingTargets = project.Children.OfType<Target>()
                .Where(t => string.Equals(t.Name, targetName, StringComparison.OrdinalIgnoreCase));

            foreach (var target in matchingTargets)
            {
                var inclusiveMs = (long)target.Duration.TotalMilliseconds;
                var exclusiveDuration = TargetTimeCalculator.CalculateExclusiveDuration(target);
                var exclusiveMs = (long)exclusiveDuration.TotalMilliseconds;

                var key = $"{project.ProjectFile}_{target.Id}_{counter++}";
                results[key] = new TargetExecutionInfo(
                    project.Id,
                    project.ProjectFile,
                    target.Id,
                    inclusiveMs,
                    exclusiveMs,
                    target.Skipped);
            }
        }

        return results;
    }
}
