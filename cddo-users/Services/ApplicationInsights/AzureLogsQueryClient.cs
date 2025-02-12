using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using cddo_users.Interface;

namespace cddo_users.Services.ApplicationInsights;

internal class AzureLogsQueryClient(
    LogsQueryClient logsQueryClient,
    IConfiguration configuration) : IAzureLogsQueryClient
{
    async Task<LogsQueryResult> IAzureLogsQueryClient.RunLogsQueryAsync(
        string logsQuery,
        TimeSpan timeRange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logsQuery);

        var workspaceId = configuration["ApplicationInsights:WorkspaceId"];

        var result = await logsQueryClient.QueryWorkspaceAsync(
            workspaceId,
            logsQuery,
            new QueryTimeRange(timeRange),
            options: new LogsQueryOptions
            {
                IncludeStatistics = true
            });

        return result.HasValue
            ? result.Value
            : throw new InvalidOperationException($"Querying Azure Logs with received KQL Query returned no result: Query: {logsQuery}, Time Range: {timeRange}");
    }
}