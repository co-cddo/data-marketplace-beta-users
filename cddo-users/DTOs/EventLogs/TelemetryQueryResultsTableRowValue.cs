using System.Diagnostics;

namespace cddo_users.DTOs.EventLogs;

[DebuggerDisplay("{ValueName}: '{Value}' (Type={ValueType})")]
public class TelemetryQueryResultsTableRowValue : ITelemetryQueryResultsTableRowValue
{
    public required string ValueName { get; set; }

    public required TelemetryQueryResultsTableValueType ValueType { get; set; }

    public required object Value { get; set; }
}