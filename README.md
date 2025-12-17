# Binlog MCP Server

This is a simple demo of a Model Context Protocol Server (MCP) that exposes tools and prompts for analyzing MSBuild binlogs to any MCP server.

## Features

The server provides comprehensive tools for analyzing MSBuild binary logs, including:

- **Target Analysis**: Identify expensive targets, search for specific targets across projects, and analyze target execution times
- **Project Analysis**: Calculate project build times, find the most expensive projects, and analyze all targets in a project at once
- **Evaluation Analysis**: List project evaluations, inspect global properties, and identify potential overbuilding issues
- **File Access**: List and retrieve source files embedded in binary logs
- **Performance**: Intelligent caching ensures fast queries even on large binlogs

See [PACKAGE_README.md](binlog.mcp/PACKAGE_README.md) for detailed tool documentation.

## Setup

To configure this:

1. build the repo with `dotnet build` in the solution root directory
2. configure [Claude](#claude) or [VSCode](#vscode) to use the server
3. launch your server app and have fun!

To locally debug, use npx to run the Model Context Protocol inspector::

```bash
npx @modelcontextprotocol/inspector ./artifacts/bin/binlog.mcp/debug/binlog.mcp.exe
```

### Claude
```json
{
  "mcpServers": {
    "msbuild": {
      "command": "<your repo root>\\artifacts\\bin\\binlog.mcp\\debug\\binlog.mcp.exe"
    }
  }
}
```

### VSCode

If you have Claude configured already, you can tell VSCode to use the same settings by adding the following to your `settings.json`:

```json
  "chat.mcp.discovery.enabled": true,
```

otherwise, you can configure the server directly:

```json
{
    "servers": {
        "binlog-mcp": {
			      "type": "stdio",
            "command": "<repo root>\\artifacts\\bin\\binlog.mcp\\debug\\binlog.mcp.exe",
            "args": []
        }
    }
}
```