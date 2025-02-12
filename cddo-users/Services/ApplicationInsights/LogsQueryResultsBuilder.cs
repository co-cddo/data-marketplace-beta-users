using System.Text.Json;
using Azure.Monitor.Query.Models;
using cddo_users.DTOs.EventLogs;
using cddo_users.Interface;

namespace cddo_users.Services.ApplicationInsights;

internal class LogsQueryResultsBuilder(
    ILogger<LogsQueryResultsBuilder> logger,
    IAnonymizedUserInformationPopulation anonymizedUserInformationPopulation) : ILogsQueryResultsBuilder
{
    async Task<ILogsQueryDataResult> ILogsQueryResultsBuilder.BuildLogsQueryDataResultFromLogsQueryResultAsync(
        LogsQueryResult logsQueryResult)
    {
        ArgumentNullException.ThrowIfNull(logsQueryResult);

        var resultsData = await DoBuildResultsDataAsync(logsQueryResult);

        return new LogsQueryDataResult
        {
            Results = resultsData,
            QueryStatistics = DoBuildQueryStatistics(logsQueryResult)
        };
    }

    private async Task<ITelemetryQueryResultsData> DoBuildResultsDataAsync(LogsQueryResult logsQueryResult)
    {
        var resultsTable = logsQueryResult.Table;

        var columnData = BuildColumnData();
        var rowData = await BuildRowDataAsync();

        return new TelemetryQueryResultsData
        {
            ColumnData = columnData,
            RowData = rowData
        };

        ITelemetryQueryResultsTableColumnSet BuildColumnData()
        {
            return new TelemetryQueryResultsTableColumnSet
            {
                Columns = resultsTable.Columns.Select(BuildColumn).ToList()
            };

            ITelemetryQueryResultsTableColumn BuildColumn(LogsTableColumn logsTableColumn)
            {
                return new TelemetryQueryResultsTableColumn
                {
                    Name = logsTableColumn.Name,
                    Type = MapColumnType()
                };

                TelemetryQueryResultsTableValueType MapColumnType()
                {
                    var columnType = logsTableColumn.Type.ToString().ToLower();

                    return columnType switch
                    {
                        "bool" => TelemetryQueryResultsTableValueType.Boolean,
                        "datetime" => TelemetryQueryResultsTableValueType.DateTime,
                        "dynamic" => TelemetryQueryResultsTableValueType.Dynamic,
                        "int" => TelemetryQueryResultsTableValueType.Integer,
                        "long" => TelemetryQueryResultsTableValueType.Long,
                        "real" => TelemetryQueryResultsTableValueType.Real,
                        "string" => TelemetryQueryResultsTableValueType.String,
                        "guid" => TelemetryQueryResultsTableValueType.Guid,
                        "decimal" => TelemetryQueryResultsTableValueType.Decimal,
                        "timespan" => TelemetryQueryResultsTableValueType.Timespan,

                        _ => throw new InvalidOperationException($"Unable to build column data for unknown column type: '{logsTableColumn.Type.ToString()}'")
                    };
                }
            }
        }

        async Task<ITelemetryQueryResultsTableRowSet> BuildRowDataAsync()
        {
            var rows = resultsTable.Rows.Select(BuildRow);

            var rowDataWithUserInformation = await anonymizedUserInformationPopulation.PopulateAnonymizedUserInformationAsync(rows);

            return new TelemetryQueryResultsTableRowSet
            {
                Rows = rowDataWithUserInformation.ToList()
            };

            ITelemetryQueryResultsTableRow BuildRow(LogsTableRow logsTableRow)
            {
                var telemetryQueryResultsTableRowValues = BuildRowValues().ToList();

                return new TelemetryQueryResultsTableRow
                {
                    RowValues = telemetryQueryResultsTableRowValues
                };

                IEnumerable<ITelemetryQueryResultsTableRowValue> BuildRowValues()
                {
                    var indexedLogTableRowValues = logsTableRow.Select((value, index) => new { LogTableRowValue = value, ColumnIndex = index });

                    foreach (var indexedLogTableRowValue in indexedLogTableRowValues)
                    {
                        yield return BuildRowValue(indexedLogTableRowValue.LogTableRowValue, indexedLogTableRowValue.ColumnIndex);
                    }

                    TelemetryQueryResultsTableRowValue BuildRowValue(object logTableRowValue, int columnIndex)
                    {
                        var column = columnData.Columns[columnIndex];

                        var columnName = column.Name;

                        return new TelemetryQueryResultsTableRowValue
                        {
                            ValueName = columnName,
                            ValueType = column.Type,
                            Value = GetRowValue()
                        };

                        object GetRowValue()
                        {
                            if (!columnName.Equals("Properties", StringComparison.InvariantCultureIgnoreCase))
                            {
                                return logTableRowValue;
                            }

                            var tableRowValueString = logTableRowValue.ToString();

                            if (string.IsNullOrWhiteSpace(tableRowValueString)) return new TelemetryQueryResultsTableRowProperties();

                            var telemetryQueryResultsTableRowProperties = JsonSerializer.Deserialize<TelemetryQueryResultsTableRowProperties>(tableRowValueString);

                            if (telemetryQueryResultsTableRowProperties == null)
                            {
                                logger.LogError("Unable to convert table Properties row value to expected type: '{tableRowValueString}'", tableRowValueString);
                                return new TelemetryQueryResultsTableRowProperties();
                            }

                            return telemetryQueryResultsTableRowProperties;
                        }
                    }
                }
            }
        }
    }

    private static ITelemetryQueryResultsQueryStatistics DoBuildQueryStatistics(LogsQueryResult logsQueryResult)
    {
        var queryExecutionTimeInSeconds = DoReadQueryExecutionTimeInSeconds();

        return new TelemetryQueryResultsQueryStatistics
        {
            QueryExecutionTime = TimeSpan.FromSeconds(queryExecutionTimeInSeconds)
        };

        double DoReadQueryExecutionTimeInSeconds()
        {
            var queryStatistics = logsQueryResult.GetStatistics();
            if (queryStatistics == null) return 0;

            using var queryStatisticsJsonDocument = JsonDocument.Parse(queryStatistics);

            var queryStatisticsElement = queryStatisticsJsonDocument.RootElement.GetProperty("query");

            return queryStatisticsElement.GetProperty("executionTime").GetDouble();
        }
    }
}