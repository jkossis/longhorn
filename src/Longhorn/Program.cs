using Longhorn.Extensions;
using Longhorn.Models;
using Longhorn.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine;

var rootCommand = new RootCommand("Longhorn console application");

var backupCommand = new Command("backup", "Create backups in longhorn");
var cleanupCommand = new Command("cleanup", "Clean up storage in longhorn");

backupCommand.SetAction(async _ => await CreateBackups(GetHost()));
cleanupCommand.SetAction(async _ => await CleanUpStorage(GetHost()));

rootCommand.Subcommands.Add(backupCommand);
rootCommand.Subcommands.Add(cleanupCommand);

return rootCommand.Parse(args).Invoke();

static IHost GetHost()
{
    return new HostBuilder()
        .ConfigureAppConfiguration(config => config.AddJsonFile("appsettings.json").AddEnvironmentVariables())
        .ConfigureLogging((host, logger) =>
        {
            logger.AddConfiguration(host.Configuration.GetSection("Logging"));
            logger.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });
        })
        .ConfigureServices((host, services) =>
        {
            services.Configure<KubernetesOptions>(host.Configuration.GetSection("Kubernetes"));
            services.Configure<LonghornOptions>(host.Configuration.GetSection("Longhorn"));
            services.Configure<NotificationOptions>(host.Configuration.GetSection("Notification"));

            services.AddTransient<IKubernetesService, KubernetesService>();

            services.AddHttpClient<ILonghornService, LonghornService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(
                    serviceProvider.GetRequiredService<IOptions<LonghornOptions>>().Value.Url
                );
            });

            services.AddHttpClient<INotificationService, NotificationService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(
                    serviceProvider.GetRequiredService<IOptions<NotificationOptions>>().Value.Url
                );
            });
        })
        .Build();
}

static async Task CreateBackups(IHost host)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var kubernetesService = host.Services.GetRequiredService<IKubernetesService>();
    var longhornService = host.Services.GetRequiredService<ILonghornService>();
    var notificationService = host.Services.GetRequiredService<INotificationService>();

    try
    {
        var backupsCreated = 0;

        var pods = await kubernetesService.GetPods();
        foreach (var pv in await kubernetesService.GetPersistentVolumes())
        {
            if (!host.Services.GetRequiredService<IOptions<LonghornOptions>>().Value.DryRun)
            {
                await longhornService.CreateBackup(pv.Metadata.Name, pv.GetContainerImageForVolume(pods));
            }

            backupsCreated++;
        }

        if (backupsCreated > 0)
        {
            await notificationService.SendNotification(
                "Longhorn Backup",
                $"Successfully created {backupsCreated} longhorn backups"
            );
        }

        logger.LogInformation("Successfully created {BackupsCreated} longhorn backups", backupsCreated);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating longhorn backups");

        await notificationService.SendNotification(
            "Longhorn Backup",
            $"An error occurred while creating longhorn backups: {ex.Message}"
        );
    }
}

static async Task CleanUpStorage(IHost host)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var kubernetesService = host.Services.GetRequiredService<IKubernetesService>();
    var longhornService = host.Services.GetRequiredService<ILonghornService>();
    var notificationService = host.Services.GetRequiredService<INotificationService>();

    try
    {
        var getOrphanedBackupVolumes = longhornService.GetOrphanedBackupVolumes();
        var getVolumeSnapshotContents = kubernetesService.GetVolumeSnapshotContents();

        await Task.WhenAll(getOrphanedBackupVolumes, getVolumeSnapshotContents);

        var orphanedBackupVolumes = await getOrphanedBackupVolumes;
        var volumeSnapshotContents = await getVolumeSnapshotContents;

        var deletedVolumes = 0;
        foreach (
            var volume
            in orphanedBackupVolumes.Where(volume =>
                !volumeSnapshotContents
                    .Select(content => content.Spec.Source.SnapshotHandle)
                    .Any(handle => handle == $"bak://{volume.Id}/{volume.LastBackupName}")
            )
        )
        {
            if (!host.Services.GetRequiredService<IOptions<LonghornOptions>>().Value.DryRun)
            {
                await longhornService.DeleteBackupVolume(volume.Id);
            }
            deletedVolumes++;
        }

        if (deletedVolumes > 0)
        {
            await notificationService.SendNotification(
                "Longhorn Storage Cleanup",
                $"Successfully cleaned up {deletedVolumes} longhorn backups"
            );
        }

        logger.LogInformation("Successfully cleaned up {DeletedVolumes} longhorn backups", deletedVolumes);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while cleaning up longhorn backups");

        await notificationService.SendNotification(
            "Longhorn Storage Cleanup",
            $"An error occurred while cleaning up longhorn backups: {ex.Message}"
        );
    }
}