using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

public class InicioController : AdministracionControllerBase
{
    public IActionResult Index() => View();
}
