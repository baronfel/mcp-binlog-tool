using System.ComponentModel;
using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.TaskAnalysis;

public record struct SimpleTaskInfo(string Name, long DurationMs);

/// <summary>
/// Detailed information about a specific MSBuild task invocation.
/// </summary>
/// <param name="name">The task name.</param>
/// <param name="assembly">The assembly containing the task.</param>
/// <param name="durationMs">The task duration in milliseconds.</param>
/// <param name="parameters">Dictionary of task parameters by name.</param>
/// <param name="messages">Messages logged by the task.</param>
public record struct TaskDetails(
    string name,
    string assembly,
    long durationMs,
    Dictionary<string, string> parameters,
    string[] messages);

/// <summary>
/// Aggregated execution data for a task across multiple invocations.
/// </summary>
public record struct TaskExecutionData(
    [Description("The task name")] string taskName,
    [Description("The assembly the task belongs to")] string assembly,
    [Description("The number of times the task was executed")] int executionCount,
    [Description("The total duration across all executions in milliseconds")] long totalDurationMs,
    [Description("The average duration per execution in milliseconds")] long averageDurationMs,
    [Description("The minimum duration of any execution in milliseconds")] long minDurationMs,
    [Description("The maximum duration of any execution in milliseconds")] long maxDurationMs);

public static class Util {
    public static TaskDetails CreateTaskDetails(Microsoft.Build.Logging.StructuredLogger.Task foundTask)
    {

            // Extract task parameters
            var parameters = foundTask.Children.OfType<Property>()
                .ToDictionary(p => p.Name, p => p.Value);

            // Extract messages
            var messages = foundTask.Children.OfType<Message>()
                .Select(m => m.Text)
                .ToArray();

            return new TaskDetails(
                foundTask.Name,
                foundTask.FromAssembly,
                (long)foundTask.Duration.TotalMilliseconds,
                parameters,
                messages);
    }
}
