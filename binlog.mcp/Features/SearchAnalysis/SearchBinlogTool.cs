using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;
using StructuredLogViewer;

namespace Binlog.MCP.Features.SearchAnalysis;

/// <summary>
/// MCP tool for performing freetext search within a binlog file.
/// </summary>
public class SearchBinlogTool
{
    [McpServerTool(Name = "search_binlog", Title = "Search Binlog", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("""
        Perform freetext search within a binlog file using the same search capabilities as the MSBuild Structured Log Viewer.

        Query Language Syntax:
        - Basic text search: Simply type words to find nodes containing that text
        - Exact match: Use quotes "exact phrase" for exact string matching
        - Multiple terms: Space-separated terms are AND'd together (all must match)

        Node Type Filtering:
        - $<type>: Filter by node type (e.g., $project, $target, $task, $csc, $rar)
        - Shortcuts: $csc expands to "$task csc", $rar expands to "$task ResolveAssemblyReference"

        Property and Field Matching:
        - name=<value>: Match nodes where the name field equals the value
        - value=<value>: Match nodes where the value field equals the value

        Hierarchical Search:
        - under(<query>): Find nodes under/within nodes matching the nested query
        - notunder(<query>): Exclude nodes under/within nodes matching the nested query
        - project(<query>): Find nodes within projects matching the nested query
        - not(<query>): Exclude nodes matching the nested query

        Time-based Filtering:
        - start<"YYYY-MM-DD HH:mm:ss": Nodes that started before the specified time
        - start>"YYYY-MM-DD HH:mm:ss": Nodes that started after the specified time
        - end<"YYYY-MM-DD HH:mm:ss": Nodes that ended before the specified time
        - end>"YYYY-MM-DD HH:mm:ss": Nodes that ended after the specified time

        Special Properties:
        - skipped=true/false: For targets, filter by whether they were skipped
        - height=<number> or height=max: Filter by tree height/depth

        Node Index Search:
        - $<number>: Find node by its unique index (e.g., $123)

        Result Enhancement:
        - $time or $duration: Include timing information in results
        - $start or $starttime: Include start time in results
        - $end or $endtime: Include end time in results

        Examples:
        - "error CS1234": Find exact error message
        - $task Copy: Find all Copy tasks
        - under($project MyProject): Find all nodes under MyProject
        - $target Build start>"2023-01-01 09:00:00": Build targets that started after 9 AM
        - name=Configuration value=Debug: Find nodes named Configuration with Debug value
        """)]
    public static SearchAnalysisResult SearchBinlog(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The search query to execute. See tool description for complete syntax documentation including node type filtering ($task, $project), hierarchical search (under, notunder), time constraints, property matching (name=, value=), and more.")] string query,
        [Description("Maximum number of search results to return (default: 300)")] int maxResults = 300,
        [Description("Whether to include duration information for timed nodes (default: true)")] bool includeDuration = true,
        [Description("Whether to include start time information for timed nodes (default: false)")] bool includeStartTime = false,
        [Description("Whether to include end time information for timed nodes (default: false)")] bool includeEndTime = false,
        [Description("Whether to include context information like project, target, task IDs (default: true)")] bool includeContext = true,
        CancellationToken cancellationToken = default)
    {
        var binlog = new BinlogPath(binlog_file);

        if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null)
        {
            return new SearchAnalysisResult(
                query: query,
                results: [],
                totalResults: 0,
                wasTruncated: false);
        }

        // Create search options
        var searchOptions = new SearchOptions(
            maxResults: maxResults,
            includeDuration: includeDuration,
            includeStartTime: includeStartTime,
            includeEndTime: includeEndTime,
            includeContext: includeContext);

        // Prepare for search
        var roots = new List<TreeNode> { build };
        var stringTable = build.StringTable.Instances ?? [];

        // Create the search instance
        var search = new Search(roots, stringTable, maxResults, markResultsInTree: false);

        // Perform the search
        var searchResults = search.FindNodes(query, cancellationToken);

        // Convert to our result format
        var resultInfos = searchResults
            .Select(sr => SearchResultUtil.CreateSearchResultInfo(sr, searchOptions))
            .ToArray();

        var wasTruncated = resultInfos.Length >= maxResults;

        return new SearchAnalysisResult(
            query: query,
            results: resultInfos,
            totalResults: resultInfos.Length,
            wasTruncated: wasTruncated);
    }
}
