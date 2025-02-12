namespace cddo_users.DTOs.EventLogs;

internal class LogsQueryDataResult : ILogsQueryDataResult
{
    public required ITelemetryQueryResultsData Results { get; init; }

    public required ITelemetryQueryResultsQueryStatistics QueryStatistics { get; init; }
}