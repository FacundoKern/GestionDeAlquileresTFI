# Gestión de Alquileres

Sistema web para que una inmobiliaria administre sus alquileres: propietarios, propiedades,
inquilinos, garantes, contratos, ajustes de precio, cobranzas, mora y reportes.

- Interfaz en español, con fechas `dd/MM/yyyy`, moneda argentina y hora de Buenos Aires.
- Accesible (WCAG 2.1 AA): se puede usar con teclado y lector de pantalla, y los estados no dependen solo del color.
- Tres roles: **Administrador**, **Operador** y **Consulta**.

> Estado: **fase 1 terminada** (base del sistema, ABM de personas y propiedades, usuarios y auditoría).
> Ver el plan completo en [CLAUDE.md](CLAUDE.md#plan-de-fases).
>
> Trabajo Final Integrador de la materia **Programación Web**. Los datos que trae la aplicación son ficticios.

## Tecnologías

| Parte | Tecnología |
|---|---|
| Aplicación | ASP.NET Core MVC (.NET 8) |
| Datos | Entity Framework Core 8 + SQL Server (LocalDB en desarrollo) |
| Usuarios | ASP.NET Core Identity |
| Pantallas | Razor + Bootstrap 5.3 + Bootstrap Icons (JavaScript mínimo) |
| Fotos | SkiaSharp (validación, orientación, sin metadatos, miniaturas) |
| PDF / Excel | QuestPDF / ClosedXML (desde las fases 3 y 5) |
| Tests | xUnit + WebApplicationFactory |

## Requisitos

- [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).
- SQL Server LocalDB. Viene con Visual Studio 2022 (carga de trabajo "ASP.NET y desarrollo web").
- Visual Studio 2022 (recomendado) o cualquier editor más la terminal.

## Primer arranque

Todos los comandos se corren en la carpeta del proyecto (donde está `GestionAlquileres.sln`), desde
PowerShell o desde la terminal de Visual Studio (**Ver → Terminal**).

**1. Restaurar la herramienta de migraciones** (una vez, y cada vez que cambie su versión):

```powershell
dotnet tool restore
```

**2. Configurar los secretos.** Nunca van en el repositorio: se guardan con User Secrets, solo en tu
computadora. En PowerShell usá **comillas simples** para las contraseñas (con comillas dobles, el `$` se pierde).

```powershell
dotnet user-secrets set "ConnectionStrings:GestionAlquileres" "Server=(localdb)\MSSQLLocalDB;Database=GestionAlquileresTFI;Trusted_Connection=True;TrustServerCertificate=True" --project src/GestionAlquileres.Web
dotnet user-secrets set "Seed:Usuarios:Administrador:Clave" 'UnaClaveLarga#2026' --project src/GestionAlquileres.Web
dotnet user-secrets set "Seed:Usuarios:Operador:Clave" 'OtraClaveLarga#2026' --project src/GestionAlquileres.Web
dotnet user-secrets set "Seed:Usuarios:Consulta:Clave" 'TerceraClave#2026' --project src/GestionAlquileres.Web
```

Las contraseñas necesitan 12 caracteres o más, con mayúsculas, minúsculas, números y algún símbolo.
Para revisar lo que quedó guardado: `dotnet user-secrets list --project src/GestionAlquileres.Web`.

**3. Ejecutar.** Desde Visual Studio, con **F5** (perfil `https`). Desde la terminal:

```powershell
dotnet run --project src/GestionAlquileres.Web --launch-profile https
```

Y abrir <https://localhost:7173>. En desarrollo, el primer arranque crea la base, aplica las
migraciones y carga **datos de prueba ficticios**: 10 contratos en todos los estados posibles, propietarios,
inquilinos, garantes y datos de ejemplo de la inmobiliaria.

**4. Ingresar** con alguno de los usuarios iniciales y la contraseña que configuraste en el paso 2:

| Rol | Email | Qué puede hacer |
|---|---|---|
| Administrador | `admin@inmobiliaria.test` | Todo, incluida la sección Administración |
| Operador | `operador@inmobiliaria.test` | Cargar y modificar datos; no entra a Administración |
| Consulta | `consulta@inmobiliaria.test` | Solo lectura, con datos personales parcialmente ocultos y sin contratos |

## Tests

```powershell
dotnet test
```

Las pruebas crean sus propias bases temporales (`GA_Tests_…`) en LocalDB y las borran al terminar:
nunca tocan la base de desarrollo. Si se corta una ejecución a la mitad, puede quedar alguna; se puede
borrar sin problema. Para usar otro servidor, definí la variable de entorno `GA_TESTS_SERVIDOR`.

## Migraciones

```powershell
# Crear una migración después de cambiar el modelo
dotnet ef migrations add NombreDelCambio --project src/GestionAlquileres.Web --output-dir Data/Migrations

# Aplicarla a mano (en desarrollo también se aplica sola al iniciar)
dotnet ef database update --project src/GestionAlquileres.Web
```

Fuera de desarrollo, las migraciones **no** se aplican solas (`BaseDeDatos:AplicarMigracionesAlIniciar = false`):
se aplican en cada despliegue, con un backup previo.

## Estructura

```
src/GestionAlquileres.Web/
  Areas/Administracion/   Sección solo para Administrador (contratos, ajustes, mora, configuración, usuarios, auditoría)
  Controllers/            Coordinan la petición; la lógica está en Services
  Data/                   DbContext, configuración del modelo, auditoría, migraciones y datos iniciales
  Infrastructure/         Cultura argentina, formatos, documentos (CUIT), enmascarado, tag helpers, configuración
  Models/                 Entidades y enums
  Security/               Roles, políticas, encabezados de seguridad, filtros
  Services/               Reglas de negocio (una carpeta por tema)
  ViewModels/             Lo que reciben y muestran las vistas
  Views/ y wwwroot/       Pantallas, CSS y JS
tests/GestionAlquileres.Tests/
  Unidad/ Persistencia/ Integracion/ Pendientes/ Infraestructura/
```

## Problemas comunes

| Síntoma | Solución |
|---|---|
| "Falta la cadena de conexión 'GestionAlquileres'" | Falta el paso 2. Revisá con `dotnet user-secrets list --project src/GestionAlquileres.Web`. |
| Error 5170 al crear la base ("the file ... already exists") | Quedaron archivos `.mdf`/`.ldf` de una base borrada en `C:\Users\<usuario>`. Renombralos o borralos y volvé a ejecutar. |
| No se crean los usuarios iniciales | La contraseña no cumple los requisitos, o se perdió un `$` por usar comillas dobles en PowerShell. |
| El navegador muestra estilos viejos | Recargá con Ctrl + F5. |
| "dotnet-ef" no encontrado o versión distinta | `dotnet tool restore`. |

## Despliegue

La demo del TFI está en **MonsterASP.NET** (plan gratis: Windows/IIS y SQL Server). La configuración va
en **variables de entorno** del panel (*Scripting → Environment variables*), nunca en archivos del repositorio:

| Variable | Valor |
|---|---|
| `ConnectionStrings__GestionAlquileres` | La cadena de la base MSSQL del panel, con `TrustServerCertificate=True` |
| `BaseDeDatos__AplicarMigracionesAlIniciar` | `true`: cada despliegue actualiza la base solo |
| `Seed__DatosDePrueba` | `true`: carga los datos ficticios de demostración |
| `Seed__ExigirCambioDeClave` | `false`: las credenciales de prueba siguen sirviendo después del primer ingreso |
| `Seed__Usuarios__<Rol>__Email` y `__NombreCompleto` | Uno por rol (`Administrador`, `Operador`, `Consulta`) |
| `Seed__Usuarios__<Rol>__Clave` | La contraseña inicial de cada rol |
| `Fotos__Carpeta` | `../private/fotos`: fuera de `wwwroot` y fuera del alcance de los despliegues |
| `DataProtection__Carpeta` | `../private/keys`: las claves de las cookies sobreviven a los reinicios |

- **HTTPS es obligatorio** (las cookies son `__Host-` y `Secure`). En el plan gratis lo activa el soporte de
  MonsterASP con un ticket, después de publicar por primera vez.
- `web.config` sube el límite de pedido de IIS a 120 MB para la subida de fotos.
- Las **fotos** se guardan en disco, no en la base: **tienen que entrar en los backups**, junto con la base.
- Fuera del TFI (un sistema real), lo correcto es `Seed__DatosDePrueba=false`, que los usuarios iniciales
  cambien la contraseña en su primer ingreso y aplicar las migraciones con un script revisado.

## Licencias de terceros

- Bootstrap y Bootstrap Icons: MIT.
- SkiaSharp: MIT.
- QuestPDF (desde la fase 3): licencia **Community**, gratuita para organizaciones con ingresos brutos
  anuales menores a 1 millón de dólares. Hay que confirmar que la inmobiliaria cumple esa condición.
- ClosedXML (desde la fase 5): MIT.
