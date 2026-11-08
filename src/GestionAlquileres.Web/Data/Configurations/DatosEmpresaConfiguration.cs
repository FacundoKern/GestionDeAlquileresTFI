using GestionAlquileres.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Web.Data.Configurations;

public class DatosEmpresaConfiguration : IEntityTypeConfiguration<DatosEmpresa>
{
    public void Configure(EntityTypeBuilder<DatosEmpresa> builder)
    {
        builder.ToTable("DatosEmpresa");
        builder.Property(d => d.NombreComercial).HasMaxLength(150).IsRequired();
        builder.Property(d => d.RazonSocial).HasMaxLength(150);
        builder.Property(d => d.Cuit).HasMaxLength(11);
        builder.Property(d => d.MatriculaCorredor).HasMaxLength(50);
        builder.Property(d => d.Direccion).HasMaxLength(200);
        builder.Property(d => d.Localidad).HasMaxLength(100);
        builder.Property(d => d.Provincia).HasMaxLength(60);
        builder.Property(d => d.CodigoPostal).HasMaxLength(10);
        builder.Property(d => d.Telefono).HasMaxLength(50);
        builder.Property(d => d.Whatsapp).HasMaxLength(50);
        builder.Property(d => d.Email).HasMaxLength(256);
        builder.Property(d => d.SitioWeb).HasMaxLength(256);
        builder.Property(d => d.HorarioAtencion).HasMaxLength(200);
        builder.Property(d => d.Descripcion).HasMaxLength(1000);
        builder.Property(d => d.SoporteNombre).HasMaxLength(100);
        builder.Property(d => d.SoporteTelefono).HasMaxLength(50);
        builder.Property(d => d.SoporteEmail).HasMaxLength(256);
    }
}
