using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TaskAnalysis;

/// <summary>
/// MCP tool for finding the most expensive tasks across the entire build.
/// </summary>
public class GetExpensiveTasksTool
{
    [McpServerTool(Name = "get_expensive_tasks", Title = "Get Expensive Tasks", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the N most expensive MSBuild tasks in the loaded binary log file, aggregated by task name.")]
    public static Dictionary<string, TaskExecutionData> GetExpensiveTasks(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top tasks to return. If not specified, returns all")] int? top_number)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null) return [];

        // Aggregate task data by name
        var taskStats = new Dictionary<string, List<Microsoft.Build.Logging.StructuredLogger.Task>>();

        foreach (var task in build.FindChildrenRecursive<Microsoft.Build.Logging.StructuredLogger.Task>())
        {
            if (task == null || task.Duration == TimeSpan.Zero) continue;

            if (!taskStats.TryGetValue(task.Name, out var tasks))
            {
                tasks = [];
                taskStats[task.Name] = tasks;
            }
            tasks.Add(task);
        }

        // Calculate aggregated statistics
        var ordered = taskStats.Select(kvp =>
        {
            var taskName = kvp.Key;
            var tasks = kvp.Value;
            var durations = tasks.Select(t => (long)t.Duration.TotalMilliseconds).ToList();

            return new
            {
                TaskName = taskName,
                Data = new TaskExecutionData(
                    taskName,
                    tasks[0].FromAssembly,
                    tasks.Count,
                    durations.Sum(),
                    durations.Sum() / tasks.Count,
                    durations.Min(),
                    durations.Max())
            };
        })
        .OrderByDescending(x => x.Data.totalDurationMs);

        return top_number.HasValue
            ? ordered.Take(top_number.Value).ToDictionary(x => x.TaskName, x => x.Data)
            : ordered.ToDictionary(x => x.TaskName, x => x.Data);
    }
}
