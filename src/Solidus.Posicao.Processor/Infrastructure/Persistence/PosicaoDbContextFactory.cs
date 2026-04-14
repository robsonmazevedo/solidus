using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public sealed class PosicaoDbContextFactory : IDesignTimeDbContextFactory<PosicaoDbContext>
{
    public PosicaoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PosicaoDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=posicao;Username=solidus;Password=solidus_dev")
            .Options;

        return new PosicaoDbContext(options);
    }
}
