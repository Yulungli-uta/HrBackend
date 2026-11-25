using WsUtaSystem.Application.Interfaces.Services;
using System.Data;
using Dapper;
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
/// Origen de datos único para el reporte SIIES Funcionarios — cubre tanto la matriz 5.7
/// (Funcionarios, TIPO_IDENTIFICACION=CEDULA) como la 5.8 (Funcionario Pasaporte,
/// TIPO_IDENTIFICACION=PASAPORTE) del Instructivo Carga Masiva CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// SIIES prohíbe mezclar CEDULA y PASAPORTE en un mismo archivo de carga. Por eso
/// <see cref="ReportFilterDto.IdentType"/> "CEDULA" o "PASAPORTE" genera un único archivo de ese tipo
/// (la matriz oficial 5.7 o 5.8). "TODOS", que es el valor por defecto cuando no se especifica,
/// junta ambos en un solo archivo con las columnas de la matriz 5.8 (las columnas propias del
/// pasaporte quedan vacías en las filas de cédula): sirve para revisión interna, no para cargar en SIIES.
/// </para>
/// <para>
/// Consulta HR.vw_SiiesFuncionarios (joins y homologación de catálogos ya resueltos en la vista)
/// y aplica aquí las reglas condicionales SIIES que no corresponden a un simple join:
/// NACIONALIDAD según ETNIA, TIPO/CATEGORIA_DOCENTE_LOSEP según TIPO_FUNCIONARIO,
/// NUMERO_CONADIS vacío si DISCAPACIDAD=NINGUNA, y separación de nombres para pasaporte.
/// </para>
/// <para>
/// Decisión documentada (actualizada 2026-08-27): INGRESO_POR_CONCURSO es obligatorio en el
/// archivo SIIES pero en BD puede estar NULL (sin clasificar todavía). Se exporta como "SI"
/// cuando no se ha clasificado (antes era "NO"); solo un false explícito exporta "NO".
/// </para>
/// </remarks>
public sealed class SiiesFuncionariosReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<SiiesFuncionariosReportSource> _logger;
    private readonly ITestDataPolicy _testDataPolicy;

    private const string DocenteLoesLabel = "DOCENTE LOES";
    private const string IndigenaLabel = "INDIGENA";
    private const string AllIdentTypes = "TODOS";
    // Sin tipo especificado se trae cédula y pasaporte juntos; cada matriz por separado se pide explícitamente
    private const string DefaultIdentType = AllIdentTypes;

    public ReportType ReportType => ReportType.SiiesFuncionarios;

    public SiiesFuncionariosReportSource(AppDbContext db, ILogger<SiiesFuncionariosReportSource> logger, ITestDataPolicy testDataPolicy)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testDataPolicy = testDataPolicy ?? throw new ArgumentNullException(nameof(testDataPolicy));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var identType = NormalizeIdentType(filter.IdentType);
        var esPasaporte = identType == "PASAPORTE";
        var esTodos = identType == AllIdentTypes;

        var (codigoIes, codigoMatriz) = await GetInstitutionalParametersAsync(context.RequestAborted);
        var employees = await GetEmployeesAsync(identType, filter, context.RequestAborted);
        await ApplyReportDefaultsAsync(employees, context.RequestAborted);

        _logger.LogInformation(
            "SiiesFuncionariosReportSource: IdentType={IdentType}, {Count} registros.",
            identType, employees.Count);

        // "TODOS": un solo archivo con cédula y pasaporte y las columnas de la matriz de cédula: el nombre completo basta
        // para identificar a cada persona. PRIMER_APELLIDO, SEGUNDO_APELLIDO, NOMBRES y FECHA_NACIMIENTO solo salen en
        // PASAPORTE, que es lo que SIIES exige para cargar esa matriz. Es para revisión: SIIES pide cargarlos por separado.
        var rows = employees
            .Select(v => esPasaporte
                ? BuildPasaporteRow(v, codigoIes, codigoMatriz)
                : BuildCedulaRow(v, codigoIes, codigoMatriz))
            .ToList();

        return new ReportDefinition
        {
            Title = esTodos ? "SIIES - Funcionarios (Cédula y Pasaporte)"
                : esPasaporte ? "SIIES - Funcionario Pasaporte" : "SIIES - Funcionarios (Cédula)",
            FilePrefix = esTodos ? "SIIES_Funcionarios_Todos"
                : esPasaporte ? "SIIES_Funcionario_Pasaporte" : "SIIES_Funcionarios_Cedula",
            Subtitle = SiiesReportSubtitle.ForEmployees(filter, rows.Count),
            PeriodStart = filter.StartDate.HasValue && filter.EndDate.HasValue ? filter.StartDate : null,
            PeriodEnd = filter.StartDate.HasValue && filter.EndDate.HasValue ? filter.EndDate : null,
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

    /// <summary>
    /// Normaliza <see cref="ReportFilterDto.IdentType"/> a "CEDULA", "PASAPORTE" o "TODOS".
    /// Cualquier valor nulo, vacío o desconocido cae a TODOS (cédula y pasaporte en un mismo archivo,
    /// para revisión: SIIES exige cargar cada matriz por separado, para eso se pide CEDULA o PASAPORTE).
    /// </summary>
    private static string NormalizeIdentType(string? identType)
    {
        var value = identType?.Trim();
        if (string.Equals(value, "PASAPORTE", StringComparison.OrdinalIgnoreCase)) return "PASAPORTE";
        if (string.Equals(value, "CEDULA", StringComparison.OrdinalIgnoreCase)) return "CEDULA";
        return DefaultIdentType;
    }

    private IReadOnlyDictionary<string, object?> BuildPasaporteRow(VwSiiesFuncionario v, string codigoIes, string codigoMatriz)
    {
        var row = BuildCommonRow(v, codigoIes, codigoMatriz);
        var (primerApellido, segundoApellido, nombres) = SplitNamesForPasaporte(v);
        row["PRIMER_APELLIDO"] = primerApellido;
        row["SEGUNDO_APELLIDO"] = segundoApellido;
        row["NOMBRES"] = nombres;
        row["FECHA_NACIMIENTO"] = v.BirthDate;
        return row;
    }

    /// <summary>Horas laborables por semana que se asumen para el personal LOSEP cuando el documento no las trae.</summary>
    private const decimal LosepDefaultWeeklyHours = 40m;

    /// <summary>
    /// Completa datos vacíos de la fila antes de armar el reporte (aplica por igual a la consulta con rango de fechas y a la
    /// vista sin fechas):
    /// <list type="bullet">
    /// <item>HORAS_LABORABLES_SEMANA vacías (o 0) en régimen LOSEP: 40 horas.</item>
    /// <item>CARGO y UNIDAD_ACADEMICA vacíos: se toman del cargo y la dependencia de destino de la acción de personal que respalda la
    /// fila (la que muestra DocumentNumber), porque una corrección de la acción no actualiza al empleado ni a su régimen.</item>
    /// </list>
    /// Solo se rellena lo que está vacío: nunca se pisa un dato existente.
    /// </summary>
    private async Task ApplyReportDefaultsAsync(List<VwSiiesFuncionario> employees, CancellationToken ct)
    {
        foreach (var v in employees)
        {
            if ((v.ContractedHours is null or <= 0) &&
                string.Equals(v.LaborRegimeName, "LOSEP", StringComparison.OrdinalIgnoreCase))
                v.ContractedHours = LosepDefaultWeeklyHours;
        }

        var documentNumbers = employees
            .Where(v => (string.IsNullOrWhiteSpace(v.DepartmentName) || string.IsNullOrWhiteSpace(v.JobDescription))
                        && !string.IsNullOrWhiteSpace(v.DocumentNumber))
            .Select(v => v.DocumentNumber!)
            .Distinct()
            .ToList();
        if (documentNumbers.Count == 0) return;

        var fromActions = await (
            from a in _db.PersonnelActions.AsNoTracking()
            where !a.IsDeleted && a.ActionNumber != null && documentNumbers.Contains(a.ActionNumber)
            join d in _db.Departments.AsNoTracking() on a.DestinationDepartmentId equals (int?)d.DepartmentId into dj
            from d in dj.DefaultIfEmpty()
            join j in _db.Jobs.AsNoTracking() on a.DestinationJobId equals (int?)j.JobID into jj
            from j in jj.DefaultIfEmpty()
            select new { a.ActionNumber, a.Status, DepartmentName = d != null ? d.Name : null, JobDescription = j != null ? j.Description : null })
            .ToListAsync(ct);

        // Una acción vigente gana sobre una anulada si el número se repitiera.
        var byNumber = fromActions
            .GroupBy(x => x.ActionNumber!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Status == "ANULADO" ? 1 : 0).First(), StringComparer.OrdinalIgnoreCase);

        foreach (var v in employees)
        {
            if (string.IsNullOrWhiteSpace(v.DocumentNumber) || !byNumber.TryGetValue(v.DocumentNumber, out var action)) continue;
            if (string.IsNullOrWhiteSpace(v.DepartmentName) && !string.IsNullOrWhiteSpace(action.DepartmentName))
                v.DepartmentName = action.DepartmentName;
            if (string.IsNullOrWhiteSpace(v.JobDescription) && !string.IsNullOrWhiteSpace(action.JobDescription))
                v.JobDescription = action.JobDescription;
        }
    }

    private IReadOnlyDictionary<string, object?> BuildCedulaRow(VwSiiesFuncionario v, string codigoIes, string codigoMatriz) =>
        BuildCommonRow(v, codigoIes, codigoMatriz);

    private async Task<List<VwSiiesFuncionario>> GetEmployeesAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        // [2026-09-21] Con rango de fechas: busqueda historica real (quien estuvo vigente
        // -via regimen, contrato o accion de personal- en algun momento del rango, sin
        // importar si hoy sigue activo). HR.vw_SiiesFuncionarios no sirve para esto (ver
        // GetEmployeesVigentesEnRangoAsync) - se consulta aparte, vía SQL parametrizado.
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            return await GetEmployeesVigentesEnRangoAsync(identTypeName, filter, ct);

        // 2026-10-08: el reporte SIIES debe traer a TODOS los que laboraron en el período, estén
        // o no activos hoy -- se quitó el filtro de activo/inactivo por defecto (antes excluía
        // inactivos salvo que se marque IncludeInactive). Sin StartDate/EndDate no hay período
        // explícito, así que se trae el universo completo de la vista sin recorte por estado.
        var query = _db.vwSiiesFuncionarios.AsNoTracking();
        query = identTypeName == AllIdentTypes
            ? query.Where(v => v.IdentTypeName == "CEDULA" || v.IdentTypeName == "PASAPORTE")
                   .OrderBy(v => v.IdentTypeName).ThenBy(v => v.LastName)
            : query.Where(v => v.IdentTypeName == identTypeName);

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        var current = await query.ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Now);
        return await AddTitularAuthorityRowsAsync(current, identTypeName, filter, today, today, ct);
    }

    /// <summary>
    /// [2026-09-21] Busqueda historica real para el filtro de fecha desde/hasta: encuentra
    /// empleados cuyo regimen laboral, contrato o accion de personal estuvo vigente en ALGUN
    /// momento dentro de [FechaDesde, FechaHasta], sin importar si hoy siguen activos.
    ///
    /// HR.vw_SiiesFuncionarios (usada por GetEmployeesAsync sin fechas) NO sirve para esto:
    /// su cascada (OUTER APPLY con TOP 1 ... ORDER BY prioridad) ya elige "la fila vigente de
    /// HOY" antes de que cualquier filtro externo pueda actuar - alguien inactivo hoy nunca
    /// aparecería ahí aunque haya estado vigente en el rango pedido. Por decision explicita
    /// del usuario (analisis 2026-09-21), esta consulta vive aqui como SQL parametrizado
    /// (Dapper) en vez de como un objeto nuevo en la base de datos: replica la MISMA cascada
    /// de la vista (Database/hr/15_siies_funcionarios.sql, regimen -> contrato -> accion) pero
    /// evaluando vigencia contra el rango en vez de contra GETDATE(). Un solo renglon por
    /// empleado (el mas relevante: regimen principal > mas reciente).
    ///
    /// IMPORTANTE: si se cambia la cascada de HR.vw_SiiesFuncionarios (joins, exclusiones de
    /// Status, calculo de TIPO_FUNCIONARIO), replicar el mismo cambio aqui - son dos copias
    /// de la misma logica de negocio a proposito (ver Opcion C del analisis: evita crear un
    /// objeto SQL nuevo, a costa de mantener la cascada en dos lugares).
    /// </summary>
    private async Task<List<VwSiiesFuncionario>> GetEmployeesVigentesEnRangoAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        var rows = await GetEmployeesVigentesEnRangoBaseAsync(identTypeName, filter, ct);
        rows = await AddMovementActionRowsAsync(rows, filter.StartDate!.Value, filter.EndDate!.Value, ct);
        return await AddTitularAuthorityRowsAsync(rows, identTypeName, filter, DateOnly.FromDateTime(filter.StartDate!.Value), DateOnly.FromDateTime(filter.EndDate!.Value), ct);
    }

    private static readonly System.Reflection.MethodInfo ShallowCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    private static VwSiiesFuncionario CloneRow(VwSiiesFuncionario source) => (VwSiiesFuncionario)ShallowCloneMethod.Invoke(source, null)!;

    private static string NormalizeName(string value) =>
        new string(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).Trim().ToUpperInvariant();

    /// <summary>Distinto solo cuando AMBOS lados tienen dato y difieren: un vacío no es un cambio.</summary>
    private static bool Differs(string? current, string? previous) =>
        !string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(previous) && NormalizeName(current) != NormalizeName(previous);

    /// <summary>Encargo y subrogación (y sus subtipos) son movimientos temporales; las finalizaciones no.</summary>
    private static bool IsTemporaryMovement(string typeCode, string? parentCode)
    {
        if (typeCode.StartsWith("FINALIZACION", StringComparison.OrdinalIgnoreCase)) return false;
        return typeCode is "ENCARGO" or "SUBROGACION_GRUPO" || parentCode is "ENCARGO" or "SUBROGACION_GRUPO";
    }

    /// <summary>
    /// 2026-11-12: además de una fila por documento del régimen (contrato/acción), el reporte con fechas agrega una fila por cada acción de
    /// personal que EMPIEZA dentro del rango y cambia el cargo o la dependencia respecto al registro anterior de la misma persona
    /// (ingreso, nombramiento, traslado, cambio administrativo, reintegro, ubicación, encargo, subrogación...).
    /// 2026-11-18: encargo y subrogación SÍ generan fila (son movimiento de puesto y cargo); al llegar su fecha fin se agrega una fila
    /// de retorno al cargo titular el día siguiente, salvo que otra acción empiece ese día o antes.
    /// La comparación es contra el último documento que empezó antes, y solo con datos conocidos de ambos lados.
    /// La acción que ya es el documento de una fila no se repite. El documento anterior termina el día previo al cambio.
    /// </summary>
    private async Task<List<VwSiiesFuncionario>> AddMovementActionRowsAsync(List<VwSiiesFuncionario> rows, DateTime desdeValue, DateTime hastaValue, CancellationToken ct)
    {
        if (rows.Count == 0) return rows;
        var desde = DateOnly.FromDateTime(desdeValue);
        var hasta = DateOnly.FromDateTime(hastaValue);
        var employeeIds = rows.Select(r => r.EmployeeID).Distinct().ToList();
        // 2026-11-17: las acciones de un docente (su docencia LOES) se reportan en Profesores; aqui solo salen las filas LOSEP de la consulta
        // base. No se le agregan filas de movimientos.
        var teacherIds = await _db.TeacherStructures.AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)).Select(t => t.EmployeeId).Distinct().ToListAsync(ct);
        var titularIds = await (from e in _db.Employees.AsNoTracking()
                                join jb in _db.Jobs.AsNoTracking() on e.JobId equals (int?)jb.JobID
                                where employeeIds.Contains(e.EmployeeId) && jb.Description != null && jb.Description.StartsWith("PROFESOR TITULAR")
                                select e.EmployeeId).Distinct().ToListAsync(ct);
        var excludedFromMovements = teacherIds.Concat(titularIds).ToHashSet();
        employeeIds = employeeIds.Where(id => !excludedFromMovements.Contains(id)).ToList();
        if (employeeIds.Count == 0) return rows;
        var knownDocuments = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.DocumentNumber))
            .Select(r => (r.EmployeeID, r.DocumentNumber!.Trim().ToUpperInvariant()))
            .ToHashSet();

        var actions = await (
            from a in _db.PersonnelActions.AsNoTracking()
            join t in _db.PersonnelActionTypes.AsNoTracking() on a.ActionTypeId equals t.PersonnelActionTypeId
            join pt in _db.PersonnelActionTypes.AsNoTracking() on t.ParentTypeId equals (int?)pt.PersonnelActionTypeId into ptj
            from pt in ptj.DefaultIfEmpty()
            join d in _db.Departments.AsNoTracking() on a.DestinationDepartmentId equals (int?)d.DepartmentId into dj
            from d in dj.DefaultIfEmpty()
            join j in _db.Jobs.AsNoTracking() on a.DestinationJobId equals (int?)j.JobID into jj
            from j in jj.DefaultIfEmpty()
            where !a.IsDeleted && a.EmployeeId != null && employeeIds.Contains(a.EmployeeId.Value)
                  && (a.Status == "VIGENTE" || a.Status == "FINALIZADO")
                  && a.EffectiveDate != null && a.EffectiveDate >= desde && a.EffectiveDate <= hasta
                  && (a.DestinationDepartmentId != null || a.DestinationJobId != null)
                  && (t.ActionCategory == "MOVEMENT" || t.ActionCategory == "ENTRY" || t.ActionCategory == "SALARY_CHANGE")
            select new
            {
                EmployeeId = a.EmployeeId!.Value,
                a.ActionNumber,
                EffectiveDate = a.EffectiveDate!.Value,
                a.EndDate,
                TypeCode = t.Code,
                ParentCode = pt != null ? pt.Code : null,
                DepartmentName = d != null ? d.Name : null,
                JobDescription = j != null ? j.Description : null
            }).ToListAsync(ct);

        if (actions.Count == 0) return rows;

        var result = new List<VwSiiesFuncionario>(rows);
        // Encargo/subrogación: movimiento temporal. Al terminar (fecha fin real) se agrega una fila de retorno al cargo titular.
        var pendingReturns = new Dictionary<int, (VwSiiesFuncionario Titular, DateOnly ReturnDate)>();
        void FlushReturns(int employeeId, DateOnly? before)
        {
            if (!pendingReturns.TryGetValue(employeeId, out var pending)) return;
            // Si otra acción empieza el mismo día del retorno (o antes), esa acción toma el relevo.
            if (before is { } limit && pending.ReturnDate >= limit) return;
            pendingReturns.Remove(employeeId);
            if (pending.ReturnDate > hasta) return;
            if (pending.Titular.EffectiveTo is { } titularEnd && titularEnd < pending.ReturnDate) return;
            var back = CloneRow(pending.Titular);
            back.EffectiveFrom = pending.ReturnDate;
            var lastIdx = result.FindLastIndex(r => r.EmployeeID == employeeId);
            result.Insert(lastIdx + 1, back);
        }

        foreach (var act in actions.OrderBy(a => a.EmployeeId).ThenBy(a => a.EffectiveDate).ThenBy(a => a.ActionNumber))
        {
            FlushReturns(act.EmployeeId, act.EffectiveDate);
            if (string.IsNullOrWhiteSpace(act.ActionNumber)) continue;
            var number = act.ActionNumber.Trim();
            if (knownDocuments.Contains((act.EmployeeId, number.ToUpperInvariant()))) continue;

            var previous = result
                .Where(r => r.EmployeeID == act.EmployeeId && r.EffectiveFrom is { } from && from < act.EffectiveDate)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            if (previous is null) continue;
            if (!Differs(act.DepartmentName, previous.DepartmentName) && !Differs(act.JobDescription, previous.JobDescription)) continue;

            var row = CloneRow(previous);
            row.DocumentNumber = number;
            row.EffectiveFrom = act.EffectiveDate;
            row.EffectiveTo = act.EndDate is { Year: >= 9999 } ? null : act.EndDate;
            row.RegimeDocumentType = "PERSONNEL_ACTION";
            if (!string.IsNullOrWhiteSpace(act.DepartmentName)) row.DepartmentName = act.DepartmentName;
            if (!string.IsNullOrWhiteSpace(act.JobDescription)) row.JobDescription = act.JobDescription;

            var isTemporary = IsTemporaryMovement(act.TypeCode, act.ParentCode);
            if (isTemporary && act.EndDate is { Year: < 9999 } endDate)
            {
                var returnDate = endDate.AddDays(1);
                if (pendingReturns.TryGetValue(act.EmployeeId, out var existing))
                    pendingReturns[act.EmployeeId] = (existing.Titular, returnDate > existing.ReturnDate ? returnDate : existing.ReturnDate);
                else
                    pendingReturns[act.EmployeeId] = (CloneRow(previous), returnDate);
            }
            else if (!isTemporary)
            {
                // Un cambio definitivo (traslado, nombramiento...) reemplaza al titular: ya no hay retorno que restituir.
                pendingReturns.Remove(act.EmployeeId);
            }

            if (previous.EffectiveTo is null || previous.EffectiveTo >= act.EffectiveDate)
                previous.EffectiveTo = act.EffectiveDate.AddDays(-1);

            var lastIndex = result.FindLastIndex(r => r.EmployeeID == act.EmployeeId);
            result.Insert(lastIndex + 1, row);
            knownDocuments.Add((act.EmployeeId, number.ToUpperInvariant()));
        }
        foreach (var employeeId in pendingReturns.Keys.ToList()) FlushReturns(employeeId, null);
        return result;
    }

    /// <summary>
    /// 2026-11-26: un docente titular que además es autoridad (Decano, Director, Vicerrector...) tiene dos documentos: su nombramiento docente
    /// (sale en Profesores) y la acción que lo designó autoridad (sale aquí). Funcionarios excluye las acciones de los docentes, así que esa
    /// designación no salía (24 de 38 titulares con autoridad no aparecían) o salía con el número del nombramiento docente.
    /// Por cada autoridad vigente en el rango se agrega una fila DIRECTIVO con el documento resuelto en este orden:
    /// 1) la acción de designación (Nombramiento, Designación, Encargo y sus subtipos) cuyo destino es la dependencia (y el cargo, si la autoridad lo tiene);
    /// 2) la última acción de personal con ese destino; 3) ninguna: la autoridad se omite y se registra una advertencia para que RRHH la complete.
    /// No se repite un documento que el empleado ya tiene en el reporte. La fila toma los datos personales de una fila existente o, si el titular
    /// no tiene ninguna, de la misma consulta base limitada a ese empleado.
    /// </summary>
    private async Task<List<VwSiiesFuncionario>> AddTitularAuthorityRowsAsync(
        List<VwSiiesFuncionario> rows, string identTypeName, ReportFilterDto filter, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var authorities = await _db.DepartmentAuthorities.AsNoTracking()
            .Where(a => a.StartDate <= hasta && (a.EndDate == null || a.EndDate >= desde)
                        && (a.IsActive || (a.EndDate != null && a.EndDate > a.StartDate)))
            .Select(a => new { a.EmployeeId, a.DepartmentId, a.JobId, a.StartDate, a.EndDate })
            .ToListAsync(ct);
        if (authorities.Count == 0) return rows;

        var candidateIds = authorities.Select(a => a.EmployeeId).Distinct().ToList();
        var teacherIds = await _db.TeacherStructures.AsNoTracking()
            .Where(t => candidateIds.Contains(t.EmployeeId)).Select(t => t.EmployeeId).Distinct().ToListAsync(ct);
        var titularIds = await (from e in _db.Employees.AsNoTracking()
                                join jb in _db.Jobs.AsNoTracking() on e.JobId equals (int?)jb.JobID
                                where candidateIds.Contains(e.EmployeeId) && jb.Description != null && jb.Description.StartsWith("PROFESOR TITULAR")
                                select e.EmployeeId).Distinct().ToListAsync(ct);
        var teachers = teacherIds.Concat(titularIds).ToHashSet();
        authorities = authorities.Where(a => teachers.Contains(a.EmployeeId)).ToList();
        if (authorities.Count == 0) return rows;

        var employeeIds = authorities.Select(a => a.EmployeeId).Distinct().ToList();
        var actions = await (
            from a in _db.PersonnelActions.AsNoTracking()
            join t in _db.PersonnelActionTypes.AsNoTracking() on a.ActionTypeId equals t.PersonnelActionTypeId
            join pt in _db.PersonnelActionTypes.AsNoTracking() on t.ParentTypeId equals (int?)pt.PersonnelActionTypeId into ptj
            from pt in ptj.DefaultIfEmpty()
            where !a.IsDeleted && a.EmployeeId != null && employeeIds.Contains(a.EmployeeId.Value)
                  && (a.Status == "VIGENTE" || a.Status == "FIRMADO_CARGADO" || a.Status == "FINALIZADO")
                  && a.EffectiveDate != null && a.EffectiveDate <= hasta
                  && a.DestinationDepartmentId != null && a.ActionNumber != null
                  && !t.Code.StartsWith("FINALIZACION")
            select new
            {
                EmployeeId = a.EmployeeId!.Value,
                a.ActionNumber,
                EffectiveDate = a.EffectiveDate!.Value,
                a.EndDate,
                DestinationDepartmentId = a.DestinationDepartmentId!.Value,
                a.DestinationJobId,
                TypeCode = t.Code,
                ParentCode = pt != null ? pt.Code : null,
                t.SiiesRelacionIesTypeId
            }).ToListAsync(ct);

        static bool IsDesignation(string typeCode, string? parentCode) =>
            typeCode is "DESIGNACION" or "NOMBRAMIENTO" or "ENCARGO" || parentCode is "NOMBRAMIENTO" or "ENCARGO";

        // Una entrada por (empleado, documento): varias autoridades pueden compartir la misma acción.
        var resolved = new Dictionary<(int EmployeeId, string Document), AuthorityDocument>();
        var withoutDocument = new List<int>();
        foreach (var authority in authorities)
        {
            var matches = actions
                .Where(x => x.EmployeeId == authority.EmployeeId && x.DestinationDepartmentId == authority.DepartmentId
                            && (authority.JobId == null || x.DestinationJobId == authority.JobId))
                .OrderByDescending(x => x.EffectiveDate)
                .ToList();
            var chosen = matches.FirstOrDefault(x => IsDesignation(x.TypeCode, x.ParentCode)) ?? matches.FirstOrDefault();
            if (chosen is null) { withoutDocument.Add(authority.EmployeeId); continue; }

            var number = chosen.ActionNumber!.Trim();
            var key = (authority.EmployeeId, number.ToUpperInvariant());
            var authorityEnd = authority.EndDate is { Year: < 9999 } end ? end : (DateOnly?)null;
            if (!resolved.TryGetValue(key, out var item))
            {
                resolved[key] = new AuthorityDocument(authority.EmployeeId, number, chosen.EffectiveDate, authorityEnd, authority.DepartmentId,
                    authority.JobId ?? chosen.DestinationJobId, chosen.SiiesRelacionIesTypeId);
            }
            else
            {
                // Misma acción para varias autoridades: queda abierta si alguna lo está; si no, la fecha de fin más tardía.
                var to = item.EffectiveTo is null || authorityEnd is null ? null : (item.EffectiveTo > authorityEnd ? item.EffectiveTo : authorityEnd);
                resolved[key] = item with { EffectiveTo = to };
            }
        }

        if (withoutDocument.Count > 0)
            _logger.LogWarning(
                "SIIES Funcionarios: {Count} autoridad(es) de docentes titulares sin acción de personal que las respalde; no se agregaron al reporte. EmployeeIds: {EmployeeIds}",
                withoutDocument.Count, string.Join(", ", withoutDocument.Distinct()));
        if (resolved.Count == 0) return rows;

        // Datos personales: de una fila existente del empleado; si no tiene ninguna, de la consulta base limitada a esos empleados.
        var templates = rows.GroupBy(r => r.EmployeeID).ToDictionary(g => g.Key, g => g.First());
        var missing = resolved.Values.Select(r => r.EmployeeId).Distinct().Where(id => !templates.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var rangeFilter = new ReportFilterDto
            {
                StartDate = desde.ToDateTime(TimeOnly.MinValue),
                EndDate = hasta.ToDateTime(TimeOnly.MinValue),
                Identification = filter.Identification
            };
            foreach (var template in await GetEmployeesVigentesEnRangoBaseAsync(identTypeName, rangeFilter, ct, missing))
                templates.TryAdd(template.EmployeeID, template);
        }

        var departmentIds = resolved.Values.Select(r => r.DepartmentId).Distinct().ToList();
        var departments = await _db.Departments.AsNoTracking().Where(d => departmentIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);
        var jobIds = resolved.Values.Where(r => r.JobId != null).Select(r => r.JobId!.Value).Distinct().ToList();
        var jobs = await _db.Jobs.AsNoTracking().Where(j => jobIds.Contains(j.JobID))
            .ToDictionaryAsync(j => j.JobID, j => new { j.Description, j.PuestoJerarquicoSuperior }, ct);
        var relationIds = resolved.Values.Where(r => r.RelationTypeId != null).Select(r => r.RelationTypeId!.Value).Distinct().ToList();
        var relations = await _db.RefTypes.AsNoTracking().Where(t => relationIds.Contains(t.TypeId))
            .ToDictionaryAsync(t => t.TypeId, t => t.SiiesLabel, ct);

        var result = new List<VwSiiesFuncionario>(rows);
        var knownDocuments = rows.Where(r => !string.IsNullOrWhiteSpace(r.DocumentNumber))
            .Select(r => (r.EmployeeID, r.DocumentNumber!.Trim().ToUpperInvariant())).ToHashSet();

        foreach (var item in resolved.Values.OrderBy(r => r.EmployeeId).ThenBy(r => r.EffectiveFrom))
        {
            if (knownDocuments.Contains((item.EmployeeId, item.DocumentNumber.ToUpperInvariant())))
            {
                // El documento ya sale (p. ej. por su fila de régimen LOSEP), pero esa fila puede haberse cerrado antes de que termine la autoridad:
                // se extiende hasta el fin de la autoridad para que el período quede cubierto.
                var existing = result.FirstOrDefault(r => r.EmployeeID == item.EmployeeId && r.DocumentNumber is not null
                    && string.Equals(r.DocumentNumber.Trim(), item.DocumentNumber, StringComparison.OrdinalIgnoreCase));
                if (existing?.EffectiveTo is { } existingEnd && existingEnd < hasta && (item.EffectiveTo is null || item.EffectiveTo > existingEnd))
                {
                    existing.EffectiveTo = item.EffectiveTo;
                    existing.RegimeIsActive = true;
                }
                continue;
            }
            if (!templates.TryGetValue(item.EmployeeId, out var template)) continue; // fuera del filtro (tipo de identificación, datos de prueba...)

            var row = CloneRow(template);
            row.DocumentNumber = item.DocumentNumber;
            row.RegimeDocumentType = "PERSONNEL_ACTION";
            row.EffectiveFrom = item.EffectiveFrom;
            row.EffectiveTo = item.EffectiveTo;
            row.RegimeIsActive = item.EffectiveTo is null || item.EffectiveTo >= hasta;
            row.TipoFuncionarioSiiesLabel = "DIRECTIVO";
            row.LaborRegimeName = "LOSEP";
            row.ContractedHours = null; // ApplyReportDefaultsAsync completa las 40 horas del LOSEP
            if (departments.TryGetValue(item.DepartmentId, out var departmentName)) row.DepartmentName = departmentName;
            if (item.JobId is { } jobId && jobs.TryGetValue(jobId, out var job))
            {
                row.JobDescription = job.Description;
                row.PuestoJerarquicoSuperior = job.PuestoJerarquicoSuperior;
            }
            if (item.RelationTypeId is { } relationId && relations.TryGetValue(relationId, out var relation) && !string.IsNullOrWhiteSpace(relation))
                row.RelacionIesSiiesLabel = relation;

            var lastIndex = result.FindLastIndex(r => r.EmployeeID == item.EmployeeId);
            if (lastIndex >= 0) result.Insert(lastIndex + 1, row); else result.Add(row);
            knownDocuments.Add((item.EmployeeId, item.DocumentNumber.ToUpperInvariant()));
        }
        return result;
    }

    /// <summary>Documento que respalda la autoridad de un docente titular (ver <see cref="AddTitularAuthorityRowsAsync"/>).</summary>
    private sealed record AuthorityDocument(
        int EmployeeId, string DocumentNumber, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int DepartmentId, int? JobId, int? RelationTypeId);

    private async Task<List<VwSiiesFuncionario>> GetEmployeesVigentesEnRangoBaseAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct, IReadOnlyCollection<int>? onlyEmployeeIds = null)
    {
        // DateOnly como parametro de Dapper falla en runtime ("cannot be used as a parameter
        // value" - esta version de Dapper no lo soporta sin un ITypeHandler registrado, que
        // no existe en el proyecto). Se usa DateTime (solo la parte de fecha): SQL Server
        // compara DATE contra DATETIME sin problema, la comparacion sigue siendo por dia.
        var fechaDesde = filter.StartDate!.Value.Date;
        var fechaHasta = filter.EndDate!.Value.Date;
        var identification = string.IsNullOrWhiteSpace(filter.Identification) ? null : filter.Identification.Trim();

        const string sql = """
            SELECT
                e.[EmployeeID],
                p.[PersonID],
                p.[IdentType],
                it.[Name]                      AS [IdentTypeName],
                p.[IDCard],
                p.[FirstName],
                p.[LastName],
                p.[BirthDate],
                sx.[SiiesLabel]                AS [SexSiiesLabel],
                gn.[SiiesLabel]                AS [GenderSiiesLabel],
                p.[CountryId],
                co.[CountryName],
                et.[Name]                      AS [EthnicityName],
                et.[SiiesLabel]                AS [EthnicitySiiesLabel],
                indig.[SiiesLabel]             AS [IndigenousNationalitySiiesLabel],
                sn.[SiiesLabel]                AS [DisabilitySiiesLabel],
                p.[DisabilityPercentage],
                p.[CONADISCard],
                e.[Email]                      AS [InstitutionalEmail],
                COALESCE(dr.[Name], d.[Name])  AS [DepartmentName],
                COALESCE(jr.[Description], j.[Description]) AS [JobDescription],
                ISNULL(COALESCE(jr.[PuestoJerarquicoSuperior], j.[PuestoJerarquicoSuperior]), 0) AS [PuestoJerarquicoSuperior],
                -- Igual que la vista, pero DepartmentAuthorities se evalua contra el rango
                -- (StartDate/EndDate), no contra GETDATE().
                CASE
                    WHEN EXISTS (
                        SELECT 1 FROM [HR].[tbl_DepartmentAuthorities] da
                        WHERE da.[EmployeeId] = e.[EmployeeID]
                          -- 2026-11-17: tambien el historial (inactivo con fecha de fin real); un director anterior sigue siendo DIRECTIVO en su periodo.
                          AND (da.[IsActive] = 1 OR (da.[EndDate] IS NOT NULL AND da.[EndDate] > da.[StartDate]))
                          AND da.[StartDate] <= @FechaHasta
                          AND (da.[EndDate] IS NULL OR da.[EndDate] >= @FechaDesde)
                    ) THEN N'DIRECTIVO'
                    WHEN lrEff.[Name] = N'LOSEP' THEN N'ADMINISTRATIVO'
                    WHEN lrEff.[Name] = N'Código Trabajo' THEN N'TRABAJADOR'
                    WHEN lrEff.[Name] = N'LOES' THEN N'DOCENTE LOES'
                    ELSE NULL
                END                             AS [TipoFuncionarioSiiesLabel],
                tdl.[SiiesLabel]                AS [TipoDocenteLoesSiiesLabel],
                cdl.[SiiesLabel]                AS [CategoriaDocenteLoesSiiesLabel],
                COALESCE(CASE WHEN aptx.[Ok] = 1 THEN apt.[ActionNumber] END, elr.[DocumentNumber], ctr.[ContractCode], pa.[ActionNumber], ctrFallback.[ContractCode], paFallback.[ActionNumber]) AS [DocumentNumber],
                -- 2026-10-14: igual que la vista — FECHA_INICIO = inicio del contrato/accion que respalda el
                -- regimen (ctr/pa); EffectiveFrom del regimen solo como respaldo y HireDate como ultimo recurso.
                COALESCE(CAST(CASE WHEN aptx.[Ok] = 1 THEN apt.[ActionDate] END AS DATE), CAST(ctr.[startdate] AS DATE), CAST(pa.[EffectiveDate] AS DATE), elr.[EffectiveFrom],
                         CAST(ctrFallback.[startdate] AS DATE), paFallback.[EffectiveDate], e.[HireDate])   AS [EffectiveFrom],
                COALESCE(elr.[EffectiveTo], CAST(ctrFallback.[enddate] AS DATE), paFallback.[EndDate])             AS [EffectiveTo],
                elr.[IsActive]                  AS [RegimeIsActive],
                elr.[IngresoPorConcurso],
                CASE WHEN aptx.[Ok] = 1 THEN 'PERSONNEL_ACTION' ELSE COALESCE(elr.[DocumentType],
                    CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
                         WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) END        AS [RegimeDocumentType],
                lrEff.[Name]                    AS [LaborRegimeName],
                COALESCE(elrRel.[SiiesLabel], aptRel.[SiiesLabel], ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel]) AS [RelacionIesSiiesLabel],
                COALESCE(ctr.[ContractedHours], ctrFallback.[ContractedHours]) AS [ContractedHours],
                e.[IsActive]                    AS [EmployeeIsActive],
                e.[HireDate]
            FROM [HR].[tbl_Employees] e
            JOIN [HR].[tbl_People] p            ON p.[PersonID] = e.[PersonID]
            LEFT JOIN [HR].[ref_Types] it        ON it.[TypeID] = p.[IdentType]
            LEFT JOIN [HR].[ref_Types] sx        ON sx.[TypeID] = p.[Sex]
            LEFT JOIN [HR].[ref_Types] gn        ON gn.[TypeID] = p.[Gender]
            LEFT JOIN [HR].[tbl_Countries] co    ON co.[CountryID] = p.[CountryId]
            LEFT JOIN [HR].[ref_Types] et        ON et.[TypeID] = p.[EthnicityTypeID]
            LEFT JOIN [HR].[ref_Types] indig     ON indig.[TypeID] = p.[IndigenousNationalityTypeId]
            LEFT JOIN [HR].[ref_Types] sn        ON sn.[Category] = 'DISABILITY_TYPE' AND sn.[Name] = p.[Disability]
            LEFT JOIN [HR].[tbl_Departments] d   ON d.[DepartmentID] = e.[DepartmentID]
            LEFT JOIN [HR].[tbl_jobs] j          ON j.[JobID] = e.[JobID]
            LEFT JOIN [HR].[ref_Types] tdl       ON tdl.[TypeID] = e.[TipoDocenteLoesTypeId]
            LEFT JOIN [HR].[ref_Types] cdl       ON cdl.[TypeID] = e.[CategoriaDocenteLoesTypeId]
            -- Regimen laboral: en vez de "el mas reciente activo/principal" (vista), el que se
            -- solapa con el rango, priorizando el principal y luego el mas reciente dentro de el.
            -- 2026-11-06: ya NO es TOP 1. Una persona con varios contratos, adendums, renovaciones o nombramientos en el rango sale
            -- en una fila por cada documento (un mismo numero de documento no se repite: queda la fila del principal / la mas reciente).
            -- 2026-11-17: tch = la persona es docente (estructura docente o cargo "Profesor titular"). Un docente NO sale aqui por su
            -- docencia (LOES, va en Profesores): solo por sus filas de regimen LOSEP (doble regimen real, p. ej. un cargo directivo).
            OUTER APPLY (
                SELECT CASE WHEN ISNULL(j.[Description], '') LIKE 'PROFESOR TITULAR%'
                              OR EXISTS (SELECT 1 FROM [HR].[tbl_TeacherStructure] ts0 WHERE ts0.[EmployeeID] = e.[EmployeeID])
                            THEN 1 ELSE 0 END AS [v]
            ) tch
            OUTER APPLY (
                SELECT x.*
                FROM (
                    SELECT r.*,
                           ROW_NUMBER() OVER (
                               PARTITION BY ISNULL(r.[DocumentNumber], CONCAT(N'#', r.[Id]))
                               ORDER BY CASE WHEN r.[IsPrincipal] = 1 THEN 0 ELSE 1 END, r.[EffectiveFrom] DESC, r.[Id] DESC) AS [rn]
                    FROM [HR].[tbl_EmployeeLaborRegime] r
                    WHERE r.[EmployeeId] = e.[EmployeeID]
                      AND r.[EffectiveFrom] <= @FechaHasta
                      AND (r.[EffectiveTo] IS NULL OR r.[EffectiveTo] >= @FechaDesde)
                      -- 2026-11-17: de un docente solo cuentan sus filas LOSEP.
                      AND (tch.[v] = 0 OR r.[LaborRegimeId] = 57)
                      -- 2026-11-12: un regimen cuyo contrato (CONTRACT_STATUS 208 = ANULADO) o accion de origen esta anulado no es un documento valido.
                      AND NOT EXISTS (SELECT 1 FROM [HR].[tbl_Contracts] c0
                                      WHERE c0.[ContractID] = r.[SourceContractId] AND (c0.[Status] = 208 OR c0.[IsDeleted] = 1))
                      AND NOT EXISTS (SELECT 1 FROM [HR].[tbl_PersonnelActions] a0
                                      WHERE a0.[ActionID] = r.[SourcePersonnelActionId] AND (a0.[Status] = 'ANULADO' OR a0.[IsDeleted] = 1))
                ) x
                WHERE x.[rn] = 1
            ) elr
            -- RELACION_IES manual por régimen (tbl_EmployeeLaborRegime.SiiesRelacionIesTypeId, 35_laborregime_siies_relacion.sql): manda sobre la derivación.
            LEFT JOIN [HR].[ref_Types] elrRel             ON elrRel.[TypeID] = elr.[SiiesRelacionIesTypeId]
            -- Cargo y dependencia propios del regimen/documento de la fila (si los tiene)
            LEFT JOIN [HR].[tbl_jobs] jr                  ON jr.[JobID] = elr.[JobId]
            LEFT JOIN [HR].[tbl_Departments] dr           ON dr.[DepartmentID] = elr.[DepartmentId]
            LEFT JOIN [HR].[tbl_Contracts] ctr           ON elr.[DocumentType] = 'CONTRACT' AND ctr.[ContractID] = elr.[SourceContractId]
            LEFT JOIN [HR].[tbl_contract_type] ct        ON ct.[ContractTypeID] = ctr.[ContractTypeID]
            LEFT JOIN [HR].[ref_Types] ctRel             ON ctRel.[TypeID] = ct.[SiiesRelacionIesTypeId]
            LEFT JOIN [HR].[tbl_PersonnelActions] pa     ON elr.[DocumentType] = 'PERSONNEL_ACTION' AND pa.[ActionID] = elr.[SourcePersonnelActionId]
            LEFT JOIN [HR].[tbl_personnel_action_type] pat ON pat.[PersonnelActionTypeId] = pa.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] patRel            ON patRel.[TypeID] = pat.[SiiesRelacionIesTypeId]
            -- Contrato (fallback): igual que la vista (excluye ANULADO/BORRADOR/etc via
            -- Status), mas la condicion de solape de fechas con el rango.
            OUTER APPLY (
                SELECT TOP 1 c.*
                FROM [HR].[tbl_Contracts] c
                WHERE c.[PersonID] = p.[PersonID] AND c.[IsDeleted] = 0
                  AND c.[Status] IN (274, 276) -- VIGENTE, VENCIDO (ref_Types CONTRACT_STATUS)
                  AND c.[startdate] <= @FechaHasta AND c.[enddate] >= @FechaDesde
                  AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
                  AND tch.[v] = 0
                ORDER BY CASE WHEN c.[Status] = 274 THEN 0 ELSE 1 END, c.[startdate] DESC
            ) ctrFallback
            LEFT JOIN [HR].[tbl_contract_type] ctFallback ON ctFallback.[ContractTypeID] = ctrFallback.[ContractTypeID]
            LEFT JOIN [HR].[ref_Types] ctRelFallback      ON ctRelFallback.[TypeID] = ctFallback.[SiiesRelacionIesTypeId]
            -- Accion de personal (fallback): igual criterio, con solape de fechas.
            OUTER APPLY (
                SELECT TOP 1 pa2.*
                FROM [HR].[tbl_PersonnelActions] pa2
                WHERE pa2.[EmployeeID] = e.[EmployeeID] AND pa2.[IsDeleted] = 0
                  AND pa2.[Status] IN ('VIGENTE', 'FINALIZADO')
                  AND pa2.[EffectiveDate] <= @FechaHasta
                  -- 2026-11-12: una accion FINALIZADO sin fecha de fin NO es vigente para siempre (antes colaba a personas que dejaron de
                  -- laborar hace anos): solo cuenta si empezo dentro del rango. Sin fecha de fin solo es abierta si esta VIGENTE.
                  AND (pa2.[EndDate] >= @FechaDesde
                       OR (pa2.[EndDate] IS NULL AND (pa2.[Status] = 'VIGENTE' OR pa2.[EffectiveDate] >= @FechaDesde)))
                  AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
                  AND ctrFallback.[ContractID] IS NULL
                  AND tch.[v] = 0
                ORDER BY CASE WHEN pa2.[Status] = 'VIGENTE' THEN 0 ELSE 1 END, pa2.[ActionDate] DESC
            ) paFallback
            LEFT JOIN [HR].[tbl_personnel_action_type] patFallback ON patFallback.[PersonnelActionTypeId] = paFallback.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] patRelFallback               ON patRelFallback.[TypeID] = patFallback.[SiiesRelacionIesTypeId]
            -- 2026-10-15: igual que la vista — documento con el que GANO el nombramiento (HR.vw_EmployeeAppointment), evaluado
            -- contra el rango: si el nombramiento actual es posterior al fin del rango se usa el primer permanente ya vigente
            -- en esa fecha (aproximacion: exacta cuando hubo a lo mas un reinicio de cadena). Sin nombramiento o con salida
            -- posterior queda la cascada anterior.
            -- Se calcula UNA vez para todos los empleados (la vista devuelve una fila por empleado) y se une; como OUTER APPLY correlacionado
            -- se ejecutaba por cada fila de documento y recorria HR.tbl_PersonnelActions miles de veces (timeout de 30 s con rangos amplios).
            -- Con varias filas por persona, el nombramiento ganador solo reemplaza al documento de la fila del regimen principal
            -- (o de quien no tiene ninguna fila de regimen); los demas documentos conservan el suyo.
            LEFT JOIN (
                SELECT ap.[EmployeeID],
                       CASE WHEN ap.[AppointmentDate] <= @FechaHasta THEN ap.[AppointmentActionId]     ELSE ap.[FirstPermanentActionId]     END AS [ActionId],
                       CASE WHEN ap.[AppointmentDate] <= @FechaHasta THEN ap.[AppointmentActionNumber] ELSE ap.[FirstPermanentActionNumber] END AS [ActionNumber],
                       CASE WHEN ap.[AppointmentDate] <= @FechaHasta THEN ap.[AppointmentDate]         ELSE ap.[FirstPermanentDate]         END AS [ActionDate]
                FROM [HR].[vw_EmployeeAppointment] ap
                WHERE ap.[HasLaterExit] = 0
                  AND (ap.[AppointmentDate] <= @FechaHasta
                       OR (ap.[AppointmentDate] > @FechaHasta AND ap.[FirstPermanentDate] <= @FechaHasta))
            ) apt ON apt.[EmployeeID] = e.[EmployeeID] AND (elr.[Id] IS NULL OR elr.[IsPrincipal] = 1) AND tch.[v] = 0
            -- Con historial por documento, el nombramiento ganador ya puede tener su propia fila: en ese caso NO reemplaza el
            -- documento de la fila principal (repetiria su numero y su fecha de inicio). Sigue aportando su relacion con la IES.
            OUTER APPLY (
                SELECT CASE WHEN apt.[ActionId] IS NOT NULL
                             AND (elr.[Id] IS NULL
                                  OR NOT EXISTS (SELECT 1 FROM [HR].[tbl_EmployeeLaborRegime] rx
                                                 WHERE rx.[EmployeeId] = e.[EmployeeID]
                                                   AND rx.[SourcePersonnelActionId] = apt.[ActionId]
                                                   AND rx.[Id] <> elr.[Id]
                                                   -- 2026-11-26: solo cuenta si esa fila se solapa con el rango; un nombramiento cuya fila terminó antes del rango
                                                   -- no sale en el reporte y, sin este filtro, dejaba de reemplazar el documento de la fila principal.
                                                   AND rx.[EffectiveFrom] <= @FechaHasta
                                                   AND (rx.[EffectiveTo] IS NULL OR rx.[EffectiveTo] >= @FechaDesde)))
                            THEN 1 ELSE 0 END AS [Ok]
            ) aptx
            LEFT JOIN [HR].[tbl_PersonnelActions] aptPa           ON aptPa.[ActionID] = apt.[ActionId]
            LEFT JOIN [HR].[tbl_personnel_action_type] aptPat     ON aptPat.[PersonnelActionTypeId] = aptPa.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] aptRel                     ON aptRel.[TypeID] = aptPat.[SiiesRelacionIesTypeId]
            -- 2026-10-14: Regimen EFECTIVO (TIPO_FUNCIONARIO / LaborRegimeName), el primero que exista:
            --   1) regimen que solapa el rango (elr)
            --   2) regimen del TIPO del contrato de respaldo (tbl_contract_type.PersonalContractTypeID: 57/58/59)
            --   3) regimen propio de la accion de respaldo (EmployeeTypeId; el tipo de accion NO se usa: el mismo
            --      tipo sale LOES y LOSEP)
            --   4) regimen ACTUAL del empleado (principal activo, o el mas reciente; acierta 98-99 % donde se puede
            --      comparar con el regimen real del periodo)
            --   5) NULL: sin documento ni regimen, no se adivina.
            -- Antes solo existia el paso 1, y tbl_EmployeeLaborRegime guarda el regimen vigente, asi que en rangos
            -- pasados TIPO_FUNCIONARIO salia vacio (617 filas en 2024).
            OUTER APPLY (
                SELECT TOP 1 x.[LaborRegimeId]
                FROM [HR].[tbl_EmployeeLaborRegime] x
                WHERE x.[EmployeeId] = e.[EmployeeID]
                ORDER BY CASE WHEN x.[IsPrincipal] = 1 AND x.[IsActive] = 1 THEN 0 ELSE 1 END, x.[EffectiveFrom] DESC
            ) curReg
            LEFT JOIN [HR].[ref_Types] lrEff ON lrEff.[TypeID] = COALESCE(
                elr.[LaborRegimeId],
                CASE WHEN ctFallback.[PersonalContractTypeID] IN (57, 58, 59) THEN ctFallback.[PersonalContractTypeID] END,
                CASE WHEN paFallback.[EmployeeTypeId] IN (57, 58, 59) THEN paFallback.[EmployeeTypeId] END,
                curReg.[LaborRegimeId])
            WHERE e.[IsDeleted] = 0
              -- 2026-11-17: antes se excluia a todo docente. Ahora un docente entra solo con una fila de regimen LOSEP que se solape con el
              -- rango (elr ya viene limitada a LOSEP para docentes); su docencia LOES sale en Profesores.
              AND (tch.[v] = 0 OR elr.[Id] IS NOT NULL)
              -- 2026-10-14: igual que la vista — los profesores ocasionales/tecnicos docentes van a
              -- vw_SiiesProfesores; sin esta exclusion se colaban aqui (557-727 filas segun el rango).
              -- Igual que la vista: el contrato ocasional solo excluye sin un régimen principal LOSEP/Código de Trabajo
              -- que se solape con el rango (doble régimen: Funcionario por el principal, Profesores por la delegación).
              AND (
                    NOT EXISTS (SELECT 1 FROM [HR].[fn_SiiesDocenteOcasionalContract](p.[PersonID]))
                    -- 2026-11-17: un docente con fila LOSEP (ya exigida arriba) sale por ella aunque su principal sea la docencia LOES.
                    OR tch.[v] = 1
                    OR EXISTS (SELECT 1 FROM [HR].[tbl_EmployeeLaborRegime] pr
                               WHERE pr.[EmployeeId] = e.[EmployeeID] AND pr.[IsPrincipal] = 1
                                 AND pr.[LaborRegimeId] IN (57, 59)
                                 AND pr.[EffectiveFrom] <= @FechaHasta
                                 AND (pr.[EffectiveTo] IS NULL OR pr.[EffectiveTo] >= @FechaDesde))
                  )
              AND (it.[Name] = @IdentTypeName OR (@IdentTypeName = N'TODOS' AND it.[Name] IN (N'CEDULA', N'PASAPORTE')))
              AND (@Identification IS NULL OR p.[IDCard] = @Identification)
              -- Personal de Codigo de Trabajo (obrero): va en su propio archivo, no en SIIES Funcionarios.
              AND ISNULL(lrEff.[Name], N'') <> N'Código Trabajo'
              -- Doble regimen: la delegacion docente (LOES) de quien es docente ocasional sale en Profesores, no aqui.
              AND NOT (lrEff.[Name] = N'LOES' AND EXISTS (SELECT 1 FROM [HR].[fn_SiiesDocenteOcasionalContract](p.[PersonID])))
              -- Empleados de prueba (IsTestData): fuera salvo TestData:Visible (ver ITestDataPolicy).
              AND (@IncludeTestData = 1 OR e.[IsTestData] = 0)
              -- A diferencia de la vista (que siempre devuelve 1 fila por empleado activo,
              -- vigente o no), aqui se excluye a quien no tuvo NINGUN origen de vigencia
              -- (regimen/contrato/accion) solapado con el rango - sin esto quedarian filas
              -- "vacias" (nadie estuvo vigente en esas fechas).
              -- 2026-09-21: BUG corregido - antes comparaba elr.[DocumentNumber] IS NOT NULL,
              -- pero DocumentNumber es un campo descriptivo que puede venir NULL aunque SI
              -- exista una fila de regimen que matcheo (excluia gente valida sin querer).
              -- Se usa elr.[Id] (PK real de tbl_EmployeeLaborRegime), igual criterio que ya
              -- se usaba correctamente para Contract/PersonnelAction (sus propias PK).
              AND (elr.[Id] IS NOT NULL OR ctrFallback.[ContractID] IS NOT NULL OR paFallback.[ActionID] IS NOT NULL)
            """;

        // 2026-11-26: con onlyEmployeeIds la misma consulta (mismos datos personales, mismas exclusiones) se limita a esos empleados y deja de
        // exigir un régimen/contrato/acción solapado: se usa para obtener los datos personales de un titular que solo sale por su autoridad.
        var effectiveSql = sql;
        if (onlyEmployeeIds is not null)
        {
            const string docenteMarker = "AND (tch.[v] = 0 OR elr.[Id] IS NOT NULL)";
            const string originMarker = "AND (elr.[Id] IS NOT NULL OR ctrFallback.[ContractID] IS NOT NULL OR paFallback.[ActionID] IS NOT NULL)";
            if (!sql.Contains(docenteMarker) || !sql.Contains(originMarker))
                throw new InvalidOperationException("La consulta base de SIIES Funcionarios cambió: actualizar los marcadores de GetEmployeesVigentesEnRangoBaseAsync.");
            effectiveSql = sql.Replace(docenteMarker, "AND e.[EmployeeID] IN @EmployeeIds").Replace(originMarker, string.Empty);
        }

        var parameters = new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            IdentTypeName = identTypeName,
            Identification = identification,
            IncludeTestData = _testDataPolicy.IncludeTestData,
            EmployeeIds = onlyEmployeeIds?.ToArray() ?? Array.Empty<int>()
        };

        var connection = _db.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(ct);
        try
        {
            // Dapper no puede mapear columnas DATE directo a propiedades DateOnly/DateOnly?
            // (InvalidCastException) - el proyecto no registra un TypeHandler para esto (ver
            // EmployeeSelfServiceSummaryRawData.cs), se mapea a un DTO con DateTime y se
            // convierte a mano, mismo criterio ya usado en el resto del codigo.
            var rawRows = await connection.QueryAsync<RawVigenteEnRangoRow>(
                new CommandDefinition(effectiveSql, parameters, cancellationToken: ct));

            return rawRows.Select(r => new VwSiiesFuncionario
            {
                EmployeeID = r.EmployeeID,
                PersonID = r.PersonID,
                IdentType = r.IdentType,
                IdentTypeName = r.IdentTypeName,
                IDCard = r.IDCard,
                FirstName = r.FirstName,
                LastName = r.LastName,
                BirthDate = ToDateOnly(r.BirthDate),
                SexSiiesLabel = r.SexSiiesLabel,
                GenderSiiesLabel = r.GenderSiiesLabel,
                CountryId = r.CountryId,
                CountryName = r.CountryName,
                EthnicityName = r.EthnicityName,
                EthnicitySiiesLabel = r.EthnicitySiiesLabel,
                IndigenousNationalitySiiesLabel = r.IndigenousNationalitySiiesLabel,
                DisabilitySiiesLabel = r.DisabilitySiiesLabel,
                DisabilityPercentage = r.DisabilityPercentage,
                CONADISCard = r.CONADISCard,
                InstitutionalEmail = r.InstitutionalEmail,
                DepartmentName = r.DepartmentName,
                JobDescription = r.JobDescription,
                PuestoJerarquicoSuperior = r.PuestoJerarquicoSuperior,
                TipoFuncionarioSiiesLabel = r.TipoFuncionarioSiiesLabel,
                TipoDocenteLoesSiiesLabel = r.TipoDocenteLoesSiiesLabel,
                CategoriaDocenteLoesSiiesLabel = r.CategoriaDocenteLoesSiiesLabel,
                DocumentNumber = r.DocumentNumber,
                EffectiveFrom = ToDateOnly(r.EffectiveFrom),
                EffectiveTo = ToDateOnly(r.EffectiveTo),
                RegimeIsActive = r.RegimeIsActive,
                IngresoPorConcurso = r.IngresoPorConcurso,
                RegimeDocumentType = r.RegimeDocumentType,
                LaborRegimeName = r.LaborRegimeName,
                RelacionIesSiiesLabel = r.RelacionIesSiiesLabel,
                ContractedHours = r.ContractedHours,
                EmployeeIsActive = r.EmployeeIsActive,
                HireDate = ToDateOnly(r.HireDate) ?? default,
            }).ToList();
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    /// <summary>
    /// Fila cruda de <see cref="GetEmployeesVigentesEnRangoAsync"/>: mismas columnas que
    /// <see cref="VwSiiesFuncionario"/>, pero con <see cref="DateTime"/> en vez de
    /// <see cref="DateOnly"/> para que Dapper pueda mapear las columnas DATE sin lanzar
    /// InvalidCastException (ver comentario en GetEmployeesVigentesEnRangoAsync).
    /// </summary>
    private sealed class RawVigenteEnRangoRow
    {
        public int EmployeeID { get; set; }
        public int PersonID { get; set; }
        public int? IdentType { get; set; }
        public string? IdentTypeName { get; set; }
        public string IDCard { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public string? SexSiiesLabel { get; set; }
        public string? GenderSiiesLabel { get; set; }
        public string? CountryId { get; set; }
        public string? CountryName { get; set; }
        public string? EthnicityName { get; set; }
        public string? EthnicitySiiesLabel { get; set; }
        public string? IndigenousNationalitySiiesLabel { get; set; }
        public string? DisabilitySiiesLabel { get; set; }
        public decimal? DisabilityPercentage { get; set; }
        public string? CONADISCard { get; set; }
        public string? InstitutionalEmail { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobDescription { get; set; }
        public bool PuestoJerarquicoSuperior { get; set; }
        public string? TipoFuncionarioSiiesLabel { get; set; }
        public string? TipoDocenteLoesSiiesLabel { get; set; }
        public string? CategoriaDocenteLoesSiiesLabel { get; set; }
        public string? DocumentNumber { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool? RegimeIsActive { get; set; }
        public bool? IngresoPorConcurso { get; set; }
        public string? RegimeDocumentType { get; set; }
        public string? LaborRegimeName { get; set; }
        public string? RelacionIesSiiesLabel { get; set; }
        public decimal? ContractedHours { get; set; }
        public bool EmployeeIsActive { get; set; }
        public DateTime HireDate { get; set; }
    }

    /// <summary>Construye las 25 columnas comunes a ambas matrices (mismo orden oficial que 5.7 Funcionarios).</summary>
    private static Dictionary<string, object?> BuildCommonRow(VwSiiesFuncionario v, string codigoIes, string codigoMatrizExtension)
    {
        var esDocenteLoes = string.Equals(v.TipoFuncionarioSiiesLabel, DocenteLoesLabel, StringComparison.OrdinalIgnoreCase);
        var esIndigena = string.Equals(v.EthnicitySiiesLabel, IndigenaLabel, StringComparison.OrdinalIgnoreCase);
        var sinDiscapacidad = string.IsNullOrEmpty(v.DisabilitySiiesLabel) || string.Equals(v.DisabilitySiiesLabel, "NINGUNA", StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, object?>
        {
            ["CODIGO_IES"] = codigoIes,
            ["CODIGO_MATRIZ_EXTENSION"] = codigoMatrizExtension,
            ["TIPO_IDENTIFICACION"] = v.IdentTypeName,
            ["IDENTIFICACION"] = v.IDCard,
            ["GENERO"] = v.GenderSiiesLabel ?? "NO DISPONE",
            ["SEXO"] = v.SexSiiesLabel ?? string.Empty,
            ["PAIS_ORIGEN"] = v.CountryName ?? v.CountryId ?? string.Empty,
            // 2026-09-02: p.Disability queda NULL cuando la persona no tiene discapacidad (nunca
            // se escribió "Ninguna" literal) — el archivo real entregado a CACES siempre trae
            // "NINGUNA" en vez de vacío para ese caso (verificado: 328 de 331 casos comparados).
            ["DISCAPACIDAD"] = v.DisabilitySiiesLabel ?? "NINGUNA",
            ["NUMERO_CONADIS"] = sinDiscapacidad ? string.Empty : (v.CONADISCard ?? "NO REGISTRA"),
            ["PORCENTAJE_DISCAPACIDAD"] = v.DisabilityPercentage ?? 0,
            ["ETNIA"] = v.EthnicitySiiesLabel ?? "NO REGISTRA",
            ["NACIONALIDAD"] = esIndigena ? (v.IndigenousNationalitySiiesLabel ?? "NO REGISTRA") : "NO APLICA",
            ["EMAIL_INSTITUCIONAL"] = v.InstitutionalEmail ?? string.Empty,
            ["NUMERO_DOCUMENTO"] = v.DocumentNumber ?? string.Empty,
            ["RELACION_IES"] = v.RelacionIesSiiesLabel ?? string.Empty,
            ["FECHA_INICIO"] = v.EffectiveFrom,
            ["FECHA_FIN"] = v.EffectiveTo,
            // Decisión de exportación 2026-08-27: NULL (sin clasificar) se exporta como "SI" —
            // solo false explícito exporta "NO". Antes era al revés; ver remarks de la clase.
            ["INGRESO_POR_CONCURSO"] = v.IngresoPorConcurso == false ? "NO" : "SI",
            ["TIPO_FUNCIONARIO"] = v.TipoFuncionarioSiiesLabel ?? string.Empty,
            ["CARGO"] = v.JobDescription ?? string.Empty,
            ["TIPO_DOCENTE_LOSEP(LOES)"] = esDocenteLoes ? (v.TipoDocenteLoesSiiesLabel ?? "NO APLICA") : "NO APLICA",
            ["CATEGORIA_DOCENTE_LOSEP(LOES)"] = esDocenteLoes ? (v.CategoriaDocenteLoesSiiesLabel ?? "NO APLICA") : "NO APLICA",
            ["UNIDAD_ACADEMICA"] = v.DepartmentName ?? string.Empty,
            ["PUESTO_JERARQUICO_SUPERIOR"] = v.PuestoJerarquicoSuperior ? "SI" : "NO",
            // Horas por semana según la cascada de la vista (contrato > horario > jornada institucional). Vacío cuando no hay
            // dato (LOES sin contrato con horas): no se declara 0 horas.
            ["HORAS_LABORABLES_SEMANA"] = v.ContractedHours,
            // Columnas adicionales fuera del esquema oficial CACES — se agregan al final para
            // no alterar el orden/cantidad de las columnas oficiales (uso interno/verificación,
            // no para la carga masiva al SIIES).
            ["NOMBRE_COMPLETO"] = $"{v.LastName} {v.FirstName}".Trim(),
            ["REGIMEN_LABORAL"] = v.LaborRegimeName ?? string.Empty,
        };
    }

    /// <summary>
    /// Separa Apellidos/Nombres a nivel de reporte (no se toca tbl_People). Heurística:
    /// LastName = "primer segundo" (primeras dos palabras); si solo trae una palabra,
    /// SEGUNDO_APELLIDO queda vacío. NOMBRES = FirstName completo, sin dividir.
    /// </summary>
    private static (string primerApellido, string segundoApellido, string nombres) SplitNamesForPasaporte(VwSiiesFuncionario v)
    {
        var (primerApellido, segundoApellido) = SiiesNameSplitter.Split(v.LastName);
        return (primerApellido, segundoApellido, v.FirstName ?? string.Empty);
    }

    private async Task<(string codigoIes, string codigoMatrizExtension)> GetInstitutionalParametersAsync(CancellationToken ct)
    {
        var codigoIes = await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_IES" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var codigoMatriz = await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_MATRIZ_EXTENSION" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return (codigoIes, codigoMatriz ?? string.Empty);
    }

    private static readonly IReadOnlyList<ReportColumn> CommonColumns =
    [
        new("CODIGO_IES", "CODIGO_IES"),
        new("CODIGO_MATRIZ_EXTENSION", "CODIGO_MATRIZ_EXTENSION"),
        new("TIPO_IDENTIFICACION", "TIPO_IDENTIFICACION"),
        new("IDENTIFICACION", "IDENTIFICACION"),
    ];

    private static readonly IReadOnlyList<ReportColumn> PasaporteNameColumns =
    [
        new("PRIMER_APELLIDO", "PRIMER_APELLIDO"),
        new("SEGUNDO_APELLIDO", "SEGUNDO_APELLIDO"),
        new("NOMBRES", "NOMBRES"),
    ];

    private static readonly IReadOnlyList<ReportColumn> RestOfCedulaColumns =
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
        new("NUMERO_CONADIS", "NUMERO_CONADIS"),
        new("PORCENTAJE_DISCAPACIDAD", "PORCENTAJE_DISCAPACIDAD"),
        new("ETNIA", "ETNIA"),
        new("NACIONALIDAD", "NACIONALIDAD"),
        new("EMAIL_INSTITUCIONAL", "EMAIL_INSTITUCIONAL"),
        new("NUMERO_DOCUMENTO", "NUMERO_DOCUMENTO"),
        new("RELACION_IES", "RELACION_IES"),
        new("FECHA_INICIO", "FECHA_INICIO"),
        new("FECHA_FIN", "FECHA_FIN"),
        new("INGRESO_POR_CONCURSO", "INGRESO_POR_CONCURSO"),
        new("TIPO_FUNCIONARIO", "TIPO_FUNCIONARIO"),
        new("CARGO", "CARGO"),
        new("TIPO_DOCENTE_LOSEP(LOES)", "TIPO_DOCENTE_LOSEP(LOES)"),
        new("CATEGORIA_DOCENTE_LOSEP(LOES)", "CATEGORIA_DOCENTE_LOSEP(LOES)"),
        new("UNIDAD_ACADEMICA", "UNIDAD_ACADEMICA"),
        new("PUESTO_JERARQUICO_SUPERIOR", "PUESTO_JERARQUICO_SUPERIOR"),
        new("HORAS_LABORABLES_SEMANA", "HORAS_LABORABLES_SEMANA"),
        // Fuera del esquema oficial CACES — ver comentario en BuildCommonRow.
        new("NOMBRE_COMPLETO", "NOMBRE_COMPLETO"),
        new("REGIMEN_LABORAL", "REGIMEN_LABORAL"),
    ];

    /// <summary>Columnas oficiales de la matriz 5.7 Funcionarios (cédula), en el orden exacto del instructivo.</summary>
    private static IReadOnlyList<ReportColumn> CedulaColumns =>
        [.. CommonColumns, .. RestOfCedulaColumns, .. TailColumns];

    /// <summary>Columnas oficiales de la matriz 5.8 Funcionario Pasaporte, en el orden exacto del instructivo.</summary>
    private static IReadOnlyList<ReportColumn> PasaporteColumns =>
        [.. CommonColumns, .. PasaporteNameColumns, .. RestOfCedulaColumns, .. FechaNacimientoColumn, .. TailColumns];
}
