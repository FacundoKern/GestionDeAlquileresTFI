using GestionAlquileres.Web.Services.Empresa;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Components;

/// <summary>Pie de página con el contacto de la inmobiliaria y de soporte técnico.</summary>
public class PiePaginaViewComponent : ViewComponent
{
    private readonly IDatosEmpresaService _servicio;

    public PiePaginaViewComponent(IDatosEmpresaService servicio)
    {
        _servicio = servicio;
    }

    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await _servicio.ObtenerAsync(HttpContext.RequestAborted));
}

/// <summary>Recuadro "Sobre la inmobiliaria" de la página de inicio.</summary>
public class ResumenEmpresaViewComponent : ViewComponent
{
    private readonly IDatosEmpresaService _servicio;

    public ResumenEmpresaViewComponent(IDatosEmpresaService servicio)
    {
        _servicio = servicio;
    }

    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await _servicio.ObtenerAsync(HttpContext.RequestAborted));
}
