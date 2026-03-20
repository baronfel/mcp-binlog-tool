using System.ComponentModel;

namespace Binlog.MCP.Features.ProjectAnalysis;

/// <summary>
/// Summary data for a project.
/// </summary>
/// <param name="projectFile">The project file path.</param>
/// <param name="id">The project ID.</param>
/// <param name="entryTargets">Dictionary of entry targets by ID.</param>
public record struct ProjectData(string projectFile, int id, Dictionary<int, EntryTargetData>? entryTargets);

/// <summary>
/// Data for an entry target of a project.
/// </summary>
/// <param name="targetName">The target name.</param>
/// <param name="id">The target ID.</param>
/// <param name="durationMs">The target duration in milliseconds.</param>
public record struct EntryTargetData(string targetName, int id, long durationMs);

/// <summary>
/// List item data for a project target.
/// </summary>
/// <param name="id">The target ID.</param>
/// <param name="name">The target name.</param>
/// <param name="durationMs">The target duration in milliseconds.</param>
public record struct ProjectTargetListData(int id, string name, long durationMs);

/// <summary>
/// Aggregated build time data for a project.
/// </summary>
public record struct ProjectBuildTimeData(
    [Description("The total exclusive duration of all targets in the project in milliseconds. This is the actual work done in this project.")] long exclusiveDurationMs,
    [Description("The total inclusive duration of all targets in the project in milliseconds. This includes child target calls.")] long inclusiveDurationMs,
    [Description("The number of targets that were executed in this project.")] int targetCount);

/// <summary>
/// Data for expensive projects ordered by build time.
/// </summary>
public record struct ExpensiveProjectData(
    [Description("The project file path")] string projectFile,
    [Description("The project ID")] int projectId,
    [Description("The total exclusive duration of all targets in the project in milliseconds")] long exclusiveDurationMs,
    [Description("The total inclusive duration of all targets in the project in milliseconds")] long inclusiveDurationMs,
    [Description("The number of targets executed in this project")] int targetCount);
