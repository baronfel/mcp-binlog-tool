using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Binlog.MCP;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Debug()
    .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();

// Register all feature vertical slices using extension methods
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .AddBinlogLoading()
    .AddTargetAnalysis()
    .AddProjectAnalysis()
    .AddEvaluationAnalysis()
    .AddBuildAnalysis()
    .AddTimelineAnalysis();

await builder.Build().RunAsync();
