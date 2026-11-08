using System.Text.Encodings.Web;
using System.Text.Unicode;
using GestionAlquileres.Web.Data.Seed;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opciones => opciones.AddServerHeader = false);

if (!builder.Environment.IsDevelopment())
{
    // Logging estructurado (JSON) fuera de desarrollo. Nunca se loguean datos personales ni contraseñas.
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

builder.Services.AddSingleton(TimeProvider.System);

// Razor escribe acentos y ñ tal cual (no como &#xED;). Los caracteres peligrosos (< > & " ') se siguen escapando.
builder.Services.Configure<WebEncoderOptions>(opciones =>
    opciones.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AgregarLocalizacionArgentina();
builder.Services.AgregarPersistencia(builder.Configuration);
builder.Services.AgregarSeguridad();
builder.Services.AgregarServiciosDeNegocio();
builder.Services.Configure<EmpresaOptions>(builder.Configuration.GetSection(EmpresaOptions.Seccion));
builder.Services.Configure<SemaforoOptions>(builder.Configuration.GetSection(SemaforoOptions.Seccion));
builder.Services.Configure<FotosOptions>(builder.Configuration.GetSection(FotosOptions.Seccion));

builder.Services.AddControllersWithViews(opciones =>
{
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    opciones.Filters.Add<CambioClaveObligatorioFilter>();
    LocalizacionExtensions.TraducirMensajesDeBinding(opciones.ModelBindingMessageProvider);
});

builder.Services.AddAntiforgery(opciones =>
{
    opciones.Cookie.Name = "__Host-GestionAlquileresTFI.Antiforgery";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHsts(opciones =>
{
    opciones.MaxAge = TimeSpan.FromDays(365);
    opciones.IncludeSubDomains = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Inicio/Estado/{0}");
app.UseHttpsRedirection();
app.UseMiddleware<EncabezadosSeguridadMiddleware>();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Inicio}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

await app.InicializarBaseDeDatosAsync();
await app.RunAsync();

/// <summary>Expuesto para los tests de integración (WebApplicationFactory).</summary>
public partial class Program;
