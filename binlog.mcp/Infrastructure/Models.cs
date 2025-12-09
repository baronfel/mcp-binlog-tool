namespace Binlog.MCP.Infrastructure;

public readonly record struct BinlogPath(string filePath)
{
    public string FullPath => new FileInfo(filePath).FullName;
}

public readonly record struct ProjectFilePath(string path);

public readonly record struct EvalId(int id);

public readonly record struct ProjectId(int id);
