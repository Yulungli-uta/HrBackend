using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Provisioning;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

// ══════════════════════════════════════════════════════════════════════════════
// EmployeeProvisioningOrchestrator
// ══════════════════════════════════════════════════════════════════════════════
//
// Implementación del orquestador de aprovisionamiento de cuentas institucionales.
//
// Este servicio reemplaza la lógica duplicada que existía en:
//   - ContractsService          → TriggerProvisioningAsync
//   - PersonnelActionService    → TriggerActionProvisioningAsync
//   - ContractProvisioningController → TriggerProvisioning (manual)
//
// DEPENDENCIAS INYECTADAS:
//   - AppDbContext              → lectura de hr.tbl_People, hr.tbl_Employees
//   - IEmployeesService        → crear/buscar registro en hr.tbl_Employees
//   - IEmployeeProvisioningClient → llamada HTTP a RepositoryUta
//   - IEmailBuilder            → encolar correo SMTP
//   - IParametersRepository    → leer plantillas de correo desde TBL_PARAMETERS
//
// LOGS ESTRUCTURADOS (prefijo [ORCHESTRATOR]):
//   [ORCHESTRATOR]          → flujo principal
//   [ORCHESTRATOR][EMAIL]   → envío de correo de bienvenida
//
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Orquestador reutilizable del flujo de aprovisionamiento de cuenta institucional.
/// <para>
/// Centraliza los pasos post-firma de documento para contratos y acciones de personal:
/// garantiza el registro de empleado, llama a RepositoryUta, actualiza el email
/// institucional y notifica al empleado por correo.
/// </para>
/// </summary>
public sealed class EmployeeProvisioningOrchestrator : IEmployeeProvisioningOrchestrator
{
    // ── Status de RepositoryUta que indican cuenta AD creada (aunque licencia falle) ──
    // 2002=CreatedInLocalAd, 2003=PendingEntraSync, 2004=SyncedInEntra,
    // 2005=LicenseAssigned, 2006=LicenseFailed
    // 2007=LocalAdFailed → sin cuenta, no se considera éxito
    private static bool AccountCreatedInAd(int statusId) => statusId is >= 2002 and <= 2006;

    // Claves de plantilla en HR.TBL_PARAMETERS (columna Name)
    private const string TemplateContract = "EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT";
    private const string TemplateAction   = "EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION";

    private readonly AppDbContext _db;
    private readonly IEmployeesService _employeesService;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly IEmailBuilder _emailBuilder;
    private readonly IParametersRepository _parametersRepository;
    private readonly IEmployeeLaborRegimeService _laborRegimeService;
    private readonly IPersonnelMovementsService _movementsService;
    private readonly ILogger<EmployeeProvisioningOrchestrator> _logger;

    public EmployeeProvisioningOrchestrator(
        AppDbContext db,
        IEmployeesService employeesService,
        IEmployeeProvisioningClient provisioningClient,
        IEmailBuilder emailBuilder,
        IParametersRepository parametersRepository,
        IEmployeeLaborRegimeService laborRegimeService,
        IPersonnelMovementsService movementsService,
        ILogger<EmployeeProvisioningOrchestrator> logger)
    {
        _db                   = db;
        _employeesService     = employeesService;
        _provisioningClient   = provisioningClient;
        _emailBuilder         = emailBuilder;
        _parametersRepository = parametersRepository;
        _laborRegimeService   = laborRegimeService;
        _movementsService     = movementsService;
        _logger               = logger;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUNTO DE ENTRADA PÚBLICO
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<OrchestratorResult> ExecuteAsync(
        ProvisioningOrchestrationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ORCHESTRATOR] ══ INICIO ══ PersonId={PersonId} | Source={Source} | Ref={SourceRef}",
            request.PersonId, request.Source, request.SourceReference);

        try
        {
            // ── [1] Garantizar registro en hr.tbl_Employees ────────────────────
            // Si la persona tiene PersonID pero aún no fue dada de alta como empleado,
            // se crea el registro aquí antes de continuar con el aprovisionamiento.
            var employee = await EnsureEmployeeAsync(request, ct);

            // ── [2] Leer datos de la persona desde hr.tbl_People ───────────────
            // Se necesita: correo personal (destino del correo de bienvenida)
            // y nombres (para generar el email institucional en RepositoryUta).
            var person = await _db.People
                .AsNoTracking()
                .Where(p => p.PersonId == request.PersonId)
                .Select(p => new { p.FirstName, p.LastName, p.Email, p.IdCard })
                .FirstOrDefaultAsync(ct);

            if (person is null || string.IsNullOrWhiteSpace(person.Email))
            {
                _logger.LogError(
                    "[ORCHESTRATOR] ✗ PersonId={PersonId} sin correo personal en hr.tbl_People. " +
                    "El aprovisionamiento requiere correo personal para notificar credenciales.",
                    request.PersonId);
                return Fail(employee.EmployeeId,
                    "Persona sin correo personal registrado en hr.tbl_People");
            }

            var personalEmail   = person.Email.Trim().ToLowerInvariant();
            var displayName     = $"{person.FirstName} {person.LastName}".Trim();
            var initialPassword = GenerateInitialPassword();

            _logger.LogInformation(
                "[ORCHESTRATOR] Persona cargada: PersonId={PersonId} | Nombre='{DisplayName}' | " +
                "CorreoPersonal={PersonalEmail} | EmployeeId={EmployeeId}",
                request.PersonId, displayName, personalEmail, employee.EmployeeId);

            // ── [3] Llamar a RepositoryUta ──────────────────────────────────────
            // RepositoryUta genera el email institucional internamente
            // (formato: iniciales.apellido@uta.edu.ec) y además crea:
            //   - auth.tbl_Users
            //   - auth.tbl_UserEmployees
            //   - auth.tbl_UserRoles  (roles configurados en Provisioning:DefaultRoleNames)
            //   - Grupo AD           (grupo configurado en Provisioning:DefaultAdGroupId)
            var provisionResult = await CallRepositoryUtaAsync(
                employee, person.FirstName, person.LastName,
                displayName, personalEmail, initialPassword, person.IdCard, request, ct);

            if (provisionResult is null)
                return Fail(employee.EmployeeId, "RepositoryUta no respondió (error de red o 4xx)");

            _logger.LogInformation(
                "[ORCHESTRATOR] Respuesta RepositoryUta: EmployeeId={EmployeeId} | " +
                "Status={Status} | Email={Email} | AlreadyExists={AlreadyExists} | Error={Error}",
                employee.EmployeeId, provisionResult.ProvisioningStatusName,
                provisionResult.Email, provisionResult.AlreadyExists,
                provisionResult.ErrorMessage ?? "ninguno");

            // Cuenta ya existía → no se re-provisiona (la creó un flujo anterior)
            if (provisionResult.AlreadyExists)
            {
                _logger.LogInformation(
                    "[ORCHESTRATOR] Cuenta ya existía para EmployeeId={EmployeeId}. No se re-provisiona.",
                    employee.EmployeeId);
                return new OrchestratorResult(
                    false, true, provisionResult.Email,
                    employee.EmployeeId, provisionResult.ProvisioningStatusName, null);
            }

            // Fallo en AD Local → sin cuenta, no continuar
            if (!AccountCreatedInAd(provisionResult.ProvisioningStatusId))
            {
                _logger.LogError(
                    "[ORCHESTRATOR] ✗ Cuenta NO creada en AD Local. EmployeeId={EmployeeId} | " +
                    "Status={Status} | Error={Error}",
                    employee.EmployeeId, provisionResult.ProvisioningStatusName,
                    provisionResult.ErrorMessage);
                return Fail(employee.EmployeeId,
                    provisionResult.ErrorMessage ?? provisionResult.ProvisioningStatusName);
            }

            // ── [4] Actualizar hr.tbl_Employees.Email con email institucional ───
            // Solo se ejecuta si la cuenta fue creada en AD (status 2002–2006).
            var institutionalEmail = provisionResult.Email ?? personalEmail;
            var emailWarning = await UpdateEmployeeEmailAsync(employee.EmployeeId, institutionalEmail, request.UpdatedBy, ct);

            // ── [5] Enviar correo de bienvenida al correo personal ──────────────
            // La plantilla se elige según el origen: contrato vs acción de personal.
            await SendWelcomeEmailAsync(
                source:             request.Source,
                firstName:          person.FirstName,
                personalEmail:      personalEmail,
                institutionalEmail: institutionalEmail,
                initialPassword:    initialPassword,
                ct:                 ct);

            // Consolidar avisos: RepositoryUta + email conflict
            var combinedWarning = string.Join(" | ", new[] { provisionResult.Warning, emailWarning }
                .Where(w => !string.IsNullOrWhiteSpace(w)));
            if (!string.IsNullOrWhiteSpace(combinedWarning))
                _logger.LogWarning("[ORCHESTRATOR] ⚠ Avisos: {Warning}", combinedWarning);

            _logger.LogInformation(
                "[ORCHESTRATOR] ══ COMPLETADO ══ EmployeeId={EmployeeId} | " +
                "EmailInstitucional={Email} | Status={Status} | Warning={Warning}",
                employee.EmployeeId, institutionalEmail,
                provisionResult.ProvisioningStatusName,
                string.IsNullOrWhiteSpace(combinedWarning) ? "ninguno" : combinedWarning);

            return new OrchestratorResult(
                true, false, institutionalEmail,
                employee.EmployeeId, provisionResult.ProvisioningStatusName,
                ErrorMessage: null,
                Warning: string.IsNullOrWhiteSpace(combinedWarning) ? null : combinedWarning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ORCHESTRATOR] ✗ ERROR INESPERADO. PersonId={PersonId} | Ref={SourceRef}",
                request.PersonId, request.SourceReference);
            return Fail(null, ex.Message);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PASO 1 — EnsureEmployee
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Garantiza que exista un registro activo en <c>hr.tbl_Employees</c>
    /// para el PersonId indicado. Si no existe, lo crea con los datos del request.
    /// <para>
    /// Caso típico: la persona tiene registro en <c>hr.tbl_People</c> (fue ingresada
    /// por RRHH) pero aún no tiene fila en <c>hr.tbl_Employees</c> porque el contrato
    /// es el primer evento que la vincula como empleado activo.
    /// </para>
    /// </summary>
    private async Task<Employees> EnsureEmployeeAsync(
        ProvisioningOrchestrationRequest request, CancellationToken ct)
    {
        // Busca empleado activo más reciente para ese PersonID
        var existing = (await _employeesService.GetByPersonIdAsync(request.PersonId, ct))
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.EmployeeId)
            .FirstOrDefault();

        if (existing is not null)
        {
            _logger.LogInformation(
                "[ORCHESTRATOR] hr.tbl_Employees: registro existente. " +
                "EmployeeId={EmployeeId} | PersonId={PersonId}",
                existing.EmployeeId, request.PersonId);
            return existing;
        }

        // No existe → crear con los datos disponibles del contrato/acción
        _logger.LogInformation(
            "[ORCHESTRATOR] hr.tbl_Employees: no existe registro activo para " +
            "PersonId={PersonId}. Creando nuevo registro...",
            request.PersonId);

        var hireDate = request.HireDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Si nadie resolvió el jefe inmediato, buscarlo desde tbl_DepartmentAuthorities.
        // Prioridad: Director (237) sobre Decano (235). Aplica a cualquier caller automáticamente.
        var bossId = request.ImmediateBossId;
        if (bossId is null && request.DepartmentId.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            bossId = await _db.DepartmentAuthorities
                .AsNoTracking()
                .Where(a => a.DepartmentId == request.DepartmentId.Value
                         && a.IsActive
                         && (a.AuthorityTypeId == 237 || a.AuthorityTypeId == 235)
                         && a.StartDate <= today
                         && (a.EndDate == null || a.EndDate >= today))
                .OrderBy(a => a.AuthorityTypeId == 237 ? 1 : 2)
                .Select(a => (int?)a.EmployeeId)
                .FirstOrDefaultAsync(ct);
        }

        var newEmployee = new Employees
        {
            PersonID        = request.PersonId,
            EmployeeType    = request.EmployeeType,
            DepartmentId    = request.DepartmentId,
            JobId           = request.JobId,
            ImmediateBossId = bossId,
            HireDate        = hireDate,
            IsActive        = true,
            CreatedBy       = request.UpdatedBy,
            CreatedAt       = DateTime.Now
        };

        var created = await _employeesService.CreateAsync(newEmployee, ct);

        _logger.LogInformation(
            "[ORCHESTRATOR] ✓ hr.tbl_Employees creado: EmployeeId={EmployeeId} | " +
            "PersonId={PersonId} | EmployeeType={Type} | DepartmentId={Dept} | HireDate={Date}",
            created.EmployeeId, request.PersonId,
            request.EmployeeType, request.DepartmentId, hireDate);

        await RegisterInitialRegimeAndMovementAsync(created, request, hireDate, ct);

        return created;
    }

    /// <summary>
    /// Registra el régimen inicial (IsPrincipal=true) y el movimiento de INGRESO
    /// para un empleado recién creado. No bloquea el aprovisionamiento si falla.
    /// </summary>
    private async Task RegisterInitialRegimeAndMovementAsync(
        Employees employee, ProvisioningOrchestrationRequest request, DateOnly hireDate, CancellationToken ct)
    {
        var (documentType, contractId, actionId) = ParseSourceReference(request.SourceReference);

        try
        {
            if (request.EmployeeType > 0)
            {
                await _laborRegimeService.CreateAsync(new DTOs.EmployeeLaborRegime.EmployeeLaborRegimeCreateDto
                {
                    EmployeeId = employee.EmployeeId,
                    LaborRegimeId = request.EmployeeType,
                    DepartmentId = request.DepartmentId,
                    JobId = request.JobId,
                    IsIndefinite = false,
                    DocumentType = documentType,
                    DocumentNumber = null,
                    SourceContractId = contractId,
                    SourcePersonnelActionId = actionId,
                    EffectiveFrom = hireDate,
                }, request.UpdatedBy, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ORCHESTRATOR][LABOR-REGIME] ERROR registrando régimen inicial. EmployeeId={EmployeeId}",
                employee.EmployeeId);
        }

        try
        {
            if (request.JobId.HasValue)
            {
                var movementTypeId = await ResolveMovementTypeIdAsync("INGRESO", ct);
                await _movementsService.CreateAsync(new PersonnelMovements
                {
                    EmployeeId = employee.EmployeeId,
                    ContractId = contractId,
                    JobId = request.JobId.Value,
                    OriginDepartmentId = null,
                    DestinationDepartmentId = request.DepartmentId ?? 0,
                    MovementDate = hireDate,
                    MovementTypeId = movementTypeId,
                    PersonnelActionId = actionId,
                    IsActive = true,
                    CreatedBy = request.UpdatedBy,
                    CreatedAt = DateTime.Now,
                }, ct);
            }
            else
            {
                _logger.LogInformation(
                    "[ORCHESTRATOR][MOVEMENT] Registro de INGRESO omitido: sin JobId. EmployeeId={EmployeeId}",
                    employee.EmployeeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ORCHESTRATOR][MOVEMENT] ERROR registrando movimiento de INGRESO. EmployeeId={EmployeeId}",
                employee.EmployeeId);
        }
    }

    /// <summary>Parsea "Contract:123" / "PersonnelAction:456" en (DocumentType, ContractId, PersonnelActionId).</summary>
    private static (string DocumentType, int? ContractId, int? ActionId) ParseSourceReference(string? sourceReference)
    {
        if (!string.IsNullOrWhiteSpace(sourceReference))
        {
            var parts = sourceReference.Split(':', 2);
            if (parts.Length == 2 && int.TryParse(parts[1], out var id))
            {
                if (parts[0].Equals("Contract", StringComparison.OrdinalIgnoreCase))
                    return ("CONTRACT", id, null);
                if (parts[0].Equals("PersonnelAction", StringComparison.OrdinalIgnoreCase))
                    return ("PERSONNEL_ACTION", null, id);
            }
        }
        return ("MIGRATION", null, null);
    }

    /// <summary>Resuelve el TypeId de HR.ref_Types (Category='MOVEMENT_TYPE') por nombre.</summary>
    private async Task<int?> ResolveMovementTypeIdAsync(string name, CancellationToken ct)
    {
        var id = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "MOVEMENT_TYPE" && r.Name == name && r.IsActive)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);
        return id == 0 ? null : id;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PASO 3 — Llamada a RepositoryUta
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Construye el request y llama al cliente HTTP de RepositoryUta.
    /// Retorna null si RepositoryUta no responde o devuelve un error no recuperable.
    /// </summary>
    private async Task<HrProvisioningResult?> CallRepositoryUtaAsync(
        Employees employee,
        string firstName, string lastName,
        string displayName, string personalEmail,
        string initialPassword, string? idCard,
        ProvisioningOrchestrationRequest request,
        CancellationToken ct)
    {
        var provRequest = new HrProvisionEmployeeRequest(
            HrEmployeeId:    employee.EmployeeId,
            Email:           null,           // RepositoryUta genera el email institucional
            DisplayName:     displayName,
            GivenName:       firstName.Trim(),
            Surname:         lastName.Trim(),
            InitialPassword: initialPassword,
            EmployeeTypeId:  request.EmployeeType,
            DepartmentId:    request.DepartmentId,
            DepartmentName:  request.DepartmentName,
            SourceReference: request.SourceReference,
            PersonalEmail:   personalEmail,
            IdCard:          idCard
        );

        _logger.LogInformation(
            "[ORCHESTRATOR] Llamando a RepositoryUta. EmployeeId={EmployeeId} | " +
            "DisplayName='{DisplayName}' | SourceRef={SourceRef}",
            employee.EmployeeId, displayName, request.SourceReference);

        return await _provisioningClient.ProvisionAsync(provRequest, request.BearerToken, ct);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PASO 4 — Actualizar email institucional en hr.tbl_Employees
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Actualiza el campo <c>Email</c> en <c>hr.tbl_Employees</c> con el
    /// email institucional generado por RepositoryUta.
    /// </summary>
    /// <returns>
    /// Aviso no bloqueante si el email ya pertenece a otro empleado (skip + warning).
    /// <c>null</c> si la actualización fue exitosa o el empleado ya tenía ese email.
    /// </returns>
    private async Task<string?> UpdateEmployeeEmailAsync(
        int employeeId, string institutionalEmail, int updatedBy, CancellationToken ct)
    {
        // Verificar conflicto: otro empleado ya tiene este email institucional
        var conflictId = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Email == institutionalEmail && e.EmployeeId != employeeId)
            .Select(e => (int?)e.EmployeeId)
            .FirstOrDefaultAsync(ct);

        if (conflictId.HasValue)
        {
            var warning =
                $"Email institucional '{institutionalEmail}' ya está asignado al empleado " +
                $"ID={conflictId.Value}. No se actualizó hr.tbl_Employees para EmployeeId={employeeId}.";
            _logger.LogWarning("[ORCHESTRATOR] ⚠ {Warning}", warning);
            return warning;
        }

        await _db.Employees
            .Where(e => e.EmployeeId == employeeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Email,     institutionalEmail)
                .SetProperty(e => e.UpdatedBy, updatedBy)
                .SetProperty(e => e.UpdatedAt, DateTime.Now),
                ct);

        _logger.LogInformation(
            "[ORCHESTRATOR] ✓ hr.tbl_Employees.Email actualizado. " +
            "EmployeeId={EmployeeId} | EmailInstitucional={Email}",
            employeeId, institutionalEmail);

        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PASO 5 — Correo de bienvenida
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Envía el correo de bienvenida al correo personal del empleado.
    /// <para>
    /// Elige la plantilla según el <see cref="ProvisioningSource"/>:
    /// <list type="bullet">
    ///   <item><see cref="ProvisioningSource.PersonnelAction"/> → <c>EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION</c></item>
    ///   <item><see cref="ProvisioningSource.Contract"/> / <see cref="ProvisioningSource.Manual"/> → <c>EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT</c></item>
    /// </list>
    /// Si la plantilla no existe en BD, usa un HTML de respaldo (fallback).
    /// </para>
    /// <para>
    /// No lanza excepciones: un fallo de correo no detiene el aprovisionamiento.
    /// </para>
    /// </summary>
    private async Task SendWelcomeEmailAsync(
        ProvisioningSource source,
        string firstName,
        string personalEmail,
        string institutionalEmail,
        string initialPassword,
        CancellationToken ct)
    {
        // Selección de plantilla según origen
        var templateKey = source == ProvisioningSource.PersonnelAction
            ? TemplateAction
            : TemplateContract;

        _logger.LogInformation(
            "[ORCHESTRATOR][EMAIL] ══ INICIO ENVÍO ══\n" +
            "  Para (correo personal) : {PersonalEmail}\n" +
            "  EmailInstitucional     : {InstitutionalEmail}\n" +
            "  Nombre                 : {FirstName}\n" +
            "  Contraseña temporal    : '{InitialPassword}'\n" +
            "  Plantilla BD           : {TemplateKey}\n" +
            "  Layout                 : AccountCreated → 'informativo'",
            personalEmail, institutionalEmail, firstName, initialPassword, templateKey);

        try
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"]          = firstName,
                ["InstitutionalEmail"] = institutionalEmail,
                ["InitialPassword"]    = initialPassword
            };

            // HTML de respaldo si la plantilla no está configurada en BD
            var fallback = $"""
                <p>Estimado/a <strong>{firstName}</strong>,</p>
                <p>Se ha creado su cuenta institucional en la Universidad Técnica de Ambato:</p>
                <ul>
                  <li><strong>Usuario:</strong> {institutionalEmail}</li>
                  <li><strong>Contraseña temporal:</strong> {initialPassword}</li>
                </ul>
                <p><em>Deberá cambiar su contraseña en el primer inicio de sesión.</em></p>
                <p>Si tiene alguna inconveniencia, comuníquese con el Departamento de Talento Humano.</p>
                """;

            var html         = await BuildEmailBodyAsync(templateKey, vars, fallback, ct);
            var usingFallback = ReferenceEquals(html, fallback);

            _logger.LogInformation(
                "[ORCHESTRATOR][EMAIL] Cuerpo construido. " +
                "UsandoFallback={UsingFallback} | LongitudHtml={Len} chars",
                usingFallback, html.Length);

            await _emailBuilder.TryNotifyAsync(
                EmailTemplateKey.AccountCreated,
                "Cuenta institucional UTA creada",
                html,
                to: personalEmail,
                timeoutSeconds: 15,
                ct: ct);

            _logger.LogInformation(
                "[ORCHESTRATOR][EMAIL] ✓ Correo encolado. Para={PersonalEmail}", personalEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ORCHESTRATOR][EMAIL] ✗ ERROR al enviar correo. Para={PersonalEmail}",
                personalEmail);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UTILIDADES PRIVADAS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Construye el cuerpo HTML del correo reemplazando los placeholders <c>{Key}</c>
    /// con los valores del diccionario. Retorna el fallback si la plantilla no existe en BD.
    /// </summary>
    private async Task<string> BuildEmailBodyAsync(
        string paramName,
        Dictionary<string, string> vars,
        string fallbackHtml,
        CancellationToken ct)
    {
        var results = await _parametersRepository.GetByNameAsync(paramName, ct);
        var param   = results.FirstOrDefault();

        if (param is null || string.IsNullOrWhiteSpace(param.Pvalues))
        {
            _logger.LogWarning(
                "[ORCHESTRATOR][EMAIL] Plantilla '{ParamName}' no encontrada en TBL_PARAMETERS — usando HTML fallback.",
                paramName);
            return fallbackHtml;
        }

        var body = param.Pvalues;
        foreach (var (key, value) in vars)
            body = body.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);

        return body;
    }

    /// <summary>
    /// Genera la contraseña temporal inicial con formato <c>Uta@YYYY!NNNN</c>.
    /// El empleado debe cambiarla en el primer inicio de sesión (ForcePasswordChange = true).
    /// </summary>
    private static string GenerateInitialPassword()
    {
        var suffix = Random.Shared.Next(1000, 9999);
        return $"Uta@{DateTime.Now.Year}!{suffix:D4}";
    }

    /// <summary>Helper para construir un resultado de fallo uniforme.</summary>
    private static OrchestratorResult Fail(int? employeeId, string? message) =>
        new(false, false, null, employeeId, null, message);
}
