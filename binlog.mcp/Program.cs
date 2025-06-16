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

    [McpServerTool(Name = "get_expensive_targets"), Description("Get the N most expensive targets in the loaded binary log file")]
    public static List<string> GetExpensiveTargets(int top_number)
    {
        if (build == null) return new List<string>();
        // the same target can be executed multiple times, so we need to group by name and sum the durations.
        // we can't use LINQ's GroupBy because the set of targets in the binlog could be huge, so we will use a dictionary to group them.
        // we should also track the _number_ of times each target was executed.
        var targetDurations = new Dictionary<string, TimeSpan>();
        var targetExecutions = new Dictionary<string, int>();
        foreach (var target in build.FindChildrenRecursive<Target>())
        {
            if (target == null || target.Duration == TimeSpan.Zero) continue;
            if (targetDurations.TryGetValue(target.Name, out var existingDuration))
            {
                targetDurations[target.Name] = existingDuration + target.Duration;
            }
            else
            {
                targetDurations[target.Name] = target.Duration;
            }
            if (targetExecutions.TryGetValue(target.Name, out var existingCount))
            {
                targetExecutions[target.Name] = existingCount + 1;
            }
            else
            {
                targetExecutions[target.Name] = 1;
            }
        }

        // Get the top N most expensive targets
        var expensiveTargets = targetDurations.OrderByDescending(kvp => kvp.Value).Take(top_number);
        return expensiveTargets.Select(kvp => $"{kvp.Key} was called {targetExecutions[kvp.Key]} times ({kvp.Value.Milliseconds} ms)").ToList();
    }

    [McpServerTool(Name = "list_projects"), Description("List all projects in the loaded binary log file")]
    public static List<string> ListProjects()
    {
        if (build == null) return new List<string>();
        return build.FindChildrenRecursive<Project>().Select(t => $"{t.ProjectFile} Id={t.Id}").ToList();
    }

    [McpServerTool(Name = "list_evaluations"), Description("List all evaluations for a specific project in the loaded binary log file. You can use the `list_projects` command to find the project file paths.")]
    public static List<string> GetEvaluationsForProject(string projectFilePath)
    {
        if (build == null) return new List<string>();
        return build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>()
            .Where(e => e.ProjectFile.Equals(projectFilePath, StringComparison.OrdinalIgnoreCase))
            .Select(e => $"{e.Id} - {e.ProjectFile} ({e.Duration.TotalMilliseconds}ms)")
            .ToList();
    }

    [McpServerTool(Name = "get_evaluation_global_properties"), Description("Get the global properties for a specific evaluation in the loaded binary log file. You can use the `list_evaluations` command to find the evaluation IDs. Global properties are what make evaluations distinct from one another within the same project.")]
    public static List<string> GetGlobalPropertiesForEvaluation(int evaluationId)
    {
        if (build == null) return new List<string>();
        var globalProperties = build.EvaluationFolder.FindChildrenRecursive<ProjectEvaluation>()
            .FirstOrDefault(e => e.Id == evaluationId)
            ?.FindChild<Folder>("Properties")
            ?.FindChild<Folder>("Global");
        if (globalProperties == null) return new List<string>();
        return globalProperties.Children.OfType<Property>()
            .Select(p => $"{p.Name} = {p.Value}")
            .ToList();
    }

    [McpServerPrompt(Name = "profile_build"), Description("Perform a build of the current workspace and profile it using the binary logger.")]
    public static IEnumerable<ChatMessage> Thing() => [
        new ChatMessage(ChatRole.User, """
            Please perform a build of the current workspace using dotnet build with the binary logger enabled. 
            You can use the `--binaryLogger` option to specify the log file name. For example: `dotnet build --binaryLogger:binlog.binlog`. 
            Create a binlog file using a name that is randomly generated, then remember it for later use.
            """),
        new ChatMessage(ChatRole.Assistant, """
            Once the build is complete, you can use the `load_binlog` command to load the newly-generated binary log file.
            Then use `get_expensive_targets` to list the most expensive targets,
            and check how many evaluations a project has using the `list_evaluations` command with the project file path.
            Multiple evaluations can sometimes be a cause of overbuilding, so it's worth checking.
            """),
        new ChatMessage(ChatRole.User, """
            Now that you have a binlog, show me the top 5 targets that took the longest time to execute in the build. 
            Also, note if any projects had multiple evaluations. You can check evaluations using the `list_evaluations` command with the project file path.
            """),
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