using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Solidus.Registros.API.Infrastructure.Persistence;

public sealed class RegistrosDbContextFactory : IDesignTimeDbContextFactory<RegistrosDbContext>
{
    public RegistrosDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveProjectDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Registros")
            ?? throw new InvalidOperationException("Connection string 'Registros' nao encontrada para design-time.");

        var options = new DbContextOptionsBuilder<RegistrosDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new RegistrosDbContext(options);
    }

    private static string ResolveProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Solidus.Registros.API.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Nao foi possivel localizar o diretorio do projeto Solidus.Registros.API.");
    }
}
