-- ============================================================================
-- 42_personnel_action_type_finalizacion_funciones.sql
-- Subtipo "Finalización de funciones" bajo "Otros" (prefijos FF, FFA y FFB del legado 12.3).
-- Copia las banderas (categoría, vigencia, AD, plantilla, SIIES) de "Otros". Semilla de numeración = último número emitido
-- (FF-710-2026 en HR y en el legado); re-sincronizar con el legado en el corte final.
-- No toca acciones: el agrupamiento de las acciones FF/FFA/FFB lo hace apply_types_regroup.py (solo cambia ActionTypeID).
-- Idempotente (se identifica por Code).
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO HR.tbl_personnel_action_type
    (Name, Code, Description, NumberingPrefix, NumberingYear, NumberingLastSequence, DefaultTemplateId, IsActive, CreatedAt,
     RequiresAdUserCreation, RequiresAdUserDisable, RequiresAdGroupAssignment, ActionCategory, ReachesVigente, SiiesRelacionIesTypeId,
     ParentTypeId, NumberingFormat, NumberingResetsYearly)
SELECT N'Finalización de funciones', N'FINALIZACION_FUNCIONES', N'Series FF, FFA y FFB del legado.', N'FF',
       YEAR(SYSDATETIME()), 710, p.DefaultTemplateId, 1, SYSDATETIME(),
       p.RequiresAdUserCreation, p.RequiresAdUserDisable, p.RequiresAdGroupAssignment, p.ActionCategory, p.ReachesVigente, p.SiiesRelacionIesTypeId,
       p.PersonnelActionTypeId, N'DTH-UTA-{PREFIX}-{SEQ}-{YEAR}', 0
FROM HR.tbl_personnel_action_type p
WHERE p.Code = N'OTROS'
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type x WHERE x.Code = N'FINALIZACION_FUNCIONES');
GO
