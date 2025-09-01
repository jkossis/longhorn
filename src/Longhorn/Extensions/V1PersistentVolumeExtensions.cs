using k8s.Models;

namespace Longhorn.Extensions;

public static class V1PersistentVolumeExtensions
{
    public static string GetContainerImageForVolume(this V1PersistentVolume persistentVolume, List<V1Pod> pods)
    {
        foreach (var pod in pods)
        {
            var volume = pod.Spec.Volumes.FirstOrDefault(v =>
                v.PersistentVolumeClaim is not null
                && v.PersistentVolumeClaim.ClaimName == persistentVolume.Spec.ClaimRef.Name
            );

            if (volume is not null)
            {
                foreach (var container in pod.Spec.Containers)
                {
                    if (
                        container.VolumeMounts is not null
                        && container.VolumeMounts.Any(vm => vm.Name == volume.Name)
                    )
                    {
                        return container.Image;
                    }
                }
            }
        }

        return string.Empty;
    }
}