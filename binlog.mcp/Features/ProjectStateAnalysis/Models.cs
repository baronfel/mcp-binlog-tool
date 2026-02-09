using System.ComponentModel;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

/// <summary>
/// Represents the state of a project's properties and items from evaluation.
/// </summary>
/// <param name="projectId">The project ID.</param>
/// <param name="projectFile">The project file path.</param>
/// <param name="evaluationId">The evaluation ID this project is based on.</param>
/// <param name="properties">All properties at evaluation time (before any targets execute).</param>
/// <param name="items">All items grouped by item type at evaluation time (before any targets execute).</param>
public record struct ProjectStateSnapshot(
    [Description("The project ID")] int projectId,
    [Description("The project file path")] string projectFile,
    [Description("The evaluation ID this project is based on")] int evaluationId,
    [Description("All properties at evaluation time (before any targets execute)")] Dictionary<string, string> properties,
    [Description("All items grouped by item type at evaluation time (before any targets execute)")] Dictionary<string, ProjectStateItem[]> items);

/// <summary>
/// Represents an item in the project state.
/// </summary>
/// <param name="identity">The item identity/include value.</param>
/// <param name="metadata">Metadata associated with the item.</param>
public record struct ProjectStateItem(
    [Description("The item identity/include value")] string identity,
    [Description("Metadata associated with the item")] Dictionary<string, string> metadata);

/// <summary>
/// Summary of targets executed for a project.
/// </summary>
/// <param name="targetName">The target name.</param>
/// <param name="targetId">The target ID.</param>
/// <param name="durationMs">The duration in milliseconds.</param>
/// <param name="startTimeMs">The start time relative to build start.</param>
public record struct TargetExecutionSummary(
    [Description("The target name")] string targetName,
    [Description("The target ID")] int targetId,
    [Description("The duration in milliseconds")] long durationMs,
    [Description("The start time relative to build start")] long startTimeMs);
