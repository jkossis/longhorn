namespace Longhorn.Models;

public record KubernetesOptions
{
    public required bool InCluster { get; init; }
}