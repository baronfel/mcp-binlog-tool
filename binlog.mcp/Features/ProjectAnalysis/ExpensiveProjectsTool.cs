using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.ProjectAnalysis;

public class ExpensiveProjectsTool
{
    [McpServerTool(Name = "get_expensive_projects", Title = "Get Expensive Projects", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the N most expensive projects in the loaded binary log file, aggregated at the project level with options to exclude specific targets and show exclusive vs inclusive time.")]
    public static Dictionary<int, ExpensiveProjectData> GetExpensiveProjects(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top projects to return. If not specified, returns all")] int? top_number,
        [Description("Optional array of target names to exclude from the calculation (e.g., ['Copy', 'CopyFilesToOutputDirectory'])")] string[]? excludeTargets = null,
        [Description("Whether to sort by exclusive time (true) or inclusive time (false). Default is exclusive.")] bool sortByExclusive = true)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null)
        {
            return [];
        }

        var excludeSet = excludeTargets?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var buildTimeData = ProjectBuildTimeCache.GetOrCompute(binlog, excludeSet);

        var projectData = buildTimeData
            .Where(kvp => kvp.Value.targetCount > 0)
            .Select(kvp => new ExpensiveProjectData(
                projects[kvp.Key].ProjectFile,
                kvp.Key.id,
                kvp.Value.exclusiveDurationMs,
                kvp.Value.inclusiveDurationMs,
                kvp.Value.targetCount));

        var sorted = sortByExclusive
            ? projectData.OrderByDescending(p => p.exclusiveDurationMs)
            : projectData.OrderByDescending(p => p.inclusiveDurationMs);

        return top_number.HasValue ? sorted.Take(top_number.Value).ToDictionary(p => p.projectId) : sorted.ToDictionary(p => p.projectId);
    }
}
