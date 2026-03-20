using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.EvaluationAnalysis;

public class GetEvaluationItemsByNameTool
{
    [McpServerTool(Name = "get_evaluation_items_by_name", Title = "Get Items by Name for Evaluation", UseStructuredContent = true, ReadOnly = true, Idempotent = true)]
    [Description("Get specific items by type name for a project evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. Returns items organized by item type (e.g., 'Compile', 'PackageReference', 'Reference').")]
    public static ItemsByType[] GetItemsByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The ID of the evaluation to get items for")] int evaluationId,
        [Description("Array of item type names to retrieve (e.g., ['Compile', 'PackageReference']). If empty or not provided, returns all item types.")] string[]? itemTypeNames = null,
        [Description("Maximum number of items to return per item type. Default is 100.")] int? maxItemsPerType = null)
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

        var itemsFolder = eval.FindChild<Folder>("Items");
        if (itemsFolder == null)
        {
            return [];
        }

        var maxPerType = maxItemsPerType ?? 100;
        var results = new List<ItemsByType>();

        // Get all AddItem nodes from the Items folder
        var addItems = itemsFolder.Children.OfType<AddItem>();

        // Filter by requested names if provided
        if (itemTypeNames != null && itemTypeNames.Length > 0)
        {
            var nameSet = new HashSet<string>(itemTypeNames, StringComparer.OrdinalIgnoreCase);
            addItems = addItems.Where(a => nameSet.Contains(a.Name));
        }

        foreach (var addItem in addItems)
        {
            var itemType = addItem.Name;
            var items = new List<EvaluationItem>();

            foreach (var child in addItem.Children.OfType<Item>().Take(maxPerType))
            {
                var metadata = new Dictionary<string, string>();

                // Extract metadata if present
                foreach (var metadataNode in child.Children.OfType<Metadata>())
                {
                    metadata[metadataNode.Name] = metadataNode.Value;
                }

                items.Add(new EvaluationItem(child.Name, metadata));
            }

            results.Add(new ItemsByType(itemType, items.ToArray()));
        }

        return results.ToArray();
    }
}
