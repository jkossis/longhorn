namespace Longhorn.Models;

public record SnapshotBackupRequest
{
    public required string Name { get; init; }
    public required Dictionary<string, string> Labels { get; init; }
}