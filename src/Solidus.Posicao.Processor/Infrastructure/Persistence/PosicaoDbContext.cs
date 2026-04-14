using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Domain.Entities;

namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public sealed class PosicaoDbContext(DbContextOptions<PosicaoDbContext> options) : DbContext(options)
{
    public DbSet<PosicaoDiaria> PosicaoDiaria => Set<PosicaoDiaria>();
    public DbSet<EventoProcessado> EventosProcessados => Set<EventoProcessado>();

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
            entity.Property(e => e.TotalCreditos).HasColumnName("total_creditos").HasColumnType("numeric(18,2)").IsRequired()
                .HasDefaultValueSql("0");
            entity.Property(e => e.TotalDebitos).HasColumnName("total_debitos").HasColumnType("numeric(18,2)").IsRequired()
                .HasDefaultValueSql("0");
            entity.Property(e => e.Saldo).HasColumnName("saldo").HasColumnType("numeric(18,2)").IsRequired()
                .HasDefaultValueSql("0");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em").HasColumnType("timestamptz").IsRequired()
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.ComercianteId, e.DataPosicao }).IsUnique();
        });

        modelBuilder.Entity<EventoProcessado>(entity =>
        {
            entity.ToTable("eventos_processados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.EventoId).HasColumnName("evento_id").HasColumnType("uuid").IsRequired();
            entity.Property(e => e.TipoEvento).HasColumnName("tipo_evento").HasColumnType("varchar(100)").IsRequired();
            entity.Property(e => e.ProcessadoEm).HasColumnName("processado_em").HasColumnType("timestamptz").IsRequired()
                .HasDefaultValueSql("now()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.EventoId).IsUnique();
        });
    }
}
