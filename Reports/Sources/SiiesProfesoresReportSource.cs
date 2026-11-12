using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte SIIES Profesores — fusiona las matrices 5.2/5.3
/// (Profesores – Contratos IES, cédula/pasaporte) y 5.4 (Distribución Horas Periodo
/// Académico) en un solo reporte, igual que el archivo manual actual que la universidad
/// ya genera con estas 3 matrices combinadas en una sola hoja. Instructivo Carga Masiva
/// CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// SIIES prohíbe mezclar CEDULA y PASAPORTE en un mismo archivo. Igual que
/// <see cref="SiiesFuncionariosReportSource"/>, es un solo <see cref="IReportSource"/> que
/// segrega por <see cref="ReportFilterDto.IdentType"/> ("TODOS" por defecto). "TODOS" junta cédula y
/// pasaporte en un solo archivo con las columnas de la matriz 5.3 (las propias del pasaporte quedan
/// vacías en las filas de cédula): sirve para revisión interna, no para cargar en SIIES.
/// </para>
/// <para>
/// Requiere que el empleado tenga un registro en HR.tbl_TeacherStructure (la vista
/// HR.vw_SiiesProfesores ya filtra por esto). Esa tabla está vacía hasta que se complete
/// una carga masiva planeada por separado — hasta entonces este reporte no devuelve filas,
/// comportamiento esperado, no es un error.
/// </para>
/// <para>
/// Regla de HORAS (actualizada 2026-09-10): se consulta vía
/// <c>HR.fn_SiiesProfesoresHoras(@PeriodCode)</c> — no <c>HR.vw_SiiesProfesores</c> directo —
/// que agrega el distributivo real de horas académicas (<c>HR.tbl_AcademicHoursDistribution</c>,
/// cargado desde el sistema "UTA Mático", servidor 10.102.12.3, ajeno a HrBackend) por período
/// académico. <see cref="ReportFilterDto.PeriodCode"/> selecciona el período; null = el más
/// reciente disponible por profesor. Si el profesor no tiene distributivo cargado para ese
/// período (hoy: todos los "Profesor Titular", el distributivo cargado hasta ahora es de
/// personal con horas de docencia ocasional), se mantiene el fallback anterior: horas
/// contratadas del contrato vigente como aproximación de HORAS_CLASE/HORAS_CLASE_TERCER_NIVEL.
/// El resto de columnas de horas sin fuente real (nivel técnico, cuarto nivel) siguen en 0.
/// </para>
/// <para>
/// TIPO_DOCUMENTO: el sistema solo distingue internamente <c>DocumentType</c> = "CONTRACT" o
/// "PERSONNEL_ACTION" (no existe un catálogo más fino de Memorando/Convenio/Adendum/etc.).
/// Se homologa CONTRACT→CONTRATO, PERSONNEL_ACTION→ACCION PERSONAL como mejor aproximación
/// posible con la granularidad actual del dato.
/// </para>
/// </remarks>
public abstract class SiiesProfesoresReportSourceBase
{
    protected readonly AppDbContext _db;
    protected readonly ILogger _logger;

    private const string IndigenaLabel = "INDIGENA";
    private const string CuartoNivelLabel = "CUARTO NIVEL";

    /// <summary>Valor de <see cref="ReportFilterDto.IdentType"/> que junta cédula y pasaporte en un mismo archivo (revisión interna).</summary>
    protected const string AllIdentTypes = "TODOS";

    protected SiiesProfesoresReportSourceBase(AppDbContext db, ILogger logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected async Task<List<VwSiiesProfesor>> GetTeachersAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        // [2026-09-21] Fecha desde/hasta: busqueda historica real, mismo requisito que
        // SiiesFuncionariosReportSource (aparece aunque el contrato ya no este vigente, si
        // trabajo dentro del rango). Ver GetTeachersVigentesEnRangoAsync para el por que no
        // se reescribe el cascade completo de vw_SiiesProfesores para esto.
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            return await GetTeachersVigentesEnRangoAsync(identTypeName, filter, ct);

        // 2026-09-10: HR.fn_SiiesProfesoresHoras(@PeriodCode) en vez de la vista directa —
        // agrega el distributivo real de horas (HR.tbl_AcademicHoursDistribution) por período
        // académico. Toda la lógica de unión/período vive en la función SQL; aquí solo se
        // reenvía el parámetro que llega del filtro del reporte. NULL = período más reciente
        // disponible por profesor (mismo comportamiento por defecto de siempre).
        var periodCode = string.IsNullOrWhiteSpace(filter.PeriodCode) ? null : filter.PeriodCode.Trim();
        var query = _db.vwSiiesProfesores
            .FromSqlInterpolated($"SELECT * FROM HR.fn_SiiesProfesoresHoras({periodCode})")
            .AsNoTracking()
            .Where(v => identTypeName == AllIdentTypes
                ? (v.IdentTypeName == "CEDULA" || v.IdentTypeName == "PASAPORTE")
                : v.IdentTypeName == identTypeName);

        // 2026-10-08: se quitó el filtro de "activo hoy" también para el listado general (sin
        // período) -- el reporte SIIES debe traer a todos los que laboraron, estén o no activos
        // hoy, igual que ya se garantizaba para el caso con período específico (ver comentario
        // histórico arriba sobre fn_SiiesProfesoresHoras).

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        // "Todos": agrupado por tipo (cédula, luego pasaporte) y por apellido, igual que SIIES Funcionarios.
        if (identTypeName == AllIdentTypes)
            query = query.OrderBy(v => v.IdentTypeName).ThenBy(v => v.LastName).ThenBy(v => v.FirstName);

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// [2026-09-21] En vez de reescribir el cascade completo de HR.vw_SiiesProfesores (escalafón
    /// docente, profesores ocasionales por tipo de contrato, cadena de ParentID para
    /// RELACION_IES, etc. - mucho más grande que el de vw_SiiesFuncionarios) en SQL parametrizado,
    /// se traduce el rango de fechas a los períodos académicos que se solapan
    /// (<see cref="SiiesAcademicPeriodHelper"/>) y se reutiliza HR.fn_SiiesProfesoresHoras
    /// (@PeriodCode) — que YA elige por distributivo de horas cargado en ese período, no por
    /// estado actual del contrato — una vez por período que califique. Si el mismo profesor
    /// aparece en más de un período dentro del rango, se conserva el del período más reciente
    /// (los períodos ya llegan ordenados por PeriodEnd DESC desde el helper).
    /// </summary>
    private async Task<List<VwSiiesProfesor>> GetTeachersVigentesEnRangoAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        var periodCodes = await SiiesAcademicPeriodHelper.GetOverlappingPeriodCodesAsync(_db, filter.StartDate!.Value, filter.EndDate!.Value, ct);

        // 2026-11-12: con una identificacion el filtro se envia a la consulta de cada periodo (antes se traian todos los profesores del
        // periodo, ~19 s por periodo, y recien despues se filtraba en memoria).
        var identificationFilter = string.IsNullOrWhiteSpace(filter.Identification) ? null : filter.Identification.Trim();

        var byEmployee = new Dictionary<int, VwSiiesProfesor>();
        foreach (var periodCode in periodCodes)
        {
            var periodRows = await _db.vwSiiesProfesores
                .FromSqlInterpolated($"SELECT * FROM HR.fn_SiiesProfesoresHoras({periodCode})")
                .AsNoTracking()
                .Where(v => (identTypeName == AllIdentTypes
                    ? (v.IdentTypeName == "CEDULA" || v.IdentTypeName == "PASAPORTE")
                    : v.IdentTypeName == identTypeName)
                    && (identificationFilter == null || v.IDCard == identificationFilter))
                .ToListAsync(ct);

            foreach (var row in periodRows)
                byEmployee.TryAdd(row.EmployeeID, row); // primer periodo visto para este empleado = el mas reciente
        }

        IEnumerable<VwSiiesProfesor> selected = byEmployee.Values;

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            selected = selected.Where(v => v.IDCard == identification);
        }

        // 2026-11-12: una fila por documento (contrato/accion) del rango, no una sola por profesor con el documento actual.
        // Se expande DESPUES de filtrar por identificacion para no consultar los documentos de todos los profesores.
        IEnumerable<VwSiiesProfesor> results = await ExpandByDocumentsAsync(selected.ToList(), filter.StartDate!.Value, filter.EndDate!.Value, ct);

        // "Todos": agrupado por tipo (cédula, luego pasaporte) y por apellido, igual que SIIES Funcionarios.
        return identTypeName == AllIdentTypes
            ? results.OrderBy(v => v.IdentTypeName, StringComparer.Ordinal).ThenBy(v => v.LastName, StringComparer.CurrentCulture).ThenBy(v => v.FirstName, StringComparer.CurrentCulture).ToList()
            : results.ToList();
    }

    private static readonly System.Reflection.MethodInfo ShallowCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    private static VwSiiesProfesor CloneRow(VwSiiesProfesor source) => (VwSiiesProfesor)ShallowCloneMethod.Invoke(source, null)!;

    /// <summary>
    /// 2026-11-12: <see cref="HR.fn_SiiesProfesoresHoras"/> devuelve UNA fila por profesor con el documento vigente hoy (o el más reciente), así que
    /// un docente con contratos en varias facultades o con cambios de dependencia dentro del rango salía con una sola dependencia, y para
    /// períodos pasados con el documento de hoy. Aquí cada profesor se expande a una fila por documento del régimen que se solapa con el rango
    /// (sin contratos ni acciones ANULADOS; un mismo número de documento no se repite: queda el principal / el más reciente).
    /// Cada fila conserva los datos personales, de escalafón y las horas del distributivo; cambian el documento, sus fechas, la dependencia,
    /// la relación IES y las horas contratadas propias de cada contrato. Sin documentos en el rango se conserva la fila original.
    /// </summary>
    private async Task<List<VwSiiesProfesor>> ExpandByDocumentsAsync(List<VwSiiesProfesor> baseRows, DateTime desdeValue, DateTime hastaValue, CancellationToken ct)
    {
        if (baseRows.Count == 0) return baseRows;
        var desde = DateOnly.FromDateTime(desdeValue);
        var hasta = DateOnly.FromDateTime(hastaValue);
        var employeeIds = baseRows.Select(r => r.EmployeeID).Distinct().ToList();

        var rawDocuments = await (
            from r in _db.EmployeeLaborRegimes.AsNoTracking()
            where employeeIds.Contains(r.EmployeeId)
                  && r.EffectiveFrom <= hasta && (r.EffectiveTo == null || r.EffectiveTo >= desde)
            join d in _db.Departments.AsNoTracking() on r.DepartmentId equals (int?)d.DepartmentId into dj
            from d in dj.DefaultIfEmpty()
            join c in _db.Contracts.AsNoTracking() on r.SourceContractId equals (int?)c.ContractID into cj
            from c in cj.DefaultIfEmpty()
            join ctype in _db.ContractType.AsNoTracking() on (c != null ? (int?)c.ContractTypeID : null) equals (int?)ctype.ContractTypeId into ctj
            from ctype in ctj.DefaultIfEmpty()
            join rel1 in _db.RefTypes.AsNoTracking() on r.SiiesRelacionIesTypeId equals (int?)rel1.TypeId into rel1j
            from rel1 in rel1j.DefaultIfEmpty()
            join rel2 in _db.RefTypes.AsNoTracking() on (ctype != null ? ctype.SiiesRelacionIesTypeId : null) equals (int?)rel2.TypeId into rel2j
            from rel2 in rel2j.DefaultIfEmpty()
            select new
            {
                r.EmployeeId,
                r.Id,
                r.DocumentNumber,
                r.DocumentType,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.IsPrincipal,
                r.IngresoPorConcurso,
                r.SourcePersonnelActionId,
                ContractVoid = c != null && (c.Status == 208 || c.IsDeleted),
                DepartmentName = d != null ? d.Name : null,
                Hours = c != null ? c.ContractedHours : null,
                Relacion = rel1 != null && rel1.SiiesLabel != null ? rel1.SiiesLabel : (rel2 != null ? rel2.SiiesLabel : null)
            }).ToListAsync(ct);

        // Acciones de origen anuladas o eliminadas: se resuelven en una consulta aparte (sin subconsultas correlacionadas).
        var actionIds = rawDocuments.Where(x => x.SourcePersonnelActionId != null).Select(x => x.SourcePersonnelActionId!.Value).Distinct().ToList();
        var voidActionIds = actionIds.Count == 0
            ? new HashSet<int>()
            : (await _db.PersonnelActions.AsNoTracking()
                .Where(a => actionIds.Contains(a.ActionId) && (a.Status == "ANULADO" || a.IsDeleted))
                .Select(a => a.ActionId)
                .ToListAsync(ct)).ToHashSet();

        var documents = rawDocuments
            .Where(x => !x.ContractVoid && !(x.SourcePersonnelActionId != null && voidActionIds.Contains(x.SourcePersonnelActionId.Value)))
            .ToList();

        var byEmployee = documents
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => string.IsNullOrWhiteSpace(x.DocumentNumber) ? "#" + x.Id : x.DocumentNumber!.Trim().ToUpperInvariant())
                      .Select(dg => dg.OrderByDescending(x => x.IsPrincipal).ThenByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).First())
                      .OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Id)
                      .ToList());

        var expanded = new List<VwSiiesProfesor>();
        foreach (var baseRow in baseRows)
        {
            if (!byEmployee.TryGetValue(baseRow.EmployeeID, out var docs) || docs.Count == 0)
            {
                expanded.Add(baseRow);
                continue;
            }

            foreach (var doc in docs)
            {
                var sameAsBase = !string.IsNullOrWhiteSpace(baseRow.DocumentNumber) && !string.IsNullOrWhiteSpace(doc.DocumentNumber)
                                 && string.Equals(baseRow.DocumentNumber.Trim(), doc.DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase);
                var row = CloneRow(baseRow);
                if (!sameAsBase)
                {
                    row.DocumentNumber = doc.DocumentNumber;
                    row.RegimeDocumentType = doc.DocumentType;
                    row.EffectiveFrom = doc.EffectiveFrom;
                    row.EffectiveTo = doc.EffectiveTo;
                    row.IngresoPorConcurso = doc.IngresoPorConcurso ?? baseRow.IngresoPorConcurso;
                    if (!string.IsNullOrWhiteSpace(doc.DepartmentName)) row.DepartmentName = doc.DepartmentName;
                    if (!string.IsNullOrWhiteSpace(doc.Relacion)) row.RelacionIesSiiesLabel = doc.Relacion;
                    if (doc.Hours is > 0) row.ContractedHours = doc.Hours;
                }
                expanded.Add(row);
            }
        }
        return expanded;
    }

    private static string HomologateTipoDocumento(string? regimeDocumentType) => regimeDocumentType switch
    {
        "CONTRACT" => "CONTRATO",
        "PERSONNEL_ACTION" => "ACCION PERSONAL",
        _ => string.Empty
    };

    /// <summary>Construye las columnas comunes a ambas matrices (mismo orden oficial que 5.2 Profesores – Contratos IES).</summary>
    private static Dictionary<string, object?> BuildCommonRow(VwSiiesProfesor v, string codigoIes)
    {
        var esIndigena = string.Equals(v.EthnicitySiiesLabel, IndigenaLabel, StringComparison.OrdinalIgnoreCase);

        // 2026-09-10: HORAS ahora vienen del distributivo real (HR.tbl_AcademicHoursDistribution,
        // vía HR.fn_SiiesProfesoresHoras) cuando el profesor tiene esa información cargada. Si no
        // la tiene (hoy es el caso de todos los "Profesor Titular" — el distributivo cargado hasta
        // ahora es de personal con horas de docencia ocasional, ver hallazgo de esta sesión), se
        // mantiene el fallback anterior: horas contratadas como aproximación de HORAS_CLASE.
        var horasContratadas = v.ContractedHours ?? 0;
        var horasClase = v.ClassHours ?? (int)horasContratadas;

        return new Dictionary<string, object?>
        {
            ["CODIGO_IES"] = codigoIes,
            ["TIPO_IDENTIFICACION"] = v.IdentTypeName,
            ["IDENTIFICACION"] = v.IDCard,
            ["GENERO"] = v.GenderSiiesLabel ?? "NO DISPONE",
            ["SEXO"] = v.SexSiiesLabel ?? string.Empty,
            ["PAIS_ORIGEN"] = v.CountryName ?? v.CountryId ?? string.Empty,
            // 2026-09-03: mismo criterio que SiiesFuncionariosReportSource — p.Disability queda NULL
            // cuando no hay discapacidad (nunca se escribió "Ninguna" literal), pero el archivo real
            // entregado a CACES siempre trae "NINGUNA" en vez de vacío para ese caso.
            ["DISCAPACIDAD"] = v.DisabilitySiiesLabel ?? "NINGUNA",
            ["PORCENTAJE_DISCAPACIDAD"] = v.DisabilityPercentage ?? 0,
            ["NUMERO_CONADIS"] = string.Equals(v.DisabilitySiiesLabel, "NINGUNA", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(v.DisabilitySiiesLabel)
                ? string.Empty
                : (v.CONADISCard ?? "NO REGISTRA"),
            ["ETNIA"] = v.EthnicitySiiesLabel ?? "NO REGISTRA",
            ["NACIONALIDAD"] = esIndigena ? (v.IndigenousNationalitySiiesLabel ?? "NO REGISTRA") : "NO APLICA",
            ["EMAIL_INSTITUCIONAL"] = v.InstitutionalEmail ?? string.Empty,
            ["TIPO_DOCUMENTO"] = HomologateTipoDocumento(v.RegimeDocumentType),
            ["NUMERO_DOCUMENTO"] = v.DocumentNumber ?? string.Empty,
            ["CONTRATO_RELACIONADO"] = string.Empty,
            // Decisión de exportación 2026-08-27: NULL (sin clasificar) se exporta como "SI" —
            // solo false explícito exporta "NO" (mismo criterio que SiiesFuncionariosReportSource).
            ["INGRESO_POR_CONCURSO"] = v.IngresoPorConcurso == false ? "NO" : "SI",
            ["RELACION_IES"] = v.RelacionIesSiiesLabel ?? string.Empty,
            // 2026-10-08: condición exacta ya resuelta en HR.vw_SiiesProfesores (ver comentario
            // ahí) -- autoridad activa (Decano/Director/etc.) sin escalafón propio = "Laboral
            // Actual"; sin TeacherStructure y sin autoridad ni contrato de ocasional = "No
            // Aplica". Este fallback en C# solo cubre el caso residual que la vista no pudo
            // clasificar en absoluto (NULL puro).
            ["TIPO_ESCALAFON_NOMBRAMIENTO"] = v.TipoEscalafonNombramientoSiiesLabel ?? "NO APLICA",
            ["CATEGORIA"] = v.CategoriaSiiesLabel ?? string.Empty,
            ["TIEMPO_DEDICACION"] = v.TiempoDedicacionSiiesLabel ?? string.Empty,
            ["FECHA_INGRESO_IES"] = v.HireDate,
            ["FECHA_INICIO"] = v.EffectiveFrom,
            ["FECHA_FIN"] = v.EffectiveTo,
            ["NIVEL"] = v.NivelSiiesLabel ?? string.Empty,
            ["UNIDAD_ACADEMICA"] = v.DepartmentName ?? string.Empty,
            ["HORAS_CLASE"] = horasClase,
            ["HORAS_TUTORIA"] = v.TutoringHours ?? 0,
            ["HORAS_ADMINISTRATIVAS"] = v.ManagementHours ?? 0,
            ["HORAS_INVESTIGACION"] = v.ResearchHours ?? 0,
            ["HORAS_VINCULACION"] = v.OutreachHours ?? 0,
            ["HORAS_OTRAS_ACTIVIDADES"] = v.OtherActivitiesHours ?? 0,
            ["HORAS_CLASE_NIVEL_TECNICO"] = 0,
            ["HORAS_CLASE_TERCER_NIVEL"] = horasClase,
            ["HORAS_CLASE_CUARTO_NIVEL"] = 0,
            // Fuera del esquema oficial CACES — se agrega al final para no alterar el orden/
            // cantidad de las columnas oficiales (uso interno/verificación, no para la carga
            // masiva al SIIES).
            ["NOMBRE_COMPLETO"] = $"{v.LastName} {v.FirstName}".Trim(),
        };
    }

    /// <summary>Separa Apellidos/Nombres a nivel de reporte (no se toca tbl_People), mismo criterio que Funcionarios.</summary>
    private static (string primerApellido, string segundoApellido, string nombres) SplitNamesForPasaporte(VwSiiesProfesor v)
    {
        var (primerApellido, segundoApellido) = SiiesNameSplitter.Split(v.LastName);
        return (primerApellido, segundoApellido, v.FirstName ?? string.Empty);
    }

    protected async Task<string> GetCodigoIesAsync(CancellationToken ct) =>
        await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_IES" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

    private static readonly IReadOnlyList<ReportColumn> HeadColumns =
    [
        new("CODIGO_IES", "CODIGO_IES"),
        new("TIPO_IDENTIFICACION", "TIPO_IDENTIFICACION"),
        new("IDENTIFICACION", "IDENTIFICACION"),
    ];

    private static readonly IReadOnlyList<ReportColumn> PasaporteNameColumns =
    [
        new("PRIMER_APELLIDO", "PRIMER_APELLIDO"),
        new("SEGUNDO_APELLIDO", "SEGUNDO_APELLIDO"),
        new("NOMBRES", "NOMBRES"),
    ];

    private static readonly IReadOnlyList<ReportColumn> GeneroSexoColumns =
    [
        new("GENERO", "GENERO"),
        new("SEXO", "SEXO"),
    ];

    private static readonly IReadOnlyList<ReportColumn> FechaNacimientoColumn =
    [
        new("FECHA_NACIMIENTO", "FECHA_NACIMIENTO"),
    ];

    private static readonly IReadOnlyList<ReportColumn> TailColumns =
    [
        new("PAIS_ORIGEN", "PAIS_ORIGEN"),
        new("DISCAPACIDAD", "DISCAPACIDAD"),
        new("PORCENTAJE_DISCAPACIDAD", "PORCENTAJE_DISCAPACIDAD"),
        new("NUMERO_CONADIS", "NUMERO_CONADIS"),
        new("ETNIA", "ETNIA"),
        new("NACIONALIDAD", "NACIONALIDAD"),
        new("EMAIL_INSTITUCIONAL", "EMAIL_INSTITUCIONAL"),
        new("TIPO_DOCUMENTO", "TIPO_DOCUMENTO"),
        new("NUMERO_DOCUMENTO", "NUMERO_DOCUMENTO"),
        new("CONTRATO_RELACIONADO", "CONTRATO_RELACIONADO"),
        new("INGRESO_POR_CONCURSO", "INGRESO_POR_CONCURSO"),
        new("RELACION_IES", "RELACION_IES"),
        new("TIPO_ESCALAFON_NOMBRAMIENTO", "TIPO_ESCALAFON_NOMBRAMIENTO"),
        new("CATEGORIA", "CATEGORIA"),
        new("TIEMPO_DEDICACION", "TIEMPO_DEDICACION"),
        new("FECHA_INGRESO_IES", "FECHA_INGRESO_IES"),
        new("FECHA_INICIO", "FECHA_INICIO"),
        new("FECHA_FIN", "FECHA_FIN"),
        new("NIVEL", "NIVEL"),
        new("UNIDAD_ACADEMICA", "UNIDAD_ACADEMICA"),
        new("HORAS_CLASE", "HORAS_CLASE"),
        new("HORAS_TUTORIA", "HORAS_TUTORIA"),
        new("HORAS_ADMINISTRATIVAS", "HORAS_ADMINISTRATIVAS"),
        new("HORAS_INVESTIGACION", "HORAS_INVESTIGACION"),
        new("HORAS_VINCULACION", "HORAS_VINCULACION"),
        new("HORAS_OTRAS_ACTIVIDADES", "HORAS_OTRAS_ACTIVIDADES"),
        new("HORAS_CLASE_NIVEL_TECNICO", "HORAS_CLASE_NIVEL_TECNICO"),
        new("HORAS_CLASE_TERCER_NIVEL", "HORAS_CLASE_TERCER_NIVEL"),
        new("HORAS_CLASE_CUARTO_NIVEL", "HORAS_CLASE_CUARTO_NIVEL"),
        // Fuera del esquema oficial CACES — ver comentario en BuildCommonRow.
        new("NOMBRE_COMPLETO", "NOMBRE_COMPLETO"),
    ];

    /// <summary>Columnas oficiales de la matriz 5.2 Profesores – Contratos IES (cédula) + 5.4 fusionadas.</summary>
    protected static IReadOnlyList<ReportColumn> CedulaColumns =>
        [.. HeadColumns, .. GeneroSexoColumns, .. TailColumns];

    /// <summary>Columnas oficiales de la matriz 5.3 Profesores – Contratos IES – Pasaporte + 5.4 fusionadas.</summary>
    protected static IReadOnlyList<ReportColumn> PasaporteColumns =>
        [.. HeadColumns, .. PasaporteNameColumns, .. GeneroSexoColumns, .. FechaNacimientoColumn, .. TailColumns];

    protected static IReadOnlyDictionary<string, object?> BuildRowCedula(VwSiiesProfesor v, string codigoIes) =>
        BuildCommonRow(v, codigoIes);

    protected static IReadOnlyDictionary<string, object?> BuildRowPasaporte(VwSiiesProfesor v, string codigoIes)
    {
        var row = BuildCommonRow(v, codigoIes);
        var (primerApellido, segundoApellido, nombres) = SplitNamesForPasaporte(v);
        row["PRIMER_APELLIDO"] = primerApellido;
        row["SEGUNDO_APELLIDO"] = segundoApellido;
        row["NOMBRES"] = nombres;
        row["FECHA_NACIMIENTO"] = v.BirthDate;
        return row;
    }
}

/// <summary>Reporte SIIES Profesores — fusiona matrices 5.2/5.3 (Contratos) y 5.4 (Horas).</summary>
public sealed class SiiesProfesoresReportSource : SiiesProfesoresReportSourceBase, IReportSource
{
    public ReportType ReportType => ReportType.SiiesProfesores;

    public SiiesProfesoresReportSource(AppDbContext db, ILogger<SiiesProfesoresReportSource> logger)
        : base(db, logger) { }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        // Cualquier valor nulo, vacío o desconocido cae a TODOS (igual que Funcionarios).
        var requested = filter.IdentType?.Trim();
        var identType = string.Equals(requested, "PASAPORTE", StringComparison.OrdinalIgnoreCase) ? "PASAPORTE"
            : string.Equals(requested, "CEDULA", StringComparison.OrdinalIgnoreCase) ? "CEDULA"
            : AllIdentTypes;
        var esPasaporte = identType == "PASAPORTE";
        var esTodos = identType == AllIdentTypes;

        var codigoIes = await GetCodigoIesAsync(context.RequestAborted);
        var teachers = await GetTeachersAsync(identType, filter, context.RequestAborted);

        _logger.LogInformation("SiiesProfesoresReportSource: IdentType={IdentType}, {Count} registros.", identType, teachers.Count);

        // "TODOS": columnas de la matriz de cédula (el nombre completo basta); los campos propios del pasaporte solo salen
        // en PASAPORTE, que es lo que SIIES exige para cargar esa matriz.
        var rows = teachers
            .Select(v => esPasaporte
                ? BuildRowPasaporte(v, codigoIes)
                : BuildRowCedula(v, codigoIes))
            .ToList();

        return new ReportDefinition
        {
            Title = esTodos ? "SIIES - Profesores (Cédula y Pasaporte)"
                : esPasaporte ? "SIIES - Profesores Pasaporte" : "SIIES - Profesores (Cédula)",
            FilePrefix = esTodos ? "SIIES_Profesores_Todos"
                : esPasaporte ? "SIIES_Profesores_Pasaporte" : "SIIES_Profesores_Cedula",
            Subtitle = await SiiesReportSubtitle.ForTeachersAsync(_db, filter, rows.Count, context.RequestAborted),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = esPasaporte ? PasaporteColumns : CedulaColumns,
            PdfPagination = PdfPagination.Expand,
            Rows = rows,
            Orientation = PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }
}
