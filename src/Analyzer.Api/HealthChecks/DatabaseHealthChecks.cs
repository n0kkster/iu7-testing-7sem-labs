using Analyzer.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo4j.Driver;

namespace Analyzer.Api.HealthChecks;

public class PostgresHealthCheck(AnalyzerDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            
            return canConnect 
                ? HealthCheckResult.Healthy() 
                : HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}

public class Neo4jHealthCheck(IDriver driver) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await driver.VerifyConnectivityAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}