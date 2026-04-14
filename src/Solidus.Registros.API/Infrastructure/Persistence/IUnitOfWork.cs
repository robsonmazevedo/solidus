namespace Solidus.Registros.API.Infrastructure.Persistence;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
