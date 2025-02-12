using cddo_users.DTOs.EventLogs;

namespace cddo_users.Interface;

public interface IAnonymizedUserInformationPopulation
{
    Task<IEnumerable<ITelemetryQueryResultsTableRow>> PopulateAnonymizedUserInformationAsync(
        IEnumerable<ITelemetryQueryResultsTableRow> rows);
}