using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");
        builder.Property(a => a.UsuarioId).HasMaxLength(450);
        builder.Property(a => a.UsuarioNombre).HasMaxLength(256);
        builder.Property(a => a.Entidad).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntidadId).HasMaxLength(100);
        builder.Property(a => a.ValoresAnteriores).HasColumnType("nvarchar(max)");
        builder.Property(a => a.ValoresNuevos).HasColumnType("nvarchar(max)");
        builder.Property(a => a.DireccionIp).HasMaxLength(45);
        builder.Property(a => a.Detalle).HasMaxLength(1000);

        builder.HasIndex(a => a.FechaUtc);
        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
        builder.HasIndex(a => a.UsuarioNombre);
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
    }
}
