# Claude Development Guide for mcp-binlog-tool

This document provides guidance and expectations for AI assistants (Claude, Copilot, etc.) working on the mcp-binlog-tool project. It captures the established patterns, standards, and architectural decisions from the project's development.

## Project Overview

This is a Model Context Protocol (MCP) server for analyzing MSBuild binary log files (.binlog). The server provides comprehensive tools for build performance analysis, diagnostic extraction, search capabilities, and build introspection.

## Architecture & Organization

### Feature-Based Structure
- Features are organized in separate directories under `binlog.mcp/Features/`
- Each feature contains:
  - `Models.cs` - Data models and utility methods
  - `*Extensions.cs` - Service registration and DI setup
  - `*Tool.cs` - MCP tool implementations
- Features communicate through shared infrastructure in `Infrastructure/`

### Current Feature Areas
- **AnalyzerAnalysis** - Roslyn analyzer performance analysis
- **BinlogLoading** - Core binlog loading and caching
- **BuildAnalysis** - Build-level analysis and prompts
- **DiagnosticAnalysis** - Error and warning extraction
- **EvaluationAnalysis** - Project evaluation analysis
- **ProjectAnalysis** - Project-level performance analysis
- **SearchAnalysis** - Powerful freetext search capabilities
- **TargetAnalysis** - Target execution analysis
- **TaskAnalysis** - Task-level analysis and timing
- **TimelineAnalysis** - Build timeline and parallelization analysis

## Coding Standards

### Data Models
- **Use record structs** for data transfer objects and API models
- **Use enums** for classifications (e.g., `DiagnosticSeverity.Error`)
- **Use nullable reference types** consistently
- **Prefer ID-based references** over string names to reduce data duplication
- **Include comprehensive Description attributes** for all public properties

### Tool Implementation
- **McpServerTool attributes** must include:
  - `Name` - kebab-case tool name
  - `Title` - Human-readable title
  - `Idempotent = true` - All our tools are read-only
  - `UseStructuredContent = true` - For performance
  - `ReadOnly = true` - All tools are read-only
- **Parameter descriptions** must be comprehensive and include:
  - Data types and constraints
  - Default values where applicable
  - Optional parameter indicators
  - Usage examples for complex parameters
- **Support filtering and pagination** where appropriate:
  - `maxResults` parameters for large result sets
  - Project/target/task ID filtering arrays
  - Boolean flags for including/excluding data
  - Optional detail levels to control response size

### Performance Patterns
- **Single-pass processing** - Process data in one iteration when possible
- **Caching** - Use static caches for expensive computations (see ProjectBuildTimeCache)
- **Lazy loading** - Only load data when needed
- **Efficient data structures** - Use dictionaries and arrays, avoid nested loops
- **Optional detail exclusion** - Provide flags to exclude expensive-to-serialize data

### MSBuild Integration
- **Use proper node types** from MSBuildStructuredLog:
  - `AbstractDiagnostic` for diagnostics (not message content inference)
  - `Project`, `Target`, `Task` for structural nodes
  - `Build.StringTable.Instances` for search operations
- **Leverage existing APIs** instead of manual parsing
- **Handle nullable properties** from MSBuild node types properly

## JSON Serialization

### Context Management
- **Register all new types** in `BinlogJsonContext.cs`
- **Use JsonSerializable attributes** for source generation
- **Follow naming conventions** for property serialization
- **Handle nullable types** appropriately in JSON contracts

## Documentation Standards

### Tool Documentation (PACKAGE_README.md)
- **Organize by feature area** with clear section headers
- **Alphabetical ordering** within each feature area
- **Comprehensive parameter documentation** including:
  - Parameter name, type, and description
  - Default values and optional indicators
  - Array types and filtering capabilities
- **Return type documentation** with structure details
- **Include use cases and examples** for complex features
- **Cross-reference related tools** where appropriate

### Query Language Documentation
- **Provide comprehensive syntax documentation** (see `search_binlog` tool)
- **Include practical examples** with real-world scenarios
- **Document shortcuts and advanced features**
- **Explain hierarchical and filtering capabilities**

### Release Documentation
- **CHANGELOG.md updates** must include:
  - Version number and release date
  - Detailed feature descriptions under "Added" section
  - Performance improvements under "Changed" section
  - Breaking changes clearly marked
- **README.md updates** for major feature additions
- **Version synchronization** across all files (server.json, CHANGELOG.md)

## API Design Principles

### Consistency
- **Consistent parameter naming** across similar tools
- **Consistent return value structures** for similar operations
- **Consistent error handling** patterns
- **Consistent filtering capabilities** where applicable

### Usability
- **Sensible defaults** for all optional parameters
- **Clear success/failure indicators** in return values
- **Helpful error messages** with actionable guidance
- **Progressive disclosure** - simple usage with advanced options available

### Performance
- **Minimize round trips** - provide bulk operations where useful
- **Cache expensive operations** - especially cross-project analysis
- **Provide filtering at source** - don't return unused data
- **Support result pagination** for large datasets

## Testing & Validation

### Tool Validation
- **Verify all tools compile** after changes
- **Test with real binlog files** when possible
- **Validate JSON serialization** of complex return types
- **Check parameter validation** and error handling

### Documentation Validation
- **Ensure all tools are documented** in PACKAGE_README.md
- **Verify parameter descriptions match implementation**
- **Check examples work with actual tool signatures**
- **Validate cross-references and links**

## Common Patterns

### Binlog Loading
```csharp
var binlog = new BinlogPath(binlog_file);
if (!BinlogLoader.TryGetBuild(binlog, out var build) || build == null)
{
    return new FailureResult("Failed to load binlog");
}
```

### Single-Pass Filtering
```csharp
var filteredResults = build.FindChildrenRecursive<NodeType>()
    .Where(node => filter.Matches(node))
    .Take(maxResults ?? int.MaxValue)
    .Select(node => ConvertToModel(node, options))
    .ToArray();
```

### Optional Detail Inclusion
```csharp
return new ResultType(
    basicData: always_included,
    detailData: includeDetails ? expensive_data : null,
    timingData: includeTiming ? timing_data : null
);
```

## Release Process

### Version Updates
1. **Increment version** in server.json (both locations)
2. **Add CHANGELOG.md entry** with comprehensive feature descriptions
3. **Update README.md** if major features added
4. **Update PACKAGE_README.md** with new tool documentation
5. **Verify all version references** are synchronized

### Quality Gates
- **All tools compile successfully**
- **Documentation is complete and accurate**
- **Examples work as documented**
- **Performance is acceptable** for typical binlog sizes
- **Error handling is comprehensive**

## Future Considerations

### Extensibility
- **Design for additional analysis types** - follow established feature patterns
- **Consider caching strategies** for new expensive operations
- **Plan for additional MSBuild node types** as they become available
- **Design APIs for future filtering capabilities**

### Performance
- **Monitor memory usage** with large binlog files
- **Consider streaming approaches** for very large result sets
- **Plan for distributed analysis** if needed
- **Optimize hot paths** in frequently used tools

### User Experience
- **Consider interactive workflows** with multi-step analysis
- **Plan for visualization support** in future versions
- **Design for integration** with other development tools
- **Consider batch processing capabilities** for multiple binlogs

## Anti-Patterns to Avoid

- **Don't infer data from string content** - use proper MSBuild node types
- **Don't duplicate string data** - use IDs and references
- **Don't skip performance optimization** - large binlogs require efficiency
- **Don't skip comprehensive documentation** - tools must be self-explanatory
- **Don't break existing APIs** without version increments
- **Don't forget JSON serialization registration** for new types
- **Don't use blocking operations** without cancellation token support

This guide should be referenced when making changes to ensure consistency with established patterns and maintain the high quality standards of the project.
