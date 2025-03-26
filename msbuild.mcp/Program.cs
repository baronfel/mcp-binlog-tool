using System.ComponentModel;
using ModelContextProtocol;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using Serilog;
using NuGet.Protocol;
using NuGet.Credentials;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using System.Collections.Concurrent;
using NuGet.Versioning;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MSBuild.MCP;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
        Name = "upgrade-all-projects",
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
                    new (){ Content = new TextContent(
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


public class PackageReference(string Name, string Version);

[McpServerToolType]
public class MSBuildTool
{
    public struct ProjectKey(string path, (string, string)[]? properties);

    private readonly SourceCacheContext _cacheSettings = new()
    {
        NoCache = false,
        DirectDownload = true,
        IgnoreFailedSources = true,
    };

    private readonly ConcurrentDictionary<PackageSource, SourceRepository> _sourceRepositories = new();
    private readonly Dictionary<ProjectKey, Project> loadedProjects = new();

    private readonly ProjectCollection projectCollection = new(new Dictionary<string, string>()
    {
        // todo: add any required msbuild global properties here
        // in real-life would likely need to be highly variable, and we might need _multiple_ project collections
        // for different projects with different global properties
    });

    /// <param name="projectPath">The path to the project file to read</param>
    /// <param name="cancellationToken"></param>
    [McpServerTool("list-target-frameworks"), Description("Returns the target frameworks of a project")]
    public async Task<string[]> ListTargetFrameworks(string projectPath, CancellationToken cancellationToken)
    {
        var project = await TryLoadProject(projectPath, cancellationToken);
        var tfms = project.GetProperty("TargetFrameworks")?.EvaluatedValue.Split(';');
        var tf = project.GetProperty("TargetFramework")?.EvaluatedValue;
        return tfms ?? (tf is not null ? new[] { tf } : Array.Empty<string>());
    }

    /// <param name="projectPath">The path to the project file to read</param>
    /// <param name="cancellationToken"></param>
    [McpServerTool("list-project-dependencies"), Description("Returns the project dependencies of a project")]
    public async Task<string[]> ListProjectDependencies(string projectPath, CancellationToken cancellationToken)
    {
        var project = await TryLoadProject(projectPath, cancellationToken);
        return project.GetItems("ProjectReference").Select(i => i.EvaluatedInclude).ToArray();
    }

    /// <param name="projectPath">The path to the project file to read</param>
    /// <param name="cancellationToken"></param>
    [McpServerTool("list-package-references"), Description("Returns the package references of a project")]
    public async Task<string[]> ListPackageReferences(string projectPath, CancellationToken cancellationToken)
    {
        var project = await TryLoadProject(projectPath, cancellationToken);
        return project.GetItems("PackageReference").Select(i => $"Package {i.EvaluatedInclude}, version {i.GetMetadataValue("Version")}").ToArray();
    }

    /// <param name="packageName">The name of the package to get versions for</param>
    /// <param name="cancellationToken"></param>
    [McpServerTool("get-package-versions"), Description("Returns the versions of a package available from the configured package sources")]
    public async Task<string[]> GetPackageVersions(string packageName, CancellationToken cancellationToken)
    {

        ISettings settings;
        try
        {
            settings = Settings.LoadDefaultSettings(Environment.CurrentDirectory); //TODO: when mcpdotnet supports roots, we should search from the registered roots
        }
        catch
        {
            return Array.Empty<string>();
        }
        var sourceProvider = new PackageSourceProvider(settings);
        var packageSources = sourceProvider.LoadPackageSources().Where(s => s.IsEnabled).ToDictionary(s => s.Name);
        var mapping = PackageSourceMapping.GetPackageSourceMapping(settings);
        var validSources = mapping.IsEnabled ? mapping.GetConfiguredPackageSources(packageName).Select(sourceName => packageSources[sourceName]) : packageSources.Values;
        var autoCompletes = await Task.WhenAll(validSources.Select(async (source) => await GetAutocompleteAsync(source, cancellationToken).ConfigureAwait(false))).ConfigureAwait(false);
        // filter down to autocomplete endpoints (not all sources support this)
        var validAutoCompletes = autoCompletes.SelectMany(x => x);
        // get versions valid for this source
        var versionTasks = validAutoCompletes.Select(autocomplete => GetPackageVersionsForSource(autocomplete, packageName, cancellationToken)).ToArray();
        var versions = await Task.WhenAll(versionTasks).ConfigureAwait(false);
        // sources may have the same versions, so we have to dedupe.
        return versions.SelectMany(v => v).Distinct().OrderDescending().Select(x => x.ToString()).ToArray();
    }

    private SourceRepository GetSourceRepository(PackageSource source)
    {
        if (!_sourceRepositories.TryGetValue(source, out SourceRepository? value))
        {
            value = Repository.Factory.GetCoreV3(source);
            _sourceRepositories.AddOrUpdate(source, _ => value, (_, _) => value);
        }

        return value;
    }

    private async Task<IEnumerable<AutoCompleteResource>> GetAutocompleteAsync(PackageSource source, CancellationToken cancellationToken)
    {
        SourceRepository repository = GetSourceRepository(source);
        if (await repository.GetResourceAsync<AutoCompleteResource>(cancellationToken).ConfigureAwait(false) is var resource)
        {
            return [resource];
        }
        else return Enumerable.Empty<AutoCompleteResource>();
    }

    private async Task<IEnumerable<NuGetVersion>> GetPackageVersionsForSource(AutoCompleteResource autocomplete, string packageId, CancellationToken cancellationToken)
    {
        try
        {
            // we use the NullLogger because we don't want to log to stdout for completions - they interfere with the completions mechanism of the shell program.
            return await autocomplete.VersionStartsWith(packageId.ToString(), versionPrefix: "", includePrerelease: true, sourceCacheContext: _cacheSettings, log: NuGet.Common.NullLogger.Instance, token: cancellationToken);
        }
        catch (FatalProtocolException)  // this most often means that the source didn't actually have a SearchAutocompleteService
        {
            return Enumerable.Empty<NuGetVersion>();
        }
        catch (Exception) // any errors (i.e. auth) should just be ignored for completions
        {
            return Enumerable.Empty<NuGetVersion>();
        }
    }

    async Task<Project> TryLoadProject(string projectPath, CancellationToken cancellationToken)
    {
        var key = new ProjectKey(projectPath, null);
        if (loadedProjects.TryGetValue(key, out var project))
        {
            return project;
        }

        return await Task.Run(() =>
        {
            project = projectCollection!.LoadProject(Path.IsPathFullyQualified(projectPath) ? projectPath : Path.Combine(Environment.CurrentDirectory, projectPath));
            loadedProjects[key] = project;
            return project;
        }, cancellationToken);
    }
}

public static class Program
{
    static void RegisterMSBuild()
    {
        // in a different method to avoid JITing any MSBuild types until after this registration.
        MSBuildLocator.RegisterDefaults();
    }

    static async Task Main(string[] args)
    {
        RegisterMSBuild();

        DefaultCredentialServiceUtility.SetupDefaultCredentialService(NuGet.Common.NullLogger.Instance, true);

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