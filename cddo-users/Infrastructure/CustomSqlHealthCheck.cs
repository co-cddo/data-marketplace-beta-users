using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace cddo_users.Infrastructure
{
    public class CustomSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public CustomSqlHealthCheck(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    using (var command = new SqlCommand("SELECT 1;", connection))
                    {
                        await command.ExecuteScalarAsync(cancellationToken);
                    }
                }

                var description = "SQL Database is up and running.";
                return HealthCheckResult.Healthy(description);
            }
            catch (Exception ex)
            {
                var description = "SQL Database is not responding.";
                return HealthCheckResult.Unhealthy(description, ex);
            }
        }
    }
}