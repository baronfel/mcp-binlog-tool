using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.EvaluationAnalysis;

public class ListEvaluationsTool
{
    [McpServerTool(Name = "list_evaluations", Title = "Get Project Evaluations", UseStructuredContent = true, ReadOnly = true)]
    [Description("List all evaluations for a specific project in the loaded binary log file. You can use the `list_projects` command to find the project file paths.")]
    public static Dictionary<int, EvaluationData> GetEvaluationsForProject(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The path to the project file to get evaluations for")] string projectFilePath)
    {
        BinlogPath binlog = new(binlog_file);
        ProjectFilePath projectFile = new(projectFilePath);

        var evaluations = BinlogLoader.GetEvaluationsByPath(binlog);

        if (evaluations != null &&
            evaluations.TryGetValue(projectFile, out var evalIds) &&
            evalIds.Length > 0)
        {
            var build = BinlogLoader.GetBuild(binlog);
            if (build != null)
            {
                var evalData = evalIds
                    .Select(e => build.FindEvaluation(e.id))
                    .Select(e => new EvaluationData(e.Id, e.ProjectFile, (long)e.Duration.TotalMilliseconds));
                return evalData.ToDictionary(e => e.id);
            }
        }

        return [];
    }
}
