using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Posicao.API.Infrastructure.Persistence;

namespace Solidus.Posicao.API.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(PosicaoReadDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
