using k8s;

namespace Longhorn.Models;

public class KubernetesCustomResource<TSpec> : KubernetesObject
{
    public required TSpec Spec { get; set; }
}

public class KubernetesCustomResourceList<T> : KubernetesObject where T : KubernetesObject
{
    public required List<T> Items { get; set; }
}