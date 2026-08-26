using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Engine;

/// <summary>
/// Resuelve los valores de los campos de una plantilla documental consultando
/// las fuentes de datos reales: EMPLOYEE, CONTRACT, MOVEMENT, SYSTEM.
/// Aplica la siguiente prioridad: overrides manuales > fuente automática > valor por defecto.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentTemplateField.SourceType"/> es un enum real (<see cref="FieldSourceType"/>)
/// con conversión EF Core (HasConversion). No se requiere <c>Enum.TryParse</c>.
/// </para>
/// <para>
/// El modelo <see cref="Contracts"/> no tiene propiedad de navegación hacia <see cref="ContractType"/>;
/// la carga se realiza en una consulta separada usando <c>ContractTypeID</c>.
/// </para>
/// </remarks>
public sealed class DocumentFieldResolver : IDocumentFieldResolver
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IInstitutionalLogoService _logoService;
    private readonly ILogger<DocumentFieldResolver> _logger;

    public DocumentFieldResolver(
        AppDbContext db,
        IConfiguration config,
        IInstitutionalLogoService logoService,
        ILogger<DocumentFieldResolver> logger)
    {
        _db          = db          ?? throw new ArgumentNullException(nameof(db));
        _config      = config      ?? throw new ArgumentNullException(nameof(config));
        _logoService = logoService ?? throw new ArgumentNullException(nameof(logoService));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> ResolveAsync(
        IReadOnlyList<DocumentTemplateField> fields,
        int? employeeId,
        int? entityId,
        Dictionary<string, string>? overrides = null,
        CancellationToken ct = default,
        int personId = 0)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Cargar datos de empleado ─────────────────────────────────────────────
        Employees? employee = null;

        if (employeeId is > 0)
        {
            employee = await _db.Employees
                .AsNoTracking()
                .Include(e => e.People)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);
        }

        // Sin empleado aún (nuevo ingreso): construir un Employees sintético desde tbl_People
        if (employee is null && personId > 0)
        {
            var person = await _db.People
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

            if (person is not null)
            {
                employee = new Employees
                {
                    EmployeeId = 0,
                    PersonID   = personId,
                    People     = person,
                    HireDate   = DateOnly.FromDateTime(DateTime.Today),
                };

                _logger.LogInformation(
                    "DocumentFieldResolver: sin empleado, usando datos de Person {PersonId} para campos EMPLOYEE_*.", personId);
            }
        }

        if (employee is null)
        {
            _logger.LogWarning(
                "DocumentFieldResolver: Empleado {EmployeeId} / Persona {PersonId} no encontrados. Solo se resolverán campos SYSTEM.", employeeId, personId);
        }

        // ── Cargar datos de contrato ─────────────────────────────────────────────
        // Contracts NO tiene propiedad de navegación hacia ContractType;
        // se carga en consulta separada.
        Contracts? contract = null;
        ContractType? contractType = null;
        Departments? department = null;
        Job? job = null;

        if (entityId.HasValue)
        {
            contract = await _db.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractID == entityId.Value, ct);
        }
        else if (employee is not null)
        {
            // Contrato activo más reciente del empleado
            contract = await _db.Contracts
                .AsNoTracking()
                .Where(c => c.PersonID == employee.PersonID && c.Status == 1)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync(ct);
        }

        if (contract is not null)
        {
            contractType = await _db.ContractType
                .AsNoTracking()
                .FirstOrDefaultAsync(ct2 => ct2.ContractTypeId == contract.ContractTypeID, ct);

            department = await _db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentId == contract.DepartmentID, ct);

            if (contract.JobID.HasValue)
            {
                job = await _db.Jobs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobID == contract.JobID.Value, ct);
            }
        }

        // Modalidad de trabajo (TC/MT/Horas) del contrato, resuelta a texto vía catálogo
        // (mismo patrón que ContractsService.cs usa para WorkModalityName en reportes).
        string? workModalityName = null;
        if (contract?.WorkModalityID is not null)
        {
            workModalityName = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.TypeId == contract.WorkModalityID.Value && r.Category == "WORK_MODALITY")
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct);
        }

        // ── Cargar movimiento de personal (si aplica) ────────────────────────────
        // FieldSourceType es enum real → comparar directamente (no usar ToString())
        PersonnelMovements? movement = null;
        if (entityId.HasValue && fields.Any(f => f.SourceType == FieldSourceType.Movement))
        {
            movement = await _db.PersonnelMovements
                .AsNoTracking()
                .Include(m => m.MovementType)
                .FirstOrDefaultAsync(m => m.MovementId == entityId.Value, ct);
        }

        // ── Cargar autoridades institucionales activas (Rector, Vicerrector, Director financiero/RRHH...) ──
        // Se resuelven dinámicamente desde HR.tbl_DepartmentAuthorities + HR.tbl_Departments.InstitutionalRoleTypeId,
        // sin IDs ni nombres quemados en código ni en appsettings.
        Dictionary<string, (string Name, string Title)>? authorities = null;
        if (fields.Any(f => f.SourceType == FieldSourceType.System))
            authorities = await LoadActiveAuthoritiesAsync(ct);

        // ── Resolver cada campo ──────────────────────────────────────────────────
        foreach (var field in fields)
        {
            // 1. Override manual tiene máxima prioridad
            if (overrides is not null && overrides.TryGetValue(field.FieldName, out var manualValue))
            {
                result[field.FieldName] = manualValue;
                continue;
            }

            // 2. Resolución automática: saltar campos de empleado/contrato si no hay datos
            string? resolved = null;
            if (field.SourceType == FieldSourceType.System)
                resolved = ResolveSystemField(field.FieldName, authorities);
            else if (employee is not null)
                resolved = ResolveField(field, employee, contract, contractType, department, job, movement, workModalityName);

            // 3. Fallback al valor por defecto de la plantilla
            result[field.FieldName] = resolved ?? field.DefaultValue ?? string.Empty;
        }

        _logger.LogDebug(
            "DocumentFieldResolver: {Count} campos resueltos para empleado {EmployeeId}.",
            result.Count, employeeId);

        return result;
    }

    // ── Resolución individual por campo ─────────────────────────────────────────

    private string? ResolveField(
        DocumentTemplateField field,
        Employees employee,
        Contracts? contract,
        ContractType? contractType,
        Departments? department,
        Job? job,
        PersonnelMovements? movement,
        string? workModalityName)
    {
        return field.SourceType switch
        {
            FieldSourceType.Employee => ResolveEmployeeField(field.FieldName, employee),
            FieldSourceType.Contract => ResolveContractField(field.FieldName, contract, contractType, department, job, workModalityName),
            FieldSourceType.Movement => ResolveMovementField(field.FieldName, movement),
            FieldSourceType.Manual   => null,
            _                        => null
        };
    }

    private static string? ResolveEmployeeField(string fieldName, Employees employee)
    {
        var p = employee.People;

        // Mapear IdentType (int) al texto del documento de identidad
        static string? IdentTypeLabel(int? v) => v switch
        {
            1 => "CÉDULA",
            2 => "PASAPORTE",
            3 => "RUC",
            _ => null
        };

        return fieldName.ToUpperInvariant() switch
        {
            "EMPLOYEE_ID"        => employee.EmployeeId.ToString(),
            "EMPLOYEE_FULLNAME"  => p is not null ? p.GetFullName().ToUpperInvariant() : null,
            "EMPLOYEE_FIRSTNAME" => p?.FirstName?.ToUpperInvariant(),
            "EMPLOYEE_LASTNAME"  => p?.LastName?.ToUpperInvariant(),
            "EMPLOYEE_IDCARD"    => p?.IdCard,
            "EMPLOYEE_EMAIL"     => employee.Email ?? p?.Email,
            "EMPLOYEE_PHONE"     => p?.Phone,
            "EMPLOYEE_ADDRESS"   => p?.Address,
            "EMPLOYEE_BIRTHDATE" => p?.BirthDate?.ToString("dd/MM/yyyy"),
            "EMPLOYEE_HIREDATE"  => employee.HireDate.ToString("dd/MM/yyyy"),
            "ID_TYPE"            => IdentTypeLabel(p?.IdentType),
            _                    => null
        };
    }

    private static string? ResolveContractField(
        string fieldName,
        Contracts? contract,
        ContractType? contractType,
        Departments? department,
        Job? job,
        string? workModalityName)
    {
        if (contract is null) return null;

        return fieldName.ToUpperInvariant() switch
        {
            "CONTRACT_CODE"        => contract.ContractCode,
            "CONTRACT_TYPE"        => contractType?.Name,
            // Horas contratadas y modalidad (TC/MT/Horas): auto-pobladas en el contrato desde
            // la solicitud (ver Contracts.ContractedHours/WorkModalityID) — nunca manuales,
            // para que la carta de Profesor Ocasional refleje la carga real del contrato.
            "WEEKLY_HOURS"         => contract.ContractedHours?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            "DEDICATION_TYPE"      => workModalityName,
            // Alias: CONTRATO_PROFESOR_OCASIONAL usa CONTRACT_START_DATE/CONTRACT_END_DATE
            // (con guión bajo extra) en vez de la convención CONTRACT_STARTDATE/ENDDATE
            // usada por las plantillas CONTRATO_TECNICO_*.
            "CONTRACT_STARTDATE" or "CONTRACT_START_DATE" => contract.StartDate.ToString("dd/MM/yyyy"),
            "CONTRACT_ENDDATE"   or "CONTRACT_END_DATE"   => contract.EndDate.ToString("dd/MM/yyyy"),
            "CONTRACT_DESCRIPTION" => contract.ContractDescription,
            "CONTRACT_RMU"         => null,
            "DEPARTMENT_NAME"      => department?.Name,
            "DEPARTMENT_CODE"      => department?.Code,
            "DEPARTMENT_SHORTNAME" => department?.ShortName ?? department?.Name,
            "JOB_DESCRIPTION"      => job?.Description,
            "BUDGET_CODE"          => department?.BudgetCode,
            // Aliases que la plantilla de acciones de personal usa
            "CURRENT_ADMIN_UNIT"   => department?.Name?.ToUpperInvariant(),
            "CURRENT_JOB_TITLE"    => job?.Description?.ToUpperInvariant(),
            "CURRENT_BUDGET_CODE"  => department?.BudgetCode,
            _                      => null
        };
    }

    private static string? ResolveMovementField(string fieldName, PersonnelMovements? movement)
    {
        if (movement is null) return null;

        return fieldName.ToUpperInvariant() switch
        {
            "MOVEMENT_DATE"   => movement.MovementDate.ToString("dd/MM/yyyy"),
            "MOVEMENT_TYPE"   => movement.MovementType?.Name,
            "MOVEMENT_REASON" => movement.Reason,
            _                 => null
        };
    }

    /// <summary>
    /// Carga las autoridades institucionales activas vigentes hoy, agrupadas por el código de rol
    /// (HR.ref_Types.Category = 'DEPARTMENT_INSTITUTIONAL_ROLE', ej. RECTORADO, FINANCE, HUMAN_RESOURCE).
    /// Solo considera tipos de autoridad que representan a la máxima autoridad del rol
    /// (Rector, Vicerrector, Director) para evitar traer Coordinadores/Secretarios del mismo departamento.
    /// </summary>
    private async Task<Dictionary<string, (string Name, string Title)>> LoadActiveAuthoritiesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = await _db.DepartmentAuthorities
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e!.People)
            .Include(a => a.AuthorityType)
            .Include(a => a.Department).ThenInclude(d => d!.InstitutionalRoleType)
            .Where(a => a.IsActive
                     && a.Department!.InstitutionalRoleType != null
                     && (a.AuthorityType!.Name == "Rector" || a.AuthorityType.Name == "Vicerrector" || a.AuthorityType.Name == "Director")
                     && a.StartDate <= today
                     && (a.EndDate == null || a.EndDate >= today))
            .ToListAsync(ct);

        var result = new Dictionary<string, (string Name, string Title)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var roleCode = row.Department!.InstitutionalRoleType!.Name;
            if (result.ContainsKey(roleCode)) continue; // ya resuelto por otra fila del mismo rol

            var person = row.Employee?.People;
            var fullName = person.GetFullName();
            var title = row.Denomination ?? row.AuthorityType!.Name;

            result[roleCode] = (fullName, title);
        }

        return result;
    }

    private string? ResolveSystemField(
        string fieldName,
        Dictionary<string, (string Name, string Title)>? authorities)
    {
        var now = DateTime.Now;

        // Leer config institucional (sección InstitutionalConfig en appsettings.json) — fallback
        // cuando no hay una autoridad activa registrada en HR.tbl_DepartmentAuthorities.
        string Cfg(string key) => _config[$"InstitutionalConfig:{key}"] ?? string.Empty;

        string AuthorityName(string roleCode, string cfgFallbackKey) =>
            authorities is not null && authorities.TryGetValue(roleCode, out var a) && a.Name.Length > 0
                ? a.Name
                : Cfg(cfgFallbackKey);

        string AuthorityTitle(string roleCode, string cfgFallbackKey) =>
            authorities is not null && authorities.TryGetValue(roleCode, out var a) && a.Name.Length > 0
                ? a.Title
                : Cfg(cfgFallbackKey);

        return fieldName.ToUpperInvariant() switch
        {
            "SYSTEM_DATE"         => now.ToString("dd/MM/yyyy"),
            "SYSTEM_DATETIME"     => now.ToString("dd/MM/yyyy HH:mm"),
            "SYSTEM_YEAR"         => now.Year.ToString(),
            "SYSTEM_MONTH"        => now.Month.ToString("00"),
            "SYSTEM_DAY"          => now.Day.ToString("00"),
            "INSTITUTION_NAME"    => "Universidad Técnica de Ambato",
            "INSTITUTION_SHORT"   => "UTA",
            // Logo institucional embebido como data URI: una sola fuente (IInstitutionalLogoService)
            // compartida con los renderers de QuestPDF, para que la vista previa y el PDF final
            // (Chromium headless) usen siempre el mismo archivo.
            "LOGO_URL"            => _logoService.GetLogoDataUri(),
            // Fechas del documento
            "APPROVAL_DATE"       => now.ToString("dd/MM/yyyy"),
            // 2026-08-24: NOTIFICATION_DATE/HOUR y EMPLOYEE_SIGNATURE_DATE/HOUR dejaron de
            // resolverse automáticamente con la fecha/hora de GENERACIÓN del documento — eso
            // era un dato falso, ya que la notificación real al servidor y su aceptación/
            // recepción ocurren después, en un momento que el sistema no conoce. Quedan en
            // blanco (caen a DefaultValue vacío) para llenarse a mano en el momento real,
            // igual que otros campos manuales de este documento (ej. LUGAR/FECHA en Posesión
            // del Puesto). A pedido del usuario.
            // Autoridades institucionales — resueltas dinámicamente desde DepartmentAuthority;
            // si no hay autoridad activa para el rol, cae al valor estático de InstitutionalConfig.
            "AUTHORITY_NAME"             => AuthorityName("RECTORADO", "AuthorityName"),
            "AUTHORITY_TITLE"            => AuthorityTitle("RECTORADO", "AuthorityTitle"),
            // Alias: CONTRATO_PROFESOR_OCASIONAL usa RECTOR_FULLNAME en vez de AUTHORITY_NAME
            // para referirse a la misma autoridad (Rectorado).
            "RECTOR_FULLNAME"            => AuthorityName("RECTORADO", "AuthorityName"),
            "VICERECTOR_NAME"            => AuthorityName("VICERRECTORADO", "VicerectorName"),
            "VICERECTOR_TITLE"           => AuthorityTitle("VICERRECTORADO", "VicerectorTitle"),
            "FINANCIAL_DIRECTOR_NAME"    => AuthorityName("FINANCE", "FinancialDirectorName"),
            "FINANCIAL_DIRECTOR_TITLE"   => AuthorityTitle("FINANCE", "FinancialDirectorTitle"),
            "DTH_DIRECTOR_NAME"          => AuthorityName("HUMAN_RESOURCE", "DthDirectorName"),
            "DTH_DIRECTOR_TITLE"         => AuthorityTitle("HUMAN_RESOURCE", "DthDirectorTitle"),
            "ELABORATOR_NAME"     => Cfg("ElaboratorName"),
            "ELABORATOR_TITLE"    => Cfg("ElaboratorTitle"),
            // Alias: CONTRATO_PROFESOR_OCASIONAL usa ELABORATOR_FULLNAME en vez de ELABORATOR_NAME.
            "ELABORATOR_FULLNAME" => Cfg("ElaboratorName"),
            "REVIEWER_NAME"       => Cfg("ReviewerName"),
            "REVIEWER_TITLE"      => Cfg("ReviewerTitle"),
            "REGISTRAR_NAME"      => Cfg("RegistrarName"),
            "REGISTRAR_TITLE"     => Cfg("RegistrarTitle"),
            _                     => null
        };
    }
}
