using System.Threading.RateLimiting;
using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GestionAlquileres.Web.Infrastructure.Configuracion;

public static class SeguridadExtensions
{
    public const string PoliticaRateLimitLogin = "login";

    /// <summary>Duración de la sesión cuando se marca "Recordarme" (la normal es de 1 hora sin actividad).</summary>
    public static readonly TimeSpan DuracionSesionRecordada = TimeSpan.FromDays(7);

    public static IServiceCollection AgregarSeguridad(this IServiceCollection services)
    {
        // AddIdentity (no AddDefaultIdentity): sin UI por defecto, por lo tanto sin registro público.
        services.AddIdentity<Usuario, IdentityRole>(opciones =>
            {
                opciones.Password.RequiredLength = 12;
                opciones.Password.RequiredUniqueChars = 5;
                opciones.Password.RequireDigit = true;
                opciones.Password.RequireLowercase = true;
                opciones.Password.RequireUppercase = true;
                opciones.Password.RequireNonAlphanumeric = true;

                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                opciones.User.RequireUniqueEmail = true;
                opciones.SignIn.RequireConfirmedAccount = false; // los usuarios los crea el administrador
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<IdentityErrorDescriberEspanol>()
            .AddClaimsPrincipalFactory<FabricaClaimsUsuario>();

        services.ConfigureApplicationCookie(opciones =>
        {
            opciones.Cookie.Name = "__Host-GestionAlquileresTFI.Sesion";
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opciones.Cookie.SameSite = SameSiteMode.Strict;
            opciones.ExpireTimeSpan = TimeSpan.FromHours(1);
            opciones.SlidingExpiration = true;
            opciones.LoginPath = "/Cuenta/IniciarSesion";
            opciones.LogoutPath = "/Cuenta/CerrarSesion";
            opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
            opciones.ReturnUrlParameter = "returnUrl";

            // Con "Recordarme" la sesión dura 7 días en lugar de 1 hora. La renovación automática
            // (SlidingExpiration) conserva el plazo original, así que se mantiene en 7 días desde el último uso.
            // Desactivar el usuario o cambiarle la contraseña la invalida igual (SecurityStampValidator).
            opciones.Events.OnSigningIn = contexto =>
            {
                if (contexto.Properties.IsPersistent && contexto.Properties.IssuedUtc is { } emitida)
                {
                    contexto.Properties.ExpiresUtc = emitida.Add(DuracionSesionRecordada);
                }

                return Task.CompletedTask;
            };
        });

        // Si el administrador desactiva un usuario o le cambia el rol, la sesión abierta se invalida en minutos.
        services.Configure<SecurityStampValidatorOptions>(opciones => opciones.ValidationInterval = TimeSpan.FromMinutes(5));

        services.AddAuthorization(opciones =>
        {
            // Todo requiere usuario autenticado salvo lo marcado explícitamente con [AllowAnonymous].
            opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

            opciones.AddPolicy(Politicas.Lectura, p => p.RequireRole(Roles.Administrador, Roles.Operador, Roles.Consulta));
            opciones.AddPolicy(Politicas.Gestion, p => p.RequireRole(Roles.Administrador, Roles.Operador));
            opciones.AddPolicy(Politicas.Administracion, p => p.RequireRole(Roles.Administrador));
            opciones.AddPolicy(Politicas.DatosPersonalesCompletos, p => p.RequireRole(Roles.Administrador, Roles.Operador));
            opciones.AddPolicy(Politicas.InformacionContratos, p => p.RequireRole(Roles.Administrador, Roles.Operador));
        });

        // Límite de intentos de login por IP (además del bloqueo por usuario de Identity).
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.AddPolicy(PoliticaRateLimitLogin, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
