using System.ComponentModel;
using ModelContextProtocol;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Concurrent;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using StructuredLogger;
using Microsoft.VisualBasic;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[McpServerToolType]
public class BinlogTool
{
    private static Build? build;
    private static Lock buildLock = new Lock();

    [McpServerTool(Name = "load_binlog")]
    [Description("Load a binary log file")]
    public static void Load(string path)
    {
        if (build != null) return;
        lock (buildLock)
        {
            build = BinaryLog.ReadBuild(path);
        }
    }

    [McpServerTool(Name = "list_targets"), Description("List all targets called for each project and their times in the loaded binary log file")]
    public static List<string> ListTargets()
    {
        if (build == null) return new List<string>();
        return build.FindChildrenRecursive<Microsoft.Build.Logging.StructuredLogger.Target>().Select(t => $"{t.Name} ({t.Duration.Milliseconds} ms) for project {Path.GetFileName(t.Project?.ProjectFile)} with id {t.Project?.Id}").ToList();
    }

    [McpServerTool(Name = "list_projects"), Description("List all projects in the loaded binary log file")]
    public static List<string> ListProjects()
    {
        if (build == null) return new List<string>();
        return build.FindChildrenRecursive<Project>().Select(t => $"{t.ProjectFile} with id {t.Id} with properties {CreateProperties(t.GlobalProperties)}").ToList();
    }

    private static string CreateProperties(IDictionary<string, string> properties)
    {
        if (properties == null) return string.Empty;
        var result = new List<string>();
        foreach (var property in properties)
        {
            result.Add($"{property.Key}={property.Value}");
        }

        return string.Join(", ", result);
    }
}

public static class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Verbose() // Capture all log levels
           .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "TestServer_.log"),
               rollingInterval: RollingInterval.Day,
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .WriteTo.Debug()
           .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
           .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<BinlogTool>();
        await builder.Build().RunAsync();
    }
}