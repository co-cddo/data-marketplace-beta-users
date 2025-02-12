using System.Diagnostics.CodeAnalysis;
using cddo_users.Interface;

namespace cddo_users.Services.ApplicationInsights;

[ExcludeFromCodeCoverage] // Justification: Little point in unit testing a POCO
internal class LogsQueryFilter : ILogsQueryFilter
{
    public bool FilterByEnvironmentAppRoleName { get; set; }
    public string? EnvironmentAppRoleName { get; set; }

    public bool FilterByOrganisation { get; set; }
    public bool FilterByDomain { get; set; }
    public bool FilterByUser { get; set; }

    public int OrganisationId { get; set; }
    public int DomainId { get; set; }
    public int UserId { get; set; }
}