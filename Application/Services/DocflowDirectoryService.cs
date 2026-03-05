using AutoMapper;
using System;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services
{
    /// <summary>
    /// Implementación del servicio de directorios Docflow.
    /// Orquesta llamadas a servicios existentes para poblar catálogos.
    /// </summary>
    public sealed class DocflowDirectoryService : IDocflowDirectoryService
    {
        private readonly IRefTypesService _refTypesService;
        private readonly IDepartmentsService _departmentsService;
        private readonly IEmployeesService _employeesService;
        private readonly IMapper _mapper;
        private readonly ILogger<DocflowDirectoryService> _logger;

        public DocflowDirectoryService(
            IRefTypesService refTypesService,
            IDepartmentsService departmentsService,
            IEmployeesService employeesService,
            IMapper mapper,
            ILogger<DocflowDirectoryService> logger)
        {
            _refTypesService = refTypesService ?? throw new ArgumentNullException(nameof(refTypesService));
            _departmentsService = departmentsService ?? throw new ArgumentNullException(nameof(departmentsService));
            _employeesService = employeesService ?? throw new ArgumentNullException(nameof(employeesService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene los tipos de movimiento disponibles.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetDocflowTypesByCatagoryAsync(String paCategory, CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo tipos de Docflow");
            //var category = paCategory.ToUpper();
            var types = await _refTypesService.GetByCategoryAsync(paCategory.ToUpper(), ct);

            if (!types.Any())
            {
                _logger.LogWarning($"No se encontraron tipos de Docflow con categoria {paCategory} configurados");
                return new List<DirectoryParameterDto>();
            }

            return types
                .OrderBy(t => t.GetType().GetProperty("SortOrder")?.GetValue(t) ?? 0)
                .Select(MapRefTypeToDirectoryParameter)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene los estados posibles de un expediente desde RefTypes con categoría DOCFLOW_INSTANCE_STATUSES.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetInstanceStatusesAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo estados de expediente");

            var statuses = await _refTypesService.GetByCategoryAsync("DOCFLOW_INSTANCE_STATUSES", ct);

            if (!statuses.Any())
            {
                _logger.LogWarning("No se encontraron estados de expediente configurados");
                return new List<DirectoryParameterDto>();
            }

            return statuses
                .OrderBy(s => s.GetType().GetProperty("SortOrder")?.GetValue(s) ?? 0)
                .Select(MapRefTypeToDirectoryParameter)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene los tipos de movimiento disponibles.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetMovementTypesAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo tipos de movimiento");

            var types = await _refTypesService.GetByCategoryAsync("DOCFLOW_MOVEMENT_TYPES", ct);

            if (!types.Any())
            {
                _logger.LogWarning("No se encontraron tipos de movimiento configurados");
                return new List<DirectoryParameterDto>();
            }

            return types
                .OrderBy(t => t.GetType().GetProperty("SortOrder")?.GetValue(t) ?? 0)
                .Select(MapRefTypeToDirectoryParameter)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene los niveles de prioridad disponibles.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetPrioritiesAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo prioridades");

            var priorities = await _refTypesService.GetByCategoryAsync("DOCFLOW_PRIORITIES", ct);

            if (!priorities.Any())
            {
                _logger.LogWarning("No se encontraron prioridades configuradas");
                return new List<DirectoryParameterDto>();
            }

            return priorities
                .OrderBy(p => p.GetType().GetProperty("SortOrder")?.GetValue(p) ?? 0)
                .Select(MapRefTypeToDirectoryParameter)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene las áreas/departamentos disponibles con su jerarquía.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetAreasAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo áreas/departamentos");

            var departments = await _departmentsService.GetAllAsync(ct);

            if (!departments.Any())
            {
                _logger.LogWarning("No se encontraron departamentos configurados");
                return new List<DirectoryParameterDto>();
            }

            return departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.ParentId)
                .ThenBy(d => d.Name)
                .Select(d => new DirectoryParameterDto
                {
                    Id = d.DepartmentId,
                    Code = d.Code,
                    Label = d.Name,
                    Description = d.ShortName,
                    IsActive = d.IsActive,
                    ParentId = d.ParentId
                })
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene los usuarios (empleados) disponibles en el sistema.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo usuarios");

            var employees = await _employeesService.GetAllAsync(ct);

            if (!employees.Any())
            {
                _logger.LogWarning("No se encontraron empleados disponibles");
                return new List<DirectoryParameterDto>();
            }

            return employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeId)
                .Select(e => new DirectoryParameterDto
                {
                    Id = e.EmployeeId,
                    Code = e.EmployeeId.ToString(),
                    Label = $"Empleado {e.EmployeeId}", // Usar ID como fallback
                    Description = e.Email ?? "Sin email",
                    IsActive = e.IsActive
                })
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Obtiene un catálogo genérico por tipo.
        /// Delega a los métodos específicos según el tipo solicitado.
        /// </summary>
        public async Task<IReadOnlyList<DirectoryParameterDto>> GetDirectoryByTypeAsync(string type, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("El tipo de catálogo no puede estar vacío", nameof(type));

            _logger.LogInformation("Obteniendo directorio de tipo: {Type}", type);

            return type.ToLower() switch
            {
                "document_type" => await GetDocflowTypesByCatagoryAsync(type,ct),
                "instance_statuses" => await GetInstanceStatusesAsync(ct),
                "movement_types" => await GetMovementTypesAsync(ct),
                "priorities" => await GetPrioritiesAsync(ct),
                "areas" => await GetAreasAsync(ct),
                "users" => await GetUsersAsync(ct),
                _ => throw new ArgumentException($"Tipo de catálogo '{type}' no válido", nameof(type))
            };
        }

        /// <summary>
        /// Mapea un RefType a DirectoryParameterDto.
        /// Método privado para reutilización.
        /// </summary>
        private static DirectoryParameterDto MapRefTypeToDirectoryParameter(dynamic refType)
        {
            return new DirectoryParameterDto
            {
                Id = refType.TypeId,
                Code = refType.Name,
                Label = refType.Name,
                Description = refType.Description,
                IsActive = refType.IsActive,
                SortOrder = (int?)refType.GetType().GetProperty("SortOrder")?.GetValue(refType) ?? 0
            };
        }
    }
}
