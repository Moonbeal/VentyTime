using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VentyTime.Server.HealthChecks
{
    public class SqlConnectionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqlConnectionHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (SqlException ex)
            {
                return HealthCheckResult.Unhealthy("SQL Server connection is unhealthy.", ex);
            }
        }
    }
}
