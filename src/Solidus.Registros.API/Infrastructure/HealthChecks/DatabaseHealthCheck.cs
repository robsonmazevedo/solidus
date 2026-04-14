using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Registros.API.Infrastructure.Persistence;

namespace Solidus.Registros.API.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(RegistrosDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx,
        CancellationToken cancellationToken = default)
        => await context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Não foi possível conectar ao banco de dados");
}
