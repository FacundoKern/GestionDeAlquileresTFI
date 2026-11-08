# Diagrama entidad-relación (DER)

Modelo de datos de Gestión de Alquileres, tomado del código (`Models/Entidades` y `Data/Configurations`).
Los diagramas están en [Mermaid](https://mermaid.js.org/): se ven dibujados en GitHub y en VS Code con la
extensión "Markdown Preview Mermaid Support". También se pueden pegar en <https://mermaid.live>.

> Mantenerlo actualizado: cada migración que cambie tablas o relaciones tiene que actualizar este archivo.

## Cómo leerlo

**Cardinalidad**

| Símbolo | Significa |
|---|---|
| `\|\|` | exactamente uno |
| `o\|` | cero o uno |
| `\|{` | uno o muchos |
| `o{` | cero o muchos |

- La línea punteada indica una relación lógica sin clave foránea.
- **PK** = clave primaria, **FK** = clave foránea, **UK** = valor único.

**Campos comunes que no se repiten en los diagramas**

| Tipo de tabla | Campos | Tablas |
|---|---|---|
| Auditable | `Id`, `CreadoEnUtc`, `CreadoPor`, `ModificadoEnUtc`, `ModificadoPor`, `RowVersion` (concurrencia) | `ConfiguracionAjuste`, `Recibo`, `DatosEmpresa` |
| De negocio | Lo anterior, más `Eliminado`, `EliminadoEnUtc`, `EliminadoPor` (borrado lógico) | `Propietario`, `Inquilino`, `Garante`, `Propiedad`, `FotoPropiedad`, `Contrato`, `Indice`, `IndiceValor`, `Cuota`, `Mora`, `ConfiguracionMora`, `Impuesto`, `ImpuestoVigencia` |
| Solo agregar | No se modifican ni se eliminan | `HistorialMonto`, `ReciboItem`, `AuditLog` |

**Reglas que no se ven en el dibujo**
- Los índices únicos de documento, código y nombre ignoran los registros eliminados.
- Todas las claves foráneas propias son `Restrict`: nada se borra en cascada.
- `Contrato.MontoActual` cambia solo junto con una fila nueva de `HistorialMonto`.
- El estado de la propiedad (alquilada o disponible) no se guarda: se calcula a partir de los contratos vigentes.

## 1. Vista general

```mermaid
erDiagram
    PROPIETARIO ||--o{ PROPIEDAD_PROPIETARIO : "es dueño de"
    PROPIEDAD ||--|{ PROPIEDAD_PROPIETARIO : "pertenece a"
    PROPIEDAD ||--o{ FOTO_PROPIEDAD : "tiene fotos"
    PROPIEDAD ||--o{ CONTRATO : "se alquila en"
    CONTRATO ||--|{ CONTRATO_INQUILINO : "lo firman"
    INQUILINO ||--o{ CONTRATO_INQUILINO : "firma"
    CONTRATO ||--o{ CONTRATO_GARANTE : "lo garantizan"
    GARANTE ||--o{ CONTRATO_GARANTE : "garantiza"
    CONTRATO ||--o| CONFIGURACION_AJUSTE : "se ajusta con"
    CONTRATO ||--|{ HISTORIAL_MONTO : "registra montos"
    INDICE ||--o{ INDICE_VALOR : "tiene valores"
    CONTRATO ||--o{ CUOTA : "genera"
    CONTRATO ||--o{ RECIBO : "emite"
    RECIBO ||--|{ RECIBO_ITEM : "detalla"
    CUOTA ||--o{ MORA : "puede tener"
    CONTRATO ||--o{ CONTRATO_IMPUESTO : "paga"
    IMPUESTO ||--o{ CONTRATO_IMPUESTO : "se asigna a"
    IMPUESTO ||--o{ IMPUESTO_VIGENCIA : "tiene valores"
```

## 2. Personas y propiedades

Copropietarios (N:M, con porcentaje que suma 100), coinquilinos (N:M, con un titular) y garantes por contrato
(N:M, con el tipo de garantía en la relación: la misma persona puede garantizar distintos contratos de
distinta forma).

```mermaid
erDiagram
    PROPIETARIO ||--o{ PROPIEDAD_PROPIETARIO : "es dueño de"
    PROPIEDAD ||--|{ PROPIEDAD_PROPIETARIO : "pertenece a"
    PROPIEDAD ||--o{ FOTO_PROPIEDAD : "tiene fotos"
    PROPIEDAD ||--o{ CONTRATO : "se alquila en"
    CONTRATO ||--|{ CONTRATO_INQUILINO : "lo firman"
    INQUILINO ||--o{ CONTRATO_INQUILINO : "firma"
    CONTRATO ||--o{ CONTRATO_GARANTE : "lo garantizan"
    GARANTE ||--o{ CONTRATO_GARANTE : "garantiza"
    CONTRATO o|--o{ CONTRATO : "se renueva en"

    FOTO_PROPIEDAD {
        int Id PK
        int PropiedadId FK
        string Archivo UK "GUID; el archivo está fuera de la base"
        string Descripcion "texto alternativo"
        bool EsPrincipal "una por propiedad"
        int Orden
        int Ancho
        int Alto
        bigint TamanioBytes
        string NombreOriginal "solo informativo"
    }

    PROPIETARIO {
        int Id PK
        enum TipoPersona "física o jurídica"
        string Nombre "o razón social"
        string Apellido "solo persona física"
        enum TipoDocumento "DNI, CUIT, CUIL, pasaporte"
        string NumeroDocumento UK "único por tipo"
        string Email
        string Telefono
        string Domicilio
        string Notas
    }
    INQUILINO {
        int Id PK
        enum TipoPersona
        string Nombre
        string Apellido
        enum TipoDocumento
        string NumeroDocumento UK
        string Email
        string Telefono
        string Domicilio
        string Notas
    }
    GARANTE {
        int Id PK
        enum TipoPersona
        string Nombre
        string Apellido
        enum TipoDocumento
        string NumeroDocumento UK
        string Email
        string Telefono
        string Domicilio
        string Notas
    }
    PROPIEDAD {
        int Id PK
        string Calle
        string Numero
        string Piso
        string Departamento
        string Localidad
        string Provincia
        string CodigoPostal
        enum Tipo "departamento, casa, local..."
        bool Activa
        string Observaciones
    }
    PROPIEDAD_PROPIETARIO {
        int PropiedadId PK, FK
        int PropietarioId PK, FK
        decimal Porcentaje "mayor a 0 y hasta 100"
    }
    CONTRATO_INQUILINO {
        int ContratoId PK, FK
        int InquilinoId PK, FK
        bool EsTitular
    }
    CONTRATO_GARANTE {
        int ContratoId PK, FK
        int GaranteId PK, FK
        enum TipoGarantia "propietaria, recibo de sueldo, caución, otra"
        string Detalle "matrícula, póliza, empleador"
    }
    CONTRATO {
        int Id PK
        int PropiedadId FK
        int ContratoAnteriorId FK "renovación"
    }
```

## 3. Contratos y ajustes de precio

El nivel de cada índice se guarda por fecha. Así el ajuste es un cociente de valores y queda trazado en el historial.

```mermaid
erDiagram
    CONTRATO ||--o| CONFIGURACION_AJUSTE : "se ajusta con"
    INDICE |o--o{ CONFIGURACION_AJUSTE : "usado por"
    INDICE ||--o{ INDICE_VALOR : "tiene valores"
    CONTRATO ||--|{ HISTORIAL_MONTO : "registra montos"
    INDICE |o--o{ HISTORIAL_MONTO : "usado en"

    CONTRATO {
        int Id PK
        int PropiedadId FK
        date FechaInicio
        date FechaFin
        decimal MontoInicial
        decimal MontoActual "solo cambia con HistorialMonto"
        enum Moneda "ARS o USD"
        int DiaVencimiento "1 a 28"
        decimal DepositoGarantia
        enum Estado "vigente, finalizado, rescindido"
        date FechaRescision
        string MotivoRescision
        int ContratoAnteriorId FK
        string Observaciones
    }
    CONFIGURACION_AJUSTE {
        int Id PK
        int ContratoId FK, UK "uno por contrato"
        enum TipoAjuste "índice, porcentaje fijo, manual"
        int IndiceId FK "si es por índice"
        decimal PorcentajeFijo "si es porcentaje"
        int PeriodicidadMeses "1 a 60"
        date ProximaFechaAjuste
    }
    INDICE {
        int Id PK
        string Codigo UK "ICL, IPC, CASA_PROPIA"
        string Nombre
        string Descripcion
        enum Periodicidad "diaria o mensual"
    }
    INDICE_VALOR {
        int Id PK
        int IndiceId FK
        date Fecha UK "una por índice"
        decimal Valor "nivel del índice, mayor a 0"
    }
    HISTORIAL_MONTO {
        int Id PK
        int ContratoId FK
        decimal MontoAnterior
        decimal MontoNuevo
        date FechaVigencia
        enum TipoAjuste "vacío en el monto inicial"
        int IndiceId FK
        decimal ValorIndiceInicial
        decimal ValorIndiceFinal
        decimal Coeficiente
        string Observaciones
        string AplicadoPor
        datetime AplicadoEnUtc
    }
```

## 4. Cobranza, mora e impuestos

Cuotas (lo que se debe), recibos (lo que se pagó, con pagos parciales), mora aplicada manualmente sobre
una cuota e impuestos con vigencia que se suman como ítems del recibo.

```mermaid
erDiagram
    CONTRATO ||--o{ CUOTA : "genera"
    CONTRATO ||--o{ RECIBO : "emite"
    CUOTA |o--o{ RECIBO : "se paga con"
    RECIBO ||--|{ RECIBO_ITEM : "detalla"
    RECIBO |o--o| RECIBO : "reemplazado por"
    CONTRATO ||--o{ MORA : "acumula"
    CUOTA ||--o{ MORA : "puede tener"
    CONFIGURACION_MORA ||--o{ MORA : "calcula"
    CONTRATO |o--o{ CONFIGURACION_MORA : "particular"
    MORA |o--o{ RECIBO_ITEM : "se cobra en"
    IMPUESTO ||--o{ IMPUESTO_VIGENCIA : "tiene valores"
    CONTRATO ||--o{ CONTRATO_IMPUESTO : "paga"
    IMPUESTO ||--o{ CONTRATO_IMPUESTO : "se asigna a"
    IMPUESTO |o--o{ RECIBO_ITEM : "se cobra en"

    CUOTA {
        int Id PK
        int ContratoId FK
        date Periodo UK "un mes por contrato"
        date FechaVencimiento
        decimal Monto
        enum Estado "pendiente, pagada parcialmente, pagada"
    }
    RECIBO {
        int Id PK
        bigint Numero UK "correlativo sin huecos"
        int ContratoId FK
        int CuotaId FK
        date FechaEmision
        date FechaPago
        enum Moneda
        decimal Total
        enum MedioPago "efectivo, transferencia, cheque, otro"
        string Observaciones
        enum Estado "emitido o anulado"
        string MotivoAnulacion
        datetime AnuladoEnUtc
        string AnuladoPor
        int ReemplazadoPorReciboId FK
    }
    RECIBO_ITEM {
        int Id PK
        int ReciboId FK
        enum Concepto "alquiler, mora, impuesto, expensas, otro"
        string Descripcion
        decimal Monto
        int ImpuestoId FK
        int MoraId FK
    }
    MORA {
        int Id PK
        int ContratoId FK
        int CuotaId FK
        int DiasAtraso
        decimal MontoSugerido
        decimal MontoAplicado "lo decide el administrador"
        date FechaAplicacion
        int ConfiguracionMoraId FK
        enum Estado "aplicada, cobrada, condonada"
        string Motivo
    }
    CONFIGURACION_MORA {
        int Id PK
        int ContratoId FK "vacío = configuración general"
        enum Criterio "monto fijo, porcentaje, interés diario"
        decimal Valor
        int DiasGracia
        date VigenciaDesde
        date VigenciaHasta
    }
    IMPUESTO {
        int Id PK
        string Nombre UK
        string Descripcion
        bool Activo
    }
    IMPUESTO_VIGENCIA {
        int Id PK
        int ImpuestoId FK
        enum TipoValor "monto fijo o porcentaje"
        decimal Valor
        date VigenciaDesde
        date VigenciaHasta
    }
    CONTRATO_IMPUESTO {
        int ContratoId PK, FK
        int ImpuestoId PK, FK
    }
```

## 5. Tablas de soporte

No se relacionan por clave foránea con el resto: los registros de auditoría guardan el tipo y el Id del
registro como texto, para no impedir nunca un cambio.

```mermaid
erDiagram
    USUARIO ||--o{ USUARIO_ROL : "tiene"
    ROL ||--o{ USUARIO_ROL : "asignado a"
    USUARIO |o..o{ AUDIT_LOG : "hizo (sin FK)"

    USUARIO {
        string Id PK "tabla AspNetUsers de Identity"
        string Email UK "también es el nombre de usuario"
        string NombreCompleto
        bool Activo
        bool DebeCambiarClave
        datetime CreadoEnUtc
        datetime UltimoIngresoUtc
        string PasswordHash "nunca se audita"
        datetimeoffset LockoutEnd "bloqueo por intentos"
    }
    ROL {
        string Id PK
        string Name UK "Administrador, Operador, Consulta"
    }
    USUARIO_ROL {
        string UserId PK, FK
        string RoleId PK, FK
    }
    AUDIT_LOG {
        bigint Id PK
        datetime FechaUtc
        string UsuarioId
        string UsuarioNombre
        enum Accion "creación, modificación, eliminación, consulta de ficha, inicio de sesión..."
        string Entidad "nombre de la tabla"
        string EntidadId
        json ValoresAnteriores
        json ValoresNuevos
        string DireccionIp
        string Detalle
    }
    DATOS_EMPRESA {
        int Id PK "una sola fila"
        string NombreComercial
        string RazonSocial
        string Cuit
        string MatriculaCorredor
        string Direccion
        string Telefono
        string Whatsapp
        string Email
        string HorarioAtencion
        string SoporteNombre
    }
    NUMERADOR {
        string Clave PK "RECIBO"
        bigint UltimoValor
    }
```

