namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsQueryStatistics
{
    TimeSpan QueryExecutionTime { get; }
}