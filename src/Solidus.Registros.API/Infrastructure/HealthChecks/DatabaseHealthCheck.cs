using Microsoft.Extensions.Diagnostics.HealthChecks;
using Solidus.Registros.API.Infrastructure.Persistence;

namespace Solidus.Registros.API.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(RegistrosDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Não foi possível conectar ao banco de dados");
}
