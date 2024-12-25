using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace VentyTime.Server.HealthChecks
{
    public class SqlConnectionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlConnectionHealthCheck> _logger;
        private const int TimeoutSeconds = 30;

        public SqlConnectionHealthCheck(
            string connectionString,
            ILogger<SqlConnectionHealthCheck> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                await using var connection = new SqlConnection(_connectionString);
                
                // Встановлюємо timeout
                connection.ConnectionString += $";Connection Timeout={TimeoutSeconds}";
                
                _logger.LogInformation("Attempting to connect to database...");
                
                await connection.OpenAsync(cancellationToken);
                
                // Перевіряємо базову функціональність бази даних
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                
                stopwatch.Stop();
                
                var data = new Dictionary<string, object>
                {
                    { "ConnectionTime", stopwatch.ElapsedMilliseconds },
                    { "Status", "Healthy" },
                    { "Timestamp", DateTime.UtcNow }
                };

                _logger.LogInformation(
                    "Database health check completed successfully in {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Healthy("SQL Server connection is healthy", data);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database health check failed");
                
                var data = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "ErrorCode", ex.Number },
                    { "Status", "Unhealthy" },
                    { "Timestamp", DateTime.UtcNow }
                };

                return HealthCheckResult.Unhealthy(
                    "SQL Server connection is unhealthy",
                    ex,
                    data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during database health check");
                
                var data = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "Status", "Unhealthy" },
                    { "Timestamp", DateTime.UtcNow }
                };

                return HealthCheckResult.Unhealthy(
                    "Unexpected error during health check",
                    ex,
                    data);
            }
        }
    }
}
