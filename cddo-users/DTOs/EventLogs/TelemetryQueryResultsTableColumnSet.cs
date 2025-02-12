namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsTableColumnSet : ITelemetryQueryResultsTableColumnSet
{
    public required List<ITelemetryQueryResultsTableColumn> Columns { get; set; }
}