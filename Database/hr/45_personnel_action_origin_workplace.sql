-- ============================================================================
-- 45_personnel_action_origin_workplace.sql
-- Lugar de trabajo de la situación de ORIGEN de la Acción de Personal.
-- Hasta ahora solo se guardaba el lugar PROPUESTO (HR.tbl_PersonnelActions.Workplace); el "actual" se derivaba de la acción
-- anterior del empleado y no se podía registrar ni corregir. Mismo patrón que OriginInstitutionalProcess / OriginManagementLevel:
-- columna opcional hacia ref_Types (Category = 'AP_LUGAR_TRABAJO'). Idempotente.
-- ============================================================================

SET NOCOUNT ON;
GO

IF COL_LENGTH('HR.tbl_PersonnelActions', 'OriginWorkplace') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD OriginWorkplace INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_OriginWorkplace')
    ALTER TABLE HR.tbl_PersonnelActions
        ADD CONSTRAINT FK_PersonnelActions_OriginWorkplace FOREIGN KEY (OriginWorkplace) REFERENCES HR.ref_Types (TypeID);
GO
