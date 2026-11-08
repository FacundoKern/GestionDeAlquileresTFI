using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public abstract class PersonaConfiguration<T> : IEntityTypeConfiguration<T> where T : Persona
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Apellido).HasMaxLength(100);
        builder.Property(p => p.NumeroDocumento).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Telefono).HasMaxLength(50);
        builder.Property(p => p.Domicilio).HasMaxLength(250);
        builder.Property(p => p.Notas).HasMaxLength(2000);

        // Único entre los no eliminados: permite volver a cargar a alguien que se borró lógicamente.
        builder.HasIndex(p => new { p.TipoDocumento, p.NumeroDocumento })
            .IsUnique()
            .HasFilter("[Eliminado] = 0");
        builder.HasIndex(p => new { p.Apellido, p.Nombre });
    }
}

public class PropietarioConfiguration : PersonaConfiguration<Propietario>;

public class InquilinoConfiguration : PersonaConfiguration<Inquilino>;

public class GaranteConfiguration : PersonaConfiguration<Garante>;
