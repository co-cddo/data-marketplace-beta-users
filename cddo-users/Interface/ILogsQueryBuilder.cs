namespace cddo_users.Interface;

public interface ILogsQueryBuilder
{
    string ProvisionRawLogsQuery(
        string searchQuery,
        ILogsQueryFilter logsQueryFilter);

    string BuildLogsQuery(
        string? tableName,
        IEnumerable<string> searchClauses,
        ILogsQueryFilter logsQueryFilter);
}