using System.Text.Json.Serialization;

namespace Longhorn.Models;

public record LonghornBackupVolumes
{
    public required List<LonghornBackupVolume> Data { get; init; }
}

public record LonghornBackupVolume
{
    public required string Id { get; init; }
    public required string LastBackupAt { get; init; }
    public required string LastBackupName { get; init; }
    public required LonghornBackupVolumeLabels? Labels { get; init; }
}

public record LonghornBackupVolumeLabels
{
    public required string KubernetesStatus { get; init; }
    [JsonPropertyName("jkossis.io/image")]
    public string? Image { get; init; }
}

public record KubernetesStatus
{
    public required string PvcName { get; init; }
}