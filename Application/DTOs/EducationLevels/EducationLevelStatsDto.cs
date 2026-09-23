namespace WsUtaSystem.Application.DTOs.EducationLevels;

/// <summary>
/// Un renglón por docente activo (Titular + Ocasional) para el gráfico "Docentes activos por
/// grado y nivel" del Dashboard de Talento Humano (pestaña Gestión de Personal). Se manda sin
/// agrupar para que el frontend pueda filtrar por Nivel/Grado/Departamento o Facultad y
/// recalcular el conteo reactivamente, mismo patrón que el resto de tarjetas del dashboard.
/// Nivel="SIN DATO" representa a un docente activo sin ningún título académico cargado;
/// Grado=null con un Nivel real significa que el nivel sí se conoce pero el grado
/// (Doctor/Maestría/Especialista/Diplomado) no se pudo clasificar o no aplica (Tercer Nivel
/// nunca tiene Grado en SIIES).
/// </summary>
public sealed class EducationLevelStatsDto
{
    public int EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public string Nivel { get; set; } = null!;
    public string? Grado { get; set; }
}
