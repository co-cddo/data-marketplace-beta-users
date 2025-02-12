namespace cddo_users.Interface;

public interface ILogsQueryFilter
{
    bool FilterByEnvironmentAppRoleName { get; }
    string? EnvironmentAppRoleName { get; }

    bool FilterByOrganisation { get; }
    bool FilterByDomain { get; }
    bool FilterByUser { get; }

    int OrganisationId { get; }
    int DomainId { get; }
    int UserId { get; }
}