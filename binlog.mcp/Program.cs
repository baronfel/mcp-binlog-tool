using Binlog.MCP;
using Binlog.MCP.Features.AnalyzerAnalysis;
using Binlog.MCP.Features.BinlogLoading;
using Binlog.MCP.Features.BuildAnalysis;
using Binlog.MCP.Features.DiagnosticAnalysis;
using Binlog.MCP.Features.EvaluationAnalysis;
using Binlog.MCP.Features.ProjectAnalysis;
using Binlog.MCP.Features.SearchAnalysis;
using Binlog.MCP.Features.TargetAnalysis;
using Binlog.MCP.Features.TaskAnalysis;
using Binlog.MCP.Features.TimelineAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
