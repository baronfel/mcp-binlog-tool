using System.Text.Json;
using System.Text.Json.Serialization;
using Binlog.MCP.Features.AnalyzerAnalysis;
using Binlog.MCP.Features.DiagnosticAnalysis;
using Binlog.MCP.Features.EvaluationAnalysis;
using Binlog.MCP.Features.ProjectAnalysis;
using Binlog.MCP.Features.TargetAnalysis;
using Binlog.MCP.Features.TaskAnalysis;
using Binlog.MCP.Features.TimelineAnalysis;
using static Binlog.MCP.Features.BinlogLoading.LoadBinlogTool;

namespace Binlog.MCP;

/// <summary>
/// JSON serialization context for custom types used in binlog tool responses.
/// This context chains with the MCP library's default JsonTypeInfo to support structured outputs.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(InterestingBuildData))]
[JsonSerializable(typeof(TargetExecutionData))]
[JsonSerializable(typeof(Dictionary<string, TargetExecutionData>))]
[JsonSerializable(typeof(ProjectData))]
[JsonSerializable(typeof(Dictionary<int, ProjectData>))]
[JsonSerializable(typeof(EntryTargetData))]
[JsonSerializable(typeof(Dictionary<int, EntryTargetData>))]
[JsonSerializable(typeof(EvaluationData))]
[JsonSerializable(typeof(Dictionary<int, EvaluationData>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(TargetBuildReason))]
[JsonSerializable(typeof(DependsOnReason))]
[JsonSerializable(typeof(BeforeTargetsReason))]
[JsonSerializable(typeof(AfterTargetsReason))]
[JsonSerializable(typeof(TargetInfo))]
[JsonSerializable(typeof(TargetInfo?))]
[JsonSerializable(typeof(ProjectTargetListData))]
[JsonSerializable(typeof(IEnumerable<ProjectTargetListData>))]
[JsonSerializable(typeof(ProjectBuildTimeData))]
[JsonSerializable(typeof(ExpensiveProjectData))]
[JsonSerializable(typeof(Dictionary<int, ExpensiveProjectData>))]
[JsonSerializable(typeof(TargetTimeData))]
[JsonSerializable(typeof(Dictionary<int, TargetTimeData>))]
[JsonSerializable(typeof(TargetExecutionInfo))]
[JsonSerializable(typeof(Dictionary<string, TargetExecutionInfo>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(Timeline))]
[JsonSerializable(typeof(Timeline.NodeStats))]
[JsonSerializable(typeof(Dictionary<int, Timeline.NodeStats>))]
[JsonSerializable(typeof(SimpleTaskInfo))]
[JsonSerializable(typeof(Dictionary<int, SimpleTaskInfo>))]
[JsonSerializable(typeof(Dictionary<int, Dictionary<int, SimpleTaskInfo>>))]
[JsonSerializable(typeof(TaskDetails))]
[JsonSerializable(typeof(TaskDetails?))]
[JsonSerializable(typeof(Dictionary<int, TaskDetails>))]
[JsonSerializable(typeof(TaskExecutionData))]
[JsonSerializable(typeof(Dictionary<string, TaskExecutionData>))]
[JsonSerializable(typeof(AnalyzerInfo))]
[JsonSerializable(typeof(Dictionary<string, AnalyzerInfo>))]
[JsonSerializable(typeof(AssemblyAnalyzerData))]
[JsonSerializable(typeof(Dictionary<string, AssemblyAnalyzerData>))]
[JsonSerializable(typeof(CscAnalyzerData))]
[JsonSerializable(typeof(CscAnalyzerData?))]
[JsonSerializable(typeof(AggregatedAnalyzerData))]
[JsonSerializable(typeof(Dictionary<string, AggregatedAnalyzerData>))]
[JsonSerializable(typeof(DiagnosticSeverity))]
[JsonSerializable(typeof(DiagnosticInfo))]
[JsonSerializable(typeof(DiagnosticInfo[]))]
[JsonSerializable(typeof(DiagnosticFilter))]
[JsonSerializable(typeof(DiagnosticAnalysisResult))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
internal partial class BinlogJsonContext : JsonSerializerContext
{
}
