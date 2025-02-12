using cddo_users.DTOs;

namespace cddo_users.Interface;

public interface ILogsQueryFilterProvision
{
    ILogsQueryFilter ProvisionLogsQueryFilter(UserProfile userProfile);
}