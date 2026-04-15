using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(PosicaoDbContext dbContext) : IHealthCheck
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
