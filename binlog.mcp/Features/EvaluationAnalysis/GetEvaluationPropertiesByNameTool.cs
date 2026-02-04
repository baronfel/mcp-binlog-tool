using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.EvaluationAnalysis;

public class GetEvaluationPropertiesByNameTool
{
    [McpServerTool(Name = "get_evaluation_properties_by_name", Title = "Get Properties by Name for Evaluation", UseStructuredContent = true, ReadOnly = true, Idempotent = true)]
    [Description("Get specific properties by name for a project evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. This returns all properties (both global and non-global) matching the requested names.")]
    public static Dictionary<string, string?> GetPropertiesByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the evaluation to get properties for")] int evaluationId,
        [Description("Array of property names to retrieve. If empty or not provided, returns all properties.")] string[]? propertyNames = null)
    {
        var binlog = new BinlogPath(binlog_file);
        var build = BinlogLoader.GetBuild(binlog);

        if (build == null)
        {
            return [];
        }

        var eval = build.FindEvaluation(evaluationId);
        if (eval == null)
        {
            return [];
        }

        var propertiesFolder = eval.FindChild<Folder>("Properties");
        if (propertiesFolder == null)
        {
            return [];
        }

        // Collect all properties from all sources (Global folder and direct properties)
        var allProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Get properties from Global folder
        var globalFolder = propertiesFolder.FindChild<Folder>("Global");
        if (globalFolder != null)
        {
            foreach (var prop in globalFolder.Children.OfType<Property>())
            {
                allProperties[prop.Name] = prop.Value;
            }
        }

        // Get direct properties (non-folder children)
        foreach (var prop in propertiesFolder.Children.OfType<Property>())
        {
            allProperties[prop.Name] = prop.Value;
        }

        // Filter by requested names if provided
        if (propertyNames != null && propertyNames.Length > 0)
        {
            var filtered = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in propertyNames)
            {
                if (allProperties.TryGetValue(name, out var value))
                {
                    filtered[name] = value;
                }
                else
                {
                    // Include requested properties even if not found (with null value)
                    filtered[name] = null;
                }
            }
            return filtered;
        }

        return allProperties;
    }
}
