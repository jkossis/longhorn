using Longhorn.Models;

namespace Longhorn.Extensions;

public static class LonghornBackupVolumeExtensions
{
    public static bool IsEmpty(this LonghornBackupVolume volume)
    {
        return volume.Labels is null
            || string.IsNullOrEmpty(volume.LastBackupName)
            || string.IsNullOrEmpty(volume.LastBackupAt);
    }
}