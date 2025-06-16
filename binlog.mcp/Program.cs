using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Serilog;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.AI;

namespace Binlog.MCP;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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

    [McpServerPrompt(Name = "profile_build"), Description("Perform a build of the current workspace and profile it using the binary logger.")]
    public static IEnumerable<ChatMessage> Thing() => [
        new ChatMessage(ChatRole.User, "Please perform a build of the current workspace using dotnet build with the binary logger enabled. You can use the `--binaryLogger` option to specify the log file name. For example: dotnet build `--binaryLogger:binlog.binlog`. Create a binlog file using a name that is randomly generated, then remember it for later use."),
        new ChatMessage(ChatRole.Assistant, "Once the build is complete, you can use the `load_binlog` command to load the newly-generated binary log file and then use `list_targets` or `list_projects` to see the results."),
        new ChatMessage(ChatRole.User, "Now that you have a binlog, show me the top 5 targets that took the longest time to execute in the build."),
    ];
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
            .WithTools<BinlogTool>()
            .WithPrompts<BinlogTool>();
        await builder.Build().RunAsync();
    }
}