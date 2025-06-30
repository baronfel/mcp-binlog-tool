# Baronfel.Binlog.MCP

This package provides a tool for reading and analyzing Microsoft Build Engine (MSBuild) binary log files (.binlog). It is designed to work with the Model Context Protocol (MCP) to facilitate structured logging and analysis of build processes.

### Configuration

#### VSCode

```json
{
	"mcp": {
		"servers": {
			"msbuild": {
				"command": "dnx",
				"args": [ "baronfel.binlog.mcp", "-y" ]
			}
		}
	}
}
```

