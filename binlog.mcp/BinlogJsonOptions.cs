using System.Text.Json;
using ModelContextProtocol;

namespace Binlog.MCP;

/// <summary>
/// Provides JSON serialization options configured for the binlog MCP server.
/// Chains the custom binlog types with the MCP library's default JsonTypeInfo.
/// </summary>
internal static class BinlogJsonOptions
{
    /// <summary>
    /// Gets the JSON serializer options that chain our custom types with the MCP library's default types.
    /// This is required for structured outputs to work correctly with our custom response types.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        // Start with the MCP library's default options which include MCP protocol types
        var options = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
        
        // Add our custom binlog types to the type info resolver chain
        // This allows both MCP protocol types and our custom types to be serialized
        options.TypeInfoResolverChain.Insert(0, BinlogJsonContext.Default);
        
        options.MakeReadOnly();
        return options;
    }
}
