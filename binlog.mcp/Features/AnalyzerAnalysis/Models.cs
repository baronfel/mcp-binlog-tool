using System.ComponentModel;

namespace Binlog.MCP.Features.AnalyzerAnalysis;

/// <summary>
/// Information about a single analyzer or generator within an assembly.
/// </summary>
/// <param name="name">The analyzer or generator name.</param>
/// <param name="durationMs">The execution duration in milliseconds.</param>
public record struct AnalyzerInfo(
    [Description("The analyzer or generator name")] string name,
    [Description("The execution duration in milliseconds")] long durationMs);

/// <summary>
/// Aggregated analyzer data for an assembly.
/// </summary>
/// <param name="assemblyName">The analyzer assembly name.</param>
/// <param name="totalDurationMs">The total execution duration for all analyzers in this assembly in milliseconds.</param>
/// <param name="analyzers">Dictionary of individual analyzers within this assembly by name.</param>
public record struct AssemblyAnalyzerData(
    [Description("The analyzer assembly name")] string assemblyName,
    [Description("The total execution duration for all analyzers in this assembly in milliseconds")] long totalDurationMs,
    [Description("Individual analyzers within this assembly by name")] Dictionary<string, AnalyzerInfo> analyzers);

/// <summary>
/// Analyzer and generator data extracted from a Csc task invocation.
/// </summary>
/// <param name="analyzerAssemblies">Dictionary of analyzer assemblies by assembly name.</param>
/// <param name="generatorAssemblies">Dictionary of source generator assemblies by assembly name.</param>
public record struct CscAnalyzerData(
    [Description("Analyzer assemblies by assembly name")] Dictionary<string, AssemblyAnalyzerData> analyzerAssemblies,
    [Description("Source generator assemblies by assembly name")] Dictionary<string, AssemblyAnalyzerData> generatorAssemblies);

/// <summary>
/// Aggregated analyzer execution data across the entire build.
/// </summary>
public record struct AggregatedAnalyzerData(
    [Description("The analyzer or generator name")] string name,
    [Description("The number of times this analyzer was executed")] int executionCount,
    [Description("The total execution duration across all invocations in milliseconds")] long totalDurationMs,
    [Description("The average execution duration per invocation in milliseconds")] long averageDurationMs,
    [Description("The minimum execution duration in milliseconds")] long minDurationMs,
    [Description("The maximum execution duration in milliseconds")] long maxDurationMs);
