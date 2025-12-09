using System.ComponentModel;

namespace Binlog.MCP.Features.TargetAnalysis;

public record struct TargetExecutionData(
    [Description("The number of times the target was actually run.")] int executionCount,
    [Description("The number of times the target was requested but not run due to incrementality. Generally the higher this number the better.")] int skippedCount,
    [Description("The total inclusive duration of the target execution in milliseconds. This time includes 'child' Target calls, so it may not be representative of the work actually done _in_ this Target.")] long inclusiveDurationMs,
    [Description("The total exclusive duration of the target execution in milliseconds. This is the work done _in_ this Target.")] long exclusiveDurationMs);

public abstract record TargetBuildReason;
public record DependsOnReason(string targetThatDependsOnCurrentTarget) : TargetBuildReason;
public record BeforeTargetsReason(string targetThatThisTargetMustRunBefore) : TargetBuildReason;
public record AfterTargetsReason(string targetThatThisTargetIsRunningAfter) : TargetBuildReason;

public record struct TargetInfo(int id, string name, long durationMs, bool succeeded, bool skipped, TargetBuildReason? builtReason, string[] targetMessages);

public record struct TargetExecutionInfo(
    [Description("The project ID containing this target execution")] int projectId,
    [Description("The project file path")] string projectFile,
    [Description("The target ID")] int targetId,
    [Description("The inclusive duration in milliseconds")] long inclusiveDurationMs,
    [Description("The exclusive duration in milliseconds")] long exclusiveDurationMs,
    [Description("Whether the target was skipped")] bool skipped);

public record struct TargetTimeData(
    [Description("The target ID")] int id,
    [Description("The target name")] string name,
    [Description("The inclusive duration of the target in milliseconds")] long inclusiveDurationMs,
    [Description("The exclusive duration of the target in milliseconds")] long exclusiveDurationMs,
    [Description("Whether the target was skipped")] bool skipped);
