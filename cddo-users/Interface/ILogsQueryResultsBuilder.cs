using Azure.Monitor.Query.Models;
using cddo_users.DTOs.EventLogs;

namespace cddo_users.Interface;

public interface ILogsQueryResultsBuilder
{
    Task<ILogsQueryDataResult> BuildLogsQueryDataResultFromLogsQueryResultAsync(
        LogsQueryResult logsQueryResult);
}