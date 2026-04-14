namespace Solidus.Registros.API.Infrastructure.Persistence;

public sealed class UnitOfWork(RegistrosDbContext context) : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
