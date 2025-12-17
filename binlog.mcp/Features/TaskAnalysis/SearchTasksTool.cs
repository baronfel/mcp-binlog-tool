using System.ComponentModel;
using Binlog.MCP.Infrastructure;
using Microsoft.Build.Logging.StructuredLogger;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.TaskAnalysis;

/// <summary>
/// MCP tool for searching for task invocations by name across all projects.
/// </summary>
public class SearchTasksTool
{
    [McpServerTool(Name = "search_tasks_by_name", Title = "Search Tasks by Name", Idempotent = true, UseStructuredContent = true, ReadOnly = true)]
    [Description("Find all invocations of a specific MSBuild task across all projects (e.g., 'Csc', 'Copy') and return execution summary. Returns a dictionary of dictionaries - the outer dictionary is keyed by project id, the inner keyed by task id.")]
    public static Dictionary<int, Dictionary<int, SimpleTaskInfo>> SearchTasksByName(
        [Description("The path to a MSBuild binlog file that has been loaded via `load_binlog`")] string binlog_file,
        [Description("The name of the task to search for (case-insensitive)")] string taskName)
    {
        var binlog = new BinlogPath(binlog_file);
        if (!BinlogLoader.TryGetProjectsById(binlog, out var projects) || projects == null)
        {
            return [];
        }

        var results = new Dictionary<int, Dictionary<int, SimpleTaskInfo>>();
        foreach (var project in projects.Values)
        {
            Dictionary<int, SimpleTaskInfo>? projectResults = null;
            foreach (var target in project.Children.OfType<Target>())
            {
                var matchingTasks = target.Children.OfType<Microsoft.Build.Logging.StructuredLogger.Task>()
                    .Where(t => string.Equals(t.Name, taskName, StringComparison.OrdinalIgnoreCase));
                if (!matchingTasks.Any())
                {
                    continue;
                }
                projectResults ??= new();
                foreach (var task in matchingTasks)
                {
                    projectResults[task.Id] = new (task.Name, (long)(task.EndTime - task.StartTime).TotalMilliseconds);
                }
            }

            if (projectResults != null)
            {
                results[project.Id] = projectResults;
            }
        }

        return results;
    }
}
