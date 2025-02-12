using cddo_users.DTOs;
using cddo_users.Interface;

namespace cddo_users.Services.ApplicationInsights;

internal class LogsQueryFilterProvision(
    IConfiguration configuration) : ILogsQueryFilterProvision
{
    private readonly List<string> systemAdministratorRoles =
    [
        "System Administrator"
    ];

    private readonly List<string> organisationAdministratorRoles =
    [
        "Organisation Administrator",
        "Organisation Publisher"
    ];

    ILogsQueryFilter ILogsQueryFilterProvision.ProvisionLogsQueryFilter(
        UserProfile userProfile)
    {
        ArgumentNullException.ThrowIfNull(userProfile);

        var userRoleNames = userProfile.Roles.Select(x => x.RoleName).ToList();

        // System Administrative users can access anything
        if (userRoleNames.Any(userRoleName => systemAdministratorRoles.Any(roleName =>
                roleName.Equals(userRoleName, StringComparison.InvariantCultureIgnoreCase))))
        {
            return CreateLogsQueryFilter(
                filterByOrganisation: false,
                filterByDomain: false,
                filterByUser: false);
        }

        // Organisation administrative users can access anything within their organisation and domain
        if (userRoleNames.Any(userRoleName => organisationAdministratorRoles.Any(roleName =>
                roleName.Equals(userRoleName, StringComparison.InvariantCultureIgnoreCase))))
        {
            return CreateLogsQueryFilter(
                filterByOrganisation: true,
                organisationId: userProfile.Organisation.OrganisationId,
                filterByDomain: true,
                domainId: userProfile.Domain.DomainId,
                filterByUser: false);
        }

        // Non-administrative users can only access things relating to themselves
        return CreateLogsQueryFilter(
            filterByOrganisation: true,
            organisationId: userProfile.Organisation.OrganisationId,
            filterByDomain: true,
            domainId: userProfile.Domain.DomainId,
            filterByUser: true,
            userId: userProfile.User.UserId);
    }

    private ILogsQueryFilter CreateLogsQueryFilter(
        bool? filterByOrganisation = null,
        bool? filterByDomain = null,
        bool? filterByUser = null,
        int? organisationId = null,
        int? domainId = null,
        int? userId = null)
    {
        var environmentAppRoleName = configuration["ApplicationInsights:EnvironmentAppRoleName"];

        return new LogsQueryFilter
        {
            FilterByEnvironmentAppRoleName = !string.IsNullOrWhiteSpace(environmentAppRoleName),
            EnvironmentAppRoleName = environmentAppRoleName,
            FilterByOrganisation = filterByOrganisation ?? false,
            FilterByDomain = filterByDomain ?? false,
            FilterByUser = filterByUser ?? false,
            OrganisationId = organisationId ?? 0,
            DomainId = domainId ?? 0,
            UserId = userId ?? 0
        };
    }
}