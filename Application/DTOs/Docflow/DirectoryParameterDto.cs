namespace WsUtaSystem.Application.DTOs.Docflow
{
    public class DirectoryParameterDto
    {
        // <summary>
        /// Identificador único del parámetro.
        /// Para RefTypes: TypeId
        /// Para Departments: DepartmentId
        /// Para Employees: EmployeeId
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Código o identificador funcional del parámetro.
        /// Para RefTypes: Name
        /// Para Departments: Code
        /// Para Employees: EmployeeCode o similar
        /// </summary>
        public string Code { get; set; } = null!;

        /// <summary>
        /// Etiqueta o nombre para mostrar en la UI.
        /// Para RefTypes: Name
        /// Para Departments: Name
        /// Para Employees: FullName
        /// </summary>
        public string Label { get; set; } = null!;

        /// <summary>
        /// Descripción detallada del parámetro.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indica si el parámetro está activo.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Orden de visualización en listas.
        /// Para RefTypes: SortOrder
        /// Para Departments: 0 (no aplicable)
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// ID del parámetro padre (para jerarquías).
        /// Para Departments: ParentId
        /// Para RefTypes: null (no se usa)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Datos adicionales específicos del parámetro en formato JSON.
        /// Para RefTypes: Metadata
        /// Para Departments: null (no aplicable)
        /// </summary>
        public string? Metadata { get; set; }
    }
}
