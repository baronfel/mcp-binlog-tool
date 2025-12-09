using Binlog.MCP.Features.BuildAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Binlog.MCP;

/// <summary>
/// Extension methods to register the BuildAnalysis feature.
/// </summary>
public static class BuildAnalysisExtensions
{
    /// <summary>
    /// Registers all prompts for high-level build analysis workflows.
    /// </summary>
    public static IMcpServerBuilder AddBuildAnalysis(this IMcpServerBuilder builder)
    {
        builder.WithPrompts<BinlogPrompts>(BinlogJsonOptions.Options);
        return builder;
    }
}
