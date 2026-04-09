using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.ScheduleChange;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class ScheduleChangePlanService : IScheduleChangePlanService
    {
        private const string PLAN_STATUS_CATEGORY = "SCHEDULE_CHANGE_STATUS";
        private const string DETAIL_STATUS_CATEGORY = "SCH_CHANGE_EMP_STATUS";

        private readonly IScheduleChangePlanRepository _repository;
        private readonly IRefTypesService _refTypesService;
        private readonly ILogger<ScheduleChangePlanService> _logger;
        private readonly IEmployeeCurrentScheduleService _employeeCurrentScheduleService;

        public ScheduleChangePlanService(
            IScheduleChangePlanRepository repository,
            IRefTypesService refTypesService,
            IEmployeeCurrentScheduleService employeeCurrentScheduleService,
            ILogger<ScheduleChangePlanService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _refTypesService = refTypesService ?? throw new ArgumentNullException(nameof(refTypesService));
            _employeeCurrentScheduleService = employeeCurrentScheduleService ?? throw new ArgumentNullException(nameof(employeeCurrentScheduleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ScheduleChangePlanResponse?> GetByIdAsync(int planId, CancellationToken ct = default)
        {
            try
            {
                if (planId <= 0) return null;

                var plan = await _repository.GetByIdAsync(planId, ct);
                return plan is null ? null : MapToResponse(plan);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[SCP-SVC] GetByIdAsync CANCELED planId={PlanId}", planId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] GetByIdAsync ERROR planId={PlanId}", planId);
                throw;
            }
        }

        public async Task<IEnumerable<ScheduleChangePlanResponse>> GetByBossIdAsync(int bossId, CancellationToken ct = default)
        {
            try
            {
                if (bossId <= 0)
                    throw new ArgumentException("BossId inválido.", nameof(bossId));

                var plans = await _repository.GetByBossIdAsync(bossId, ct);
                return plans.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] GetByBossIdAsync ERROR bossId={BossId}", bossId);
                throw;
            }
        }

        public async Task<IEnumerable<ScheduleChangePlanResponse>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            try
            {
                if (statusTypeId <= 0)
                    throw new ArgumentException("StatusTypeId inválido.", nameof(statusTypeId));

                var plans = await _repository.GetByStatusAsync(statusTypeId, ct);
                return plans.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] GetByStatusAsync ERROR statusTypeId={StatusTypeId}", statusTypeId);
                throw;
            }
        }

        public async Task<PagedResult<ScheduleChangePlanResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var paged = await _repository.GetPagedAsync(page, pageSize, ct);
                return new PagedResult<ScheduleChangePlanResponse>
                {
                    Items = paged.Items.Select(MapToResponse).ToList(),
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] GetPagedAsync ERROR page={Page} pageSize={PageSize}", page, pageSize);
                throw;
            }
        }

        public async Task<ScheduleChangePlanResponse> CreateAsync(CreateScheduleChangePlanRequest request, CancellationToken ct = default)
        {
            try
            {
                ValidateCreateRequest(request);

                var approvedPlanStatusId = await GetStatusTypeIdAsync(
                    PLAN_STATUS_CATEGORY,
                    "Aprobado",
                    ct);

                var pendingDetailStatusId = await GetStatusTypeIdAsync(
                    DETAIL_STATUS_CATEGORY,
                    "Pendiente",
                    ct);

                var now = DateTime.Now;

                var employeeIds = request.Details
                    .Select(d => d.EmployeeID)
                    .Distinct()
                    .ToList();

                var currentSchedules = await _employeeCurrentScheduleService.GetByEmployeeIdsAsync(employeeIds, ct);

                var currentSchedulesByEmployeeId = currentSchedules.ToDictionary(x => x.EmployeeId, x => x);

                var details = request.Details.Select(d =>
                {
                    currentSchedulesByEmployeeId.TryGetValue(d.EmployeeID, out var currentSchedule);

                    return new ScheduleChangePlanDetail
                    {
                        EmployeeID = d.EmployeeID,
                        Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim(),
                        StatusTypeID = pendingDetailStatusId,

                        // Aquí guardas la foto del horario actual al momento de crear la planificación
                        PreviousScheduleID = currentSchedule?.ScheduleId,
                        PreviousEmpScheduleID = currentSchedule?.EmpScheduleId,

                        CreatedAt = now,
                        UpdatedAt = now
                    };
                }).ToList();

                var plan = new ScheduleChangePlan
                {
                    Title = request.Title.Trim(),
                    Justification = string.IsNullOrWhiteSpace(request.Justification)
                        ? null
                        : request.Justification.Trim(),
                    RequestedByBossID = request.RequestedByBossID,
                    NewScheduleID = request.NewScheduleID,
                    EffectiveDate = request.EffectiveDate,
                    ApplyAfterHours = request.ApplyAfterHours,
                    IsPermanent = request.IsPermanent,
                    TemporalEndDate = request.IsPermanent ? null : request.TemporalEndDate,
                    StatusTypeID = approvedPlanStatusId,
                    ApprovedByID = request.RequestedByBossID,
                    ApprovedAt = now,
                    Details = details
                };

                var created = await _repository.CreateAsync(plan, ct);

                created.PlanCode = $"SCP-{created.PlanID:D6}";
                created.UpdatedAt = DateTime.Now;
                created.UpdatedBy = request.RequestedByBossID;

                await _repository.UpdateAsync(created, ct);

                return MapToResponse(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] CreateAsync ERROR");
                throw;
            }
        }

        public async Task ApproveAsync(ApproveScheduleChangePlanRequest request, CancellationToken ct = default)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(request.PlanID, ct)
                    ?? throw new KeyNotFoundException($"Plan {request.PlanID} no encontrado.");

                if (!request.IsApproved && string.IsNullOrWhiteSpace(request.RejectionReason))
                    throw new ArgumentException("RejectionReason es obligatorio al rechazar.");

                var approvedStatusId = await GetStatusTypeIdAsync(
                    PLAN_STATUS_CATEGORY,
                    "Aprobado",
                    ct);

                var rejectedStatusId = await GetStatusTypeIdAsync(
                    PLAN_STATUS_CATEGORY,
                    "Rechazado",
                    ct);

                plan.StatusTypeID = request.IsApproved ? approvedStatusId : rejectedStatusId;
                plan.ApprovedByID = request.ApprovedByID;
                plan.ApprovedAt = DateTime.Now;
                plan.RejectionReason = request.IsApproved ? null : request.RejectionReason?.Trim();
                plan.UpdatedAt = DateTime.Now;
                plan.UpdatedBy = request.ApprovedByID;

                await _repository.UpdateAsync(plan, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] ApproveAsync ERROR planId={PlanId}", request.PlanID);
                throw;
            }
        }

        public async Task CancelAsync(CancelScheduleChangePlanRequest request, CancellationToken ct = default)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(request.PlanID, ct)
                    ?? throw new KeyNotFoundException($"Plan {request.PlanID} no encontrado.");

                var cancelledStatusId = await GetStatusTypeIdAsync(
                    PLAN_STATUS_CATEGORY,
                    "Cancelado",
                    ct);

                plan.StatusTypeID = cancelledStatusId;
                plan.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
                plan.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(plan, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCP-SVC] CancelAsync ERROR planId={PlanId}", request.PlanID);
                throw;
            }
        }

        private void ValidateCreateRequest(CreateScheduleChangePlanRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("El título es obligatorio.", nameof(request.Title));

            if (string.IsNullOrWhiteSpace(request.Justification))
                throw new ArgumentException("La justificación es obligatoria.", nameof(request.Justification));

            if (request.RequestedByBossID <= 0)
                throw new ArgumentException("RequestedByBossID inválido.", nameof(request.RequestedByBossID));

            if (request.NewScheduleID <= 0)
                throw new ArgumentException("NewScheduleID inválido.", nameof(request.NewScheduleID));

            if (request.ApplyAfterHours != 24 && request.ApplyAfterHours != 48)
                throw new ArgumentException("ApplyAfterHours solo acepta 24 o 48.", nameof(request.ApplyAfterHours));

            if (!request.IsPermanent && request.TemporalEndDate is null)
                throw new ArgumentException("TemporalEndDate es obligatorio cuando el cambio no es permanente.");

            if (request.Details is null || request.Details.Count == 0)
                throw new ArgumentException("Debe existir al menos un empleado en el detalle.", nameof(request.Details));

            if (request.Details.Any(d => d.EmployeeID <= 0))
                throw new ArgumentException("Todos los detalles deben tener un EmployeeID válido.", nameof(request.Details));
        }

        private async Task<int> GetStatusTypeIdAsync(
            string category,
            string statusName,
            CancellationToken ct)
        {
            var statuses = await _refTypesService.GetByCategoryAsync(category, ct);

            var match = statuses.FirstOrDefault(x =>
                string.Equals(x.Name, statusName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró el estado '{statusName}' en la categoría '{category}'.");
            }

            return match.TypeId;
        }

        private static ScheduleChangePlanResponse MapToResponse(ScheduleChangePlan p) =>
            new()
            {
                PlanID = p.PlanID,
                PlanCode = p.PlanCode,
                Title = p.Title,
                Justification = p.Justification,
                RequestedByBossID = p.RequestedByBossID,
                NewScheduleID = p.NewScheduleID,
                EffectiveDate = p.EffectiveDate,
                ApplyAfterHours = p.ApplyAfterHours,
                EffectiveApplyDate = p.EffectiveApplyDate,
                IsPermanent = p.IsPermanent,
                TemporalEndDate = p.TemporalEndDate,
                StatusTypeID = p.StatusTypeID,
                ApprovedByID = p.ApprovedByID,
                ApprovedAt = p.ApprovedAt,
                RejectionReason = p.RejectionReason,
                AppliedAt = p.AppliedAt,
                CreatedAt = (DateTime)p.CreatedAt!,
                UpdatedAt = p.UpdatedAt,
                Details = p.Details.Select(d => new ScheduleChangePlanDetailResponse
                {
                    DetailID = d.DetailID,
                    PlanID = d.PlanID,
                    EmployeeID = d.EmployeeID,
                    PreviousScheduleID = d.PreviousScheduleID,
                    PreviousEmpScheduleID = d.PreviousEmpScheduleID,
                    AppliedEmpScheduleID = d.AppliedEmpScheduleID,
                    StatusTypeID = d.StatusTypeID,
                    Notes = d.Notes,
                    OmissionReason = d.OmissionReason,
                    AppliedAt = d.AppliedAt,
                    CreatedAt = d.CreatedAt
                }).ToList()
            };
    }
}