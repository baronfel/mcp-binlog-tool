using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.AnalyzerAnalysis;

/// <summary>
/// MCP tool for finding the most expensive analyzers and generators across the entire build.
/// </summary>
public class GetExpensiveAnalyzersTool
{
    [McpServerTool(Name = "get_expensive_analyzers", Title = "Get Expensive Analyzers", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the N most expensive Roslyn analyzers and source generators across the entire build, aggregated by analyzer name.")]
    public static Dictionary<string, AggregatedAnalyzerData> GetExpensiveAnalyzers(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The number of top analyzers to return. If not specified, returns all")] int? top_number)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null)
        {
            return [];
        }

        // Dictionary to aggregate analyzer data: analyzerName -> list of durations
        var analyzerStats = new Dictionary<string, List<long>>();

        // Find all Csc tasks across the build and parse their analyzer data
        foreach (var task in build.FindChildrenRecursive<Microsoft.Build.Logging.StructuredLogger.Task>())
        {
            if (task == null)
                continue;

            var analyzerData = AnalyzerParser.ParseCscTask(task);
            if (analyzerData == null)
                continue;

            var data = analyzerData.Value;

            // Aggregate all analyzers and generators
            AggregateAnalyzers(data.analyzerAssemblies, analyzerStats);
            AggregateAnalyzers(data.generatorAssemblies, analyzerStats);
        }

        // Calculate aggregated statistics
        var query = analyzerStats.Select(kvp =>
        {
            var analyzerName = kvp.Key;
            var durations = kvp.Value;

            return new
            {
                AnalyzerName = analyzerName,
                Data = new AggregatedAnalyzerData(
                    analyzerName,
                    durations.Count,
                    durations.Sum(),
                    durations.Sum() / durations.Count,
                    durations.Min(),
                    durations.Max())
            };
        })
        .OrderByDescending(x => x.Data.totalDurationMs);

        if (top_number.HasValue)
        {
            return query.Take(top_number.Value).ToDictionary(x => x.AnalyzerName, x => x.Data);
        }
        else
        {
            return query.ToDictionary(x => x.AnalyzerName, x => x.Data);
        }
    }

    private static void AggregateAnalyzers(
        Dictionary<string, AssemblyAnalyzerData> assemblies,
        Dictionary<string, List<long>> analyzerStats)
    {
        foreach (var assembly in assemblies.Values)
        {
            foreach (var analyzer in assembly.analyzers.Values)
            {
                if (!analyzerStats.TryGetValue(analyzer.name, out var durations))
                {
                    durations = [];
                    analyzerStats[analyzer.name] = durations;
                }
                durations.Add(analyzer.durationMs);
            }
        }
    }
}
