namespace Longhorn.Models;

public class VolumeSnapshotContents : KubernetesCustomResourceList<KubernetesCustomResource<VolumeSnapshotContentSpec>> { }

public record VolumeSnapshotContentSpec
{
    public required VolumeSnapshotContentSpecSource Source { get; init; }
}

public record VolumeSnapshotContentSpecSource
{
    public required string SnapshotHandle { get; init; }
}