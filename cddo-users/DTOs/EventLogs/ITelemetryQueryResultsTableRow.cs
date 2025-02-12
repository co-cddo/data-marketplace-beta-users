namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsTableRow
{
    List<ITelemetryQueryResultsTableRowValue> RowValues { get; }
}