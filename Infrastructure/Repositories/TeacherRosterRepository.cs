using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Repositories;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Listado y conteos de docentes titulares. Una sola fuente de datos para la página y el reporte, para que
/// ambos coincidan siempre.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Docente titular = estructura docente activa cuyo escalón pertenece a una categoría titular (por NOMBRE).</item>
/// <item>RMU: se lee de <c>HR.vw_EmployeeCurrentSalary</c> (libro de sueldos). Nunca de <c>TeacherStructure.Rmu</c>.</item>
/// <item>Nombramiento: <c>HR.vw_EmployeeAppointment</c> (la misma regla que usa SIIES).</item>
/// <item>Todo se lee en vivo: una corrección de acción o de sueldo se refleja en la siguiente consulta.</item>
/// </list>
/// </remarks>
public sealed class TeacherRosterRepository : ITeacherRosterRepository
{
    private readonly string _connectionString;

    /// <summary>Tipos de acción que cambian la categoría o la dedicación del docente (HR.tbl_Personnel_Action_Type.Code).</summary>
    private static readonly string[] ScaleActionCodes =
        ["PROMOCION", "RECATEGORIZACION", "UBICACION_ESCALAFONARIA", "CAMBIO_DEDICACION"];

    /// <summary>
    /// Opciones de ejecución de las consultas del listado. Son ~400 filas, pero sin límites el optimizador eligió un plan
    /// paralelo (8 hilos) que pedía ~900 MB de memoria de trabajo para ordenar: más que el máximo del servidor (~745 MB),
    /// así que la consulta quedaba en espera (RESOURCE_SEMAPHORE) indefinidamente y de paso podía dejar sin memoria a otras
    /// sesiones de la BD compartida. Un solo hilo y un tope de reserva evitan ambas cosas.
    /// </summary>
    private const string QueryOptions = "OPTION (MAXRECURSION 20, MAXDOP 1, MAX_GRANT_PERCENT = 10)";

    public TeacherRosterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Missing connection string: SqlServerConn");
    }

    public async Task<IReadOnlyList<TeacherRosterRow>> GetRowsAsync(
        TeacherRosterQuery query, bool includeSalary, int? page, int? pageSize, CancellationToken ct)
    {
        var (cte, parameters) = BuildBase(query, includeSalary, TeacherRosterIgnore.None);

        var sql = new StringBuilder(cte);
        sql.Append("""

            SELECT r.*, COUNT(*) OVER() AS TotalCount
            FROM TRows r
            ORDER BY r.FullName, r.TeacherStructureId
            """);

        if (page.HasValue && pageSize.HasValue)
        {
            sql.Append(" OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
            parameters.Add("skip", (page.Value - 1) * pageSize.Value);
            parameters.Add("take", pageSize.Value);
        }
        sql.Append(" ").Append(QueryOptions);

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<TeacherRosterRow>(new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<TeacherRosterGroupRow>> GetGroupsAsync(
        TeacherRosterQuery query, TeacherRosterIgnore ignore, CancellationToken ct)
    {
        // Cada grupo de tarjetas cuenta con todos los filtros activos menos el suyo propio (ver TeacherRosterIgnore):
        // así elegir "Medio Tiempo" no vacía las demás dedicaciones y se pueden comparar.
        var (cte, parameters) = BuildBase(query, includeSalary: false, ignore);

        var sql = cte + """

            SELECT r.CategoryTypeId, r.CategoryName, r.LevelTypeId, r.LevelName, r.DedicationTypeId, r.DedicationName,
                   r.EmployeeIsActive, COUNT(*) AS Total,
                   SUM(r.PendingScaleFlag) AS PendingScale, SUM(r.LadderMismatchFlag) AS Mismatch
            FROM TRows r
            GROUP BY r.CategoryTypeId, r.CategoryName, r.LevelTypeId, r.LevelName, r.DedicationTypeId, r.DedicationName, r.EmployeeIsActive
            """ + " " + QueryOptions;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<TeacherRosterGroupRow>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountActiveWithScaleActionWithoutStructureAsync(CancellationToken ct)
    {
        var parameters = new DynamicParameters();
        for (var i = 0; i < ScaleActionCodes.Length; i++) parameters.Add($"sc{i}", ScaleActionCodes[i]);
        var codeList = string.Join(", ", ScaleActionCodes.Select((_, i) => $"@sc{i}"));

        var sql = $"""
            SELECT COUNT(*)
            FROM HR.tbl_Employees e
            WHERE e.IsActive = 1
              AND NOT EXISTS (SELECT 1 FROM HR.tbl_TeacherStructure t WHERE t.EmployeeID = e.EmployeeID AND t.IsActive = 1)
              AND EXISTS (
                    SELECT 1
                    FROM HR.tbl_PersonnelActions pa
                    JOIN HR.tbl_Personnel_Action_Type ty ON ty.PersonnelActionTypeID = pa.ActionTypeID
                    WHERE pa.EmployeeID = e.EmployeeID AND pa.IsDeleted = 0
                      AND ty.Code IN ({codeList})
                      AND pa.Status IN ('FIRMADO_CARGADO', 'VIGENTE', 'FINALIZADO'))
            """;

        await using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    /// <summary>
    /// Arma el CTE <c>Rows</c> (una fila por docente titular con estructura activa) con los filtros aplicados.
    /// Solo se concatenan fragmentos fijos; todo valor del usuario viaja como parámetro.
    /// </summary>
    private static (string Cte, DynamicParameters Parameters) BuildBase(
        TeacherRosterQuery q, bool includeSalary, TeacherRosterIgnore ignore)
    {
        var p = new DynamicParameters();
        p.Add("includeSalary", includeSalary ? 1 : 0);

        var titularNames = AcademicCategories.TitularNames.ToArray();
        for (var i = 0; i < titularNames.Length; i++) p.Add($"tit{i}", titularNames[i]);
        var titularList = string.Join(", ", titularNames.Select((_, i) => $"@tit{i}"));

        for (var i = 0; i < ScaleActionCodes.Length; i++) p.Add($"sc{i}", ScaleActionCodes[i]);
        var scaleList = string.Join(", ", ScaleActionCodes.Select((_, i) => $"@sc{i}"));

        var where = new StringBuilder();

        if (q.EmployeeIsActive.HasValue)
        {
            where.Append(" AND e.IsActive = @empActive");
            p.Add("empActive", q.EmployeeIsActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Append(" AND (pe.IDCard LIKE @search OR CONCAT(pe.LastName, ' ', pe.FirstName) LIKE @search)");
            p.Add("search", $"%{EscapeLike(q.Search.Trim())}%");
        }
        if (q.DepartmentId.HasValue)
        {
            // Up contiene la dependencia de la estructura y todos sus ancestros: filtrar por un ancestro incluye a sus hijas.
            where.Append(" AND EXISTS (SELECT 1 FROM Up x WHERE x.Sid = t.TeacherStructureId AND x.Did = @dept)");
            p.Add("dept", q.DepartmentId.Value);
        }
        if (!ignore.HasFlag(TeacherRosterIgnore.CategoryAndLevel))
        {
            if (q.CategoryTypeId.HasValue)
            {
                where.Append(" AND l.CategoryTypeId = @cat");
                p.Add("cat", q.CategoryTypeId.Value);
            }
            if (q.LevelTypeId.HasValue)
            {
                where.Append(" AND l.LevelTypeId = @lvl");
                p.Add("lvl", q.LevelTypeId.Value);
            }
        }
        if (!ignore.HasFlag(TeacherRosterIgnore.Dedication) && q.DedicationTypeId.HasValue)
        {
            where.Append(" AND t.DedicationTypeID = @ded");
            p.Add("ded", q.DedicationTypeId.Value);
        }
        if (q.PendingScaleAction.HasValue)
        {
            where.Append(q.PendingScaleAction.Value
                ? " AND CAST(la.EffectiveDate AS date) > CAST(t.StartDate AS date)"
                : " AND (la.EffectiveDate IS NULL OR CAST(la.EffectiveDate AS date) <= CAST(t.StartDate AS date))");
        }
        if (q.LadderMismatch.HasValue)
        {
            where.Append(q.LadderMismatch.Value ? " AND " + MismatchExpr + " = 1" : " AND " + MismatchExpr + " = 0");
        }

        var cte = $"""
            WITH Up AS (
                SELECT t.TeacherStructureId AS Sid, d.DepartmentID AS Did, d.ParentID AS Pid, d.Name AS Nm, d.DepartmentType AS Ty, 0 AS Lvl
                FROM HR.tbl_TeacherStructure t
                JOIN HR.tbl_Departments d ON d.DepartmentID = t.DepartmentId
                WHERE t.IsActive = 1
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
            TRows AS (
                SELECT
                    t.TeacherStructureId,
                    t.EmployeeID AS EmployeeId,
                    LTRIM(RTRIM(CONCAT(pe.LastName, ' ', pe.FirstName))) AS FullName,
                    pe.IDCard AS IdCard,
                    e.IsActive AS EmployeeIsActive,
                    t.DepartmentId,
                    d.Name AS DepartmentName,
                    f.FacultyName,
                    t.LadderId,
                    l.Name AS LadderName,
                    l.CategoryTypeId,
                    cat.Name AS CategoryName,
                    l.LevelTypeId,
                    lv.Name AS LevelName,
                    t.DedicationTypeID AS DedicationTypeId,
                    ded.Name AS DedicationName,
                    t.WeeklyClassHours,
                    t.StartDate AS StructureStartDate,
                    ap.AppointmentDate,
                    ap.AppointmentKind,
                    ap.AppointmentActionNumber,
                    CASE WHEN @includeSalary = 1 THEN cs.NewSalary END AS Rmu,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL THEN
                        CASE WHEN spa.ActionID IS NOT NULL THEN CONCAT(sty.Name, ' ', spa.ActionNumber)
                             ELSE CONCAT('Contrato #', sh.ContractID) END
                    END AS RmuSource,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL THEN cs.InForce END AS RmuInForce,
                    CASE WHEN @includeSalary = 1 AND cs.EmployeeID IS NOT NULL
                         THEN CASE WHEN cs.IsFallback = 1 OR cs.IsAmbiguous = 1 THEN 1 ELSE 0 END END AS RmuNeedsReview,
                    CASE WHEN CAST(la.EffectiveDate AS date) > CAST(t.StartDate AS date) THEN la.ActionNumber END AS PendingScaleActionNumber,
                    CASE WHEN CAST(la.EffectiveDate AS date) > CAST(t.StartDate AS date) THEN la.TypeName END AS PendingScaleActionType,
                    CASE WHEN CAST(la.EffectiveDate AS date) > CAST(t.StartDate AS date) THEN la.EffectiveDate END AS PendingScaleActionDate,
                    CASE WHEN CAST(la.EffectiveDate AS date) > CAST(t.StartDate AS date) THEN 1 ELSE 0 END AS PendingScaleFlag,
                    {MismatchExpr} AS LadderMismatchFlag
                FROM HR.tbl_TeacherStructure t
                JOIN HR.tbl_Employees e ON e.EmployeeID = t.EmployeeID
                JOIN HR.tbl_People pe ON pe.PersonID = e.PersonID
                JOIN HR.tbl_AcademicLadder l ON l.LadderId = t.LadderId
                JOIN HR.ref_Types cat ON cat.TypeId = l.CategoryTypeId AND cat.Name IN ({titularList})
                LEFT JOIN HR.ref_Types lv ON lv.TypeId = l.LevelTypeId
                LEFT JOIN HR.ref_Types ded ON ded.TypeId = t.DedicationTypeID
                LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = t.DepartmentId
                LEFT JOIN Fac f ON f.Sid = t.TeacherStructureId AND f.rn = 1
                LEFT JOIN HR.tbl_jobs j ON j.JobID = e.JobID
                LEFT JOIN HR.tbl_AcademicLadder jl ON jl.LadderId = j.AcademicLadderId
                LEFT JOIN HR.vw_EmployeeAppointment ap ON ap.EmployeeID = t.EmployeeID
                LEFT JOIN HR.vw_EmployeeCurrentSalary cs ON cs.EmployeeID = t.EmployeeID AND @includeSalary = 1
                LEFT JOIN HR.tbl_SalaryHistory sh ON sh.SalaryHistoryID = cs.SalaryHistoryID
                LEFT JOIN HR.tbl_PersonnelActions spa ON spa.ActionID = sh.ActionID
                LEFT JOIN HR.tbl_Personnel_Action_Type sty ON sty.PersonnelActionTypeID = spa.ActionTypeID
                OUTER APPLY (
                    SELECT TOP 1 pa.ActionNumber, pa.EffectiveDate, ty.Name AS TypeName
                    FROM HR.tbl_PersonnelActions pa
                    JOIN HR.tbl_Personnel_Action_Type ty ON ty.PersonnelActionTypeID = pa.ActionTypeID
                    WHERE pa.EmployeeID = t.EmployeeID AND pa.IsDeleted = 0
                      AND ty.Code IN ({scaleList})
                      AND pa.Status IN ('FIRMADO_CARGADO', 'VIGENTE', 'FINALIZADO')
                    ORDER BY pa.EffectiveDate DESC, pa.ActionID DESC
                ) la
                WHERE t.IsActive = 1{where}
            )
            """;

        return (cte, p);
    }

    /// <summary>Escalón del cargo distinto (categoría o nivel) al de la estructura; 0 si el cargo no tiene escalón.</summary>
    private const string MismatchExpr =
        "(CASE WHEN jl.LadderId IS NOT NULL AND (jl.CategoryTypeId <> l.CategoryTypeId OR ISNULL(jl.LevelTypeId, 0) <> ISNULL(l.LevelTypeId, 0)) THEN 1 ELSE 0 END)";

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
