using Binlog.MCP.Cli;
using System.CommandLine;

// All invocations go through the CLI root command.
// The "mcp" subcommand starts the MCP server; no-args shows help.
CliRunner.RegisterCallbacks();
var rootCommand = CliCommands.BuildRootCommand();
return await rootCommand.InvokeAsync(args);
