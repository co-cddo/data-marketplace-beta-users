using cddo_users.DTOs;
using cddo_users.DTOs.EventLogs;

namespace cddo_users.Interface
{
    public interface IApplicationInsightsService
    {
        Task<ILogsQueryDataResult> GetEventLogsFromRawQueryAsync(
            string searchQuery,
            TimeSpan timeRange,
            UserProfile userProfile);

        Task<EventLogResponse> GetEventLogsAsync(int pageSize, string searchQuery);
        Task<ILogsQueryDataResult> GetEventLogsAsync(
            string? tableName,
            IEnumerable<string> searchClauses,
            TimeSpan timeRange,
            UserProfile userProfile);
    }
}