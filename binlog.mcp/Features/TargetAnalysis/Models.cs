using System.ComponentModel;

namespace Binlog.MCP.Features.TargetAnalysis;

/// <summary>
/// Aggregated execution data for a target across multiple executions.
/// </summary>
public record struct TargetExecutionData(
    [Description("The number of times the target was actually run.")] int executionCount,
    [Description("The number of times the target was requested but not run due to incrementality. Generally the higher this number the better.")] int skippedCount,
    [Description("The total inclusive duration of the target execution in milliseconds. This time includes 'child' Target calls, so it may not be representative of the work actually done _in_ this Target.")] long inclusiveDurationMs,
    [Description("The total exclusive duration of the target execution in milliseconds. This is the work done _in_ this Target.")] long exclusiveDurationMs);

/// <summary>
/// Base class for reasons why a target was executed.
/// </summary>
public abstract record TargetBuildReason;

/// <summary>
/// Indicates the target was built because another target depends on it.
/// </summary>
/// <param name="targetThatDependsOnCurrentTarget">The name of the target that depends on the current target.</param>
public record DependsOnReason(string targetThatDependsOnCurrentTarget) : TargetBuildReason;

/// <summary>
/// Indicates the target was built to run before another target.
/// </summary>
/// <param name="targetThatThisTargetMustRunBefore">The name of the target that this target must run before.</param>
public record BeforeTargetsReason(string targetThatThisTargetMustRunBefore) : TargetBuildReason;

/// <summary>
/// Indicates the target was built to run after another target.
/// </summary>
/// <param name="targetThatThisTargetIsRunningAfter">The name of the target that this target is running after.</param>
public record AfterTargetsReason(string targetThatThisTargetIsRunningAfter) : TargetBuildReason;

/// <summary>
/// Detailed information about a specific target execution.
/// </summary>
/// <param name="id">Target ID.</param>
/// <param name="name">Target name.</param>
/// <param name="durationMs">Duration in milliseconds.</param>
/// <param name="succeeded">Whether the target succeeded.</param>
/// <param name="skipped">Whether the target was skipped.</param>
/// <param name="builtReason">The reason the target was built.</param>
/// <param name="targetMessages">Messages logged by the target.</param>
public record struct TargetInfo(int id, string name, long durationMs, bool succeeded, bool skipped, TargetBuildReason? builtReason, string[] targetMessages);

/// <summary>
/// Information about a single target execution within a specific project.
/// </summary>
public record struct TargetExecutionInfo(
    [Description("The project ID containing this target execution")] int projectId,
    [Description("The project file path")] string projectFile,
    [Description("The target ID")] int targetId,
    [Description("The inclusive duration in milliseconds")] long inclusiveDurationMs,
    [Description("The exclusive duration in milliseconds")] long exclusiveDurationMs,
    [Description("Whether the target was skipped")] bool skipped);

/// <summary>
/// Timing data for a target including both inclusive and exclusive durations.
/// </summary>
public record struct TargetTimeData(
    [Description("The target ID")] int id,
    [Description("The target name")] string name,
    [Description("The inclusive duration of the target in milliseconds")] long inclusiveDurationMs,
    [Description("The exclusive duration of the target in milliseconds")] long exclusiveDurationMs,
    [Description("Whether the target was skipped")] bool skipped);
