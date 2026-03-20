using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.DiagnosticAnalysis;

/// <summary>
/// MCP tool for extracting diagnostic information (errors, warnings) from a binlog file.
/// </summary>
public class GetDiagnosticsTool
{
    [McpServerTool(Name = "get_diagnostics", Title = "Get Build Diagnostics", UseStructuredContent = true, ReadOnly = true)]
    [Description("Extract diagnostic information (errors, warnings) from a binlog file with optional filtering.")]
    public static DiagnosticAnalysisResult GetDiagnostics(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("Include error diagnostics (default: true)")] bool includeErrors = true,
        [Description("Include warning diagnostics (default: true)")] bool includeWarnings = true,
        [Description("Include detailed diagnostic information like file paths, line numbers, etc. (default: true)")] bool includeDetails = true,
        [Description("Filter by specific project IDs (optional)")] int[]? projectIds = null,
        [Description("Filter by specific target IDs (optional)")] int[]? targetIds = null,
        [Description("Filter by specific task IDs (optional)")] int[]? taskIds = null,
        [Description("Maximum number of diagnostics to return (optional)")] int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        var binlog = new BinlogPath(binlog_file);

        if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null)
        {
            return new DiagnosticAnalysisResult(
                diagnostics: [],
                errorCount: 0,
                warningCount: 0,
                wasTruncated: false);
        }

        // Create filter
        var filter = new DiagnosticFilter(
            includeErrors: includeErrors,
            includeWarnings: includeWarnings,

            includeDetails: includeDetails,
            projectIds: projectIds,
            targetIds: targetIds,
            taskIds: taskIds,
            maxResults: maxResults);

        var filteredDiagnostics = includeDetails ? new List<DiagnosticInfo>() : null;
        var totalErrorCount = 0;
        var totalWarningCount = 0;
        var currentIndex = 0;
        var wasTruncated = false;

        // Single pass: filter and convert diagnostics
        foreach (var diagnostic in build.FindChildrenRecursive<AbstractDiagnostic>())
        {
            currentIndex++;
            if (currentIndex % 100 == 0) // check for cancellation every 100 items
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            // Count totals regardless of filtering
            switch (diagnostic)
            {
                case Error:
                    totalErrorCount++;
                    break;
                case Warning:
                    totalWarningCount++;
                    break;
            }

            // Check if this diagnostic matches the filter criteria
            if (DiagnosticUtil.MatchesFilter(diagnostic, filter))
            {
                // Check max results limit
                if (maxResults.HasValue && (totalErrorCount + totalWarningCount) >= maxResults.Value)
                {
                    wasTruncated = true;
                    break;
                }

                if (includeDetails)
                {
                    var diagnosticInfo = DiagnosticUtil.CreateDiagnosticInfo(diagnostic);
                    filteredDiagnostics!.Add(diagnosticInfo);
                }
            }
        }

        return new DiagnosticAnalysisResult(
            diagnostics: filteredDiagnostics?.ToArray(),
            errorCount: totalErrorCount,
            warningCount: totalWarningCount,
            wasTruncated: wasTruncated);
    }
}
