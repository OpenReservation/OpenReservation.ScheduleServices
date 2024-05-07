using System.Collections.Concurrent;
using Hangfire;
using NuGet.Versioning;
using OpenReservation.ScheduleServices.Services;
using ReferenceResolver;
using WeihanLi.Extensions;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class NuGetPackageWatchJob : AbstractJob
{
    private static readonly ConcurrentDictionary<string, NuGetVersion> Versions = new();
    
    public NuGetPackageWatchJob(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override string CronExpression { get; } = Cron.Hourly();

    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        var configuration = scopeServiceProvider.GetRequiredService<IConfiguration>();
        var packageIds = configuration.GetAppSetting("WatchingNugetPackageIds")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (packageIds.IsNullOrEmpty()) return;

        var nugetHelper = scopeServiceProvider.GetRequiredService<INuGetHelper>();
        var notificationService = scopeServiceProvider.GetRequiredService<INotificationService>();
        foreach (var pkgId in packageIds)
        {
            var packageVersion = await nugetHelper.GetLatestPackageVersion(pkgId, true, null, cancellationToken);
            if (packageVersion is null)
            {
                Logger.LogInformation("No version found for package {PackageId}", pkgId);
                continue;
            }
            
            if (Versions.TryGetValue(pkgId, out var version))
            {
                if (packageVersion != version)
                {
                    Versions[pkgId] = packageVersion;
                    Logger.LogInformation("Package `{PackageId}` latest version changed to {PackageVersion}", 
                        pkgId, packageVersion);
                    // notification
                    await notificationService.SendNotificationAsync(
                        $"[NuGetPackageWatcher]Package {pkgId} version change from {version} to {packageVersion}");
                }
                else
                {
                    Logger.LogInformation("Package `{PackageId}` latest version is still {PackageVersion}", 
                        pkgId, packageVersion);
                }
            }
            else
            {
                Versions[pkgId] = packageVersion;
                Logger.LogInformation("Package `{PackageId}` latest version is {PackageVersion}", 
                    pkgId, packageVersion);
            }
        }
    }
}