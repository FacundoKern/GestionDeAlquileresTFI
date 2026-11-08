using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Data.Seed;

/// <summary>
/// Datos de prueba realistas para desarrollo. TODOS los datos son ficticios: personas, documentos,
/// direcciones y, en particular, los VALORES DE LOS ÍNDICES (no son los publicados por BCRA/INDEC).
/// Las fechas se generan relativas a "hoy" para que el tablero muestre casos de todos los estados.
/// Solo se cargan si la base no tiene propietarios.
/// </summary>
internal sealed class DatosDePrueba
{
    private const string UsuarioSeed = "sistema";
    private const string Provincia = "Buenos Aires";
    private const string Caba = "Ciudad Autónoma de Buenos Aires";

    private readonly AppDbContext _db;
    private readonly DateOnly _hoy;
    private readonly DateOnly _mesActual;
    private readonly DateTime _ahoraUtc;
    private readonly Dictionary<(string Codigo, DateOnly Mes), decimal> _valoresIndices = [];
    private readonly List<PagoPendiente> _pagos = [];

    public DatosDePrueba(AppDbContext db, TimeProvider reloj)
    {
        _db = db;
        _hoy = reloj.HoyEnArgentina();
        _mesActual = new DateOnly(_hoy.Year, _hoy.Month, 1);
        _ahoraUtc = reloj.AhoraUtc();
    }

    public async Task<bool> CargarAsync(CancellationToken ct)
    {
        if (await _db.Propietarios.IgnoreQueryFilters().AnyAsync(ct))
        {
            return false;
        }

        var indices = await _db.Indices.ToDictionaryAsync(i => i.Codigo, ct);
        var numerador = await _db.Numeradores.SingleAsync(n => n.Clave == Numerador.Recibos, ct);

        CargarValoresIndices(indices);
        var propiedades = CrearPropietariosYPropiedades();
        var inquilinos = CrearInquilinos();
        var garantes = CrearGarantes();
        var abl = CrearImpuestos();
        var moraGeneral = new ConfiguracionMora
        {
            Criterio = CriterioMora.InteresDiario,
            Valor = 0.5m,
            DiasGracia = 0,
            VigenciaDesde = new DateOnly(_hoy.Year - 1, 1, 1),
        };
        _db.ConfiguracionesMora.Add(moraGeneral);

        CrearContratos(propiedades, inquilinos, garantes, indices, abl, moraGeneral);
        numerador.UltimoValor = EmitirRecibos(numerador.UltimoValor);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Datos de ejemplo de la inmobiliaria (se reemplazan desde Administración → Datos de la inmobiliaria).
    /// Independiente del resto: se carga aunque la base ya tenga datos de prueba.
    /// </summary>
    public async Task<bool> CargarDatosEmpresaAsync(CancellationToken ct)
    {
        if (await _db.DatosEmpresa.AnyAsync(ct))
        {
            return false;
        }

        const string prefijoCuit = "3071000000";
        _db.DatosEmpresa.Add(new DatosEmpresa
        {
            NombreComercial = "Inmobiliaria Ejemplo",
            RazonSocial = "Inmobiliaria Ejemplo S.R.L. (dato de ejemplo)",
            Cuit = prefijoCuit + Documentos.CalcularDigitoVerificadorCuit(prefijoCuit),
            MatriculaCorredor = "Matrícula N° 0000 (ejemplo)",
            Direccion = "Av. Ejemplo 1234, piso 2",
            Localidad = "Ciudad Autónoma de Buenos Aires",
            Provincia = "CABA",
            CodigoPostal = "C1000",
            Telefono = "11 5555-0000",
            Whatsapp = "11 5555-0001",
            Email = "contacto@inmobiliaria-ejemplo.test",
            SitioWeb = "https://www.inmobiliaria-ejemplo.test",
            HorarioAtencion = "Lunes a viernes de 9 a 18 h",
            Descripcion = "Texto de ejemplo: administración de alquileres residenciales y comerciales en CABA y GBA. " +
                "Reemplazalo desde Administración → Datos de la inmobiliaria.",
            SoporteNombre = "Soporte técnico (ejemplo)",
            SoporteTelefono = "11 5555-0002",
            SoporteEmail = "soporte@ejemplo.test",
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------------------------------------------------------- Índices (valores FICTICIOS)

    private void CargarValoresIndices(Dictionary<string, Indice> indices)
    {
        var series = new (string Codigo, decimal Inicial, decimal VariacionMensual)[]
        {
            ("ICL", 9.8m, 0.022m),
            ("IPC", 6900m, 0.023m),
            ("CASA_PROPIA", 3.9m, 0.020m),
        };

        foreach (var (codigo, inicial, variacion) in series)
        {
            var valor = inicial;
            for (var mes = _mesActual.AddMonths(-48); mes <= _mesActual; mes = mes.AddMonths(1))
            {
                valor = Math.Round(valor * (1 + variacion), 4);
                _valoresIndices[(codigo, mes)] = valor;
                _db.IndicesValores.Add(new IndiceValor { Indice = indices[codigo], Fecha = mes, Valor = valor });
            }
        }
    }

    private decimal ValorIndice(string codigo, DateOnly fecha) =>
        _valoresIndices[(codigo, new DateOnly(fecha.Year, fecha.Month, 1))];

    // ---------------------------------------------------------------- Personas y propiedades

    private Propiedad[] CrearPropietariosYPropiedades()
    {
        var gonzalez = Fisica<Propietario>("María Laura", "González", "20345678", "maria.gonzalez@correo.test", "11 5555-0101", "Av. Santa Fe 2450, CABA");
        var fernandez = Fisica<Propietario>("Jorge Alberto", "Fernández", "17890123", "jorge.fernandez@correo.test", "11 5555-0102", "Av. Pueyrredón 1180, CABA");
        var rodriguez = Fisica<Propietario>("Silvia Noemí", "Rodríguez", "22456789", "silvia.rodriguez@correo.test", "11 5555-0103", "Cabildo 3020, CABA");
        var lopez = Fisica<Propietario>("Carlos Daniel", "López", "25678901", "carlos.lopez@correo.test", "11 5555-0104", "Cabildo 3020, CABA");
        var martinez = Fisica<Propietario>("Ana Beatriz", "Martínez", "28901234", "ana.martinez@correo.test", "11 5555-0105", "Libertador 15400, San Isidro");
        var pereyra = Fisica<Propietario>("Ricardo Omar", "Pereyra", "14567890", "ricardo.pereyra@correo.test", "11 5555-0106", "Yatay 640, CABA");
        var delPlata = Juridica<Propietario>("Inversiones del Plata S.A.", "3071234567", "administracion@delplata.test", "11 4555-0107", "Reconquista 550, piso 8, CABA");
        var aromos = Juridica<Propietario>("Los Aromos S.R.L.", "3070987654", "contacto@losaromos.test", "11 4555-0108", "Av. Rivadavia 17900, Morón");
        _db.Propietarios.AddRange(gonzalez, fernandez, rodriguez, lopez, martinez, pereyra, delPlata, aromos);

        Propiedad[] propiedades =
        [
            Propiedad("Gurruchaga", "1543", "3", "B", "Palermo", Caba, "C1414", TipoPropiedad.Departamento, (gonzalez, 100)),
            Propiedad("Av. Rivadavia", "5120", "7", "A", "Caballito", Caba, "C1424", TipoPropiedad.Departamento, (fernandez, 100)),
            Propiedad("Juramento", "2280", "2", "C", "Belgrano", Caba, "C1428", TipoPropiedad.Departamento, (rodriguez, 50), (lopez, 50)),
            Propiedad("Thames", "845", null, null, "Villa Crespo", Caba, "C1414", TipoPropiedad.Ph, (martinez, 100)),
            Propiedad("Acoyte", "450", "10", "D", "Caballito", Caba, "C1405", TipoPropiedad.Departamento, (delPlata, 100)),
            Propiedad("Av. Cabildo", "1820", null, null, "Belgrano", Caba, "C1426", TipoPropiedad.Local, (delPlata, 100)),
            Propiedad("Humahuaca", "3950", "1", "A", "Almagro", Caba, "C1192", TipoPropiedad.Departamento, (pereyra, 100)),
            Propiedad("Av. Corrientes", "4321", "5", "F", "Almagro", Caba, "C1195", TipoPropiedad.Oficina, (aromos, 100)),
            Propiedad("Rivadavia", "255", null, null, "Quilmes", Provincia, "B1878", TipoPropiedad.Casa, (gonzalez, 100)),
            Propiedad("9 de Julio", "1420", "4", "B", "Lanús", Provincia, "B1824", TipoPropiedad.Departamento, (fernandez, 100)),
            Propiedad("Belgrano", "380", null, null, "San Isidro", Provincia, "B1642", TipoPropiedad.Casa, (martinez, 70), (pereyra, 30)),
            Propiedad("Av. Rivadavia", "18200", "2", "A", "Morón", Provincia, "B1708", TipoPropiedad.Departamento, (aromos, 100)),
            Propiedad("Bonpland", "1750", null, null, "Palermo", Caba, "C1414", TipoPropiedad.Cochera, (rodriguez, 100)),
            Propiedad("Av. San Martín", "2900", null, null, "Villa del Parque", Caba, "C1417", TipoPropiedad.Casa, (lopez, 100)),
        ];
        propiedades[13].Activa = false;
        propiedades[13].Observaciones = "En refacción: no se ofrece hasta terminar la obra.";
        _db.Propiedades.AddRange(propiedades);
        return propiedades;
    }

    private Inquilino[] CrearInquilinos()
    {
        Inquilino[] inquilinos =
        [
            Fisica<Inquilino>("Lucía Belén", "Sosa", "38123456", "lucia.sosa@correo.test", "11 6555-0201", null),
            Fisica<Inquilino>("Martín Ezequiel", "Díaz", "35987654", "martin.diaz@correo.test", "11 6555-0202", null),
            Fisica<Inquilino>("Florencia", "Aguirre", "40234567", "florencia.aguirre@correo.test", "11 6555-0203", null),
            Fisica<Inquilino>("Tomás", "Benítez", "37456123", "tomas.benitez@correo.test", "11 6555-0204", null),
            Fisica<Inquilino>("Valentina", "Romero", "41345678", "valentina.romero@correo.test", "11 6555-0205", null),
            Fisica<Inquilino>("Federico", "Álvarez", "33678912", "federico.alvarez@correo.test", "11 6555-0206", null),
            Fisica<Inquilino>("Camila", "Torres", "39876543", "camila.torres@correo.test", "11 6555-0207", null),
            Fisica<Inquilino>("Nicolás", "Ruiz", "36543210", "nicolas.ruiz@correo.test", "11 6555-0208", null),
            Fisica<Inquilino>("Julieta", "Medina", "42123987", "julieta.medina@correo.test", "11 6555-0209", null),
            Fisica<Inquilino>("Matías", "Castro", "34789012", "matias.castro@correo.test", "11 6555-0210", null),
            Fisica<Inquilino>("Agustina", "Herrera", "40987123", "agustina.herrera@correo.test", "11 6555-0211", null),
            Juridica<Inquilino>("Cafetería La Esquina S.R.L.", "3071555666", "laesquina@correo.test", "11 4555-0212", "Av. Cabildo 1820, CABA"),
        ];
        _db.Inquilinos.AddRange(inquilinos);
        return inquilinos;
    }

    private Garante[] CrearGarantes()
    {
        Garante[] garantes =
        [
            Fisica<Garante>("Roberto", "Sosa", "16234567", "roberto.sosa@correo.test", "11 7555-0301", "Mitre 1200, Avellaneda"),
            Fisica<Garante>("Graciela", "Díaz", "15876543", "graciela.diaz@correo.test", "11 7555-0302", "Belgrano 980, Ramos Mejía"),
            Fisica<Garante>("Héctor", "Aguirre", "18345612", "hector.aguirre@correo.test", "11 7555-0303", "San Martín 455, Banfield"),
            Fisica<Garante>("Marta", "Benítez", "19456723", "marta.benitez@correo.test", "11 7555-0304", "Alsina 870, Quilmes"),
            Fisica<Garante>("Osvaldo", "Romero", "13567834", "osvaldo.romero@correo.test", "11 7555-0305", "Sarmiento 210, Morón"),
            Fisica<Garante>("Patricia", "Álvarez", "21678945", "patricia.alvarez@correo.test", "11 7555-0306", "Av. Maipú 2300, Olivos"),
            Fisica<Garante>("Eduardo", "Torres", "17789056", "eduardo.torres@correo.test", "11 7555-0307", "Moreno 1500, Lomas de Zamora"),
            Juridica<Garante>("Seguros del Sur S.A.", "3050001234", "caucion@segurosdelsur.test", "11 4555-0308", "Av. Leandro N. Alem 800, CABA"),
        ];
        _db.Garantes.AddRange(garantes);
        return garantes;
    }

    private Impuesto CrearImpuestos()
    {
        var abl = new Impuesto
        {
            Nombre = "ABL",
            Descripcion = "Alumbrado, Barrido y Limpieza que abona el inquilino junto con el alquiler (ejemplo, a confirmar con el cliente).",
        };
        abl.Vigencias.Add(new ImpuestoVigencia
        {
            TipoValor = TipoValorImpuesto.MontoFijo,
            Valor = 18_500m,
            VigenciaDesde = new DateOnly(_hoy.Year, 1, 1),
        });
        _db.Impuestos.Add(abl);
        return abl;
    }

    // ---------------------------------------------------------------- Contratos

    private void CrearContratos(
        Propiedad[] p,
        Inquilino[] i,
        Garante[] g,
        Dictionary<string, Indice> indices,
        Impuesto abl,
        ConfiguracionMora moraGeneral)
    {
        // 1. Al día. ICL anual, todavía sin ajustes.
        var c1 = Contrato(p[0], _mesActual.AddMonths(-8), 36, 780_000m, Moneda.Ars, 10, [i[0]],
            [(g[0], TipoGarantia.Propietaria, "Inmueble en Mitre 1200, Avellaneda. Matrícula 3-45678.")]);
        ConfigurarAjuste(c1, TipoAjuste.Indice, indices["ICL"], null, 12);
        GenerarCuotas(c1, anularPrimerRecibo: true);

        // 2. IPC cuatrimestral, ajuste próximo y pago parcial el mes pasado. Paga ABL.
        var c2 = Contrato(p[1], _mesActual.AddMonths(-11), 24, 620_000m, Moneda.Ars, 10, [i[1]],
            [(g[1], TipoGarantia.ReciboSueldo, "Empleada administrativa en relación de dependencia, antigüedad 9 años.")]);
        ConfigurarAjuste(c2, TipoAjuste.Indice, indices["IPC"], null, 4);
        c2.Impuestos.Add(new ContratoImpuesto { Impuesto = abl });
        GenerarCuotas(c2, parcial: _mesActual.AddMonths(-1));

        // 3. Por vencer (termina en 45 días). Copropiedad. 10 % semestral.
        var finC3 = _hoy.AddDays(45);
        var c3 = Contrato(p[2], finC3.AddMonths(-24).AddDays(1), 24, 540_000m, Moneda.Ars, 5, [i[2]],
            [(g[2], TipoGarantia.Propietaria, "Inmueble en San Martín 455, Banfield. Matrícula 63-12034.")]);
        ConfigurarAjuste(c3, TipoAjuste.PorcentajeFijo, null, 10m, 6);
        GenerarCuotas(c3);

        // 4. En dólares, ajuste manual anual, seguro de caución.
        var c4 = Contrato(p[4], _mesActual.AddMonths(-5), 24, 850m, Moneda.Usd, 10, [i[3]],
            [(g[7], TipoGarantia.SeguroCaucion, "Póliza de caución N° 0045-778812.")]);
        ConfigurarAjuste(c4, TipoAjuste.Manual, null, null, 12);
        GenerarCuotas(c4);

        // 5. En mora: cuotas impagas desde el mes pasado y mora aplicada.
        var c5 = Contrato(p[6], _mesActual.AddMonths(-7), 36, 590_000m, Moneda.Ars, 10, [i[4]],
            [(g[4], TipoGarantia.Propietaria, "Inmueble en Sarmiento 210, Morón. Matrícula 101-5521.")]);
        ConfigurarAjuste(c5, TipoAjuste.Indice, indices["CASA_PROPIA"], null, 3);
        var cuotasC5 = GenerarCuotas(c5, impagasDesde: _mesActual.AddMonths(-1));
        AplicarMora(c5, cuotasC5.Single(c => c.Periodo == _mesActual.AddMonths(-1)), moraGeneral);

        // 6. Coinquilinos y dos garantes. ICL anual con un ajuste ya aplicado.
        var c6 = Contrato(p[8], _mesActual.AddMonths(-20), 36, 700_000m, Moneda.Ars, 10, [i[5], i[6]],
            [
                (g[5], TipoGarantia.ReciboSueldo, "Docente, antigüedad 12 años."),
                (g[6], TipoGarantia.Propietaria, "Inmueble en Moreno 1500, Lomas de Zamora. Matrícula 63-88213."),
            ]);
        ConfigurarAjuste(c6, TipoAjuste.Indice, indices["ICL"], null, 12);
        GenerarCuotas(c6);

        // 7. Vencido: la fecha de fin pasó hace 10 días y nadie lo cerró.
        var finC7 = _hoy.AddDays(-10);
        var c7 = Contrato(p[9], finC7.AddMonths(-24).AddDays(1), 24, 450_000m, Moneda.Ars, 10, [i[7]],
            [(g[3], TipoGarantia.Propietaria, "Inmueble en Alsina 870, Quilmes. Matrícula 86-2290.")]);
        ConfigurarAjuste(c7, TipoAjuste.Indice, indices["IPC"], null, 6);
        GenerarCuotas(c7);

        // 8. Finalizado hace 3 meses.
        var finC8 = _mesActual.AddMonths(-3).AddDays(-1);
        var c8 = Contrato(p[10], finC8.AddMonths(-36).AddDays(1), 36, 380_000m, Moneda.Ars, 10, [i[8]],
            [(g[0], TipoGarantia.Propietaria, "Inmueble en Mitre 1200, Avellaneda. Matrícula 3-45678.")]);
        ConfigurarAjuste(c8, TipoAjuste.Indice, indices["ICL"], null, 12);
        c8.Estado = EstadoContrato.Finalizado;
        GenerarCuotas(c8, ultimosMeses: 3);

        // 9. Rescindido hace 2 meses.
        var c9 = Contrato(p[11], _mesActual.AddMonths(-10), 36, 510_000m, Moneda.Ars, 10, [i[9]],
            [(g[1], TipoGarantia.ReciboSueldo, "Empleada administrativa en relación de dependencia, antigüedad 9 años.")]);
        ConfigurarAjuste(c9, TipoAjuste.Indice, indices["IPC"], null, 6);
        c9.Estado = EstadoContrato.Rescindido;
        c9.FechaRescision = _hoy.AddDays(-60);
        c9.MotivoRescision = "Rescisión anticipada solicitada por el inquilino con preaviso de 30 días.";
        GenerarCuotas(c9, ultimosMeses: 3);

        // 10. Local comercial con pago próximo (vence en 3 días). Paga ABL.
        var vencimientoProximo = _hoy.AddDays(3);
        var c10 = Contrato(p[5], _mesActual.AddMonths(-3), 36, 1_850_000m, Moneda.Ars, Math.Min(vencimientoProximo.Day, 28), [i[11]],
            [(g[3], TipoGarantia.Propietaria, "Inmueble en Alsina 870, Quilmes. Matrícula 86-2290.")]);
        ConfigurarAjuste(c10, TipoAjuste.Indice, indices["IPC"], null, 3);
        c10.Impuestos.Add(new ContratoImpuesto { Impuesto = abl });
        GenerarCuotas(c10, hasta: new DateOnly(vencimientoProximo.Year, vencimientoProximo.Month, 1));
    }

    private Contrato Contrato(
        Propiedad propiedad,
        DateOnly inicio,
        int meses,
        decimal monto,
        Moneda moneda,
        int diaVencimiento,
        Inquilino[] inquilinos,
        (Garante Garante, TipoGarantia Tipo, string Detalle)[] garantes)
    {
        var contrato = new Contrato
        {
            Propiedad = propiedad,
            FechaInicio = inicio,
            FechaFin = inicio.AddMonths(meses).AddDays(-1),
            Moneda = moneda,
            DiaVencimiento = diaVencimiento,
            DepositoGarantia = monto,
        };
        contrato.EstablecerMontoInicial(monto, UsuarioSeed, _ahoraUtc);

        for (var n = 0; n < inquilinos.Length; n++)
        {
            contrato.Inquilinos.Add(new ContratoInquilino { Inquilino = inquilinos[n], EsTitular = n == 0 });
        }

        foreach (var (garante, tipo, detalle) in garantes)
        {
            contrato.Garantes.Add(new ContratoGarante { Garante = garante, TipoGarantia = tipo, Detalle = detalle });
        }

        _db.Contratos.Add(contrato);
        return contrato;
    }

    /// <summary>Configura el ajuste y registra en el historial los ajustes que ya deberían haberse aplicado.</summary>
    private void ConfigurarAjuste(Contrato contrato, TipoAjuste tipo, Indice? indice, decimal? porcentaje, int periodicidad)
    {
        var limite = contrato.Estado == EstadoContrato.Vigente ? Min(_hoy, contrato.FechaFin) : contrato.FechaFin;
        var numero = 1;
        for (; contrato.FechaInicio.AddMonths(numero * periodicidad) <= limite; numero++)
        {
            var fecha = contrato.FechaInicio.AddMonths(numero * periodicidad);
            var cambio = new HistorialMonto
            {
                FechaVigencia = fecha,
                TipoAjuste = tipo,
                AplicadoPor = UsuarioSeed,
                AplicadoEnUtc = _ahoraUtc,
            };

            switch (tipo)
            {
                case TipoAjuste.Indice:
                    var inicial = ValorIndice(indice!.Codigo, fecha.AddMonths(-periodicidad));
                    var final = ValorIndice(indice.Codigo, fecha);
                    cambio.Indice = indice;
                    cambio.ValorIndiceInicial = inicial;
                    cambio.ValorIndiceFinal = final;
                    cambio.Coeficiente = Math.Round(final / inicial, 8);
                    break;
                case TipoAjuste.PorcentajeFijo:
                    cambio.Coeficiente = 1 + porcentaje!.Value / 100m;
                    break;
                default:
                    cambio.Coeficiente = 1.10m;
                    cambio.Observaciones = "Ajuste acordado entre las partes.";
                    break;
            }

            cambio.MontoNuevo = Math.Round(contrato.MontoActual * cambio.Coeficiente.Value, 2);
            contrato.RegistrarNuevoMonto(cambio);
        }

        contrato.ConfiguracionAjuste = new ConfiguracionAjuste
        {
            TipoAjuste = tipo,
            Indice = indice,
            PorcentajeFijo = porcentaje,
            PeriodicidadMeses = periodicidad,
            ProximaFechaAjuste = contrato.FechaInicio.AddMonths(numero * periodicidad),
        };
    }

    /// <summary>
    /// Genera las cuotas de los últimos meses. Por defecto, las ya vencidas quedan pagadas y las
    /// futuras pendientes; los parámetros permiten crear impagas y pagos parciales.
    /// </summary>
    private List<Cuota> GenerarCuotas(
        Contrato contrato,
        DateOnly? impagasDesde = null,
        DateOnly? parcial = null,
        int ultimosMeses = 6,
        DateOnly? hasta = null,
        bool anularPrimerRecibo = false)
    {
        var fin = contrato.FechaRescision ?? contrato.FechaFin;
        var ultimo = Min(hasta ?? _mesActual, new DateOnly(fin.Year, fin.Month, 1));
        var primero = Max(new DateOnly(contrato.FechaInicio.Year, contrato.FechaInicio.Month, 1), ultimo.AddMonths(1 - ultimosMeses));
        var cuotas = new List<Cuota>();

        for (var periodo = primero; periodo <= ultimo; periodo = periodo.AddMonths(1))
        {
            var cuota = new Cuota
            {
                Contrato = contrato,
                Periodo = periodo,
                FechaVencimiento = new DateOnly(periodo.Year, periodo.Month, contrato.DiaVencimiento),
                Monto = MontoVigente(contrato, periodo),
            };
            contrato.Cuotas.Add(cuota);
            cuotas.Add(cuota);

            var vencida = cuota.FechaVencimiento < _hoy;
            if (!vencida || periodo >= impagasDesde)
            {
                continue; // pendiente
            }

            var fechaPago = cuota.FechaVencimiento.AddDays(-2);
            var medio = periodo.Month % 3 == 0 ? MedioPago.Efectivo : MedioPago.Transferencia;
            if (periodo == parcial)
            {
                cuota.Estado = EstadoCuota.PagadaParcial;
                _pagos.Add(new PagoPendiente(contrato, cuota, Math.Round(cuota.Monto * 0.6m, 2), fechaPago, medio, false));
                continue;
            }

            cuota.Estado = EstadoCuota.Pagada;
            var anular = anularPrimerRecibo && cuotas.Count == 1;
            _pagos.Add(new PagoPendiente(contrato, cuota, cuota.Monto, fechaPago, medio, anular));
        }

        return cuotas;
    }

    private void AplicarMora(Contrato contrato, Cuota cuota, ConfiguracionMora configuracion)
    {
        var fechaAplicacion = _hoy.AddDays(-2);
        var dias = fechaAplicacion.DayNumber - cuota.FechaVencimiento.DayNumber;
        var sugerido = Math.Round(cuota.Monto * configuracion.Valor / 100m * dias, 2);
        _db.Moras.Add(new Mora
        {
            Contrato = contrato,
            Cuota = cuota,
            DiasAtraso = dias,
            MontoSugerido = sugerido,
            MontoAplicado = sugerido,
            FechaAplicacion = fechaAplicacion,
            ConfiguracionMora = configuracion,
            Motivo = "Interés punitorio según cláusula del contrato.",
        });
    }

    /// <summary>Emite los recibos en orden cronológico para que la numeración correlativa sea coherente.</summary>
    private long EmitirRecibos(long ultimoNumero)
    {
        foreach (var pago in _pagos.OrderBy(p => p.Fecha))
        {
            var recibo = NuevoRecibo(++ultimoNumero, pago, pago.Monto);
            if (pago.ConErrorAnulado)
            {
                // Un recibo mal emitido (monto equivocado) que se anuló y se reemplazó por el correcto.
                recibo.Total = recibo.Items.Single().Monto = pago.Monto - 10_000m;
                recibo.Estado = EstadoRecibo.Anulado;
                recibo.MotivoAnulacion = "Monto cargado incorrectamente.";
                recibo.AnuladoEnUtc = _ahoraUtc;
                recibo.AnuladoPor = UsuarioSeed;
                recibo.ReemplazadoPorRecibo = NuevoRecibo(++ultimoNumero, pago, pago.Monto);
            }
        }

        return ultimoNumero;
    }

    private Recibo NuevoRecibo(long numero, PagoPendiente pago, decimal monto)
    {
        var recibo = new Recibo
        {
            Numero = numero,
            Contrato = pago.Contrato,
            Cuota = pago.Cuota,
            FechaEmision = pago.Fecha,
            FechaPago = pago.Fecha,
            Moneda = pago.Contrato.Moneda,
            Total = monto,
            MedioPago = pago.Medio,
        };
        recibo.Items.Add(new ReciboItem
        {
            Concepto = ConceptoReciboItem.Alquiler,
            Descripcion = $"Alquiler {pago.Cuota.Periodo:MM/yyyy}",
            Monto = monto,
        });
        _db.Recibos.Add(recibo);
        return recibo;
    }

    // ---------------------------------------------------------------- Auxiliares

    private static decimal MontoVigente(Contrato contrato, DateOnly periodo) =>
        contrato.HistorialMontos
            .Where(h => h.FechaVigencia <= periodo)
            .OrderBy(h => h.FechaVigencia)
            .LastOrDefault()?.MontoNuevo ?? contrato.MontoInicial;

    private static T Fisica<T>(string nombre, string apellido, string dni, string email, string telefono, string? domicilio)
        where T : Persona, new() => new()
        {
            TipoPersona = TipoPersona.Fisica,
            Nombre = nombre,
            Apellido = apellido,
            TipoDocumento = TipoDocumento.Dni,
            NumeroDocumento = dni,
            Email = email,
            Telefono = telefono,
            Domicilio = domicilio,
        };

    private static T Juridica<T>(string razonSocial, string cuitSinVerificador, string email, string telefono, string domicilio)
        where T : Persona, new() => new()
        {
            TipoPersona = TipoPersona.Juridica,
            Nombre = razonSocial,
            TipoDocumento = TipoDocumento.Cuit,
            NumeroDocumento = cuitSinVerificador + Documentos.CalcularDigitoVerificadorCuit(cuitSinVerificador),
            Email = email,
            Telefono = telefono,
            Domicilio = domicilio,
        };

    private static Propiedad Propiedad(
        string calle,
        string numero,
        string? piso,
        string? depto,
        string localidad,
        string provincia,
        string codigoPostal,
        TipoPropiedad tipo,
        params (Propietario Propietario, decimal Porcentaje)[] propietarios)
    {
        var propiedad = new Propiedad
        {
            Calle = calle,
            Numero = numero,
            Piso = piso,
            Departamento = depto,
            Localidad = localidad,
            Provincia = provincia,
            CodigoPostal = codigoPostal,
            Tipo = tipo,
        };
        foreach (var (propietario, porcentaje) in propietarios)
        {
            propiedad.Propietarios.Add(new PropiedadPropietario { Propietario = propietario, Porcentaje = porcentaje });
        }

        return propiedad;
    }

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private sealed record PagoPendiente(Contrato Contrato, Cuota Cuota, decimal Monto, DateOnly Fecha, MedioPago Medio, bool ConErrorAnulado);
}
