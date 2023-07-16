namespace OpenReservation.ScheduleServices;

public interface IJob
{
    string JobName { get; }
    string CronExpression { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}