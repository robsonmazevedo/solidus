using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Posicao.API.Infrastructure.Persistence;

namespace Solidus.Posicao.API.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(PosicaoReadDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        try
        {
            await context.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
