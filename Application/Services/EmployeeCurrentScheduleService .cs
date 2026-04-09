using WsUtaSystem.Application.DTOs.VwEmployeeCurrentSchedule;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class EmployeeCurrentScheduleService : IEmployeeCurrentScheduleService
    {
        private readonly IEmployeeCurrentScheduleRepository _repository;

        public EmployeeCurrentScheduleService(IEmployeeCurrentScheduleRepository repository)
        {
            _repository = repository;
        }

        public async Task<EmployeeCurrentScheduleDto?> GetByEmployeeIdAsync(
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByEmployeeIdAsync(employeeId, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<EmployeeCurrentScheduleDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetAllAsync(cancellationToken);
            return entities.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<EmployeeCurrentScheduleDto>> GetByEmployeeIdsAsync(
            IEnumerable<int> employeeIds,
            CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetByEmployeeIdsAsync(employeeIds, cancellationToken);
            return entities.Select(MapToDto).ToList();
        }

        private static EmployeeCurrentScheduleDto MapToDto(VwEmployeeCurrentSchedule entity)
        {
            return new EmployeeCurrentScheduleDto
            {
                EmployeeId = entity.EmployeeId,
                PersonId = entity.PersonId,
                EmployeeType = entity.EmployeeType,
                DepartmentId = entity.DepartmentId,
                ImmediateBossId = entity.ImmediateBossId,
                HireDate = entity.HireDate,
                Email = entity.Email,
                IsActive = entity.IsActive,
                EmpScheduleId = entity.EmpScheduleId,
                ScheduleId = entity.ScheduleId,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                ScheduleAssignedAt = entity.ScheduleAssignedAt,
                ScheduleAssignedBy = entity.ScheduleAssignedBy,
                ScheduleDescription = entity.ScheduleDescription,
                EntryTime = entity.EntryTime,
                ExitTime = entity.ExitTime,
                WorkingDays = entity.WorkingDays,
                RequiredHoursPerDay = entity.RequiredHoursPerDay,
                HasLunchBreak = entity.HasLunchBreak,
                LunchStart = entity.LunchStart,
                LunchEnd = entity.LunchEnd,
                IsRotating = entity.IsRotating,
                RotationPattern = entity.RotationPattern
            };
        }
    }
}
