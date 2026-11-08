using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class ConfiguracionAjusteConfiguration : IEntityTypeConfiguration<ConfiguracionAjuste>
{
    public void Configure(EntityTypeBuilder<ConfiguracionAjuste> builder)
    {
        builder.HasOne(c => c.Contrato)
            .WithOne(c => c.ConfiguracionAjuste)
            .HasForeignKey<ConfiguracionAjuste>(c => c.ContratoId);
        builder.HasOne(c => c.Indice).WithMany().HasForeignKey(c => c.IndiceId);
        builder.Property(c => c.PorcentajeFijo).HasPrecision(9, 4);
        builder.HasIndex(c => c.ProximaFechaAjuste);
        builder.HasQueryFilter(c => !c.Contrato.Eliminado);

        builder.ToTable(t => t.HasCheckConstraint("CK_ConfiguracionAjuste_Periodicidad", "[PeriodicidadMeses] BETWEEN 1 AND 60"));
    }
}

public class IndiceConfiguration : IEntityTypeConfiguration<Indice>
{
    public void Configure(EntityTypeBuilder<Indice> builder)
    {
        builder.Property(i => i.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Descripcion).HasMaxLength(500);
        builder.HasIndex(i => i.Codigo).IsUnique().HasFilter("[Eliminado] = 0");
    }
}

public class IndiceValorConfiguration : IEntityTypeConfiguration<IndiceValor>
{
    public void Configure(EntityTypeBuilder<IndiceValor> builder)
    {
        builder.Property(v => v.Valor).HasPrecision(18, 6);
        builder.HasOne(v => v.Indice).WithMany(i => i.Valores).HasForeignKey(v => v.IndiceId);
        builder.HasIndex(v => new { v.IndiceId, v.Fecha }).IsUnique().HasFilter("[Eliminado] = 0");

        builder.ToTable(t => t.HasCheckConstraint("CK_IndiceValor_Valor", "[Valor] > 0"));
    }
}

public class HistorialMontoConfiguration : IEntityTypeConfiguration<HistorialMonto>
{
    public void Configure(EntityTypeBuilder<HistorialMonto> builder)
    {
        builder.HasOne(h => h.Contrato).WithMany(c => c.HistorialMontos).HasForeignKey(h => h.ContratoId);
        builder.HasOne(h => h.Indice).WithMany().HasForeignKey(h => h.IndiceId);
        builder.Property(h => h.ValorIndiceInicial).HasPrecision(18, 6);
        builder.Property(h => h.ValorIndiceFinal).HasPrecision(18, 6);
        builder.Property(h => h.Coeficiente).HasPrecision(18, 8);
        builder.Property(h => h.Observaciones).HasMaxLength(1000);
        builder.Property(h => h.AplicadoPor).HasMaxLength(256).IsRequired();
        builder.HasIndex(h => new { h.ContratoId, h.FechaVigencia });
        builder.HasQueryFilter(h => !h.Contrato.Eliminado);
    }
}
