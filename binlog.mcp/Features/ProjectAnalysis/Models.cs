using System.ComponentModel;

namespace Binlog.MCP.Features.ProjectAnalysis;

public record struct ProjectData(string projectFile, int id, Dictionary<int, EntryTargetData>? entryTargets);

public record struct EntryTargetData(string targetName, int id, long durationMs);

public record struct ProjectTargetListData(int id, string name, long durationMs);

public record struct ProjectBuildTimeData(
    [Description("The total exclusive duration of all targets in the project in milliseconds. This is the actual work done in this project.")] long exclusiveDurationMs,
    [Description("The total inclusive duration of all targets in the project in milliseconds. This includes child target calls.")] long inclusiveDurationMs,
    [Description("The number of targets that were executed in this project.")] int targetCount);

public record struct ExpensiveProjectData(
    [Description("The project file path")] string projectFile,
    [Description("The project ID")] int projectId,
    [Description("The total exclusive duration of all targets in the project in milliseconds")] long exclusiveDurationMs,
    [Description("The total inclusive duration of all targets in the project in milliseconds")] long inclusiveDurationMs,
    [Description("The number of targets executed in this project")] int targetCount);
