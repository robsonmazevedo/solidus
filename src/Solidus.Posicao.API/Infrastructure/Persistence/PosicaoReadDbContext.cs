using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.API.Domain.ReadModels;

namespace Solidus.Posicao.API.Infrastructure.Persistence;

public sealed class PosicaoReadDbContext(DbContextOptions<PosicaoReadDbContext> options) : DbContext(options)
{
    public DbSet<PosicaoDiaria> PosicaoDiaria => Set<PosicaoDiaria>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("posicao");

        modelBuilder.Entity<PosicaoDiaria>(entity =>
        {
            entity.ToTable("posicao_diaria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.ComercianteId).HasColumnName("comerciante_id").HasColumnType("uuid").IsRequired();
            entity.Property(e => e.DataPosicao).HasColumnName("data_posicao").IsRequired();
            entity.Property(e => e.TotalCreditos).HasColumnName("total_creditos").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(e => e.TotalDebitos).HasColumnName("total_debitos").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(e => e.Saldo).HasColumnName("saldo").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em").HasColumnType("timestamp").IsRequired();
        });
    }
}
