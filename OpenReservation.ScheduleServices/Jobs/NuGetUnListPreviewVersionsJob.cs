using NuGet.Common;
using NuGet.Protocol.Core.Types;
using ReferenceResolver;
using WeihanLi.Extensions;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class NuGetUnListPreviewVersionsJob : AbstractJob
{
    public NuGetUnListPreviewVersionsJob(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    
    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        var configuration = scopeServiceProvider.GetRequiredService<IConfiguration>();
        var packages = configuration.GetAppSetting("DeletePreviewNuGetPackages")
            ?.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        if (packages.IsNullOrEmpty()) return;
        
        var nugetHelper = scopeServiceProvider.GetRequiredService<INuGetHelper>();
        var sourceRepository = nugetHelper.GetNuGetOrgSourceRepository();
        var packageUpdateResource = sourceRepository.GetResource<PackageUpdateResource>();
        foreach (var package in packages)
        {
            await foreach (var (_, v) in 
                           nugetHelper.GetPackageVersions(package, true, cancellationToken: cancellationToken))
            {
                if (v.IsPrerelease)
                {
                    await packageUpdateResource.Delete(package, v.ToString(), _ => configuration.GetAppSetting("NuGetDeleteApiKey"),
                        _ => true, true, new NuGetLoggerLoggingAdapter(Logger));
                    Logger.LogInformation("Package [{Package}] version[{PackageVersion}] unlisted",
                        package, v.ToString());
                }
            }
        }
    }

}