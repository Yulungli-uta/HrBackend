using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Repositories;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Listado y conteos de docentes NO titulares = docentes con contrato ocasional (la misma regla de SIIES,
/// <c>HR.fn_SiiesDocenteOcasionalContract</c>, que excluye anulados y técnicos docentes).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Una fila por empleado, con su contrato ocasional más reciente.</item>
/// <item>Población: contrato VIGENTE (cualquier empleado) + contrato VENCIDO solo si el empleado sigue activo
/// (los vencidos de ex-empleados son historia y no se listan).</item>
/// <item>Dedicación: sale de la modalidad del contrato (Día completo = TC, Medio día = MT, Horas = HORAS) y se
/// expresa con los mismos tipos <c>ACADEMIC_DEDICATION</c> que usan los titulares, resueltos por NOMBRE.</item>
/// <item>RMU: <c>HR.vw_EmployeeCurrentSalary</c> (libro de sueldos), solo si se pidió (permiso verificado arriba).</item>
/// </list>
/// </remarks>
public sealed class NonTitularRosterRepository : INonTitularRosterRepository
{
    private readonly string _connectionString;

    /// <summary>Mismos topes de ejecución que el listado de titulares (BD compartida con memoria de trabajo limitada).</summary>
    private const string QueryOptions = "OPTION (MAXRECURSION 20, MAXDOP 1, MAX_GRANT_PERCENT = 10)";

    /// <summary>Tipo de contrato ocasional de la cadena contrato → padre → abuelo (los adendums heredan el del contrato base).</summary>
    private const string ContractTypeIdExpr =
        "CASE WHEN ISNULL(ctB.IsDocenteOcasional, 0) = 1 THEN ctB.ContractTypeID " +
        "WHEN ISNULL(ctP1.IsDocenteOcasional, 0) = 1 THEN ctP1.ContractTypeID " +
        "WHEN ISNULL(ctP2.IsDocenteOcasional, 0) = 1 THEN ctP2.ContractTypeID END";

    private const string ContractTypeNameExpr =
        "CASE WHEN ISNULL(ctB.IsDocenteOcasional, 0) = 1 THEN ctB.name " +
        "WHEN ISNULL(ctP1.IsDocenteOcasional, 0) = 1 THEN ctP1.name " +
        "WHEN ISNULL(ctP2.IsDocenteOcasional, 0) = 1 THEN ctP2.name END";

    public NonTitularRosterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Missing connection string: SqlServerConn");
    }

    public async Task<IReadOnlyList<NonTitularRosterRow>> GetRowsAsync(
        NonTitularRosterQuery query, bool includeSalary, int? page, int? pageSize, CancellationToken ct)
    {
        var (cte, parameters) = BuildBase(query, includeSalary, NonTitularRosterIgnore.None);

        var sql = new StringBuilder(cte);
        sql.Append("""

            SELECT r.*, COUNT(*) OVER() AS TotalCount
            FROM NRows r
            ORDER BY r.FullName, r.EmployeeId
            """);

        if (page.HasValue && pageSize.HasValue)
        {
            sql.Append(" OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
            parameters.Add("skip", (page.Value - 1) * pageSize.Value);
            parameters.Add("take", pageSize.Value);
        }
        sql.Append(" ").Append(QueryOptions);

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<NonTitularRosterRow>(new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<NonTitularRosterGroupRow>> GetGroupsAsync(
        NonTitularRosterQuery query, NonTitularRosterIgnore ignore, CancellationToken ct)
    {
        var (cte, parameters) = BuildBase(query, includeSalary: false, ignore);

        var sql = cte + """

            SELECT r.DedicationTypeId, r.DedicationName, r.ContractTypeId, r.ContractTypeName, r.ContractState, COUNT(*) AS Total
            FROM NRows r
            GROUP BY r.DedicationTypeId, r.DedicationName, r.ContractTypeId, r.ContractTypeName, r.ContractState
            """ + " " + QueryOptions;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<NonTitularRosterGroupRow>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Arma el CTE <c>NRows</c> con los filtros aplicados. Solo se concatenan fragmentos fijos;
    /// todo valor del usuario viaja como parámetro.
    /// </summary>
    private static (string Cte, DynamicParameters Parameters) BuildBase(
        NonTitularRosterQuery q, bool includeSalary, NonTitularRosterIgnore ignore)
    {
        var p = new DynamicParameters();
        p.Add("includeSalary", includeSalary ? 1 : 0);

        var where = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(q.ContractState))
        {
            where.Append(" AND s.ContractState = @state");
            p.Add("state", q.ContractState);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Append(" AND (pe.IDCard LIKE @search OR CONCAT(pe.LastName, ' ', pe.FirstName) LIKE @search)");
            p.Add("search", $"%{EscapeLike(q.Search.Trim())}%");
        }
        if (q.DepartmentId.HasValue)
        {
            // Up contiene la dependencia del contrato y todos sus ancestros: filtrar por un ancestro incluye a sus hijas.
            where.Append(" AND EXISTS (SELECT 1 FROM Up x WHERE x.Sid = s.EmployeeID AND x.Did = @dept)");
            p.Add("dept", q.DepartmentId.Value);
        }
        if (!ignore.HasFlag(NonTitularRosterIgnore.Dedication) && q.DedicationTypeId.HasValue)
        {
            where.Append(" AND ded.TypeId = @ded");
            p.Add("ded", q.DedicationTypeId.Value);
        }
        if (!ignore.HasFlag(NonTitularRosterIgnore.ContractType) && q.ContractTypeId.HasValue)
        {
            where.Append(" AND (").Append(ContractTypeIdExpr).Append(") = @ctype");
            p.Add("ctype", q.ContractTypeId.Value);
        }

        var cte = $"""
            DECLARE @hoy date = CAST(SYSDATETIME() AS date);
            WITH Pe AS (
                SELECT e.EmployeeID, e.PersonID, e.IsActive AS EmployeeIsActive, e.JobID AS EmpJobID,
                       COALESCE(f.DepartmentID, e.DepartmentID) AS DeptId,
                       f.ParentID, f.ContractTypeID, f.startdate, f.enddate, f.WorkModalityID, f.ContractedHours,
                       f.ContractCode, f.Status AS ContractStatus
                FROM HR.tbl_Employees e
                CROSS APPLY HR.fn_SiiesDocenteOcasionalContract(e.PersonID) f
                WHERE e.IsDeleted = 0
            ),
            St AS (
                SELECT x.*,
                       CASE WHEN x.enddate IS NOT NULL AND x.enddate < @hoy THEN 'VENCIDO' ELSE 'VIGENTE' END AS ContractState
                FROM Pe x
                LEFT JOIN HR.ref_Types cs ON cs.TypeId = x.ContractStatus AND cs.Category = 'CONTRACT_STATUS'
                WHERE ISNULL(cs.Name, '') <> 'ANULADO'
                  AND x.startdate <= @hoy
                  AND (x.enddate IS NULL OR x.enddate >= @hoy OR x.EmployeeIsActive = 1)
            ),
            Up AS (
                SELECT s.EmployeeID AS Sid, d.DepartmentID AS Did, d.ParentID AS Pid, d.Name AS Nm, d.DepartmentType AS Ty, 0 AS Lvl
                FROM St s
                JOIN HR.tbl_Departments d ON d.DepartmentID = s.DeptId
                UNION ALL
                SELECT u.Sid, pd.DepartmentID, pd.ParentID, pd.Name, pd.DepartmentType, u.Lvl + 1
                FROM Up u
                JOIN HR.tbl_Departments pd ON pd.DepartmentID = u.Pid
                WHERE u.Lvl < 8
            ),
            Fac AS (
                SELECT u.Sid, u.Nm AS FacultyName, ROW_NUMBER() OVER (PARTITION BY u.Sid ORDER BY u.Lvl) AS rn
                FROM Up u
                JOIN HR.ref_Types rt ON rt.TypeId = u.Ty AND rt.Name = 'FACULTAD'
            ),
            NRows AS (
                SELECT
                    s.EmployeeID AS EmployeeId,
                    LTRIM(RTRIM(CONCAT(pe.LastName, ' ', pe.FirstName))) AS FullName,
                    pe.IDCard AS IdCard,
                    s.EmployeeIsActive,
                    s.DeptId AS DepartmentId,
                    d.Name AS DepartmentName,
                    f.FacultyName,
                    j.Description AS JobName,
                    ded.TypeId AS DedicationTypeId,
                    ded.Name AS DedicationName,
                    s.ContractedHours,
                    {ContractTypeIdExpr} AS ContractTypeId,
                    {ContractTypeNameExpr} AS ContractTypeName,
                    s.ContractCode,
                    s.startdate AS ContractStartDate,
                    s.enddate AS ContractEndDate,
                    s.ContractState,
                    CASE WHEN @includeSalary = 1 THEN cs.NewSalary END AS Rmu,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL THEN
                        CASE WHEN spa.ActionID IS NOT NULL THEN CONCAT(sty.Name, ' ', spa.ActionNumber)
                             ELSE CONCAT('Contrato #', sh.ContractID) END
                    END AS RmuSource,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL THEN cs.InForce END AS RmuInForce,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL
                         THEN CASE WHEN cs.IsFallback = 1 OR cs.IsAmbiguous = 1 THEN 1 ELSE 0 END END AS RmuNeedsReview
                FROM St s
                JOIN HR.tbl_People pe ON pe.PersonID = s.PersonID
                LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = s.DeptId
                LEFT JOIN Fac f ON f.Sid = s.EmployeeID AND f.rn = 1
                LEFT JOIN HR.tbl_jobs j ON j.JobID = s.EmpJobID
                LEFT JOIN HR.ref_Types wm ON wm.TypeId = s.WorkModalityID
                LEFT JOIN HR.ref_Types ded ON ded.Category = 'ACADEMIC_DEDICATION' AND ded.Name =
                    CASE WHEN LTRIM(RTRIM(wm.Name)) LIKE 'D_a Completo%' THEN 'TC'
                         WHEN LTRIM(RTRIM(wm.Name)) LIKE 'Medio%' THEN 'MT'
                         WHEN LTRIM(RTRIM(wm.Name)) LIKE 'Horas%' THEN 'HORAS' END
                LEFT JOIN HR.tbl_Contracts p1 ON p1.ContractID = s.ParentID
                LEFT JOIN HR.tbl_Contracts p2 ON p2.ContractID = p1.ParentID
                LEFT JOIN HR.tbl_contract_type ctB ON ctB.ContractTypeID = s.ContractTypeID
                LEFT JOIN HR.tbl_contract_type ctP1 ON ctP1.ContractTypeID = p1.ContractTypeID
                LEFT JOIN HR.tbl_contract_type ctP2 ON ctP2.ContractTypeID = p2.ContractTypeID
                LEFT JOIN HR.vw_EmployeeCurrentSalary cs ON cs.EmployeeID = s.EmployeeID AND @includeSalary = 1
                LEFT JOIN HR.tbl_SalaryHistory sh ON sh.SalaryHistoryID = cs.SalaryHistoryID
                LEFT JOIN HR.tbl_PersonnelActions spa ON spa.ActionID = sh.ActionID
                LEFT JOIN HR.tbl_Personnel_Action_Type sty ON sty.PersonnelActionTypeID = spa.ActionTypeID
                WHERE 1 = 1{where}
            )
            """;

        return (cte, p);
    }

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
