using System.Net;
using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Integracion;

/// <summary>
/// Permisos por rol verificados contra la aplicación real (no alcanza con ocultar botones:
/// el servidor tiene que rechazar el pedido).
/// </summary>
[Collection(ColeccionAplicacion.Nombre)]
public class PermisosTests
{
    private const string Admin = Roles.Administrador;
    private const string Oper = Roles.Operador;
    private const string Cons = Roles.Consulta;

    private readonly AplicacionFixture _app;

    public PermisosTests(AplicacionFixture app)
    {
        _app = app;
    }

    public static TheoryData<string> PaginasPrivadas => new()
    {
        "/", "/Propietarios", "/Propiedades", "/Inquilinos", "/Garantes", "/Administracion/Usuarios", "/Administracion/Auditoria",
    };

    [Theory]
    [MemberData(nameof(PaginasPrivadas))]
    public async Task Sin_sesion_todo_redirige_al_inicio_de_sesion(string url)
    {
        var respuesta = await _app.Anonimo.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/IniciarSesion", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task La_pagina_de_inicio_de_sesion_es_publica()
    {
        var respuesta = await _app.Anonimo.GetAsync("/Cuenta/IniciarSesion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    /// <summary>Qué roles pueden abrir cada página.</summary>
    public static TheoryData<string, string[]> Matriz => new()
    {
        // Lectura: todos los roles.
        { "/", [Admin, Oper, Cons] },
        { "/Propietarios", [Admin, Oper, Cons] },
        { "/Propiedades", [Admin, Oper, Cons] },
        { "/Inquilinos", [Admin, Oper, Cons] },
        { "/Garantes", [Admin, Oper, Cons] },
        { "/Reportes", [Admin, Oper, Cons] },

        // Gestión: Administrador y Operador.
        { "/Propietarios/Crear", [Admin, Oper] },
        { "/Propiedades/Crear", [Admin, Oper] },
        { "/Inquilinos/Crear", [Admin, Oper] },
        { "/Garantes/Crear", [Admin, Oper] },
        { "/Pagos", [Admin, Oper] },

        // Administración: solo Administrador.
        { "/Administracion", [Admin] },
        { "/Administracion/Contratos", [Admin] },
        { "/Administracion/Ajustes", [Admin] },
        { "/Administracion/Mora", [Admin] },
        { "/Administracion/Indices", [Admin] },
        { "/Administracion/ImpuestosMora", [Admin] },
        { "/Administracion/DatosEmpresa", [Admin] },
        { "/Administracion/Usuarios", [Admin] },
        { "/Administracion/Usuarios/Crear", [Admin] },
        { "/Administracion/Auditoria", [Admin] },
    };

    [Theory]
    [MemberData(nameof(Matriz))]
    public async Task Cada_rol_entra_solo_a_lo_que_le_corresponde(string url, string[] permitidos)
    {
        foreach (var rol in Roles.Todos)
        {
            var respuesta = await _app.ClienteDe(rol).GetAsync(url);

            if (permitidos.Contains(rol))
            {
                Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{rol} debería poder abrir {url} (devolvió {respuesta.StatusCode})");
            }
            else
            {
                Assert.True(respuesta.StatusCode == HttpStatusCode.Redirect, $"{rol} no debería poder abrir {url} (devolvió {respuesta.StatusCode})");
                Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", respuesta.Headers.Location!.ToString());
            }
        }
    }

    [Fact]
    public async Task Consulta_no_puede_crear_aunque_envie_el_formulario_a_mano()
    {
        var dni = "39111222";
        var respuesta = await AplicacionFixture.EnviarFormularioAsync(_app.Consulta, "/Propietarios", "/Propietarios/Crear",
            new Dictionary<string, string>
            {
                ["TipoPersona"] = "Fisica",
                ["Nombre"] = "Intruso",
                ["Apellido"] = "Consulta",
                ["TipoDocumento"] = "Dni",
                ["NumeroDocumento"] = dni,
            });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", respuesta.Headers.Location!.ToString());
        Assert.False(await _app.ConsultarAsync(db => db.Propietarios.AnyAsync(p => p.NumeroDocumento == dni)));
    }

    [Fact]
    public async Task Operador_no_puede_gestionar_usuarios_aunque_envie_el_formulario_a_mano()
    {
        var email = "intruso@tests.test";
        var respuesta = await AplicacionFixture.EnviarFormularioAsync(_app.Operador, "/Propietarios", "/Administracion/Usuarios/Crear",
            new Dictionary<string, string>
            {
                ["NombreCompleto"] = "Intruso",
                ["Email"] = email,
                ["Rol"] = Roles.Administrador,
                ["Clave"] = "Intruso#123456",
                ["ConfirmarClave"] = "Intruso#123456",
            });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", respuesta.Headers.Location!.ToString());
        Assert.False(await _app.ConsultarAsync(db => db.Users.AnyAsync(u => u.Email == email)));
    }

    [Fact]
    public async Task Un_formulario_sin_token_antiforgery_se_rechaza()
    {
        var respuesta = await _app.Administrador.PostAsync("/Propietarios/Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TipoPersona"] = "Fisica",
            ["Nombre"] = "Sin",
            ["Apellido"] = "Token",
            ["TipoDocumento"] = "Dni",
            ["NumeroDocumento"] = "39333444",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.False(await _app.ConsultarAsync(db => db.Propietarios.AnyAsync(p => p.NumeroDocumento == "39333444")));
    }

    [Fact]
    public async Task El_administrador_no_puede_desactivarse_a_si_mismo()
    {
        var id = await _app.ConsultarAsync(db => db.Users.Where(u => u.Email == AplicacionFixture.EmailDe(Admin)).Select(u => u.Id).SingleAsync());

        await AplicacionFixture.EnviarFormularioAsync(_app.Administrador, $"/Administracion/Usuarios/Editar/{id}",
            $"/Administracion/Usuarios/Desactivar/{id}", new Dictionary<string, string>());

        Assert.True(await _app.ConsultarAsync(db => db.Users.Where(u => u.Id == id).Select(u => u.Activo).SingleAsync()));
    }
}
