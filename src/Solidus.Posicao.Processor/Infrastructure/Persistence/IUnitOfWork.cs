namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
