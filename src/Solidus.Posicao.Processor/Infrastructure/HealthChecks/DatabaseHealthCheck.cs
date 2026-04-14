using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(PosicaoDbContext context) : IHealthCheck
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
