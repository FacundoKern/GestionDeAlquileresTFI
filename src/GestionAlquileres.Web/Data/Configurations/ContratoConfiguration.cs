using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.Property(c => c.MotivoRescision).HasMaxLength(1000);
        builder.Property(c => c.Observaciones).HasMaxLength(2000);

        builder.HasOne(c => c.Propiedad).WithMany(p => p.Contratos).HasForeignKey(c => c.PropiedadId);
        builder.HasOne(c => c.ContratoAnterior).WithMany().HasForeignKey(c => c.ContratoAnteriorId);

        builder.HasIndex(c => new { c.Estado, c.FechaFin });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Contrato_DiaVencimiento", "[DiaVencimiento] BETWEEN 1 AND 28");
            t.HasCheckConstraint("CK_Contrato_Fechas", "[FechaFin] > [FechaInicio]");
            t.HasCheckConstraint("CK_Contrato_MontoInicial", "[MontoInicial] > 0");
        });
    }
}

public class ContratoInquilinoConfiguration : IEntityTypeConfiguration<ContratoInquilino>
{
    public void Configure(EntityTypeBuilder<ContratoInquilino> builder)
    {
        builder.HasKey(ci => new { ci.ContratoId, ci.InquilinoId });
        builder.HasOne(ci => ci.Contrato).WithMany(c => c.Inquilinos).HasForeignKey(ci => ci.ContratoId);
        builder.HasOne(ci => ci.Inquilino).WithMany(i => i.Contratos).HasForeignKey(ci => ci.InquilinoId);
        builder.HasQueryFilter(ci => !ci.Contrato.Eliminado && !ci.Inquilino.Eliminado);
    }
}

public class ContratoGaranteConfiguration : IEntityTypeConfiguration<ContratoGarante>
{
    public void Configure(EntityTypeBuilder<ContratoGarante> builder)
    {
        builder.HasKey(cg => new { cg.ContratoId, cg.GaranteId });
        builder.Property(cg => cg.Detalle).HasMaxLength(500);
        builder.HasOne(cg => cg.Contrato).WithMany(c => c.Garantes).HasForeignKey(cg => cg.ContratoId);
        builder.HasOne(cg => cg.Garante).WithMany(g => g.Contratos).HasForeignKey(cg => cg.GaranteId);
        builder.HasQueryFilter(cg => !cg.Contrato.Eliminado && !cg.Garante.Eliminado);
    }
}

public class ContratoImpuestoConfiguration : IEntityTypeConfiguration<ContratoImpuesto>
{
    public void Configure(EntityTypeBuilder<ContratoImpuesto> builder)
    {
        builder.HasKey(ci => new { ci.ContratoId, ci.ImpuestoId });
        builder.HasOne(ci => ci.Contrato).WithMany(c => c.Impuestos).HasForeignKey(ci => ci.ContratoId);
        builder.HasOne(ci => ci.Impuesto).WithMany(i => i.Contratos).HasForeignKey(ci => ci.ImpuestoId);
        builder.HasQueryFilter(ci => !ci.Contrato.Eliminado && !ci.Impuesto.Eliminado);
    }
}
