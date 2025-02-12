using Azure.Monitor.Query.Models;

namespace cddo_users.Interface;

public interface IAzureLogsQueryClient
{
    Task<LogsQueryResult> RunLogsQueryAsync(
        string logsQuery,
        TimeSpan timeRange);
}