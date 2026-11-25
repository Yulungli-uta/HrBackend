/* =====================================================================
   66_employee_positions_01_regimes_and_jobs.sql
   Reconstrucción del historial de regímenes laborales y asignación de cargo/unidad.
   Uso: sqlcmd ... -v Commit=0 IncluirHistoricos=1 IncluirActivar=1 -i 66_employee_positions_01_regimes_and_jobs.sql
        Commit=0 simula (todo se revierte y se imprime el panorama); Commit=1 aplica (exige respaldo previo).
   Reglas:
   - NUNCA toca a los empleados validados por RRHH (acciones corregidas con auditoría CORRECTION, fichas editadas por una
     persona y acciones creadas/editadas por una persona). Sus documentos solo se leen.
   - Cargo de un contrato = el del tipo de contrato de su contrato principal (adendos y prórrogas heredan):
     Profesor/a ocasional (17, 51) -> 117; Técnico docente (23, 52, 41) -> 238; Técnico de laboratorio 1 (47, 53, 48) -> 247.
     Solo se asigna si el régimen del cargo coincide con el de la fila.
   - Filas nuevas de régimen: un registro por documento (número de acción o código de contrato), como RegisterDocumentAsync:
     históricas cerradas (IsActive = 0); solo queda activa la última vigente cuando el empleado no tiene activa de ese régimen.
   - Las filas cerradas no cambian el régimen actual, la tarifa ni EmployeeType; se verifica antes y después.
   - Encargos, subrogaciones y comisiones (temporales) NO crean fila de régimen: se muestran como posición temporal.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit BIT = $(Commit);
DECLARE @IncHist BIT = $(IncluirHistoricos);
DECLARE @IncActivar BIT = $(IncluirActivar);
DECLARE @hoy DATE = CAST(SYSDATETIME() AS DATE);

IF @Commit = 1 AND OBJECT_ID(N'HR.tbl_EmployeeLaborRegime_bak_20261126', N'U') IS NULL
    THROW 50001, 'Falta el respaldo (66_employee_positions_00_backup.sql).', 1;

/* ---------- 0) Empleados validados por RRHH: no se tocan ---------------------------------- */
CREATE TABLE #prot (EmployeeID INT PRIMARY KEY);
INSERT INTO #prot
SELECT DISTINCT pa.EmployeeID FROM HR.tbl_Audit a JOIN HR.tbl_PersonnelActions pa ON pa.ActionID = CAST(a.RecordID AS INT)
WHERE a.TableName = 'PersonnelActions' AND a.Action = 'CORRECTION' AND a.UserName NOT LIKE 'PRUEBA%' AND pa.EmployeeID IS NOT NULL;
INSERT INTO #prot SELECT e.EmployeeID FROM HR.tbl_Employees e
WHERE e.UpdatedBy IS NOT NULL AND e.UpdatedBy <> 4 AND NOT EXISTS (SELECT 1 FROM #prot p WHERE p.EmployeeID = e.EmployeeID);
INSERT INTO #prot SELECT DISTINCT pa.EmployeeID FROM HR.tbl_PersonnelActions pa
WHERE pa.EmployeeID IS NOT NULL AND pa.IsDeleted = 0 AND ((pa.UpdatedBy IS NOT NULL AND pa.UpdatedBy <> 4) OR (pa.CreatedBy IS NOT NULL AND pa.CreatedBy <> 4))
  AND NOT EXISTS (SELECT 1 FROM #prot p WHERE p.EmployeeID = pa.EmployeeID);

/* ---------- 1) Cadena de contratos y cargo por tipo ------------------------------------- */
CREATE TABLE #chain (start_id INT PRIMARY KEY, root_type INT);
;WITH r AS (
    SELECT c.ContractID AS start_id, c.ContractID AS cur_id, c.ParentID, c.ContractTypeID, 0 AS lvl FROM HR.tbl_Contracts c
    UNION ALL
    SELECT r.start_id, p.ContractID, p.ParentID, p.ContractTypeID, r.lvl + 1 FROM r JOIN HR.tbl_Contracts p ON p.ContractID = r.ParentID WHERE r.lvl < 6)
INSERT INTO #chain
SELECT start_id, ContractTypeID FROM (SELECT *, ROW_NUMBER() OVER (PARTITION BY start_id ORDER BY lvl DESC) rn FROM r) x WHERE rn = 1;

CREATE TABLE #tmap (typeid INT PRIMARY KEY, jobid INT);
INSERT INTO #tmap VALUES (17,117),(51,117),(23,238),(52,238),(41,238),(47,247),(53,247),(48,247);

CREATE TABLE #kc (ContractID INT PRIMARY KEY, cargo INT NULL, via VARCHAR(10) NULL, conflicto INT NOT NULL DEFAULT 0);
INSERT INTO #kc (ContractID, cargo, via, conflicto)
SELECT c.ContractID, COALESCE(m.jobid, c.JobID),
       CASE WHEN m.jobid IS NOT NULL THEN 'tipo' WHEN c.JobID IS NOT NULL THEN 'contrato' END,
       CASE WHEN m.jobid IS NOT NULL AND c.JobID IS NOT NULL AND m.jobid <> c.JobID THEN 1 ELSE 0 END
FROM HR.tbl_Contracts c LEFT JOIN #chain ch ON ch.start_id = c.ContractID LEFT JOIN #tmap m ON m.typeid = ch.root_type;

/* ---------- 2) Acciones candidatas a fila de régimen (sin protegidos) ---------------------- */
CREATE TABLE #ra (aid INT PRIMARY KEY);
INSERT INTO #ra SELECT SourcePersonnelActionId FROM HR.tbl_EmployeeLaborRegime WHERE SourcePersonnelActionId IS NOT NULL GROUP BY SourcePersonnelActionId;

SELECT pa.ActionID, pa.EmployeeID, pa.ActionNumber, ty.Name AS tipo, ty.Code AS tcode, ty.ActionCategory AS cat, pa.Status,
       CAST(ISNULL(pa.EffectiveDate, pa.ActionDate) AS DATE) AS from_d, CAST(pa.EndDate AS DATE) AS end_d,
       pa.DestinationJobId, pa.DestinationDepartmentId, pa.OriginJobId, pa.OriginDepartmentId,
       COALESCE(j.LaborRegimeID, j.JobTypeID, pa.EmployeeTypeId) AS reg_id,
       CAST(NULL AS VARCHAR(30)) AS clase, CAST(NULL AS VARCHAR(24)) AS naturaleza, CAST(NULL AS INT) AS cambia, CAST(NULL AS INT) AS es_ultima, CAST(NULL AS INT) AS tiene_activa
INTO #cand
FROM HR.tbl_PersonnelActions pa
JOIN HR.tbl_Personnel_Action_Type ty ON ty.PersonnelActionTypeID = pa.ActionTypeID
JOIN HR.tbl_Employees e ON e.EmployeeID = pa.EmployeeID AND e.IsDeleted = 0 AND ISNULL(e.IsTestData, 0) = 0
LEFT JOIN HR.tbl_jobs j ON j.JobID = pa.DestinationJobId
WHERE pa.IsDeleted = 0 AND pa.Status IN ('VIGENTE', 'FINALIZADO')
  AND (pa.DestinationJobId IS NOT NULL OR pa.DestinationDepartmentId IS NOT NULL)
  AND (ty.ActionCategory IS NULL OR ty.ActionCategory NOT IN ('LEAVE','SCHEDULE','VACATION','DISCIPLINARY','EXIT'))
  AND NOT EXISTS (SELECT 1 FROM #ra WHERE #ra.aid = pa.ActionID)
  AND pa.EmployeeID NOT IN (SELECT EmployeeID FROM #prot);

UPDATE #cand SET cambia = CASE WHEN DestinationJobId IS NOT NULL AND ISNULL(OriginJobId, -1) <> DestinationJobId THEN 1
                               WHEN DestinationDepartmentId IS NOT NULL AND ISNULL(OriginDepartmentId, -1) <> DestinationDepartmentId THEN 1 ELSE 0 END;
UPDATE #cand SET naturaleza = CASE
    WHEN cat = 'ENTRY' THEN 'A_ingreso'
    WHEN tipo LIKE 'Encargo%' OR tipo LIKE 'Subrog%' OR tipo LIKE 'Comisi%' THEN 'C_temporal'
    WHEN cat = 'SALARY_CHANGE' OR tipo LIKE 'ACTUALIZ%' OR tipo LIKE 'Promoci%' OR tipo LIKE 'Recategor%' OR tipo LIKE 'Ubicaci%' THEN 'D_ajuste_salarial'
    WHEN tipo LIKE 'Finalizaci%' THEN 'E_fin_de_funciones'
    WHEN cat = 'MOVEMENT' OR tipo LIKE 'Rotaci%' OR tipo LIKE 'Traspaso%' OR tipo LIKE 'Reintegro%' THEN 'B_movimiento'
    ELSE 'F_otros' END;
UPDATE c SET clase = CASE
    WHEN c.tcode = 'DESIGNACION' AND EXISTS (SELECT 1 FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = c.EmployeeID AND r.IsActive = 1 AND r.DocumentType = 'CONTRACT' AND r.LaborRegimeId = c.reg_id) THEN '1_designacion_concurrente'
    WHEN c.reg_id IS NULL OR c.reg_id NOT IN (57, 58, 59) THEN '2_regimen_no_derivable'
    WHEN c.naturaleza = 'C_temporal' THEN '3_temporal_no_crea_fila'
    WHEN c.naturaleza IN ('D_ajuste_salarial', 'E_fin_de_funciones') THEN '4_sin_cambio_de_posicion'
    WHEN c.cambia = 0 AND c.naturaleza <> 'A_ingreso' THEN '4_sin_cambio_de_posicion'
    ELSE '5_crear_fila' END
FROM #cand c;
UPDATE c SET es_ultima = CASE WHEN NOT EXISTS (SELECT 1 FROM #cand c2 WHERE c2.clase = '5_crear_fila' AND c2.EmployeeID = c.EmployeeID AND c2.reg_id = c.reg_id AND c2.ActionID <> c.ActionID
                                   AND (ISNULL(c2.from_d, '1900-01-01') > ISNULL(c.from_d, '1900-01-01') OR (ISNULL(c2.from_d, '1900-01-01') = ISNULL(c.from_d, '1900-01-01') AND c2.ActionID > c.ActionID)))
                              AND NOT EXISTS (SELECT 1 FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = c.EmployeeID AND r.LaborRegimeId = c.reg_id AND r.EffectiveFrom > ISNULL(c.from_d, '1900-01-01'))
                         THEN 1 ELSE 0 END,
             tiene_activa = CASE WHEN EXISTS (SELECT 1 FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = c.EmployeeID AND r.LaborRegimeId = c.reg_id AND r.IsActive = 1) THEN 1 ELSE 0 END
FROM #cand c WHERE c.clase = '5_crear_fila';

/* Filas nuevas (acciones) */
CREATE TABLE #new (
    EmployeeId INT NOT NULL, LaborRegimeId INT NOT NULL, DepartmentId INT NULL, JobId INT NULL,
    DocumentType VARCHAR(30) NOT NULL, DocumentNumber NVARCHAR(100) NULL, SourceContractId INT NULL, SourcePersonnelActionId INT NULL,
    EffectiveFrom DATE NOT NULL, EffectiveTo DATE NULL, IsActive BIT NOT NULL, tier VARCHAR(40) NOT NULL);

INSERT INTO #new (EmployeeId, LaborRegimeId, DepartmentId, JobId, DocumentType, DocumentNumber, SourcePersonnelActionId, EffectiveFrom, EffectiveTo, IsActive, tier)
SELECT c.EmployeeID, c.reg_id, c.DestinationDepartmentId, c.DestinationJobId, 'PERSONNEL_ACTION', c.ActionNumber, c.ActionID, c.from_d,
       CASE WHEN c.es_ultima = 0 THEN (SELECT CASE WHEN MIN(f) IS NULL THEN c.end_d WHEN c.end_d IS NOT NULL AND c.end_d < DATEADD(DAY, -1, MIN(f)) THEN c.end_d WHEN DATEADD(DAY, -1, MIN(f)) < c.from_d THEN c.from_d ELSE DATEADD(DAY, -1, MIN(f)) END
                                        FROM (SELECT r.EffectiveFrom f FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = c.EmployeeID AND r.LaborRegimeId = c.reg_id AND r.EffectiveFrom > c.from_d
                                              UNION ALL SELECT c2.from_d FROM #cand c2 WHERE c2.clase = '5_crear_fila' AND c2.EmployeeID = c.EmployeeID AND c2.reg_id = c.reg_id AND c2.from_d > c.from_d) u)
            ELSE c.end_d END,
       CASE WHEN c.es_ultima = 1 AND c.tiene_activa = 0 AND (c.end_d IS NULL OR c.end_d >= @hoy) AND @IncActivar = 1 THEN 1 ELSE 0 END,
       CASE WHEN c.es_ultima = 0 THEN 'accion_historica'
            WHEN c.tiene_activa = 1 THEN 'accion_ultima_con_activa_REVISAR'
            WHEN c.end_d IS NOT NULL AND c.end_d < @hoy THEN 'accion_ultima_terminada'
            ELSE 'accion_ultima_vigente' END
FROM #cand c WHERE c.clase = '5_crear_fila';
-- las "última pero ya hay activa del régimen" no se crean: se listan para RRHH
DELETE FROM #new WHERE tier = 'accion_ultima_con_activa_REVISAR';
DELETE n FROM #new n WHERE n.tier = 'accion_ultima_vigente' AND @IncActivar = 0;
-- fecha fin nunca antes del inicio
UPDATE #new SET EffectiveTo = EffectiveFrom WHERE EffectiveTo IS NOT NULL AND EffectiveTo < EffectiveFrom;

/* ---------- 3) Contratos sin fila de régimen --------------------------------------------- */
CREATE TABLE #rc (cid INT PRIMARY KEY);
INSERT INTO #rc SELECT SourceContractId FROM HR.tbl_EmployeeLaborRegime WHERE SourceContractId IS NOT NULL GROUP BY SourceContractId;
SELECT ContractTypeID, LaborRegimeID, COUNT(*) n INTO #modal FROM HR.tbl_Contracts WHERE LaborRegimeID IS NOT NULL GROUP BY ContractTypeID, LaborRegimeID;

SELECT c.ContractID, c.ContractCode, c.ContractTypeID, c.DepartmentID, CAST(c.startdate AS DATE) AS sd, CAST(c.enddate AS DATE) AS ed,
       e.EmployeeID, k.cargo,
       CASE WHEN c.LaborRegimeID IS NOT NULL THEN c.LaborRegimeID
            WHEN (SELECT COUNT(DISTINCT m.LaborRegimeID) FROM #modal m WHERE m.ContractTypeID = c.ContractTypeID) = 1 THEN (SELECT TOP 1 m.LaborRegimeID FROM #modal m WHERE m.ContractTypeID = c.ContractTypeID)
            WHEN e.EmployeeType IN (57, 58, 59) AND c.ContractTypeID = 57 THEN e.EmployeeType END AS reg_id,
       CASE WHEN c.LaborRegimeID IS NOT NULL THEN 'contrato_regimen_en_contrato'
            WHEN (SELECT COUNT(DISTINCT m.LaborRegimeID) FROM #modal m WHERE m.ContractTypeID = c.ContractTypeID) = 1 THEN 'contrato_regimen_por_tipo'
            WHEN e.EmployeeType IN (57, 58, 59) AND c.ContractTypeID = 57 THEN 'contrato_historico_ficha' END AS tier
INTO #kk
FROM HR.tbl_Contracts c
JOIN HR.tbl_Employees e ON e.PersonID = c.PersonID AND e.IsDeleted = 0 AND ISNULL(e.IsTestData, 0) = 0
LEFT JOIN #kc k ON k.ContractID = c.ContractID
WHERE NOT EXISTS (SELECT 1 FROM #rc WHERE #rc.cid = c.ContractID) AND e.EmployeeID NOT IN (SELECT EmployeeID FROM #prot);

INSERT INTO #new (EmployeeId, LaborRegimeId, DepartmentId, JobId, DocumentType, DocumentNumber, SourceContractId, EffectiveFrom, EffectiveTo, IsActive, tier)
SELECT k.EmployeeID, k.reg_id, k.DepartmentID, CASE WHEN EXISTS (SELECT 1 FROM HR.tbl_jobs j WHERE j.JobID = k.cargo AND COALESCE(j.LaborRegimeID, j.JobTypeID) = k.reg_id) THEN k.cargo END,
       'CONTRACT', k.ContractCode, k.ContractID, k.sd, k.ed, 0, k.tier
FROM #kk k WHERE k.reg_id IN (57, 58, 59) AND k.sd IS NOT NULL AND (k.tier <> 'contrato_historico_ficha' OR @IncHist = 1);
UPDATE #new SET EffectiveTo = EffectiveFrom WHERE EffectiveTo IS NOT NULL AND EffectiveTo < EffectiveFrom;

-- Los contratos nuevos se crean SIEMPRE cerrados (IsActive = 0): activar uno reetiquetaría el régimen actual (p. ej. LOSEP -> Código de Trabajo)
-- y cambiaría la tarifa; los vigentes sin fila activa se entregan a RRHH.

-- Confirmado por el usuario: el empleado 5456 (cédula 1803727229) es LOES; sus acciones de cargo LOSEP no generan fila de régimen.
DELETE FROM #new WHERE EmployeeId = 5456 AND LaborRegimeId = 57;

/* ---------- Línea base: lo que NO debe cambiar --------------------------------------------- */
DECLARE @mA TABLE (k VARCHAR(90), v INT);
INSERT INTO @mA SELECT 'siies_func_filas', COUNT(*) FROM HR.vw_SiiesFuncionarios;
INSERT INTO @mA SELECT 'siies_func_empleados', COUNT(DISTINCT EmployeeID) FROM HR.vw_SiiesFuncionarios;
INSERT INTO @mA SELECT 'siies_func_sin_cargo', COUNT(*) FROM HR.vw_SiiesFuncionarios WHERE JobDescription IS NULL;
INSERT INTO @mA SELECT 'siies_func_sin_dependencia', COUNT(*) FROM HR.vw_SiiesFuncionarios WHERE DepartmentName IS NULL;
INSERT INTO @mA SELECT 'siies_prof_filas', COUNT(*) FROM HR.vw_SiiesProfesores;
INSERT INTO @mA SELECT 'regimenes_total', COUNT(*) FROM HR.tbl_EmployeeLaborRegime;
INSERT INTO @mA SELECT 'regimenes_activos', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE IsActive = 1;
INSERT INTO @mA SELECT 'regimenes_sin_cargo', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE JobId IS NULL;
INSERT INTO @mA SELECT 'regimenes_activos_sin_cargo', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE JobId IS NULL AND IsActive = 1;
INSERT INTO @mA SELECT 'fichas_sin_cargo', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND JobID IS NULL;
INSERT INTO @mA SELECT 'fichas_sin_unidad', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND DepartmentID IS NULL;
INSERT INTO @mA SELECT 'fichas_activas_sin_cargo', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND IsActive = 1 AND ISNULL(IsTestData,0) = 0 AND JobID IS NULL;
INSERT INTO @mA SELECT 'fichas_activas_sin_unidad', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND IsActive = 1 AND ISNULL(IsTestData,0) = 0 AND DepartmentID IS NULL;

SELECT Id, EmployeeId, LaborRegimeId, IsActive, IsPrincipal INTO #regOld FROM HR.tbl_EmployeeLaborRegime;
SELECT EmployeeID, EmployeeType INTO #etOld FROM HR.tbl_Employees;
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal INTO #rateOld FROM HR.fn_ResolveEmployeeRate(@hoy, NULL);
SELECT EmployeeID, JobID, DepartmentID INTO #fichaOld FROM HR.tbl_Employees;
SELECT EmployeeID, DepartmentName, JobDescription INTO #siiesOld FROM HR.vw_SiiesFuncionarios;
SELECT EmployeeId, JobId, DepartmentId, Id INTO #regJobOld FROM HR.tbl_EmployeeLaborRegime;

DECLARE @log TABLE (paso VARCHAR(70), clave VARCHAR(90), n INT);
DECLARE @det TABLE (EmployeeId INT, LaborRegimeId INT, DepartmentId INT, JobId INT, DocumentType VARCHAR(30), DocumentNumber NVARCHAR(100), SourceContractId INT, SourcePersonnelActionId INT, EffectiveFrom DATE, EffectiveTo DATE, IsActive BIT, tier VARCHAR(40));
DECLARE @mB TABLE (k VARCHAR(90), v INT);
DECLARE @dRate TABLE (EmployeeID INT, lado VARCHAR(8), LaborRegimeID INT, RMU DECIMAL(18,2), SourceType VARCHAR(30), IsPrincipal BIT);
DECLARE @dType TABLE (EmployeeID INT, tipo_antes INT, tipo_despues INT);
DECLARE @dSiies TABLE (EmployeeID INT, IsActive BIT, motivo VARCHAR(40));

BEGIN TRAN;

/* ---------- A) Cargo y unidad en las filas de régimen existentes --------------------------- */
-- A1. Filas de contrato sin cargo: cargo por tipo de contrato (si el régimen del cargo coincide con el de la fila)
UPDATE r SET JobId = k.cargo, UpdatedAt = SYSDATETIME()
FROM HR.tbl_EmployeeLaborRegime r JOIN #kc k ON k.ContractID = r.SourceContractId JOIN HR.tbl_jobs j ON j.JobID = k.cargo
WHERE r.JobId IS NULL AND k.cargo IS NOT NULL AND COALESCE(j.LaborRegimeID, j.JobTypeID) = r.LaborRegimeId
  AND r.EmployeeId NOT IN (SELECT EmployeeID FROM #prot);
INSERT INTO @log SELECT 'A1 cargo a filas de contrato', 'filas actualizadas', @@ROWCOUNT;

-- A2. Filas de acción sin cargo: cargo de destino de su acción
UPDATE r SET JobId = pa.DestinationJobId, UpdatedAt = SYSDATETIME()
FROM HR.tbl_EmployeeLaborRegime r JOIN HR.tbl_PersonnelActions pa ON pa.ActionID = r.SourcePersonnelActionId JOIN HR.tbl_jobs j ON j.JobID = pa.DestinationJobId
WHERE r.JobId IS NULL AND pa.DestinationJobId IS NOT NULL AND COALESCE(j.LaborRegimeID, j.JobTypeID) = r.LaborRegimeId
  AND r.EmployeeId NOT IN (SELECT EmployeeID FROM #prot);
INSERT INTO @log SELECT 'A2 cargo a filas de acción', 'filas actualizadas', @@ROWCOUNT;

-- A3. Filas sin unidad: unidad de su contrato o de su acción
UPDATE r SET DepartmentId = COALESCE(c.DepartmentID, pa.DestinationDepartmentId), UpdatedAt = SYSDATETIME()
FROM HR.tbl_EmployeeLaborRegime r LEFT JOIN HR.tbl_Contracts c ON c.ContractID = r.SourceContractId LEFT JOIN HR.tbl_PersonnelActions pa ON pa.ActionID = r.SourcePersonnelActionId
WHERE r.DepartmentId IS NULL AND COALESCE(c.DepartmentID, pa.DestinationDepartmentId) IS NOT NULL AND r.EmployeeId NOT IN (SELECT EmployeeID FROM #prot);
INSERT INTO @log SELECT 'A3 unidad a filas', 'filas actualizadas', @@ROWCOUNT;

/* ---------- B) Filas nuevas de régimen ------------------------------------------------------ */
INSERT INTO HR.tbl_EmployeeLaborRegime (EmployeeId, LaborRegimeId, DepartmentId, JobId, IsIndefinite, DocumentType, DocumentNumber, SourceContractId, SourcePersonnelActionId, EffectiveFrom, EffectiveTo, IsActive, IsPrincipal, CreatedAt)
OUTPUT INSERTED.EmployeeId, INSERTED.LaborRegimeId, INSERTED.DepartmentId, INSERTED.JobId, INSERTED.DocumentType, INSERTED.DocumentNumber, INSERTED.SourceContractId, INSERTED.SourcePersonnelActionId, INSERTED.EffectiveFrom, INSERTED.EffectiveTo, INSERTED.IsActive, NULL
  INTO @det (EmployeeId, LaborRegimeId, DepartmentId, JobId, DocumentType, DocumentNumber, SourceContractId, SourcePersonnelActionId, EffectiveFrom, EffectiveTo, IsActive, tier)
SELECT EmployeeId, LaborRegimeId, DepartmentId, JobId, 0, DocumentType, DocumentNumber, SourceContractId, SourcePersonnelActionId, EffectiveFrom, EffectiveTo, IsActive, 0, SYSDATETIME() FROM #new;
UPDATE d SET tier = n.tier FROM @det d JOIN #new n ON n.EmployeeId = d.EmployeeId AND n.LaborRegimeId = d.LaborRegimeId AND n.EffectiveFrom = d.EffectiveFrom AND ISNULL(n.SourceContractId, 0) = ISNULL(d.SourceContractId, 0) AND ISNULL(n.SourcePersonnelActionId, 0) = ISNULL(d.SourcePersonnelActionId, 0);
INSERT INTO @log SELECT 'B filas nuevas', tier, COUNT(*) FROM @det GROUP BY tier;

/* ---------- C) Ficha del empleado: solo vacíos, solo no protegidos -------------------------- */
SELECT e.EmployeeID, e.IsActive,
  (SELECT TOP 1 pa.DestinationJobId FROM HR.tbl_PersonnelActions pa WHERE pa.EmployeeID = e.EmployeeID AND pa.IsDeleted = 0 AND pa.Status IN ('VIGENTE','FINALIZADO','FIRMADO_CARGADO') AND pa.DestinationJobId IS NOT NULL ORDER BY pa.EffectiveDate DESC, pa.ActionID DESC) act_job,
  (SELECT TOP 1 r.JobId FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = e.EmployeeID AND r.JobId IS NOT NULL ORDER BY r.IsActive DESC, r.IsPrincipal DESC, r.EffectiveFrom DESC, r.Id DESC) reg_job,
  (SELECT TOP 1 pa.DestinationDepartmentId FROM HR.tbl_PersonnelActions pa WHERE pa.EmployeeID = e.EmployeeID AND pa.IsDeleted = 0 AND pa.Status IN ('VIGENTE','FINALIZADO','FIRMADO_CARGADO') AND pa.DestinationDepartmentId IS NOT NULL ORDER BY pa.EffectiveDate DESC, pa.ActionID DESC) act_dep,
  (SELECT TOP 1 r.DepartmentId FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId = e.EmployeeID AND r.DepartmentId IS NOT NULL ORDER BY r.IsActive DESC, r.IsPrincipal DESC, r.EffectiveFrom DESC, r.Id DESC) reg_dep
INTO #fc FROM HR.tbl_Employees e
WHERE e.IsDeleted = 0 AND ISNULL(e.IsTestData, 0) = 0 AND (e.JobID IS NULL OR e.DepartmentID IS NULL) AND e.EmployeeID NOT IN (SELECT EmployeeID FROM #prot);

UPDATE e SET JobID = COALESCE(f.act_job, f.reg_job), UpdatedAt = SYSDATETIME()
FROM HR.tbl_Employees e JOIN #fc f ON f.EmployeeID = e.EmployeeID WHERE e.JobID IS NULL AND COALESCE(f.act_job, f.reg_job) IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_jobs jt WHERE jt.JobID = COALESCE(f.act_job, f.reg_job) AND jt.Description LIKE 'PROFESOR TITULAR%');
INSERT INTO @log SELECT 'C1 ficha cargo', CASE WHEN f.IsActive = 1 THEN 'activos' ELSE 'inactivos' END, COUNT(*) FROM #fc f JOIN HR.tbl_Employees e ON e.EmployeeID = f.EmployeeID
  JOIN #fichaOld o ON o.EmployeeID = e.EmployeeID WHERE o.JobID IS NULL AND e.JobID IS NOT NULL GROUP BY f.IsActive;
UPDATE e SET DepartmentID = COALESCE(f.act_dep, f.reg_dep), UpdatedAt = SYSDATETIME()
FROM HR.tbl_Employees e JOIN #fc f ON f.EmployeeID = e.EmployeeID WHERE e.DepartmentID IS NULL AND COALESCE(f.act_dep, f.reg_dep) IS NOT NULL;
INSERT INTO @log SELECT 'C2 ficha unidad', CASE WHEN f.IsActive = 1 THEN 'activos' ELSE 'inactivos' END, COUNT(*) FROM #fc f JOIN HR.tbl_Employees e ON e.EmployeeID = f.EmployeeID
  JOIN #fichaOld o ON o.EmployeeID = e.EmployeeID WHERE o.DepartmentID IS NULL AND e.DepartmentID IS NOT NULL GROUP BY f.IsActive;

/* ---------- Verificaciones: lo que NO debe cambiar -------------------------------------------- */
INSERT INTO @mB SELECT 'siies_func_filas', COUNT(*) FROM HR.vw_SiiesFuncionarios;
INSERT INTO @mB SELECT 'siies_func_empleados', COUNT(DISTINCT EmployeeID) FROM HR.vw_SiiesFuncionarios;
INSERT INTO @mB SELECT 'siies_func_sin_cargo', COUNT(*) FROM HR.vw_SiiesFuncionarios WHERE JobDescription IS NULL;
INSERT INTO @mB SELECT 'siies_func_sin_dependencia', COUNT(*) FROM HR.vw_SiiesFuncionarios WHERE DepartmentName IS NULL;
INSERT INTO @mB SELECT 'siies_prof_filas', COUNT(*) FROM HR.vw_SiiesProfesores;
INSERT INTO @mB SELECT 'regimenes_total', COUNT(*) FROM HR.tbl_EmployeeLaborRegime;
INSERT INTO @mB SELECT 'regimenes_activos', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE IsActive = 1;
INSERT INTO @mB SELECT 'regimenes_sin_cargo', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE JobId IS NULL;
INSERT INTO @mB SELECT 'regimenes_activos_sin_cargo', COUNT(*) FROM HR.tbl_EmployeeLaborRegime WHERE JobId IS NULL AND IsActive = 1;
INSERT INTO @mB SELECT 'fichas_sin_cargo', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND JobID IS NULL;
INSERT INTO @mB SELECT 'fichas_sin_unidad', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND DepartmentID IS NULL;
INSERT INTO @mB SELECT 'fichas_activas_sin_cargo', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND IsActive = 1 AND ISNULL(IsTestData,0) = 0 AND JobID IS NULL;
INSERT INTO @mB SELECT 'fichas_activas_sin_unidad', COUNT(*) FROM HR.tbl_Employees WHERE IsDeleted = 0 AND IsActive = 1 AND ISNULL(IsTestData,0) = 0 AND DepartmentID IS NULL;

INSERT INTO @mB SELECT 'INFO empleados con EmployeeType corregido por el disparador (aceptado)', COUNT(*) FROM HR.tbl_Employees e JOIN #etOld o ON o.EmployeeID = e.EmployeeID WHERE ISNULL(e.EmployeeType, -1) <> ISNULL(o.EmployeeType, -1);
INSERT INTO @mB SELECT 'CHK filas existentes con activo/principal distinto', COUNT(*) FROM HR.tbl_EmployeeLaborRegime r JOIN #regOld o ON o.Id = r.Id WHERE o.IsActive <> r.IsActive OR o.IsPrincipal <> r.IsPrincipal;
SELECT EmployeeID, LaborRegimeID, RMU, HourRate, SourceType, IsPrincipal INTO #rateNew FROM HR.fn_ResolveEmployeeRate(@hoy, NULL);
INSERT INTO @mB SELECT 'INFO tarifa: filas con otra etiqueta de régimen (aceptado)', (SELECT COUNT(*) FROM (SELECT * FROM #rateNew EXCEPT SELECT * FROM #rateOld) x) + (SELECT COUNT(*) FROM (SELECT * FROM #rateOld EXCEPT SELECT * FROM #rateNew) y);
INSERT INTO @mB SELECT 'CHK tarifa de nómina: empleados cuyo monto (RMU/hora) cambia', COUNT(*) FROM (SELECT ISNULL(o.EmployeeID, n.EmployeeID) e FROM (SELECT EmployeeID, SUM(RMU) rmu, SUM(HourRate) hr, COUNT(*) c FROM #rateOld GROUP BY EmployeeID) o FULL JOIN (SELECT EmployeeID, SUM(RMU) rmu, SUM(HourRate) hr, COUNT(*) c FROM #rateNew GROUP BY EmployeeID) n ON n.EmployeeID = o.EmployeeID WHERE ISNULL(o.rmu,-1) <> ISNULL(n.rmu,-1) OR ISNULL(o.hr,-1) <> ISNULL(n.hr,-1) OR ISNULL(o.c,-1) <> ISNULL(n.c,-1)) z;
INSERT INTO @mB SELECT 'CHK activos duplicados por empleado+régimen', COUNT(*) FROM (SELECT EmployeeId, LaborRegimeId FROM HR.tbl_EmployeeLaborRegime WHERE IsActive = 1 GROUP BY EmployeeId, LaborRegimeId HAVING COUNT(*) > 1) x;
INSERT INTO @mB SELECT 'CHK empleados con más de un principal', COUNT(*) FROM (SELECT EmployeeId FROM HR.tbl_EmployeeLaborRegime WHERE IsActive = 1 AND IsPrincipal = 1 GROUP BY EmployeeId HAVING COUNT(*) > 1) x;
INSERT INTO @mB SELECT 'CHK protegidos con cambios en ficha', COUNT(*) FROM HR.tbl_Employees e JOIN #fichaOld o ON o.EmployeeID = e.EmployeeID WHERE e.EmployeeID IN (SELECT EmployeeID FROM #prot) AND (ISNULL(e.JobID,-1) <> ISNULL(o.JobID,-1) OR ISNULL(e.DepartmentID,-1) <> ISNULL(o.DepartmentID,-1));
INSERT INTO @mB SELECT 'CHK protegidos con filas de régimen nuevas/cambiadas', (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId IN (SELECT EmployeeID FROM #prot) AND r.Id NOT IN (SELECT Id FROM #regJobOld)) + (SELECT COUNT(*) FROM HR.tbl_EmployeeLaborRegime r JOIN #regJobOld o ON o.Id = r.Id WHERE r.EmployeeId IN (SELECT EmployeeID FROM #prot) AND (ISNULL(r.JobId,-1) <> ISNULL(o.JobId,-1) OR ISNULL(r.DepartmentId,-1) <> ISNULL(o.DepartmentId,-1)));
INSERT INTO @mB SELECT 'SIIES: filas que cambian de cargo o dependencia', COUNT(*) FROM HR.vw_SiiesFuncionarios v JOIN #siiesOld o ON o.EmployeeID = v.EmployeeID WHERE ISNULL(v.JobDescription,'') <> ISNULL(o.JobDescription,'') OR ISNULL(v.DepartmentName,'') <> ISNULL(o.DepartmentName,'');


INSERT INTO @dRate SELECT x.EmployeeID, 'antes', x.LaborRegimeID, x.RMU, x.SourceType, x.IsPrincipal FROM (SELECT * FROM #rateOld EXCEPT SELECT * FROM #rateNew) x;
INSERT INTO @dRate SELECT y.EmployeeID, 'despues', y.LaborRegimeID, y.RMU, y.SourceType, y.IsPrincipal FROM (SELECT * FROM #rateNew EXCEPT SELECT * FROM #rateOld) y;
INSERT INTO @dType SELECT e.EmployeeID, o.EmployeeType, e.EmployeeType FROM HR.tbl_Employees e JOIN #etOld o ON o.EmployeeID = e.EmployeeID WHERE ISNULL(e.EmployeeType,-1) <> ISNULL(o.EmployeeType,-1);
INSERT INTO @dSiies SELECT o.EmployeeID, e.IsActive, CASE WHEN EXISTS (SELECT 1 FROM HR.tbl_EmployeeLaborRegime r WHERE r.EmployeeId=o.EmployeeID AND r.Id NOT IN (SELECT Id FROM #regJobOld)) THEN 'tiene fila nueva' ELSE 'sin fila nueva' END FROM #siiesOld o JOIN HR.tbl_Employees e ON e.EmployeeID=o.EmployeeID WHERE NOT EXISTS (SELECT 1 FROM HR.vw_SiiesFuncionarios v WHERE v.EmployeeID=o.EmployeeID);

/* ---------- Resultado ---------------------------------------------------------------------------- */
IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT 'APLICADO (COMMIT).';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT 'SIMULACIÓN: todo se revirtió (ROLLBACK).';
END;

PRINT '== PROTEGIDOS (no se tocan)';
SELECT COUNT(*) AS empleados_protegidos FROM #prot;
PRINT '== QUÉ SE HACE (conteos)';
SELECT paso, clave, n FROM @log ORDER BY paso, clave;
PRINT '== ACCIONES: clasificación completa de las candidatas (sin protegidos)';
SELECT clase, naturaleza, COUNT(*) acciones FROM #cand GROUP BY clase, naturaleza ORDER BY clase, naturaleza;
PRINT '== CONTRATOS sin fila: lo que se pudo derivar';
SELECT ISNULL(tier,'(régimen no derivable)') tier, COUNT(*) contratos, SUM(CASE WHEN cargo IS NOT NULL THEN 1 ELSE 0 END) con_cargo_derivado FROM #kk GROUP BY ISNULL(tier,'(régimen no derivable)') ORDER BY 1;
PRINT '== CARGO por tipo de contrato: conflictos (el contrato traía otro cargo)';
SELECT SUM(conflicto) contratos_con_cargo_distinto_al_del_tipo FROM #kc;
PRINT '== ANTES -> DESPUÉS';
SELECT a.k AS metrica, a.v AS antes, b.v AS despues, b.v - a.v AS diferencia FROM @mA a JOIN @mB b ON b.k = a.k ORDER BY a.k;
PRINT '== VERIFICACIONES (deben ser 0)';
SELECT k AS verificacion, v AS valor FROM @mB WHERE k LIKE 'CHK%' OR k LIKE 'SIIES:%' ORDER BY k;

PRINT '== DIAG tarifa: empleados con cambio (top 25)';
SELECT TOP 25 d.EmployeeID, d.lado, d.LaborRegimeID, d.RMU, d.SourceType, d.IsPrincipal FROM @dRate d ORDER BY d.EmployeeID, d.lado;
PRINT '== DIAG tipo de empleado';
SELECT * FROM @dType;
PRINT '== DIAG SIIES: empleados que dejan de salir';
SELECT IsActive, motivo, COUNT(*) n FROM @dSiies GROUP BY IsActive, motivo;
