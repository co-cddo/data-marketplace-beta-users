using cddo_users.DTOs.EventLogs;
using cddo_users.Interface;
using cddo_users.Repositories;

namespace cddo_users.Services.ApplicationInsights;

internal class AnonymizedUserInformationPopulation(
    ILogger<AnonymizedUserInformationPopulation> logger,
    IUserRepository userRepository) : IAnonymizedUserInformationPopulation
{
    #region Types
    private sealed class UserIdValues
    {
        public required string OrganisationId { get; init; }
        public required string DomainId { get; init; }
        public required string UserId { get; init; }
    }

    private sealed class UserNameValues
    {
        public required string OrganisationName { get; init; }
        public required string DomainName { get; init; }
        public required string UserName { get; init; }
    }
    #endregion

    private readonly Dictionary<string, UserNameValues> deAnonymizedUserNameValuesCache = new();

    async Task<IEnumerable<ITelemetryQueryResultsTableRow>> IAnonymizedUserInformationPopulation.PopulateAnonymizedUserInformationAsync(
        IEnumerable<ITelemetryQueryResultsTableRow> rows)
    {
        deAnonymizedUserNameValuesCache.Clear();

        return await Task.Run(() => rows.Select(async row => await PopulateAnonymizedUserInformationInRowAsync(row))
            .Select(x => x.Result)
            .ToList());

        async Task<ITelemetryQueryResultsTableRow> PopulateAnonymizedUserInformationInRowAsync(ITelemetryQueryResultsTableRow row)
        {
            // User information is stored within custom Properties.  So first get those properties, and if there
            // are none then the row cannot contain anonymized user information
            var propertiesValue = row.RowValues.FirstOrDefault(x =>
                x.ValueName.Equals("Properties", StringComparison.InvariantCultureIgnoreCase));

            if (propertiesValue is not {Value: TelemetryQueryResultsTableRowProperties properties}) return row;

            var storedUserIds = GetStoredUserIds(properties);
            var storedUserNames = GetStoredUserNames(properties);

            try
            {
                await DeAnonymizeUserInformationIfApplicableAsync(properties, storedUserIds, storedUserNames);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception thrown de-anonymizing log event");
            }
            
            return row;
        }
    }

    private static UserIdValues GetStoredUserIds(TelemetryQueryResultsTableRowProperties properties)
    {
        properties.TryGetValue("OrganisationId", out var organisationIdValue);
        properties.TryGetValue("DomainId", out var domainIdValue);
        properties.TryGetValue("UserId", out var userIdValue);

        var organisationId = organisationIdValue?.ToString();
        var domainId = domainIdValue?.ToString();
        var userId = userIdValue?.ToString();

        return new UserIdValues
        {
            OrganisationId = organisationId ?? string.Empty,
            DomainId = domainId ?? string.Empty,
            UserId = userId ?? string.Empty
        };
    }

    private static UserNameValues GetStoredUserNames(TelemetryQueryResultsTableRowProperties properties)
    {
        properties.TryGetValue("OrganisationName", out var organisationNameValue);
        properties.TryGetValue("DomainName", out var domainNameValue);
        properties.TryGetValue("UserName", out var userNameValue);

        var organisationName = organisationNameValue?.ToString();
        var domainName = domainNameValue?.ToString();
        var userName = userNameValue?.ToString();

        return new UserNameValues
        {
            OrganisationName = organisationName ?? string.Empty,
            DomainName = domainName ?? string.Empty,
            UserName = userName ?? string.Empty
        };
    }

    private async Task DeAnonymizeUserInformationIfApplicableAsync(
        TelemetryQueryResultsTableRowProperties properties,
        UserIdValues storedUserIdValues,
        UserNameValues storedUserNameValues)
    {
        if (!RowShouldBeDeAnonymized(storedUserIdValues, storedUserNameValues)) return;

        var deAnonymizedUserNameValuesCacheKey = BuildDeAnonymizedUserNameValuesCacheKey(storedUserIdValues);

        if (!deAnonymizedUserNameValuesCache.TryGetValue(deAnonymizedUserNameValuesCacheKey, out var deAnonymizedUserNameValues))
        {
            deAnonymizedUserNameValues = await GetDeAnonymizedUserNameValuesAsync(storedUserIdValues);

            deAnonymizedUserNameValuesCache[deAnonymizedUserNameValuesCacheKey] = deAnonymizedUserNameValues;
        }

        PopulateDeAnonymizedUserInformation(properties, deAnonymizedUserNameValues);
    }

    private static bool RowShouldBeDeAnonymized(
        UserIdValues storedUserIdValues,
        UserNameValues storedUserNameValues)
    {
        var notPopulatedUserIdValues = new List<string>{ "-1" };

        // If there is not a stored user id then the user information cannot be de-anonymized
        if (string.IsNullOrWhiteSpace(storedUserIdValues.OrganisationId) ||
            string.IsNullOrWhiteSpace(storedUserIdValues.DomainId) ||
            string.IsNullOrWhiteSpace(storedUserIdValues.UserId)) return false;

        // If any of the stored user ids are a known 'not populated' value then it cannot be de-anonymized
        if (notPopulatedUserIdValues.Contains(storedUserIdValues.OrganisationId) ||
            notPopulatedUserIdValues.Contains(storedUserIdValues.DomainId) ||
            notPopulatedUserIdValues.Contains(storedUserIdValues.UserId)) return false;

        // The user information can be de-anonymized if any of the name values are not populated
        return string.IsNullOrWhiteSpace(storedUserNameValues.OrganisationName) ||
               string.IsNullOrWhiteSpace(storedUserNameValues.DomainName) ||
               string.IsNullOrWhiteSpace(storedUserNameValues.UserName);
    }

    private async Task<UserNameValues> GetDeAnonymizedUserNameValuesAsync(UserIdValues storedUserIdValues)
    {
        var userProfile = await userRepository.GetUserByIdAsync(storedUserIdValues.UserId);

        if (userProfile.User.UserId.ToString() != storedUserIdValues.UserId ||
            userProfile.Domain.DomainId.ToString() != storedUserIdValues.DomainId ||
            userProfile.Organisation.OrganisationId.ToString() != storedUserIdValues.OrganisationId)
        {
            throw new InvalidOperationException("De-Anonymized user profile information obtained from database does not match that in the analytics log event");
        }

        return new UserNameValues
        {
            OrganisationName = userProfile.Organisation.OrganisationName,
            DomainName = userProfile.Domain.DomainName,
            UserName = userProfile.User.UserName
        };
    }

    private static void PopulateDeAnonymizedUserInformation(
        TelemetryQueryResultsTableRowProperties properties,
        UserNameValues deAnonymizedUserNameValues)
    {
        properties["OrganisationName"] = deAnonymizedUserNameValues.OrganisationName;
        properties["DomainName"] = deAnonymizedUserNameValues.DomainName;
        properties["UserName"] = deAnonymizedUserNameValues.UserName;
    }

    private static string BuildDeAnonymizedUserNameValuesCacheKey(
        UserIdValues userIdValues)
    {
        return $"{userIdValues.OrganisationId}-{userIdValues.DomainId}-{userIdValues.UserId}";
    }
}