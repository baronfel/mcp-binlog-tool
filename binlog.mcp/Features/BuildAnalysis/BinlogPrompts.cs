using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Binlog.MCP.Features.BuildAnalysis;

public class BinlogPrompts
{
    [McpServerPrompt(Name = "initial_build_analysis", Title = "Analyze Binary Log"), Description("Perform a build of the current workspace and profile it using the binary logger.")]
    public static IEnumerable<ChatMessage> InitialBuildAnalysis() => [
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

    [McpServerPrompt(Name = "compare_binlogs", Title = "Compare Binary Logs"), Description("Compare two binary logs.")]
    public static IEnumerable<ChatMessage> CompareBinlogs() => [
        new ChatMessage(ChatRole.System, """
            Get paths to two binary log files from the user.
            Then load both binlogs using the `load_binlog` command.
            After loading the binlogs, list all of the projects, get the evaluations for each project, and check if any projects have multiple evaluations.
            Finally, compare the timings of the evaluations and display the comparisons in a table that compares the same evaluation across both binlogs.
            """),
    ];
}
