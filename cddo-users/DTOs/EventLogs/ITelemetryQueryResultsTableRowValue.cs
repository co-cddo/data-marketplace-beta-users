namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsTableRowValue
{
    string ValueName { get; }

    TelemetryQueryResultsTableValueType ValueType { get; }

    object Value { get; }
}