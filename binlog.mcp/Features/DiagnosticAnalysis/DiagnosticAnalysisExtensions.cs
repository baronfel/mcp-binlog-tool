using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the DiagnosticAnalysis feature.
/// </summary>
public static class DiagnosticAnalysisExtensions
{
    /// <summary>
    /// Registers all tools for analyzing MSBuild diagnostics (errors, warnings).
    /// </summary>
    public static IMcpServerBuilder AddDiagnosticAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithTools<Features.DiagnosticAnalysis.GetDiagnosticsTool>(BinlogJsonOptions.Options);
        return builder;
    }
}
