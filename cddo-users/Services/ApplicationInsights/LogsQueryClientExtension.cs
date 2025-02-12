using Azure.Identity;
using Azure.Monitor.Query;

namespace cddo_users.Services.ApplicationInsights;

public static class LogsQueryClientExtension
{
    public static IServiceCollection AddLogsQueryClient(this IServiceCollection services)
    {
        services.AddSingleton(new LogsQueryClient(new DefaultAzureCredential()));

        return services;
    }
}