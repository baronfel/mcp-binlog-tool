# Binlog MCP Server

This is a simple demo of a Model Context Protocol Server (MCP) that exposes tools and prompts for analyzing MSBuild binlogs to any MCP server.

To configure this:

1. build the repo with `dotnet build` in the `msbuild.mcp` directory
2. configure [Claude](#claude) or [VSCode](#vscode) to use the server
3. launch your server app and have fun!

To locally debug, use npx to run the Model Context Protocol inspector::

```bash
npx @modelcontextprotocol/inspector ./bin/Debug/net9.0/msbuild.mcp
```

### Claude
```json
{
  "mcpServers": {
    "msbuild": {
      "command": "<your repo root>\\binlog.mcp\\bin\\Debug\\net9.0\\binlog.mcp.exe"
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
    "mcp": {
        "inputs": [],
        "servers": {
            "msbuild": {
                "command": "<repo root>\\binlog.mcp\\bin\\Debug\\net9.0\\binlog.mcp.exe",
                "args": [],
                "env": {}
            }
        }
    }
}
```