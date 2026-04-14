namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public sealed class UnitOfWork(PosicaoDbContext context) : IUnitOfWork
{
    public Task CommitAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
