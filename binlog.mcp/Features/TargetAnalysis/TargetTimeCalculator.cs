using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.TargetAnalysis;

/// <summary>
/// Shared logic for calculating target execution times.
/// </summary>
public static class TargetTimeCalculator
{
    /// <summary>
    /// Calculate exclusive duration for a target by subtracting MSBuild task durations that invoke
    /// ProjectReference Protocol targets. Per the ProjectReference Protocol, only MSBuild task calls
    /// to specific protocol targets (GetTargetFrameworks, GetTargetFrameworkProperties, GetTargetPath,
    /// GetNativeManifest, GetCopyToOutputDirectoryItems, Clean, etc.) are considered "inclusive" time.
    /// All other MSBuild task usage is considered exclusive work by the parent target.
    /// See: https://github.com/dotnet/msbuild/blob/main/documentation/ProjectReference-Protocol.md
    /// </summary>
    public static TimeSpan CalculateExclusiveDuration(Target target)
    {
        // ProjectReference Protocol targets that represent cross-project calls
        var projectReferenceProtocolTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GetTargetFrameworks",
            "GetTargetFrameworksWithPlatformForSingleTargetFramework",
            "GetTargetFrameworkProperties",
            "GetTargetPath",
            "GetNativeManifest",
            "GetCopyToOutputDirectoryItems",
            "_GetCopyToOutputDirectoryItemsFromTransitiveProjectReferences",
            "Clean",
            "CleanReferencedProjects",
            "GetPackagingOutputs",
            // Default target is typically "Build" but can vary
            "Build"
        };

        // Find MSBuild tasks that invoke ProjectReference Protocol targets
        var projectReferenceCallsDuration = target.Children
            .OfType<Microsoft.Build.Logging.StructuredLogger.Task>()
            .Where(task =>
            {
                if (!string.Equals(task.Name, "MSBuild", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Check if this MSBuild task is calling a ProjectReference Protocol target
                // Look for target names in the task's parameters (stored as Property children with Name="Targets")
                var targetsParam = task.Children.OfType<Property>()
                    .FirstOrDefault(p => string.Equals(p.Name, "Targets", StringComparison.OrdinalIgnoreCase));

                if (targetsParam != null && !string.IsNullOrEmpty(targetsParam.Value))
                {
                    // Parse semicolon-separated target list
                    var targetsCalled = targetsParam.Value
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim());

                    return targetsCalled.Any(t => projectReferenceProtocolTargets.Contains(t));
                }

                // If no explicit targets specified, it calls the default target (usually Build)
                // which is part of the ProjectReference Protocol
                return true;
            })
            .Aggregate(TimeSpan.Zero, (counter, task) => counter + task.Duration);

        return projectReferenceCallsDuration != TimeSpan.Zero
            ? target.Duration - projectReferenceCallsDuration
            : target.Duration;
    }
}
