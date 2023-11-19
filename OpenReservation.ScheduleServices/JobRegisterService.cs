using Hangfire;

namespace OpenReservation.ScheduleServices;

public sealed class JobRegisterService(IServiceProvider appServices): BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var job in appServices.GetServices<IJob>())
        {
            RecurringJob.AddOrUpdate(job.JobName, () => job.ExecuteAsync(CancellationToken.None), job.CronExpression);
        }
        return Task.CompletedTask;
    }
}