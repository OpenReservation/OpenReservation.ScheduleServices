using Hangfire;

namespace OpenReservation.ScheduleServices.Jobs;

public abstract class AbstractJob: IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    protected AbstractJob(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var jobType = GetType();
        _logger = loggerFactory.CreateLogger(jobType.Name);
        JobName = jobType.Name;
        CronExpression = Cron.Never();
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
        }
    }

    protected abstract Task ExecuteInternalAsync(IServiceProvider scopeServiceProvider,
        CancellationToken cancellationToken);
}