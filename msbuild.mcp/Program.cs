using System.ComponentModel;
using McpDotNet;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MSBuild.MCP;

public static class Prompts
{
    public class PromptArgument<T> : PromptArgument
    {
        public PromptArgument() : base() { }
    }

    public class TextContent : Content
    {
        public TextContent(string message) : base()
        {
            Type = "text";
            Text = message;
        }
    }
    public class ImageContent : Content
    {
        public ImageContent(string data, string mimeType) : base()
        {
            Type = "image";
            Data = data;
            MimeType = mimeType;
        }
    }
    public class EmbeddedResource : Content
    {
        public EmbeddedResource(ResourceContents resource) : base()
        {
            Type = "resource";
            Resource = resource;
        }
    }

    public static Prompt UpgradeProjectPrompt = new()
    {
        Name = "upgrade-project",
        Description = "Upgrade a project to the given target framework",
        Arguments = new() {
            new PromptArgument<string>(){
                Name = "projectPath",
                Description = "The path to the project file to upgrade",
                Required = true
            },
            new PromptArgument<string>() {
                Name = "targetFramework",
                Description = "The target framework to upgrade to",
                Required = true
            }
        }
    };

    public static Prompt UpgradeAllProjectsPrompt = new()
    {
        Name = "upgrade-all-project",
        Description = "Upgrade a project to the given target framework",
        Arguments = new() {
            new PromptArgument<string[]>(){
                Name = "projects",
                Description = "The paths to the projects to upgrade.",
                Required = true
            },
            new PromptArgument<string>() {
                Name = "targetFramework",
                Description = "The target framework to upgrade to.",
                Required = true
            }
        }
    };

    public static List<Prompt> All = [UpgradeProjectPrompt, UpgradeAllProjectsPrompt];

    public static GetPromptResult GetPrompt(GetPromptRequestParams req)
    {
        return req.Name switch
        {
            "upgrade-project" => new GetPromptResult()
            {
                Description = "Upgrade a project to the given target framework",
                Messages = new() {
                    new (){ Role = Role.User, Content = new TextContent($"Update the project at {req.Arguments!["projectPath"]} to target framework {req.Arguments!["targetFramework"]}")},
                 }
            },
            "upgrade-all-projects" => new GetPromptResult()
            {
                Description = "Upgrade the selected projects to the given target framework",
                Messages = new() {
                    new (){ Role = Role.User, Content = new TextContent(
                        $"""
                        Update the projects at {string.Join(", ", req.Arguments!["projects"])} to target framework {req.Arguments!["targetFramework"]}.
                        Make me a plan to update the target frameworks for these projects.
                        The plan should update the projects in dependency order - projects with no project dependencies should be first, then projects whose project dependencies have been upgraded, and so on.
                        Determine the dependency order by using the list-project-dependencies tool on each project.
                        Also output this plan to a file named `plan.md`.
                        """
                    ) },
                 }
            },
            _ => new GetPromptResult()
        };
    }
}

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

    [McpTool("list-package-references"), Description("Returns the package references of a project")]
    /// <param name="projectPath">The path to the project file to read</param>
    public string[] ListPackageReferences(string projectPath)
    {
        var project = TryLoadProject(projectPath);
        return project.GetItems("PackageReference").Select(i => i.EvaluatedInclude).ToArray();
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
            .WithTools()
            .WithListPromptsHandler((ctx, ctok) => Task.FromResult(new ListPromptsResult() { Prompts = Prompts.All }))
            .WithGetPromptHandler((req, ctok) => Task.FromResult(Prompts.GetPrompt(req.Params!)));
        await builder.Build().RunAsync();
    }
}