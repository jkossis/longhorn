using Longhorn.Extensions;
using Longhorn.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Longhorn.Services;

public interface ILonghornService
{
    Task CreateBackup(string persistentVolumeName, string image);
    Task<List<LonghornBackupVolume>> GetOrphanedBackupVolumes();
    Task DeleteBackupVolume(string id);
}

public class LonghornService(HttpClient httpClient, IOptions<LonghornOptions> longhornOptions) : ILonghornService
{
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task CreateBackup(string persistentVolumeName, string image)
    {
        var snapshotCreateResponse = await httpClient.PostAsJsonAsync(
            $"v1/volumes/{persistentVolumeName}?action=snapshotCreate",
            new Dictionary<string, object>(),
            _serializerOptions
        );

        snapshotCreateResponse.EnsureSuccessStatusCode();

        var snapshotCreate =
            await snapshotCreateResponse.Content.ReadFromJsonAsync<SnapshotCreateResponse>(_serializerOptions)
            ?? throw new InvalidOperationException("An error occurred while getting the snapshot create response");

        var snapshotBackupResponse = await httpClient.PostAsJsonAsync(
            $"v1/volumes/{persistentVolumeName}?action=snapshotBackup",
            new SnapshotBackupRequest
            {
                Name = snapshotCreate.Id,
                Labels = new Dictionary<string, string>
                {
                    { "jkossis.io/image", image }
                }
            },
            _serializerOptions
        );

        snapshotBackupResponse.EnsureSuccessStatusCode();
    }

    public async Task<List<LonghornBackupVolume>> GetOrphanedBackupVolumes()
    {
        var response = await httpClient.GetAsync("/v1/backupvolumes");

        response.EnsureSuccessStatusCode();

        var volumes =
            await response.Content.ReadFromJsonAsync<LonghornBackupVolumes>(_serializerOptions)
            ?? throw new InvalidOperationException("Unable to determine LonghornBackupVolumes");

        return [
            .. volumes.Data.Where(volume => volume.IsEmpty()),
            .. volumes.Data
                .Where(volume => !volume.IsEmpty())
                .GroupBy(volume =>
                {
                    var kubernetesStatus =
                        JsonSerializer.Deserialize<KubernetesStatus>(volume.Labels!.KubernetesStatus, _serializerOptions)
                        ?? throw new InvalidOperationException("Unable to determine KubernetesStatus");

                    return kubernetesStatus.PvcName;
                })
                .SelectMany(volumes =>
                {
                    var sorted = volumes.OrderByDescending(volume =>
                        string.IsNullOrEmpty(volume.LastBackupAt)
                            ? DateTime.MinValue
                            : DateTime.Parse(volume.LastBackupAt)
                    );

                    return sorted.Except(
                        sorted
                            .GroupBy(volume => volume.Labels!.Image ?? string.Empty)
                            .Take(longhornOptions.Value.NumberOfBackupsToKeep)
                            .Select(images => images.First())
                    );
                })
        ];
    }

    public async Task DeleteBackupVolume(string id)
    {
        var response = await httpClient.DeleteAsync($"/v1/backupvolumes/{id}");

        response.EnsureSuccessStatusCode();
    }
}