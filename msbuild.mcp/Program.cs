using System.ComponentModel;
using McpDotNet;
using McpDotNet.Server;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MSBuild.MCP;

[McpToolType]
public class MSBuildTool
{
    public struct ProjectKey(string path, (string, string)[]? properties);

    public Dictionary<ProjectKey, Project> loadedProjects = new();
    public ProjectCollection projectCollection = new();

    [McpTool("list-target-frameworks"), Description("Returns the target frameworks of a project")]
    /// <param name="projectPath">The path to the project file to read</param>
    public string[] ListTargetFrameworks(string projectPath)
    {
        var project = TryLoadProject(projectPath);
        var tfms = project.GetProperty("TargetFrameworks")?.EvaluatedValue.Split(';');
        var tf = project.GetProperty("TargetFramework")?.EvaluatedValue;
        return tfms ?? (tf is not null ? new[] { tf } : Array.Empty<string>());
    }

    [McpTool("list-project-dependencies"), Description("Returns the project dependencies of a project")]
    /// <param name="projectPath">The path to the project file to read</param>
    public string[] ListProjectDependencies(string projectPath)
    {
        var project = TryLoadProject(projectPath);
        return project.GetItems("ProjectReference").Select(i => i.EvaluatedInclude).ToArray();
    }

    Project TryLoadProject(string projectPath)
    {
        var key = new ProjectKey(projectPath, null);
        if (loadedProjects.TryGetValue(key, out var project))
        {
            return project;
        }

        project = projectCollection!.LoadProject(Path.IsPathFullyQualified(projectPath) ? projectPath : Path.Combine(Environment.CurrentDirectory, projectPath));
        loadedProjects[key] = project;
        return project;
    }
}

public static class Program
{
    static void RegisterMSBuild()
    {
        MSBuildLocator.RegisterDefaults();
    }

    static async Task Main(string[] args)
    {
        RegisterMSBuild();

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
            .WithTools();
        await builder.Build().RunAsync();
    }
}