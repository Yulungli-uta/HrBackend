using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
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
    private readonly ILogger<DocumentFieldResolver> _logger;

    public DocumentFieldResolver(AppDbContext db, IConfiguration config, ILogger<DocumentFieldResolver> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> ResolveAsync(
        IReadOnlyList<DocumentTemplateField> fields,
        int employeeId,
        int? entityId,
        Dictionary<string, string>? overrides = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Cargar datos de empleado (siempre necesarios) ────────────────────────
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.People)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);

        if (employee is null)
        {
            _logger.LogWarning(
                "DocumentFieldResolver: Empleado {EmployeeId} no encontrado.", employeeId);
            return result;
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
        else
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

        // ── Cargar movimiento de personal (si aplica) ────────────────────────────
        // FieldSourceType es enum real → comparar directamente (no usar ToString())
        PersonnelMovements? movement = null;
        if (entityId.HasValue && fields.Any(f => f.SourceType == FieldSourceType.Movement))
        {
            movement = await _db.PersonnelMovements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MovementId == entityId.Value, ct);
        }

        // ── Resolver cada campo ──────────────────────────────────────────────────
        foreach (var field in fields)
        {
            // 1. Override manual tiene máxima prioridad
            if (overrides is not null && overrides.TryGetValue(field.FieldName, out var manualValue))
            {
                result[field.FieldName] = manualValue;
                continue;
            }

            // 2. Resolución automática según SourceType (enum real)
            var resolved = ResolveField(field, employee, contract, contractType, department, job, movement);

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
        PersonnelMovements? movement)
    {
        return field.SourceType switch
        {
            FieldSourceType.Employee => ResolveEmployeeField(field.FieldName, employee),
            FieldSourceType.Contract => ResolveContractField(field.FieldName, contract, contractType, department, job),
            FieldSourceType.Movement => ResolveMovementField(field.FieldName, movement),
            FieldSourceType.System   => ResolveSystemField(field.FieldName, _config),
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
            "EMPLOYEE_FULLNAME"  => p is not null ? $"{p.FirstName} {p.LastName}".ToUpperInvariant() : null,
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
        Job? job)
    {
        if (contract is null) return null;

        return fieldName.ToUpperInvariant() switch
        {
            "CONTRACT_CODE"        => contract.ContractCode,
            "CONTRACT_TYPE"        => contractType?.Name,
            "CONTRACT_STARTDATE"   => contract.StartDate.ToString("dd/MM/yyyy"),
            "CONTRACT_ENDDATE"     => contract.EndDate.ToString("dd/MM/yyyy"),
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
            "MOVEMENT_TYPE"   => movement.MovementType,
            "MOVEMENT_REASON" => movement.Reason,
            _                 => null
        };
    }

    private static string? ResolveSystemField(string fieldName, IConfiguration config)
    {
        var now = DateTime.Now;

        // Leer config institucional (sección InstitutionalConfig en appsettings.json)
        string Cfg(string key) => config[$"InstitutionalConfig:{key}"] ?? string.Empty;

        return fieldName.ToUpperInvariant() switch
        {
            "SYSTEM_DATE"         => now.ToString("dd/MM/yyyy"),
            "SYSTEM_DATETIME"     => now.ToString("dd/MM/yyyy HH:mm"),
            "SYSTEM_YEAR"         => now.Year.ToString(),
            "SYSTEM_MONTH"        => now.Month.ToString("00"),
            "SYSTEM_DAY"          => now.Day.ToString("00"),
            "INSTITUTION_NAME"    => "Universidad Técnica de Ambato",
            "INSTITUTION_SHORT"   => "UTA",
            // Fechas del documento
            "APPROVAL_DATE"       => now.ToString("dd/MM/yyyy"),
            "NOTIFICATION_DATE"   => now.ToString("dd/MM/yyyy"),
            "NOTIFICATION_HOUR"   => now.ToString("HH:mm"),
            // Responsables institucionales (leídos de configuración)
            "DTH_DIRECTOR_NAME"   => Cfg("DthDirectorName"),
            "DTH_DIRECTOR_TITLE"  => Cfg("DthDirectorTitle"),
            "AUTHORITY_NAME"      => Cfg("AuthorityName"),
            "AUTHORITY_TITLE"     => Cfg("AuthorityTitle"),
            "ELABORATOR_NAME"     => Cfg("ElaboratorName"),
            "ELABORATOR_TITLE"    => Cfg("ElaboratorTitle"),
            "REVIEWER_NAME"       => Cfg("ReviewerName"),
            "REVIEWER_TITLE"      => Cfg("ReviewerTitle"),
            "REGISTRAR_NAME"      => Cfg("RegistrarName"),
            "REGISTRAR_TITLE"     => Cfg("RegistrarTitle"),
            _                     => null
        };
    }
}
