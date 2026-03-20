using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Binlog.MCP.Features.BinlogLoading;
using Binlog.MCP.Features.TimelineAnalysis;
using Binlog.MCP.Infrastructure;
using ModelContextProtocol;
using static Binlog.MCP.Features.BinlogLoading.LoadBinlogTool;

namespace Binlog.MCP.Cli;

internal static class CliRunner
{
    /// <summary>
    /// Registers post-load callbacks required by CLI commands (timeline, etc.).
    /// Must be called before any command that auto-loads a binlog.
    /// </summary>
    public static void RegisterCallbacks()
    {
        BinlogLoader.RegisterPostLoadCallback((binlog, build) =>
        {
            var timeline = TimelineCache.GetOrCompute(binlog, build);
            return timeline.NodesByNodeId.Keys.Count;
        });
    }

    /// <summary>
    /// Loads the binlog if not already cached, writing progress to stderr.
    /// Delegates to <see cref="LoadBinlogTool.Load"/> so CLI and MCP share the same load path.
    /// Returns the same <see cref="InterestingBuildData"/> that the MCP tool exposes.
    /// </summary>
    public static InterestingBuildData EnsureLoaded(string binlogPath)
    {
        var binlog = new BinlogPath(binlogPath);
        bool alreadyLoaded = BinlogLoader.TryGetBuild(binlog, out _);
        IProgress<ProgressNotificationValue> progress = alreadyLoaded
            ? NullProgress.Instance
            : new ConsoleProgress();

        var result = LoadBinlogTool.Load(binlogPath, progress);

        if (!alreadyLoaded)
            Console.Error.WriteLine();

        return result;
    }

    /// <summary>
    /// Serializes <paramref name="value"/> as compact JSON to stdout using the same
    /// source-generated <see cref="BinlogJsonContext"/> that the MCP server uses.
    /// Compact output is intentional — the primary consumers are agents, not humans.
    /// </summary>
    public static void PrintJson<T>(T value)
    {
        JsonTypeInfo typeInfo = BinlogJsonContext.Default.GetTypeInfo(typeof(T))
            ?? throw new InvalidOperationException(
                $"Type {typeof(T).Name} is not registered in BinlogJsonContext.");

        Console.WriteLine(JsonSerializer.Serialize(value, typeInfo));
    }

    private sealed class ConsoleProgress : IProgress<ProgressNotificationValue>
    {
        public void Report(ProgressNotificationValue value)
        {
            if (value.Message is not null)
                Console.Error.Write($"\r  {value.Message,-60}");
        }
    }

    private sealed class NullProgress : IProgress<ProgressNotificationValue>
    {
        public static readonly NullProgress Instance = new();
        public void Report(ProgressNotificationValue value) { }
    }
}
