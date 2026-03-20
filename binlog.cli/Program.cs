using Binlog.MCP.Cli;
using System.CommandLine;

CliRunner.RegisterCallbacks();
var rootCommand = CliCommands.BuildRootCommand();
return await rootCommand.InvokeAsync(args);
