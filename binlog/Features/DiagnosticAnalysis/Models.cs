using System.ComponentModel;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.DiagnosticAnalysis;

/// <summary>
/// Represents the severity of a diagnostic message.
/// </summary>
public enum DiagnosticSeverity
{
    Error,
    Warning
}

/// <summary>
/// Represents a diagnostic message (warning or error) from the build log.
/// </summary>
public record struct DiagnosticInfo(
    [Description("The diagnostic message text")] string message,
    [Description("The severity of the diagnostic")] DiagnosticSeverity severity,
    [Description("The diagnostic code if available")] string? code,
    [Description("The source file path if available")] string? file,
    [Description("The line number in the source file if available")] int? lineNumber,
    [Description("The column number in the source file if available")] int? columnNumber,
    [Description("The ID of the project where this diagnostic occurred")] int projectId,
    [Description("The ID of the target where this diagnostic occurred, if applicable")] int? targetId,
    [Description("The ID of the task where this diagnostic occurred, if applicable")] int? taskId);

/// <summary>
/// Filter options for diagnostic retrieval.
/// </summary>
public record struct DiagnosticFilter(
    [Description("Include error diagnostics")] bool includeErrors = true,
    [Description("Include warning diagnostics")] bool includeWarnings = true,
    [Description("Include detailed diagnostic information (file, line, column, etc.)")] bool includeDetails = true,
    [Description("Filter by specific project IDs")] int[]? projectIds = null,
    [Description("Filter by specific target IDs")] int[]? targetIds = null,
    [Description("Filter by specific task IDs")] int[]? taskIds = null,
    [Description("Maximum number of diagnostics to return")] int? maxResults = null);

/// <summary>
/// Result container for diagnostic analysis.
/// </summary>
public record struct DiagnosticAnalysisResult(
    [Description("Array of diagnostic messages. Null if details were not included.")] DiagnosticInfo[]? diagnostics,
    [Description("Total number of errors found")] int errorCount,
    [Description("Total number of warnings found")] int warningCount,
    [Description("Whether the results were truncated due to maxResults limit")] bool wasTruncated);

public static class DiagnosticUtil
{

    /// <summary>
    /// Creates a DiagnosticInfo from a StructuredLogger AbstractDiagnostic with optional detail inclusion.
    /// </summary>
    public static DiagnosticInfo CreateDiagnosticInfo(AbstractDiagnostic diagnostic)
    {
        // Walk up the tree to find project, target, and task context
        var project = FindParent<Project>(diagnostic);
        var target = FindParent<Target>(diagnostic);
        var task = FindParent<Microsoft.Build.Logging.StructuredLogger.Task>(diagnostic);

        DiagnosticSeverity severity = diagnostic switch
        {
            Error => DiagnosticSeverity.Error,
            Warning => DiagnosticSeverity.Warning,
            _ => throw new NotImplementedException()
        };

        return new DiagnosticInfo(
            message: diagnostic.Text,
            severity: severity,
            code: diagnostic.Code,
            file: diagnostic.File,
            lineNumber: diagnostic.LineNumber != 0 ? diagnostic.LineNumber : null,
            columnNumber: diagnostic.ColumnNumber != 0 ? diagnostic.ColumnNumber : null,
            projectId: project?.Id ?? 0,
            targetId: target?.Id,
            taskId: task?.Id);
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

    /// <summary>
    /// Checks if an AbstractDiagnostic matches the filter criteria without creating a full DiagnosticInfo.
    /// </summary>
    public static bool MatchesFilter(AbstractDiagnostic diagnostic, DiagnosticFilter filter)
    {
        // Check severity filter
        var severity = diagnostic switch
        {
            Error => DiagnosticSeverity.Error,
            Warning => DiagnosticSeverity.Warning,
            _ => throw new NotImplementedException()
        };

        if (!filter.includeErrors && severity == DiagnosticSeverity.Error) return false;
        if (!filter.includeWarnings && severity == DiagnosticSeverity.Warning) return false;

        // Check project ID filter
        if (filter.projectIds != null && filter.projectIds.Length > 0)
        {
            var project = FindParent<Project>(diagnostic);
            var projectId = project?.Id ?? 0;
            if (!filter.projectIds.Contains(projectId)) return false;
        }

        // Check target ID filter
        if (filter.targetIds != null && filter.targetIds.Length > 0)
        {
            var target = FindParent<Target>(diagnostic);
            if (target == null || !filter.targetIds.Contains(target.Id)) return false;
        }

        // Check task ID filter
        if (filter.taskIds != null && filter.taskIds.Length > 0)
        {
            var task = FindParent<Microsoft.Build.Logging.StructuredLogger.Task>(diagnostic);
            if (task == null || !filter.taskIds.Contains(task.Id)) return false;
        }

        return true;
    }
}
