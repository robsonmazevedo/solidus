using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public sealed class PosicaoDbContextFactory : IDesignTimeDbContextFactory<PosicaoDbContext>
{
    public PosicaoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveProjectDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Posicao")
            ?? throw new InvalidOperationException("Connection string 'Posicao' nao encontrada para design-time.");

        var options = new DbContextOptionsBuilder<PosicaoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PosicaoDbContext(options);
    }

    private static string ResolveProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Solidus.Posicao.Processor.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Nao foi possivel localizar o diretorio do projeto Solidus.Posicao.Processor.");
    }
}
