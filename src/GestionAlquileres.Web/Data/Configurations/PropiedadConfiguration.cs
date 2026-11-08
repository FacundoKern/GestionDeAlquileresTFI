using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class PropiedadConfiguration : IEntityTypeConfiguration<Propiedad>
{
    public void Configure(EntityTypeBuilder<Propiedad> builder)
    {
        builder.Property(p => p.Calle).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Numero).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Piso).HasMaxLength(10);
        builder.Property(p => p.Departamento).HasMaxLength(10);
        builder.Property(p => p.Localidad).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Provincia).HasMaxLength(60).IsRequired();
        builder.Property(p => p.CodigoPostal).HasMaxLength(10);
        builder.Property(p => p.Observaciones).HasMaxLength(2000);

        builder.HasIndex(p => new { p.Calle, p.Numero });
    }
}

public class FotoPropiedadConfiguration : IEntityTypeConfiguration<FotoPropiedad>
{
    public void Configure(EntityTypeBuilder<FotoPropiedad> builder)
    {
        builder.ToTable("FotosPropiedades");
        builder.Property(f => f.Archivo).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Descripcion).HasMaxLength(200);
        builder.Property(f => f.NombreOriginal).HasMaxLength(255);

        builder.HasOne(f => f.Propiedad).WithMany(p => p.Fotos).HasForeignKey(f => f.PropiedadId);

        builder.HasIndex(f => f.Archivo).IsUnique();
        builder.HasIndex(f => new { f.PropiedadId, f.Orden });
    }
}

public class PropiedadPropietarioConfiguration : IEntityTypeConfiguration<PropiedadPropietario>
{
    public void Configure(EntityTypeBuilder<PropiedadPropietario> builder)
    {
        builder.HasKey(pp => new { pp.PropiedadId, pp.PropietarioId });
        builder.Property(pp => pp.Porcentaje).HasPrecision(5, 2);

        builder.HasOne(pp => pp.Propiedad).WithMany(p => p.Propietarios).HasForeignKey(pp => pp.PropiedadId);
        builder.HasOne(pp => pp.Propietario).WithMany(p => p.Propiedades).HasForeignKey(pp => pp.PropietarioId);

        // Coherente con el borrado lógico de ambos extremos.
        builder.HasQueryFilter(pp => !pp.Propiedad.Eliminado && !pp.Propietario.Eliminado);

        builder.ToTable(t => t.HasCheckConstraint("CK_PropiedadPropietario_Porcentaje", "[Porcentaje] > 0 AND [Porcentaje] <= 100"));
    }
}
