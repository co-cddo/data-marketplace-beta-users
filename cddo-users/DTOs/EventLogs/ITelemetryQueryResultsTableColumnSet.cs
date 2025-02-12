namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsTableColumnSet
{
    List<ITelemetryQueryResultsTableColumn> Columns { get; }
}