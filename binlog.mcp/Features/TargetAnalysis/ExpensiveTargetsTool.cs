using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TargetAnalysis;

public class ExpensiveTargetsTool
{
    [McpServerTool(Name = "get_expensive_targets", Title = "Get Expensive Targets", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the N most expensive targets in the loaded binary log file")]
    public static Dictionary<string, TargetExecutionData> GetExpensiveTargets(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top targets to return. If not specified, returns all")] int? top_number)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null) return [];

        // the same target can be executed multiple times, so we need to group by name and sum the durations.
        // we can't use LINQ's GroupBy because the set of targets in the binlog could be huge, so we will use a dictionary to group them.
        // we should also track the _number_ of times each target was executed.
        var targetInclusiveDurations = new Dictionary<string, TimeSpan>();
        var targetExclusiveDurations = new Dictionary<string, TimeSpan>();
        var targetExecutions = new Dictionary<string, int>();
        var targetSkips = new Dictionary<string, int>();

        foreach (var target in build.FindChildrenRecursive<Target>())
        {
            if (target == null || target.Duration == TimeSpan.Zero) continue;

            if (targetInclusiveDurations.TryGetValue(target.Name, out var existingDuration))
            {
                targetInclusiveDurations[target.Name] = existingDuration + target.Duration;
            }
            else
            {
                targetInclusiveDurations[target.Name] = target.Duration;
            }

            var exclusiveDuration = TargetTimeCalculator.CalculateExclusiveDuration(target);

            if (targetExclusiveDurations.TryGetValue(target.Name, out var existingExclusiveDuration))
            {
                targetExclusiveDurations[target.Name] = existingExclusiveDuration + exclusiveDuration;
            }
            else
            {
                targetExclusiveDurations[target.Name] = exclusiveDuration;
            }

            if (target.Skipped)
            {
                if (targetSkips.TryGetValue(target.Name, out var existingSkipCount))
                {
                    targetSkips[target.Name] = existingSkipCount + 1;
                }
                else
                {
                    targetSkips[target.Name] = 1;
                }
            }
            else
            {
                if (targetExecutions.TryGetValue(target.Name, out var existingCount))
                {
                    targetExecutions[target.Name] = existingCount + 1;
                }
                else
                {
                    targetExecutions[target.Name] = 1;
                }
            }
        }

        // Get the top N most expensive targets by exclusive duration
        var orderedByCost =
            targetExclusiveDurations
                .OrderByDescending(kvp => kvp.Value);
        var limited = top_number.HasValue ? orderedByCost.Take(top_number.Value) : orderedByCost;
        return limited.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new TargetExecutionData(
                        executionCount: targetExecutions.TryGetValue(kvp.Key, out var execCount) ? execCount : 0,
                        skippedCount: targetSkips.TryGetValue(kvp.Key, out var skipCount) ? skipCount : 0,
                        inclusiveDurationMs: (long)targetInclusiveDurations[kvp.Key].TotalMilliseconds,
                        exclusiveDurationMs: (long)kvp.Value.TotalMilliseconds));
    }
}
