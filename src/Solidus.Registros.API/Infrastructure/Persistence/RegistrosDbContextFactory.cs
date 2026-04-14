using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Solidus.Registros.API.Infrastructure.Persistence;

public sealed class RegistrosDbContextFactory : IDesignTimeDbContextFactory<RegistrosDbContext>
{
    public RegistrosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RegistrosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=registros;Username=solidus;Password=solidus_dev")
            .Options;

        return new RegistrosDbContext(options);
    }
}
