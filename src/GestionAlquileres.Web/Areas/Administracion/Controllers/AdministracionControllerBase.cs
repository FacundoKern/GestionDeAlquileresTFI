using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

/// <summary>Base de todos los controladores del área: solo el rol Administrador.</summary>
[Area("Administracion")]
[Authorize(Policy = Politicas.Administracion)]
public abstract class AdministracionControllerBase : Controller;
