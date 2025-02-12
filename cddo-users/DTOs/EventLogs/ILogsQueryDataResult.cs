namespace cddo_users.DTOs.EventLogs;

public interface ILogsQueryDataResult
{
    ITelemetryQueryResultsData Results { get; }

    ITelemetryQueryResultsQueryStatistics QueryStatistics { get; }
}