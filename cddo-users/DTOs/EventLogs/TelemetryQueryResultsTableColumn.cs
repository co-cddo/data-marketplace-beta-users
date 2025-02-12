namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsTableColumn : ITelemetryQueryResultsTableColumn
{
    public required string Name { get; set; }

    public required TelemetryQueryResultsTableValueType Type { get; set; }
}