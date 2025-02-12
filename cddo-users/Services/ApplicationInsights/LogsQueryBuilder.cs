using cddo_users.Interface;

namespace cddo_users.Services.ApplicationInsights;

internal class LogsQueryBuilder : ILogsQueryBuilder
{
    private const string defaultTableName = "AppEvents";
    private const char operatorPrefix = '|';

    string ILogsQueryBuilder.ProvisionRawLogsQuery(
        string searchQuery,
        ILogsQueryFilter logsQueryFilter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchQuery);
        ArgumentNullException.ThrowIfNull(logsQueryFilter);

        var originalQuery = searchQuery;
        int summarizeIndex = searchQuery.IndexOf("| serialize");
        if (summarizeIndex != -1)
        {
            searchQuery = searchQuery.Substring(0, summarizeIndex);
        }
        string result = summarizeIndex != -1
            ? originalQuery.Substring(summarizeIndex)
            : string.Empty;

        var searchQueryWithoutPagination = result;

        var cleanSearchClauses = BuildCleanSearchClauses(searchQuery);

        var searchOperators = BuildSearchOperatorsWithApplicableUserFilterOperators(cleanSearchClauses, logsQueryFilter);

        BuildOrderByOperators();

        var composedQuery = ComposeSearchQuery(searchOperators);

        return composedQuery + searchQueryWithoutPagination;

        IEnumerable<string> BuildCleanSearchClauses(
            string query)
        {
            return query.Split(operatorPrefix, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Select(x => x.Trim(operatorPrefix))
                .Select(x => x.Trim())
                .ToList();
        }
    }

    string ILogsQueryBuilder.BuildLogsQuery(
        string? tableName,
        IEnumerable<string> searchClauses,
        ILogsQueryFilter logsQueryFilter)
    {
        ArgumentNullException.ThrowIfNull(logsQueryFilter);

        var searchTableName = tableName ?? defaultTableName;

        var cleanSearchClauses = CleanSearchClauses(searchClauses);

        var searchOperators = BuildSearchOperatorsWithApplicableUserFilterOperators(cleanSearchClauses, logsQueryFilter);

        var orderByOperators = BuildOrderByOperators();

        var queryOperators = new List<string> { searchTableName }
            .Concat(searchOperators)
            .Concat(orderByOperators);

        return ComposeSearchQuery(queryOperators);

        IEnumerable<string> CleanSearchClauses(
            IEnumerable<string> clauses)
        {
            return clauses
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();
        }
    }

    private static IEnumerable<string> BuildOrderByOperators()
    {
        return new List<string>
        {
            "order by TimeGenerated desc"
        };
    }

    private static string ComposeSearchQuery(
        IEnumerable<string> operators)
    {
        return string.Join($" {operatorPrefix} ", operators);
    }

    private static IEnumerable<string> BuildSearchOperatorsWithApplicableUserFilterOperators(
        IEnumerable<string> searchClauses,
        ILogsQueryFilter logsQueryFilter)
    {
        var filterClause = BuildQueryFilterClause(logsQueryFilter);
        if (filterClause == null) return searchClauses;

        var searchClausesList = searchClauses.ToList();

        var indexOfFinalWhereClause = searchClausesList.FindLastIndex(x => x.StartsWith("where", StringComparison.InvariantCultureIgnoreCase));
        if (indexOfFinalWhereClause >= 0)
        {
            searchClausesList.Insert(indexOfFinalWhereClause + 1, filterClause);
        }
        else
        {
            searchClausesList.Add(filterClause);
        }

        return searchClausesList;
    }

    private static string? BuildQueryFilterClause(ILogsQueryFilter logsQueryFilter)
    {
        var filterTerms = new List<string>();
        if (logsQueryFilter.FilterByEnvironmentAppRoleName) filterTerms.Add($"AppRoleName == \"{logsQueryFilter.EnvironmentAppRoleName}\"");
        if (logsQueryFilter.FilterByOrganisation) filterTerms.Add($"Properties.OrganisationId == {logsQueryFilter.OrganisationId}");
        if (logsQueryFilter.FilterByDomain) filterTerms.Add($"Properties.DomainId == {logsQueryFilter.DomainId}");
        if (logsQueryFilter.FilterByUser) filterTerms.Add($"Properties.UserId == {logsQueryFilter.UserId}");

        return filterTerms.Any()
            ? $"where {string.Join(" and ", filterTerms)}"
            : null;
    }
}