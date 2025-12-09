namespace Binlog.MCP.Features.EvaluationAnalysis;

/// <summary>
/// Data for a project evaluation.
/// </summary>
/// <param name="id">The evaluation ID.</param>
/// <param name="projectFile">The project file path.</param>
/// <param name="durationMs">The evaluation duration in milliseconds.</param>
public record struct EvaluationData(int id, string projectFile, long durationMs);
