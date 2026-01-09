using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.EvaluationAnalysis;

public class GetEvaluationPropertiesTool
{
    [McpServerTool(Name = "get_evaluation_global_properties", Title = "Get Properties for Evaluation", UseStructuredContent = true, ReadOnly = true)]
    [Description("Get the global properties for a specific evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. Global properties are what make evaluations distinct from one another within the same project.")]
    public static Dictionary<string, string> GetGlobalPropertiesForEvaluation(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the evaluation to get properties for")] int evaluationId)
    {
        var binlog = new BinlogPath(binlog_file);
        var build = BinlogLoader.GetBuild(binlog);

        if (build != null &&
            build.FindEvaluation(evaluationId) is var eval &&
            eval.FindChild<Folder>("Properties") is var propertiesFolder &&
            propertiesFolder.FindChild<Folder>("Global") is var globalProperties)
        {
            return globalProperties.Children.OfType<Property>().ToDictionary(p => p.Name, p => p.Value);
        }

        return [];
    }
}
