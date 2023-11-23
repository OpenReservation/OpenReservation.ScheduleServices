using Hangfire;
using WeihanLi.Common.Helpers;

namespace OpenReservation.ScheduleServices.Jobs;

public sealed class DotnetConfHookCheckJob : AbstractJob
{
    public DotnetConfHookCheckJob(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
    {
    }

    public override string CronExpression => Cron.Minutely();

    protected override async Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        using var response = await HttpHelper.HttpClient.GetAsync("http://hook.dotnetconf.cn", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}