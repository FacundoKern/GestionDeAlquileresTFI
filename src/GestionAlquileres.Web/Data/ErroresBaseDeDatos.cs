using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Data;

public static class ErroresBaseDeDatos
{
    /// <summary>
    /// True si el error es una violación de índice único (2601) o clave única (2627). Los servicios validan
    /// duplicados antes de guardar; esto cubre la carrera de dos usuarios guardando a la vez.
    /// </summary>
    public static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
