namespace cddo_users.DTOs.EventLogs;

public class TelemetryQueryResultsData : ITelemetryQueryResultsData
{
    public int TotalNumberOfResults => RowData.Rows.Count;

    public required ITelemetryQueryResultsTableColumnSet ColumnData { get; set; }

    public required ITelemetryQueryResultsTableRowSet RowData { get; set; }
}