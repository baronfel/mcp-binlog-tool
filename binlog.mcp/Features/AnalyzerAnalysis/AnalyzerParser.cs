using System.Text.RegularExpressions;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.AnalyzerAnalysis;

/// <summary>
/// Shared helper class for parsing Roslyn analyzer and source generator data from Csc task messages.
/// </summary>
internal static class AnalyzerParser
{
    // Regex patterns to detect analyzer and generator sections
    private static readonly Regex TotalAnalyzerExecutionTimeRegex = new Regex(@"Total analyzer execution time:\s+[\d\.]+\s+seconds", RegexOptions.Compiled);
    private static readonly Regex TotalGeneratorExecutionTimeRegex = new Regex(@"Total generator execution time:\s+[\d\.]+\s+seconds", RegexOptions.Compiled);

    /// <summary>
    /// Parse analyzer and generator data from Csc task messages.
    /// </summary>
    /// <param name="messages">The messages from a Csc task.</param>
    /// <param name="analyzerAssemblies">Output dictionary for analyzer assemblies.</param>
    /// <param name="generatorAssemblies">Output dictionary for generator assemblies.</param>
    public static void ParseAnalyzerData(
        IEnumerable<string> messages,
        Dictionary<string, AssemblyAnalyzerData> analyzerAssemblies,
        Dictionary<string, AssemblyAnalyzerData> generatorAssemblies)
    {
        Dictionary<string, AssemblyAnalyzerData>? currentSection = null;
        string? currentAssembly = null;
        var currentAnalyzers = new Dictionary<string, AnalyzerInfo>();

        foreach (var message in messages)
        {
            // Skip compiler server messages
            if (message.Contains("CompilerServer:"))
                continue;

            // Check if we're starting an analyzer section
            if (TotalAnalyzerExecutionTimeRegex.IsMatch(message))
            {
                currentSection = analyzerAssemblies;
                currentAssembly = null;
                continue;
            }

            // Check if we're starting a generator section
            if (TotalGeneratorExecutionTimeRegex.IsMatch(message))
            {
                currentSection = generatorAssemblies;
                currentAssembly = null;
                continue;
            }

            // If we're not in a section, skip
            if (currentSection == null)
                continue;

            // Check if this is an assembly line (contains version info)
            if (message.Contains(", Version="))
            {
                // Save previous assembly if any
                if (currentAssembly != null && currentAnalyzers.Count > 0)
                {
                    SaveAssembly(currentSection, currentAssembly, currentAnalyzers);
                }

                // Parse the assembly line
                var parsed = ParseLine(message);
                currentAssembly = parsed.name;
                currentAnalyzers = new Dictionary<string, AnalyzerInfo>();
                continue;
            }

            // This is an analyzer/generator line within the current assembly
            if (currentAssembly != null)
            {
                var parsed = ParseLine(message);
                if (parsed.durationMs > 0 || !string.IsNullOrWhiteSpace(parsed.name))
                {
                    currentAnalyzers[parsed.name] = new AnalyzerInfo(parsed.name, parsed.durationMs);
                }
            }
        }

        // Don't forget the last assembly
        if (currentAssembly != null && currentAnalyzers.Count > 0 && currentSection != null)
        {
            SaveAssembly(currentSection, currentAssembly, currentAnalyzers);
        }
    }

    /// <summary>
    /// Extract analyzer data from a specific Csc task.
    /// </summary>
    /// <param name="task">The Csc task to parse.</param>
    /// <returns>The parsed analyzer data.</returns>
    public static CscAnalyzerData? ParseCscTask(Microsoft.Build.Logging.StructuredLogger.Task task)
    {
        if (!string.Equals(task.Name, "Csc", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Extract messages from the task
        var messages = task.Children.OfType<Message>()
            .Select(m => m.Text);

        var analyzerAssemblies = new Dictionary<string, AssemblyAnalyzerData>();
        var generatorAssemblies = new Dictionary<string, AssemblyAnalyzerData>();

        ParseAnalyzerData(messages, analyzerAssemblies, generatorAssemblies);

        if (analyzerAssemblies.Count == 0 && generatorAssemblies.Count == 0)
        {
            return null;
        }
        return new CscAnalyzerData( analyzerAssemblies, generatorAssemblies);
    }

    private static void SaveAssembly(
        Dictionary<string, AssemblyAnalyzerData> section,
        string assemblyName,
        Dictionary<string, AnalyzerInfo> analyzers)
    {
        var totalMs = analyzers.Values.Sum(a => a.durationMs);
        section[assemblyName] = new AssemblyAnalyzerData(
            assemblyName,
            totalMs,
            new Dictionary<string, AnalyzerInfo>(analyzers));
    }

    private static (string name, long durationMs) ParseLine(string line)
    {
        // Lines are formatted with double-space separation: "  duration  percentage  name"
        var columns = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);

        if (columns.Length >= 3)
        {
            // First column is duration in seconds
            if (double.TryParse(columns[0].Trim(), out var seconds))
            {
                var name = columns[2].Trim();
                return (name, (long)(seconds * 1000));
            }
        }

        // Fallback: use entire line as name with zero duration
        return (line.Trim(), 0);
    }
}
