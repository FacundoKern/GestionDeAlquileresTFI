using System.Linq.Expressions;
using GestionAlquileres.Web.Data.Auditoria;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Data;

public class AppDbContext : IdentityDbContext<Usuario>
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _reloj;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IUsuarioActual? usuarioActual = null,
        TimeProvider? reloj = null)
        : base(options)
    {
        _usuarioActual = usuarioActual ?? UsuarioSistema.Instancia;
        _reloj = reloj ?? TimeProvider.System;
    }

    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<PropiedadPropietario> PropiedadesPropietarios => Set<PropiedadPropietario>();
    public DbSet<FotoPropiedad> FotosPropiedades => Set<FotoPropiedad>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Garante> Garantes => Set<Garante>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<ContratoInquilino> ContratosInquilinos => Set<ContratoInquilino>();
    public DbSet<ContratoGarante> ContratosGarantes => Set<ContratoGarante>();
    public DbSet<ContratoImpuesto> ContratosImpuestos => Set<ContratoImpuesto>();
    public DbSet<ConfiguracionAjuste> ConfiguracionesAjuste => Set<ConfiguracionAjuste>();
    public DbSet<Indice> Indices => Set<Indice>();
    public DbSet<IndiceValor> IndicesValores => Set<IndiceValor>();
    public DbSet<HistorialMonto> HistorialMontos => Set<HistorialMonto>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<Recibo> Recibos => Set<Recibo>();
    public DbSet<ReciboItem> RecibosItems => Set<ReciboItem>();
    public DbSet<Numerador> Numeradores => Set<Numerador>();
    public DbSet<ConfiguracionMora> ConfiguracionesMora => Set<ConfiguracionMora>();
    public DbSet<Mora> Moras => Set<Mora>();
    public DbSet<Impuesto> Impuestos => Set<Impuesto>();
    public DbSet<ImpuestoVigencia> ImpuestosVigencias => Set<ImpuestoVigencia>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DatosEmpresa> DatosEmpresa => Set<DatosEmpresa>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Montos: decimal(18,2) por defecto. Porcentajes e índices definen su propia precisión.
        // (Aplica también a decimal?.)
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        // Ningún string queda como nvarchar(max) por descuido.
        configurationBuilder.Properties<string>().HaveMaxLength(256);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var tipo in modelBuilder.Model.GetEntityTypes())
        {
            var clr = tipo.ClrType;

            if (typeof(EntidadAuditable).IsAssignableFrom(clr))
            {
                modelBuilder.Entity(clr).Property(nameof(EntidadAuditable.RowVersion)).IsRowVersion();
                modelBuilder.Entity(clr).Property(nameof(EntidadAuditable.CreadoPor)).IsRequired();
            }

            if (typeof(IBorradoLogico).IsAssignableFrom(clr) && tipo.BaseType is null)
            {
                tipo.SetQueryFilter(CrearFiltroNoEliminado(clr));
            }

            // Con borrado lógico nunca se borran filas principales: ninguna FK propia borra en cascada.
            // (Las tablas de Identity conservan su comportamiento original.)
            if (clr.Namespace?.StartsWith("GestionAlquileres", StringComparison.Ordinal) == true)
            {
                foreach (var fk in tipo.GetForeignKeys())
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        GuardarConAuditoriaAsync(acceptAllChangesOnSuccess, sincronico: true, CancellationToken.None)
            .GetAwaiter().GetResult();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        GuardarConAuditoriaAsync(acceptAllChangesOnSuccess, sincronico: false, cancellationToken);

    /// <summary>
    /// Guarda los cambios y su AuditLog en la MISMA transacción. Se guarda en dos pasos porque el
    /// AuditLog necesita los Id generados por la base para las filas nuevas.
    /// </summary>
    private async Task<int> GuardarConAuditoriaAsync(bool aceptarCambios, bool sincronico, CancellationToken ct)
    {
        var procesador = new ProcesadorCambios(_usuarioActual, _reloj.AhoraUtc());
        var cambios = procesador.Procesar(ChangeTracker);

        if (cambios.Count == 0)
        {
            return sincronico ? base.SaveChanges(aceptarCambios) : await base.SaveChangesAsync(aceptarCambios, ct);
        }

        if (!aceptarCambios)
        {
            // El segundo guardado volvería a enviar los cambios no aceptados.
            throw new NotSupportedException("SaveChanges(acceptAllChangesOnSuccess: false) no está soportado con auditoría.");
        }

        var transaccionPropia = Database.CurrentTransaction is null
            ? (sincronico ? Database.BeginTransaction() : await Database.BeginTransactionAsync(ct))
            : null;

        try
        {
            var resultado = sincronico ? base.SaveChanges(true) : await base.SaveChangesAsync(true, ct);

            AuditLogs.AddRange(cambios.Select(procesador.CrearRegistro));
            _ = sincronico ? base.SaveChanges(true) : await base.SaveChangesAsync(true, ct);

            if (transaccionPropia is not null)
            {
                if (sincronico)
                {
                    transaccionPropia.Commit();
                }
                else
                {
                    await transaccionPropia.CommitAsync(ct);
                }
            }

            return resultado;
        }
        finally
        {
            transaccionPropia?.Dispose();
        }
    }

    private static LambdaExpression CrearFiltroNoEliminado(Type tipo)
    {
        var parametro = Expression.Parameter(tipo, "e");
        var eliminado = Expression.Property(parametro, nameof(IBorradoLogico.Eliminado));
        return Expression.Lambda(Expression.Not(eliminado), parametro);
    }
}
