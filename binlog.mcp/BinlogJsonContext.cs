using System.Text.Json;
using System.Text.Json.Serialization;

namespace Binlog.MCP;

/// <summary>
/// JSON serialization context for custom types used in binlog tool responses.
/// This context chains with the MCP library's default JsonTypeInfo to support structured outputs.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(BinlogTool.InterestingBuildData))]
[JsonSerializable(typeof(BinlogTool.TargetExecutionData))]
[JsonSerializable(typeof(Dictionary<string, BinlogTool.TargetExecutionData>))]
[JsonSerializable(typeof(BinlogTool.ProjectData))]
[JsonSerializable(typeof(Dictionary<int, BinlogTool.ProjectData>))]
[JsonSerializable(typeof(BinlogTool.EntryTargetData))]
[JsonSerializable(typeof(Dictionary<int, BinlogTool.EntryTargetData>))]
[JsonSerializable(typeof(BinlogTool.EvaluationData))]
[JsonSerializable(typeof(Dictionary<int, BinlogTool.EvaluationData>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(BinlogTool.TargetBuildReason))]
[JsonSerializable(typeof(BinlogTool.DependsOnReason))]
[JsonSerializable(typeof(BinlogTool.BeforeTargetsReason))]
[JsonSerializable(typeof(BinlogTool.AfterTargetsReason))]
[JsonSerializable(typeof(BinlogTool.TargetInfo))]
[JsonSerializable(typeof(BinlogTool.TargetInfo?))]
[JsonSerializable(typeof(BinlogTool.ProjectTargetListData))]
[JsonSerializable(typeof(IEnumerable<BinlogTool.ProjectTargetListData>))]
[JsonSerializable(typeof(BinlogTool.ProjectBuildTimeData))]
[JsonSerializable(typeof(BinlogTool.ExpensiveProjectData))]
[JsonSerializable(typeof(Dictionary<int, BinlogTool.ExpensiveProjectData>))]
[JsonSerializable(typeof(BinlogTool.TargetTimeData))]
[JsonSerializable(typeof(Dictionary<int, BinlogTool.TargetTimeData>))]
[JsonSerializable(typeof(BinlogTool.TargetExecutionInfo))]
[JsonSerializable(typeof(Dictionary<string, BinlogTool.TargetExecutionInfo>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
internal partial class BinlogJsonContext : JsonSerializerContext
{
}
