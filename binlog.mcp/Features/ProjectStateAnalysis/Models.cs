using System.ComponentModel;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

/// <summary>
/// Represents the state of a project's properties and items at a specific point during the build.
/// </summary>
/// <param name="projectId">The project ID.</param>
/// <param name="projectFile">The project file path.</param>
/// <param name="evaluationId">The evaluation ID this project is based on.</param>
/// <param name="beforeTarget">If specified, the state is captured before this target executed. Null means after evaluation but before any targets.</param>
/// <param name="afterTarget">If specified, the state is captured after this target executed. Null means only evaluation state.</param>
/// <param name="properties">All properties at this point in the build.</param>
/// <param name="items">All items grouped by item type at this point in the build.</param>
public record struct ProjectStateSnapshot(
    [Description("The project ID")] int projectId,
    [Description("The project file path")] string projectFile,
    [Description("The evaluation ID this project is based on")] int evaluationId,
    [Description("If specified, the state is captured before this target executed")] string? beforeTarget,
    [Description("If specified, the state is captured after this target executed")] string? afterTarget,
    [Description("All properties at this point in the build")] Dictionary<string, string> properties,
    [Description("All items grouped by item type at this point in the build")] Dictionary<string, ProjectStateItem[]> items);

/// <summary>
/// Represents an item in the project state.
/// </summary>
/// <param name="identity">The item identity/include value.</param>
/// <param name="metadata">Metadata associated with the item.</param>
public record struct ProjectStateItem(
    [Description("The item identity/include value")] string identity,
    [Description("Metadata associated with the item")] Dictionary<string, string> metadata);

/// <summary>
/// Represents a state change event during the build.
/// </summary>
/// <param name="targetName">The target where this change occurred.</param>
/// <param name="taskName">The task that caused this change.</param>
/// <param name="changeType">The type of change (PropertySet, ItemAdded, ItemRemoved, etc.).</param>
/// <param name="name">The name of the property or item type affected.</param>
/// <param name="value">The new value (for properties) or item identity (for items).</param>
public record struct StateChangeEvent(
    [Description("The target where this change occurred")] string targetName,
    [Description("The task that caused this change")] string taskName,
    [Description("The type of change")] StateChangeType changeType,
    [Description("The name of the property or item type affected")] string name,
    [Description("The new value or item identity")] string? value);

/// <summary>
/// Types of state changes that can occur during a build.
/// </summary>
public enum StateChangeType
{
    /// <summary>A property was set or modified.</summary>
    PropertySet,
    /// <summary>An item was added.</summary>
    ItemAdded,
    /// <summary>An item was removed.</summary>
    ItemRemoved,
    /// <summary>Item metadata was modified.</summary>
    MetadataModified
}

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
