using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Inquilinos;
using GestionAlquileres.Web.ViewModels.Propiedades;

namespace GestionAlquileres.Tests.Unidad;

public class ValidacionFormulariosTests
{
    /// <summary>Valida como lo hace MVC: atributos y, si pasan, IValidatableObject. Devuelve los campos con error.</summary>
    private static List<string> CamposConError(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores.SelectMany(e => e.MemberNames).ToList();
    }

    private static InquilinoFormViewModel PersonaValida() => new()
    {
        TipoPersona = TipoPersona.Fisica,
        Nombre = "Lucía",
        Apellido = "Sosa",
        TipoDocumento = TipoDocumento.Dni,
        NumeroDocumento = "38.123.456",
    };

    [Fact]
    public void Persona_fisica_valida()
    {
        Assert.Empty(CamposConError(PersonaValida()));
    }

    [Fact]
    public void Persona_fisica_sin_apellido()
    {
        var modelo = PersonaValida();
        modelo.Apellido = " ";

        Assert.Contains(nameof(modelo.Apellido), CamposConError(modelo));
    }

    [Fact]
    public void Persona_juridica_debe_usar_cuit()
    {
        var modelo = PersonaValida();
        modelo.TipoPersona = TipoPersona.Juridica;

        Assert.Contains(nameof(modelo.TipoDocumento), CamposConError(modelo));
    }

    [Fact]
    public void Cuit_con_digito_verificador_incorrecto()
    {
        var modelo = PersonaValida();
        modelo.TipoDocumento = TipoDocumento.Cuit;
        modelo.NumeroDocumento = "20-12345678-5";

        Assert.Contains(nameof(modelo.NumeroDocumento), CamposConError(modelo));
    }

    [Fact]
    public void Email_invalido()
    {
        var modelo = PersonaValida();
        modelo.Email = "no-es-un-email";

        Assert.Contains(nameof(modelo.Email), CamposConError(modelo));
    }

    private static PropiedadFormViewModel Propiedad(params (int Id, decimal? Porcentaje)[] propietarios) => new()
    {
        Calle = "Gurruchaga",
        Numero = "1543",
        Localidad = "Palermo",
        Propietarios = propietarios.Select(p => new PropietarioAsignadoViewModel { PropietarioId = p.Id, Porcentaje = p.Porcentaje }).ToList(),
    };

    [Fact]
    public void Propiedad_con_un_propietario_al_100()
    {
        Assert.Empty(CamposConError(Propiedad((1, 100m))));
    }

    [Fact]
    public void Copropietarios_en_tres_partes_que_suman_100()
    {
        Assert.Empty(CamposConError(Propiedad((1, 33.34m), (2, 33.33m), (3, 33.33m))));
    }

    [Fact]
    public void Propiedad_sin_propietarios()
    {
        var modelo = Propiedad();
        modelo.CompletarFilasVacias(); // filas vacías del formulario: se ignoran

        Assert.Contains("Propietarios[0].PropietarioId", CamposConError(modelo));
    }

    [Fact]
    public void Participaciones_que_no_suman_100()
    {
        Assert.Contains(nameof(PropiedadFormViewModel.Propietarios), CamposConError(Propiedad((1, 50m), (2, 40m))));
    }

    [Fact]
    public void Propietario_repetido()
    {
        Assert.Contains(nameof(PropiedadFormViewModel.Propietarios), CamposConError(Propiedad((1, 50m), (1, 50m))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(120)]
    public void Participacion_fuera_de_rango(decimal porcentaje)
    {
        Assert.Contains("Propietarios[0].Porcentaje", CamposConError(Propiedad((1, porcentaje))));
    }
}
