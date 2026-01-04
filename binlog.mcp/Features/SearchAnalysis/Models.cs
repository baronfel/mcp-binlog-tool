using System.ComponentModel;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.SearchAnalysis;

/// <summary>
/// Represents a search result from the binlog freetext search.
/// </summary>
public record struct SearchResultInfo(
    [Description("The type of the node that matched")] string nodeType,
    [Description("The text content of the matched node")] string text,
    [Description("The ID of the matched node if available")] int? nodeId,
    [Description("Duration of the node if it's a timed node")] TimeSpan? duration,
    [Description("Start time of the node if it's a timed node")] DateTime? startTime,
    [Description("End time of the node if it's a timed node")] DateTime? endTime,
    [Description("Fields that matched the search query")] MatchedField[] matchedFields,
    [Description("Whether the node was matched by its type rather than content")] bool matchedByType,
    [Description("The ID of the project containing this node, if applicable")] int? projectId,
    [Description("The ID of the target containing this node, if applicable")] int? targetId,
    [Description("The ID of the task containing this node, if applicable")] int? taskId);

/// <summary>
/// Represents a field that matched the search query.
/// </summary>
public record struct MatchedField(
    [Description("The name of the field that matched")] string fieldName,
    [Description("The matching text within the field")] string matchText);

/// <summary>
/// Options for controlling the search behavior.
/// </summary>
public record struct SearchOptions(
    [Description("Maximum number of search results to return")] int maxResults = 300,
    [Description("Whether to include duration information for timed nodes")] bool includeDuration = true,
    [Description("Whether to include start time information for timed nodes")] bool includeStartTime = false,
    [Description("Whether to include end time information for timed nodes")] bool includeEndTime = false,
    [Description("Whether to include context information (project, target, task names)")] bool includeContext = true);

/// <summary>
/// Result container for search analysis.
/// </summary>
public record struct SearchAnalysisResult(
    [Description("The search query that was executed")] string query,
    [Description("Array of search results")] SearchResultInfo[] results,
    [Description("Total number of results found")] int totalResults,
    [Description("Whether the results were truncated due to maxResults limit")] bool wasTruncated);

public static class SearchResultUtil
{
    /// <summary>
    /// Creates a SearchResultInfo from a StructuredLogViewer SearchResult.
    /// </summary>
    public static SearchResultInfo CreateSearchResultInfo(StructuredLogViewer.SearchResult searchResult, SearchOptions options)
    {
        var node = searchResult.Node;
        var nodeType = node?.GetType().Name ?? "Unknown";
        var text = node?.ToString() ?? "";
        var nodeId = GetNodeId(node);

        // Extract matched fields
        var matchedFields = searchResult.WordsInFields
            .Select(wif => new MatchedField(wif.field, wif.match))
            .ToArray();

        // Get context IDs
        int? projectId = null;
        int? targetId = null;
        int? taskId = null;

        if (options.includeContext)
        {
            var project = FindParent<Project>(node);
            var target = FindParent<Target>(node);
            var task = FindParent<Microsoft.Build.Logging.StructuredLogger.Task>(node);

            projectId = project?.Id;
            targetId = target?.Id;
            taskId = task?.Id;
        }

        return new SearchResultInfo(
            nodeType: nodeType,
            text: text,
            nodeId: nodeId,
            duration: options.includeDuration ? searchResult.Duration : null,
            startTime: options.includeStartTime ? searchResult.StartTime : null,
            endTime: options.includeEndTime ? searchResult.EndTime : null,
            matchedFields: matchedFields,
            matchedByType: searchResult.MatchedByType,
            projectId: projectId,
            targetId: targetId,
            taskId: taskId);
    }

    private static int? GetNodeId(BaseNode? node)
    {
        return node switch
        {
            Project p => p.Id,
            Target t => t.Id,
            Microsoft.Build.Logging.StructuredLogger.Task task => task.Id,
            _ => null
        };
    }

    private static T? FindParent<T>(BaseNode? node) where T : BaseNode
    {
        while (node != null)
        {
            if (node is T typed)
                return typed;
            node = node.Parent;
        }
        return null;
    }
}
