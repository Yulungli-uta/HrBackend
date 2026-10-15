using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Consultas compartidas sobre <c>HR.tbl_SalaryHistory</c>. La regla de "último sueldo" vive en UN solo
/// lugar (la vista <c>HR.vw_EmployeeCurrentSalary</c>) para que repositorio, reportes y vistas
/// <c>vw_EmployeeDetails*</c> nunca se desvíen entre sí.
/// </summary>
internal static class SalaryHistoryQueries
{
    /// <summary>
    /// Fila vigente de <c>SalaryHistory</c> por empleado (una por empleado). Se ordena por la VIGENCIA del
    /// documento fuente (acción: EffectiveDate; contrato: StartDate), nunca por ChangedAt (se reescribe en
    /// cada edición) ni por ID (un histórico puede cargarse después). Es componible: admite Where/ToList.
    /// </summary>
    public static IQueryable<SalaryHistory> CurrentSalaries(this AppDbContext db) =>
        db.Set<SalaryHistory>().FromSqlRaw(
            "SELECT sh.* FROM HR.tbl_SalaryHistory sh " +
            "JOIN HR.vw_EmployeeCurrentSalary cs ON cs.SalaryHistoryID = sh.SalaryHistoryID");
}
