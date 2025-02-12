namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsTableRow : ITelemetryQueryResultsTableRow
{
    public required List<ITelemetryQueryResultsTableRowValue> RowValues { get; set; }
}