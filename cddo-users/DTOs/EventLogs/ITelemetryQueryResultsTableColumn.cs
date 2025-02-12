namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsTableColumn
{
    string Name { get; }

    TelemetryQueryResultsTableValueType Type { get; }
}