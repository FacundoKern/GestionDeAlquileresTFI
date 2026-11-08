using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.Services.Garantes;
using GestionAlquileres.Web.Services.Inquilinos;
using GestionAlquileres.Web.Services.Propiedades;
using GestionAlquileres.Web.Services.Propietarios;
using GestionAlquileres.Web.Services.Empresa;
using GestionAlquileres.Web.Services.Fotos;
using GestionAlquileres.Web.Services.Inicio;
using GestionAlquileres.Web.Services.Usuarios;

namespace GestionAlquileres.Web.Infrastructure.Configuracion;

public static class ServiciosExtensions
{
    /// <summary>Servicios de negocio (la lógica vive acá, no en los controladores).</summary>
    public static IServiceCollection AgregarServiciosDeNegocio(this IServiceCollection services)
    {
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<IVisorAuditoriaService, VisorAuditoriaService>();
        services.AddScoped<IUsuariosService, UsuariosService>();
        services.AddMemoryCache();
        services.AddScoped<IDatosEmpresaService, DatosEmpresaService>();
        services.AddScoped<ITableroService, TableroService>();
        services.AddScoped<IResumenContratosService, ResumenContratosService>();
        services.AddScoped<IPropietariosService, PropietariosService>();
        services.AddScoped<IPropiedadesService, PropiedadesService>();
        services.AddSingleton<IAlmacenFotos, AlmacenFotosEnDisco>();
        services.AddScoped<IFotosPropiedadesService, FotosPropiedadesService>();
        services.AddScoped<IInquilinosService, InquilinosService>();
        services.AddScoped<IGarantesService, GarantesService>();
        return services;
    }
}
