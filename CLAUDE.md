# CLAUDE.md — Gestión de Alquileres

Guía para trabajar en este repositorio: reglas, convenciones, decisiones tomadas y pendientes.
La instalación y el arranque están en el [README](README.md).

Sistema web de gestión de alquileres para una inmobiliaria de Argentina. Esta copia es la base del
**Trabajo Final Integrador de Programación Web** (ver la sección "Trabajo Final Integrador" al final).
Prioridades, en este orden: **seguridad**, **código limpio** y **accesibilidad (WCAG 2.1 AA)**.

## Reglas que no se negocian

- **Secretos:** las cadenas de conexión y las contraseñas van solo en User Secrets (desarrollo) o en
  variables de entorno (producción). Nunca en el repositorio, en `appsettings*.json` ni en los logs.
- **Datos personales** (Ley 25.326): el rol Consulta ve el documento, el teléfono y el email
  parcialmente ocultos, y el ocultamiento se hace **en el servidor**: el dato completo nunca llega al
  navegador. La apertura de cualquier ficha con datos personales se audita. Los logs nunca contienen
  datos personales.
- **Permisos en el servidor:** ocultar un botón no alcanza. Cada acción lleva `[Authorize(Policy = ...)]`
  y la `FallbackPolicy` exige usuario autenticado. Todo POST valida antiforgery (filtro global).
- **Nada se borra físicamente:** borrado lógico (`IBorradoLogico`) y auditoría automática de cada cambio.
  No se elimina lo que tiene historial (por ejemplo, un inquilino con contratos).
- **Idioma y formatos:** la interfaz y el dominio están en español; fechas `dd/MM/yyyy`; moneda `$` / `US$`
  con `1.234,56`; zona `America/Argentina/Buenos_Aires`. Las fechas de auditoría se guardan en UTC y
  se convierten al mostrarlas. Nunca se suman montos de monedas distintas.
- **Accesibilidad:** etiquetas en todos los campos, "(obligatorio)" en texto, errores asociados al
  campo y resumen con foco, tablas con `caption` y `scope`, y estados con texto e ícono además del color.
  Todo funciona sin JavaScript.
- **CSP sin inline:** no hay `<script>` ni `style=""` inline. Todo el JS y el CSS va en `wwwroot`.
- **Recibos:** registran el pago, pero **no son comprobantes fiscales**. La facturación se hace en ARCA.

## Comandos

```powershell
dotnet build
dotnet run --project src/GestionAlquileres.Web --launch-profile https   # https://localhost:7173
dotnet test
dotnet tool restore                                                      # herramienta dotnet-ef 8.0.32
dotnet ef migrations add Nombre --project src/GestionAlquileres.Web --output-dir Data/Migrations
```

- **Paquetes:** se mantienen en la última versión de parche de .NET 8 (hoy 8.0.32). Revisar
  `dotnet list package --vulnerable --include-transitive` antes de cada entrega.
- **Commits:** chicos, con prefijo (`feat:`, `fix:`, `test:`, `chore:`, `docs:`) y mensaje en español.

## Arquitectura

Un solo proyecto MVC con capas por carpeta (ver la estructura en el README):

- **Controllers:** solo coordinan. Los de ABM heredan de `AppControllerBase`.
  - `ObtenerAlcanceDatosAsync()` devuelve `AlcanceDatos(DatosPersonalesCompletos, Contratos)`, que se
    pasa a los servicios.
  - `AgregarError(ResultadoOperacion)` muestra el error junto al campo.
- **Services:** las reglas de negocio. Devuelven `ResultadoOperacion` (`Ok`, `Fallo(msg, campo)`,
  `ConflictoConcurrencia`, `NoEncontrado`) en lugar de lanzar excepciones para errores esperables.
- **ViewModels:** solo exponen los campos editables, para evitar overposting. Las opciones de los
  desplegables llevan `[BindNever]`, y el `Id` siempre sale de la ruta.
- **Data/AppDbContext:** al guardar, `ProcesadorCambios`:
  - convierte las eliminaciones en borrado lógico;
  - completa los datos de creación y modificación;
  - escribe el `AuditLog` (JSON antes/después, solo los campos que cambiaron) en la **misma transacción**;
  - bloquea los cambios a `ISoloAgregar` (AuditLog, HistorialMonto, ReciboItem), a `INoEliminable`
    (Recibo) y a `IInmutableTrasEmision` (un recibo emitido solo se puede anular).
- **Filtros globales** ocultan lo eliminado. Las FK propias son `Restrict`. Los índices únicos de
  documento se filtran con `[Eliminado] = 0`.
- **Concurrencia:** `RowVersion` en base64 en los formularios, asignado como `OriginalValue` antes de guardar.
  Para los usuarios de Identity, el `ConcurrencyStamp` se compara **a mano**, porque `UserStore.UpdateAsync`
  pisa el valor original.
- **Auditoría de eventos** (inicios de sesión, aperturas de fichas, gestión de usuarios): `IAuditoriaService`.
  Usa su propio DbContext de vida corta.
- **Tiempo:** se inyecta `TimeProvider` y se usa `HoyEnArgentina()` / `AhoraUtc()`. Nunca `DateTime.Now`.

## Convenciones de código

- **Búsqueda sin acentos:** `EF.Functions.Collate(col, BusquedaTexto.SinAcentos).Contains(palabra)`,
  escrito **en línea** dentro de la consulta (EF no traduce métodos propios). Las palabras se separan
  con `BusquedaTexto.Palabras(texto)`.
- **Listados:** `ToListaPaginadaAsync` + `Mapear` + el parcial `_Paginacion`, que conserva los filtros.
- **Vistas:**
  - el título va en `ViewData["Title"]` (el layout pone el `<h1>`);
  - las migas se declaran con `ViewData.EstablecerMigas(new Miga(...))`, y si hay migas el layout agrega
    el botón "Volver";
  - los botones de la página van en `@section Acciones`.
- **Personas** (propietario, inquilino, garante):
  - `PersonaFormViewModel` y `PersonaMapeo` (`CopiarA`, `AFormulario`, `ADatos`, `MostrarDocumento/Telefono/Email`);
  - parciales `_CamposPersona`, `_DatosPersona` y `_ConfirmarEliminarPersona`.
- **Contratos en las fichas:** `IResumenContratosService` + `_TablaContratos`, o `_ContratosNoDisponibles`
  para Consulta. El semáforo se calcula **solo** con `CalculadoraSemaforo`, una función pura.
- **Ficha nueva con datos personales:** llamar a `_auditoria.RegistrarConsultaDatosPersonalesAsync`.
- **Configuración** (Administración): las subsecciones se declaran en `SeccionesConfiguracion`, que alimenta
  el menú, la página índice y las migas.
- **Comentarios y documentación XML** en español. Explican el porqué, no lo obvio.

## Roles y permisos

| Política | Administrador | Operador | Consulta |
|---|:-:|:-:|:-:|
| `Lectura`: listados y fichas | ✓ | ✓ | ✓ |
| `Gestion`: crear, editar y eliminar datos operativos y pagos | ✓ | ✓ | |
| `Administracion`: área Administración (contratos, ajustes, mora, configuración, usuarios, auditoría) | ✓ | | |
| `DatosPersonalesCompletos`: documento, teléfono y email completos | ✓ | ✓ | (ocultos) |
| `InformacionContratos`: contratos en fichas y listados, tablero de inicio | ✓ | ✓ | |

- Consulta **sí** ve el estado de una propiedad (alquilada o disponible). Lo decidió el usuario.
- Los filtros por contrato se ignoran en el servidor si el rol no tiene `InformacionContratos`.

**Seguridad de la sesión**
- Cookie `__Host-`, HttpOnly, Secure y SameSite=Strict. Dura 1 hora deslizante, o 7 días con "Recordarme",
  sin aviso ni opción de desactivarlo (decisión del usuario).
- Bloqueo de la cuenta tras 5 intentos fallidos por 15 minutos, y límite de 10 intentos de login por minuto
  por IP. Contraseñas de 12 caracteres o más.
- Al desactivar un usuario, o al cambiarle el rol, el email o la contraseña, se renueva su sello de seguridad
  y sus sesiones abiertas se cierran en 5 minutos o menos.

**Usuarios**
- No hay registro público: los usuarios los crea el Administrador, con una contraseña temporal que el
  usuario debe cambiar al ingresar.
- No se eliminan: se desactivan.
- Siempre queda al menos un Administrador activo, y el administrador no puede cambiarse su propio rol
  ni desactivarse.

## Modelo: reglas importantes

- **`Contrato.MontoActual`** solo cambia con `RegistrarNuevoMonto`, que agrega el `HistorialMonto` en el mismo
  SaveChanges. Nunca se asigna por separado. Hay tests de esto en `MontoContratoTests` y
  `MontoContratoPersistenciaTests`, y en la fase 2 se suma el test del servicio de ajustes.
- **Estado de la propiedad:** no se guarda. Se deriva de `Activa` y de si tiene un contrato vigente.
- **Copropietarios** (`PropiedadPropietario`, con un porcentaje que debe sumar 100) y **coinquilinos**
  (`ContratoInquilino`, con un titular): relaciones N:M.
- **El tipo de garantía** está en `ContratoGarante`, no en `Garante`: la misma persona puede garantizar
  distintos contratos con distintas garantías. El modelo admite contratos **sin garante**.
- **Numeración de recibos:** correlativa y sin huecos, con `Numerador` y bloqueo de fila dentro de la misma
  transacción (no una SEQUENCE).
- **Índices:** se guarda el **nivel** del índice, no la variación, así el ajuste es un cociente.
  Los valores del seed de desarrollo son **ficticios**.
- **Impuestos y configuración de mora:** tienen vigencia. Modificarlos crea una vigencia nueva y no pisa la anterior.
- **Fotos de propiedades** (`FotoPropiedad`, 2026-10-17):
  - **Dónde se guardan:** los archivos van fuera de `wwwroot`, en la carpeta configurada en `Fotos:Carpeta`
    (por defecto `App_Data/fotos`, ignorada por git). Solo se sirven desde `FotosPropiedadesController.Ver`,
    con sesión iniciada.
  - **Procesamiento** (`ProcesadorImagenes`, con SkiaSharp):
    - identifica el formato por el contenido (solo JPG, PNG o WebP);
    - rechaza resoluciones enormes antes de decodificar;
    - corrige la orientación EXIF y vuelve a codificar en JPEG, sin metadatos (se quita el GPS del celular);
    - genera la versión normal (lado mayor de 1600 px) y la miniatura (400 px).
  - **Nombre del archivo:** lo genera el sistema (un GUID). El nombre original solo se guarda como texto.
  - **Permisos:** las ven todos los roles. Subirlas, describirlas, elegir la principal y eliminarlas es para la
    política `Gestion`.
  - **Dónde se muestran** (decisión del usuario): arriba de todo en la ficha, en un carrusel (`_CarruselFotos`)
    que empieza por la principal, tiene miniaturas y **no avanza solo**. Más abajo está la sección "Administrar
    fotos" (`_GaleriaFotos`), que solo ve la política `Gestion`.
  - **Eliminar** es borrado lógico y el archivo se conserva. Si se elimina la principal, la siguiente pasa a serlo.
  - **Backups:** en producción, la carpeta de fotos tiene que estar incluida en las copias de seguridad, además
    de la base.

## Tests

`tests/GestionAlquileres.Tests` tiene 149 tests: 142 pasan y 7 están omitidos a propósito.

| Carpeta | Qué prueba |
|---|---|
| `Unidad/` | Funciones puras: semáforo, CUIT y documentos, enmascarado, validaciones, monto del contrato, procesamiento de fotos |
| `Persistencia/` | DbContext sobre una base efímera (`BaseDeDatosFixture`): borrado lógico, auditoría, inmutables, monto + historial |
| `Integracion/` | La aplicación entera (`AplicacionFixture`) con un cliente por rol: matriz de permisos, formularios enviados a mano, rol Consulta |
| `Pendientes/` | Tests omitidos con la descripción de lo acordado para las fases 2 a 4: se reemplazan por los reales al implementar |

- Las bases se llaman `GA_Tests_*` y se borran al final.
- `AplicacionFixture` reemplaza el DbContext (`ConfigureTestServices`) y verifica que no esté usando la base
  de desarrollo. Crea sus propios usuarios con contraseñas generadas. Las fotos van a una carpeta temporal
  (`GA_Tests_Fotos_*`) que se borra al final.
- Hay un solo login por rol, por el límite de 10 por minuto.
- Al agregar una página nueva, sumarla a la matriz de `PermisosTests`.

## Trampas conocidas

- **PowerShell 5.1:** `Get-Content` y `Set-Content` rompen el UTF-8 de los archivos. Editar con el editor
  o con `[IO.File]` y `UTF8Encoding($false)`. En los comandos, las contraseñas van entre comillas simples.
- **`RenderSectionAsync`** escribe donde se llama: la sección `Acciones` se renderiza en el encabezado del layout.
- **Bibliotecas en `wwwroot/lib`:** usan `asp-append-version`, porque el navegador cacheaba versiones viejas.
- **Encoder de Razor:** acepta todo Unicode (`WebEncoderOptions`) para que los acentos salgan legibles.
  Los caracteres peligrosos se siguen escapando.
- **LocalDB, error 5170:** quedan `.mdf` huérfanos en `C:\Users\<usuario>` si se borra la instancia y no la base.
- **Rutas largas:** `Microsoft.Data.SqlClient.SNI.dll` no carga si la carpeta de compilación es muy larga.
- **ImageSharp 4 descartado:** exige una clave de licencia de Six Labors para compilar en Release, aunque el uso sea
  gratuito. Para las imágenes se usa **SkiaSharp** (MIT). `SkiaSharp.NativeAssets.Linux.NoDependencies` está
  incluido para poder publicar en Linux.
- **Subida de archivos en IIS:** el límite de pedido de la acción (`RequestSizeLimit`) no alcanza. IIS tiene su
  propio límite (`maxAllowedContentLength`, 30 MB por defecto) y hay que subirlo en `web.config` si se publica ahí.

## Plan de fases

| Fase | Contenido | Estado |
|---|---|---|
| 1 | Arquitectura, modelo completo y migración, seed, autenticación, roles y políticas, layout accesible con menú lateral, ABM de Propietarios, Propiedades, Inquilinos y Garantes, tablero, datos de la inmobiliaria, usuarios, auditoría, tests | **Terminada** |
| 2 | Contratos completos (alta, renovación, rescisión, cuotas), índices, ajustes de precio con tests, ficha del contrato | Siguiente |
| 3 | Cuotas y pagos (incluidos parciales), recibos con numeración y anulación, PDF del recibo, **PDF "Descargar ficha"** en las 4 fichas | |
| 4 | Mora (bandeja y monto sugerido), impuestos con vigencia, PDF de aviso de mora y de actualización de contrato, umbrales del semáforo editables | |
| 5 | Reportes con exportación a PDF y Excel | |
| 6 | Endurecimiento de seguridad, despliegue y backups | |

Las secciones de las fases siguientes ya existen como páginas provisorias "en construcción", con su lista
de pendientes. Están en `Controllers/SeccionesProvisoriasController.cs` y en
`Areas/Administracion/Controllers/SeccionesProvisoriasController.cs`.

## Decisiones tomadas con el usuario

- **Copropietarios y coinquilinos:** relaciones N:M.
- **Semáforo:** "por vencer" a 60 días, "pago próximo" a 5 días y "ajuste próximo" a 30 días.
  Configurables en `appsettings` (sección `Semaforo`).
- **Contrato en USD con ajuste por ICL:** muestra una advertencia, no se bloquea.
- **ABM mínimo de usuarios en la fase 1:** hecho.
- **Auditoría:** se registra la apertura de fichas con datos personales.
- **Rol Consulta:**
  - documento, teléfono y email parcialmente ocultos;
  - no ve contratos (en fichas, listados ni tablero de inicio);
  - en el listado de inquilinos tampoco ve la propiedad donde vive, porque sale de su contrato.
- **Ficha del contrato** (fase 2): solo para Administrador, por ahora.
- **Diseño:**
  - menú lateral;
  - en Administración, acceso directo a Contratos, Ajustes de precio y Bandeja de mora, y el resto en
    Configuración (Índices, Impuestos y mora, Datos de la inmobiliaria, Usuarios, Auditoría);
  - colores azul marino provisorios (variables `--ga-*` en `site.css`).
- **Datos de la inmobiliaria** (pie de página, inicio y documentos): editables en Configuración. Hoy tienen
  datos de ejemplo.
- **Logo:** está en `wwwroot/img` y se configura en la sección `Empresa` de `appsettings.json`. En esta copia
  es **genérico** ("GA" y "Gestión de Alquileres"), no el de la inmobiliaria real.
  - `logo.png`: a color, con fondo transparente. Se usa en el inicio de sesión y en el recuadro de la
    inmobiliaria del inicio, y se usará en los PDF.
  - `logo-claro.png`: el mismo logo con el texto en blanco, para la barra superior oscura.
  - `favicon-32.png` y `apple-touch-icon.png`: el isotipo.

  El texto alternativo del logo es el nombre comercial cargado en Datos de la inmobiliaria.
- **PDF "Descargar ficha":**
  - contenido aprobado: encabezado de la inmobiliaria con la fecha, datos sin notas internas,
    propiedades o contratos con el estado en texto, y pie con número de página;
  - enmascarado para Consulta y descarga auditada;
  - se hace en la fase 3. Hay un TODO en cada `Detalle.cshtml`.
- **Registro de usuarios:** descartado por ahora (expondría datos personales). Si vuelve, hacerlo con
  aprobación del administrador o por invitación con un enlace de un solo uso, **nunca** abierto con acceso inmediato.
- **Los pasos ya hechos se pueden modificar:** se hacen en commits chicos y se registran acá.

## Trabajo Final Integrador (TFI)

Esta copia es la base del TFI de la materia **Programación Web**. El cliente de la inmobiliaria dio permiso
para usar el sistema con ese fin. El sistema real sigue en su propio repositorio y no se toca desde acá.

**Esta copia va a un repositorio público:**
- no lleva datos del cliente: ni su marca, ni sus datos de contacto, ni sus documentos o notas de reuniones;
- no lleva secretos, como la cadena de conexión, las contraseñas o la clave de la API de IA;
- el seed de demostración usa solo datos ficticios.

**Separada del sistema real en la misma PC:**
- tiene su propio `UserSecretsId`;
- usa la base `GestionAlquileresTFI`;
- corre en los puertos 7173 y 5116;
- sus cookies se llaman `__Host-GestionAlquileresTFI.*`.

Así los dos sistemas pueden estar abiertos a la vez sin pisarse.

### La consigna, comparada con lo que ya está

| Requisito | Estado |
|---|---|
| ASP.NET Core MVC, EF Core con migraciones y SQL Server | Hecho |
| Identity con 2 roles o más | Hecho (Administrador, Operador y Consulta) |
| 7 tablas o más, sin contar las de Identity | Hecho |
| Validación en el servidor y en el cliente (jQuery Unobtrusive) | Hecho: todos los formularios de alta y edición cargan `_ValidationScriptsPartial` |
| Adjuntos: imágenes y PDF | Las imágenes están hechas (fotos de propiedades). **Falta adjuntar un PDF**: va a ser el contrato firmado, en el ABM mínimo de contratos |
| Modales interactivos (Bootstrap, AJAX o SweetAlert2) | **Falta.** Ver "Cómo sumarlo sin romper las reglas" |
| Layout con Bootstrap 5, responsivo | Hecho |
| Auditoría de quién hizo cada operación | Hecho |
| Módulo de IA (chatbot o generación y análisis de contenido) | **Falta** |
| Repositorio de GitHub con README | El README está. Falta crear el repositorio y sumarle la URL y cómo probar el sistema |
| URL pública con el sistema desplegado | **Falta** |
| Credenciales de prueba de un Administrador y de un Cliente | El "Cliente" es el rol **Consulta**. Faltan los usuarios de demostración en producción |
| Mensajes claros y en español (Identity, SweetAlert2, ABM) | Identity hecho (`IdentityErrorDescriberEspanol`). Falta SweetAlert2 |

### Cómo sumarlo sin romper las reglas

- **Modales:** como mejora progresiva. El enlace o el formulario sigue funcionando sin JavaScript, y con
  JavaScript abre el modal. SweetAlert2 va en `wwwroot/lib` con su CSS aparte, porque la CSP no permite estilos
  inline. Hay que usar el build sin estilos embebidos.
- **IA:** la consigna sugiere Gemini, con la clave en User Secrets en desarrollo y en variables de entorno en
  producción.
  - **No mandar datos personales a la API** (Ley 25.326): solo preguntas sobre el uso del sistema o datos
    agregados y anónimos.
  - Ponerle un límite de pedidos por usuario.
  - Mostrar un mensaje claro si la API no responde.
- **Despliegue:** el hosting tiene que ofrecer SQL Server, como SmarterASP.NET o Azure SQL; Render no lo ofrece.
  La carpeta de fotos tiene que persistir entre despliegues. En producción, las migraciones no se aplican solas
  (ver el README).
- **Credenciales de prueba:** se entregan con el trabajo, no en el repositorio público. Los usuarios de
  demostración trabajan sobre datos ficticios.
- **Rol "Cliente":** es Consulta (decidido el 2026-11-07). No se crea un rol nuevo.

### Entregas

| Hito | Contenido | Estado |
|---|---|---|
| 1 | Propuesta, DER preliminar y roles | El DER está en `docs/diagrama-entidad-relacion.md` |
| 2 | Base con EF Core, Identity y los ABM principales | Cumplido con la fase 1 |
| 3 | Repositorio con README, URL pública y credenciales de prueba | Pendiente |
| 4 | Sistema terminado con IA y defensa de 15 minutos: demo por rol, explicación del código y preguntas | Pendiente |

### Plan acordado (2026-11-07)

Entrega: **viernes 13/11/2026**.

- **ABM mínimo de contratos** (alta, listado, ficha, rescisión y adjuntar el contrato firmado en PDF), sin ajustes
  ni cuotas. Reemplaza la página provisoria de Contratos y cubre el requisito del PDF.
- **Páginas "en construcción":** salen del menú para la demo, porque la consigna pide un sistema 100% funcional.
  El código queda.
- **Orden:** repositorio, despliegue temprano del estado actual, modales con SweetAlert2, contratos con PDF, IA con
  Gemini (chat de ayuda; el reporte con datos agregados es opcional) y README final. Se vuelve a desplegar al
  cerrar cada etapa.
- **Hosting:** pendiente de confirmar. La recomendación es MonsterASP.NET (gratis y con SQL Server). Somee y
  Railway quedaron descartados: Somee inserta publicidad que choca con la CSP, y Railway no ofrece SQL Server.
