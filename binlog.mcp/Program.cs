using Binlog.MCP;
using Binlog.MCP.Cli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;

// "mcp" subcommand (or no args for backward compat with MCP hosts) → run the MCP server.
if (args.Length == 0 || args[0] == "mcp")
{
    await RunMcpServer();
    return 0;
}

// All other invocations → CLI mode.
CliRunner.RegisterCallbacks();
var rootCommand = CliCommands.BuildRootCommand();
return await rootCommand.InvokeAsync(args);

static async Task RunMcpServer()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Debug()
        .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
        .CreateLogger();

    var builder = Host.CreateApplicationBuilder();
    builder.Services.AddSerilog();

    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .AddBinlogLoading()
        .AddTargetAnalysis()
        .AddTaskAnalysis()
        .AddAnalyzerAnalysis()
        .AddProjectAnalysis()
        .AddEvaluationAnalysis()
        .AddBuildAnalysis()
        .AddTimelineAnalysis()
        .AddDiagnosticAnalysis()
        .AddSearchAnalysis();

    await builder.Build().RunAsync();
}
