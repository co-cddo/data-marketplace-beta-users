namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsTableRowSet : ITelemetryQueryResultsTableRowSet
{
    public required List<ITelemetryQueryResultsTableRow> Rows { get; set; }
}