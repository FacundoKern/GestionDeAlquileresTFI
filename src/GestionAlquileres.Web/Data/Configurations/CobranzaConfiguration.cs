using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
{
    public void Configure(EntityTypeBuilder<Cuota> builder)
    {
        builder.HasOne(c => c.Contrato).WithMany(c => c.Cuotas).HasForeignKey(c => c.ContratoId);
        builder.HasIndex(c => new { c.ContratoId, c.Periodo }).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasIndex(c => new { c.Estado, c.FechaVencimiento });
    }
}

public class ReciboConfiguration : IEntityTypeConfiguration<Recibo>
{
    public void Configure(EntityTypeBuilder<Recibo> builder)
    {
        builder.HasIndex(r => r.Numero).IsUnique();
        builder.Property(r => r.Observaciones).HasMaxLength(1000);
        builder.Property(r => r.MotivoAnulacion).HasMaxLength(1000);
        builder.Property(r => r.AnuladoPor).HasMaxLength(256);

        builder.HasOne(r => r.Contrato).WithMany(c => c.Recibos).HasForeignKey(r => r.ContratoId);
        builder.HasOne(r => r.Cuota).WithMany(c => c.Recibos).HasForeignKey(r => r.CuotaId);
        builder.HasOne(r => r.ReemplazadoPorRecibo).WithMany().HasForeignKey(r => r.ReemplazadoPorReciboId);
        builder.HasIndex(r => r.FechaPago);
        builder.HasQueryFilter(r => !r.Contrato.Eliminado);

        builder.ToTable(t => t.HasCheckConstraint("CK_Recibo_Anulacion",
            "[Estado] <> 2 OR ([MotivoAnulacion] IS NOT NULL AND [AnuladoEnUtc] IS NOT NULL)"));
    }
}

public class ReciboItemConfiguration : IEntityTypeConfiguration<ReciboItem>
{
    public void Configure(EntityTypeBuilder<ReciboItem> builder)
    {
        builder.Property(i => i.Descripcion).HasMaxLength(250).IsRequired();
        builder.HasOne(i => i.Recibo).WithMany(r => r.Items).HasForeignKey(i => i.ReciboId);
        builder.HasOne(i => i.Impuesto).WithMany().HasForeignKey(i => i.ImpuestoId);
        builder.HasOne(i => i.Mora).WithMany().HasForeignKey(i => i.MoraId);
        builder.HasQueryFilter(i => !i.Recibo.Contrato.Eliminado);
    }
}

public class NumeradorConfiguration : IEntityTypeConfiguration<Numerador>
{
    public void Configure(EntityTypeBuilder<Numerador> builder)
    {
        builder.HasKey(n => n.Clave);
        builder.Property(n => n.Clave).HasMaxLength(30);
    }
}

public class ConfiguracionMoraConfiguration : IEntityTypeConfiguration<ConfiguracionMora>
{
    public void Configure(EntityTypeBuilder<ConfiguracionMora> builder)
    {
        builder.Property(c => c.Valor).HasPrecision(18, 4);
        builder.HasOne(c => c.Contrato).WithMany().HasForeignKey(c => c.ContratoId);
        builder.HasIndex(c => new { c.ContratoId, c.VigenciaDesde });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ConfiguracionMora_Valor", "[Valor] >= 0");
            t.HasCheckConstraint("CK_ConfiguracionMora_DiasGracia", "[DiasGracia] >= 0");
            t.HasCheckConstraint("CK_ConfiguracionMora_Vigencia", "[VigenciaHasta] IS NULL OR [VigenciaHasta] >= [VigenciaDesde]");
        });
    }
}

public class MoraConfiguration : IEntityTypeConfiguration<Mora>
{
    public void Configure(EntityTypeBuilder<Mora> builder)
    {
        builder.Property(m => m.Motivo).HasMaxLength(1000);
        builder.HasOne(m => m.Contrato).WithMany(c => c.Moras).HasForeignKey(m => m.ContratoId);
        builder.HasOne(m => m.Cuota).WithMany(c => c.Moras).HasForeignKey(m => m.CuotaId);
        builder.HasOne(m => m.ConfiguracionMora).WithMany().HasForeignKey(m => m.ConfiguracionMoraId);
        builder.HasIndex(m => m.Estado);
    }
}

public class ImpuestoConfiguration : IEntityTypeConfiguration<Impuesto>
{
    public void Configure(EntityTypeBuilder<Impuesto> builder)
    {
        builder.Property(i => i.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Descripcion).HasMaxLength(500);
        builder.HasIndex(i => i.Nombre).IsUnique().HasFilter("[Eliminado] = 0");
    }
}

public class ImpuestoVigenciaConfiguration : IEntityTypeConfiguration<ImpuestoVigencia>
{
    public void Configure(EntityTypeBuilder<ImpuestoVigencia> builder)
    {
        builder.Property(v => v.Valor).HasPrecision(18, 4);
        builder.HasOne(v => v.Impuesto).WithMany(i => i.Vigencias).HasForeignKey(v => v.ImpuestoId);
        builder.HasIndex(v => new { v.ImpuestoId, v.VigenciaDesde });

        builder.ToTable(t => t.HasCheckConstraint("CK_ImpuestoVigencia_Vigencia", "[VigenciaHasta] IS NULL OR [VigenciaHasta] >= [VigenciaDesde]"));
    }
}
