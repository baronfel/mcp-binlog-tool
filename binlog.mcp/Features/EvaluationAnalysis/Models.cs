namespace Binlog.MCP.Features.EvaluationAnalysis;

/// <summary>
/// Data for a project evaluation.
/// </summary>
/// <param name="id">The evaluation ID.</param>
/// <param name="projectFile">The project file path.</param>
/// <param name="durationMs">The evaluation duration in milliseconds.</param>
public record struct EvaluationData(int id, string projectFile, long durationMs);

/// <summary>
/// Represents an item instance from a project evaluation.
/// </summary>
/// <param name="name">The item name (identity/include value).</param>
/// <param name="metadata">Optional metadata associated with the item. Empty if no metadata.</param>
public record struct EvaluationItem(string name, Dictionary<string, string> metadata);

/// <summary>
/// Result for items grouped by item type.
/// </summary>
/// <param name="itemType">The item type name (e.g., "Compile", "PackageReference").</param>
/// <param name="items">The items of this type.</param>
public record struct ItemsByType(string itemType, EvaluationItem[] items);

