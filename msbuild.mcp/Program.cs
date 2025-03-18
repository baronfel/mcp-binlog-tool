using MCPSharp;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

namespace MSBuild.MCP;


[McpTool("msbuild", "MSBuild tool")]
public class MSBuildTool
{
    public struct ProjectKey(string path, (string, string)[]? properties);

    public Dictionary<ProjectKey, Project> loadedProjects = new();
    public ProjectCollection projectCollection = new();

    [McpTool("list-target-frameworks", "Returns the target frameworks of a project")]
    public string[] ListTargetFrameworks([McpParameter(required: true, description: "The path to the project to inspect")] string projectPath)
    {
        var project = TryLoadProject(projectPath);
        var tfms = project.GetProperty("TargetFrameworks")?.EvaluatedValue.Split(';');
        var tf = project.GetProperty("TargetFramework")?.EvaluatedValue;
        return tfms ?? (tf is not null ? new[] { tf } : Array.Empty<string>());
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
        await MCPServer.StartAsync("msbuild-server", "1.0.0");
    }
}