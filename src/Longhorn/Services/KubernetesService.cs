using k8s;
using k8s.Models;
using Longhorn.Models;
using Microsoft.Extensions.Options;

namespace Longhorn.Services;

public interface IKubernetesService
{
    Task<List<V1PersistentVolume>> GetPersistentVolumes();
    Task<List<V1Pod>> GetPods();
    Task<List<KubernetesCustomResource<VolumeSnapshotContentSpec>>> GetVolumeSnapshotContents();
}

public class KubernetesService(IOptions<KubernetesOptions> kubernetesOptions) : IKubernetesService
{
    private readonly KubernetesClientConfiguration _config = kubernetesOptions.Value.InCluster
        ? KubernetesClientConfiguration.InClusterConfig()
        : KubernetesClientConfiguration.BuildConfigFromConfigFile();

    public async Task<List<V1PersistentVolume>> GetPersistentVolumes()
    {
        using var client = new Kubernetes(_config);

        var persistentVolumes = await client.ListPersistentVolumeAsync();

        return [.. persistentVolumes.Items.Where(pv => pv.Spec.StorageClassName == "longhorn")];
    }

    public async Task<List<V1Pod>> GetPods()
    {
        using var client = new Kubernetes(_config);

        var pods = await client.ListPodForAllNamespacesAsync();

        return [.. pods.Items];
    }

    public async Task<List<KubernetesCustomResource<VolumeSnapshotContentSpec>>> GetVolumeSnapshotContents()
    {
        using var client = new Kubernetes(_config);

        var volumeSnapshotContents = await new GenericClient(
                client,
                "snapshot.storage.k8s.io",
                "v1",
                "volumesnapshotcontents"
            )
            .ListAsync<VolumeSnapshotContents>();

        return volumeSnapshotContents.Items;
    }
}