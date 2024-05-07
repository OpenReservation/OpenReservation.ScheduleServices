using Hangfire;
using OpenReservation.ScheduleServices.Services;

namespace OpenReservation.ScheduleServices.Jobs;

public abstract class AbstractJob: IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    protected AbstractJob(IServiceProvider serviceProvider)
    {
        var jobType = GetType();
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(jobType.Name);
        JobName = jobType.Name;
        CronExpression = Cron.Never();
        _serviceProvider = serviceProvider;
    }
    
    public virtual string JobName { get; }
    public virtual string CronExpression { get; }
    protected ILogger Logger => _logger;
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        try
        {
            _logger.LogInformation("{Summary} {JobName}",
                "Begin to execute job", JobName);
            await ExecuteInternalAsync(scope.ServiceProvider, cancellationToken);
            _logger.LogInformation("{Summary} {JobName}",
                "Job execute completed", JobName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Summary} {JobName}",
                "Execute job exception", JobName);
            await scope.ServiceProvider.GetRequiredService<INotificationService>()
                .SendNotificationAsync($"Exception when execute job {JobName}, exception: {e}");
        }
    }

    protected abstract Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider,
        CancellationToken cancellationToken);
}