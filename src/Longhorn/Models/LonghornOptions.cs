namespace Longhorn.Models;

public record LonghornOptions
{
    public required string Url { get; init; }
    public bool DryRun { get; init; } = true;
    public int NumberOfBackupsToKeep { get; init; } = 3;
}