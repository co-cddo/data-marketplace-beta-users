namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsTableRowSet
{
    List<ITelemetryQueryResultsTableRow> Rows { get; }
}