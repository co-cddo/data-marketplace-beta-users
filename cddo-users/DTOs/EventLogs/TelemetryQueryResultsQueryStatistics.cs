namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsQueryStatistics : ITelemetryQueryResultsQueryStatistics
{
    public required TimeSpan QueryExecutionTime { get; set; }
}