namespace Binlog.MCP.Infrastructure;

/// <summary>
/// Represents a path to a binlog file.
/// </summary>
/// <param name="filePath">The file path to the binlog file.</param>
public readonly record struct BinlogPath(string filePath)
{
    /// <summary>
    /// Gets the absolute path to the binlog file.
    /// </summary>
    public string FullPath => new FileInfo(filePath).FullName;
}

/// <summary>
/// Represents a path to a project file.
/// </summary>
/// <param name="path">The path to the project file.</param>
public readonly record struct ProjectFilePath(string path);

/// <summary>
/// Represents a unique identifier for a project evaluation.
/// </summary>
/// <param name="id">The evaluation ID.</param>
public readonly record struct EvalId(int id);

/// <summary>
/// Represents a unique identifier for a project.
/// </summary>
/// <param name="id">The project ID.</param>
public readonly record struct ProjectId(int id);
