namespace cddo_users.DTOs.EventLogs;

public interface ITelemetryQueryResultsData
{
    int TotalNumberOfResults { get; }

    ITelemetryQueryResultsTableColumnSet ColumnData { get; }

    ITelemetryQueryResultsTableRowSet RowData { get; }
}