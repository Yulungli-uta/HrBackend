using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Repositories;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class EmployeeSchedulesService : Service<EmployeeSchedules, int>, IEmployeeSchedulesService
{
    private readonly IEmployeeSchedulesRepository _repository;
    private readonly DbContext _dbContext; // Necesitas inyectar tu DbContext
    //private readonly ApplicationDbContext _dbContext;

    public EmployeeSchedulesService(
        IEmployeeSchedulesRepository repo) : base(repo)
    {
        _repository = repo;
        //_dbContext = dbContext;
    }

    public Task<IEnumerable<EmployeeSchedules>> FindByEmployeeIdAsync(int id, CancellationToken ct)
    {
        return _repository.findByEmployeeID(id, ct);
    }

    public async Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeScheduler(
        EmployeeSchedules employeeSchedules,
        CancellationToken ct)
    {
        try
        {
            var maxDate = new DateOnly(9999, 12, 31);
            var currentDate = DateOnly.FromDateTime(DateTime.Now);

            // Validar cancelación temprana
            ct.ThrowIfCancellationRequested();

            // Buscar horarios del empleado
            var existingSchedules = await FindByEmployeeIdAsync(employeeSchedules.EmployeeId, ct);
            var activeSchedule = existingSchedules?.FirstOrDefault(x => x.ValidTo == maxDate);

            if (activeSchedule != null)
            {
                // Cerrar vigencia del horario actual
                activeSchedule.ValidTo = currentDate;
                activeSchedule.UpdatedAt = DateTime.Now;
                activeSchedule.UpdatedBy = employeeSchedules.CreatedBy;

                // Actualizar registro existente
                await UpdateAsync(activeSchedule.EmpScheduleId, activeSchedule, ct);
            }

            // Verificar nuevamente antes de crear
            ct.ThrowIfCancellationRequested();

            // Configurar nuevo registro
            employeeSchedules.ValidFrom = currentDate.AddDays(1);
            employeeSchedules.ValidTo = maxDate;
            employeeSchedules.CreatedAt = DateTime.Now;

            // Crear nuevo registro
            await CreateAsync(employeeSchedules, ct);

            // Retornar todos los registros actualizados
            return await FindByEmployeeIdAsync(employeeSchedules.EmployeeId, ct);
        }
        catch (OperationCanceledException ex)
        {
            throw new Exception($"La operación fue cancelada: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al actualizar horario del empleado: {ex.Message}", ex);
        }
    }
}