-- ============================================================================
-- 50_personnel_action_type_subrogation_family.sql
-- Tipo de acción "Subrogación" como familia propia (padre) fuera de "Encargo de Funciones":
--   Subrogación (padre)
--     ├─ Subrogación                (SUBROGACION, id existente, conserva sus acciones y numeración SUB)
--     └─ Finalización Subrogación   (FINALIZACION_SUBROGACION, id existente, prefijo FSUB)
-- El padre copia las banderas de "Encargo de Funciones". Solo cambia ParentTypeId; ninguna acción cambia de tipo
-- salvo 7 acciones de 2019 (series SUB-A / SUBA / SUB) que estaban en el tipo general Encargo y pasan al subtipo Subrogación.
-- Idempotente.
-- ============================================================================
SET XACT_ABORT ON;
BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type WHERE Code = N'ENCARGO')
    THROW 50060, 'Falta el tipo base ENCARGO.', 1;
IF (SELECT COUNT(*) FROM HR.tbl_personnel_action_type WHERE Code IN (N'SUBROGACION', N'FINALIZACION_SUBROGACION')) <> 2
    THROW 50061, 'Faltan los subtipos SUBROGACION / FINALIZACION_SUBROGACION.', 1;

-- 1) Padre nuevo (banderas copiadas de Encargo) -------------------------------------
INSERT INTO HR.tbl_personnel_action_type
    (Name, Code, Description, NumberingPrefix, NumberingYear, NumberingLastSequence, DefaultTemplateId, IsActive, CreatedAt,
     RequiresAdUserCreation, RequiresAdUserDisable, RequiresAdGroupAssignment, ActionCategory, ReachesVigente, SiiesRelacionIesTypeId,
     ParentTypeId, NumberingFormat, NumberingResetsYearly)
SELECT N'Subrogación', N'SUBROGACION_GRUPO', N'Familia de subrogaciones (subrogación y su finalización); antes colgaba de Encargo de Funciones.',
       N'SUBG', YEAR(SYSDATETIME()), 0, f.DefaultTemplateId, 1, SYSDATETIME(),
       f.RequiresAdUserCreation, f.RequiresAdUserDisable, f.RequiresAdGroupAssignment, f.ActionCategory, f.ReachesVigente, f.SiiesRelacionIesTypeId,
       NULL, N'DTH-UTA-{PREFIX}-{SEQ}-{YEAR}', 0
  FROM HR.tbl_personnel_action_type f
 WHERE f.Code = N'ENCARGO'
   AND NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type x WHERE x.Code = N'SUBROGACION_GRUPO');

-- 2) Subtipos pasan al padre nuevo ---------------------------------------------------
UPDATE c
   SET c.ParentTypeId = p.PersonnelActionTypeId, c.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_personnel_action_type c
  JOIN HR.tbl_personnel_action_type p ON p.Code = N'SUBROGACION_GRUPO'
 WHERE c.Code IN (N'SUBROGACION', N'FINALIZACION_SUBROGACION')
   AND ISNULL(c.ParentTypeId, 0) <> p.PersonnelActionTypeId;

-- 3) Acciones de subrogación que estaban en el tipo general Encargo -------------------
UPDATE a
   SET a.ActionTypeId = s.PersonnelActionTypeId
  FROM HR.tbl_PersonnelActions a
  JOIN HR.tbl_personnel_action_type e ON e.PersonnelActionTypeId = a.ActionTypeId AND e.Code = N'ENCARGO'
  JOIN HR.tbl_personnel_action_type s ON s.Code = N'SUBROGACION'
 WHERE a.IsDeleted = 0
   AND a.ActionNumber IN (N'DTH-UTA-SUB-A-15-2019', N'DTH-UTA-SUB-A-10-2019', N'DTH-UTA-SUBA-20-2019', N'DTH-UTA-SUBA-28-2019',
                          N'UTA-DTH-SUB-95-2019', N'UTA-DTH-SUB-74-2019', N'DTH-UTA-SUB-A-42-2019');
COMMIT;
GO
