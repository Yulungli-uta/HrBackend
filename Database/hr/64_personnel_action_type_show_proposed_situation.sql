-- 64_personnel_action_type_show_proposed_situation.sql
-- Campo configurable "Mostrar situación propuesta" en el tipo de acción de personal.
--   true  = el documento imprime la grilla completa de SITUACIÓN PROPUESTA (dependencia, cargo, sueldo, ...)
--   false = el documento imprime solo el nombre de la acción
-- Es independiente de ActionCategory: la categoría sigue decidiendo movimientos, historial laboral y SIIES.
-- Valor inicial: igual al comportamiento actual (ENTRY / MOVEMENT / SALARY_CHANGE = true; el resto = false),
-- salvo la familia Subrogación, que sigue siendo MOVEMENT pero imprime solo su nombre.
-- Idempotente: se puede ejecutar más de una vez.

IF COL_LENGTH('HR.tbl_personnel_action_type', 'ShowProposedSituation') IS NULL
BEGIN
    ALTER TABLE HR.tbl_personnel_action_type
        ADD ShowProposedSituation BIT NOT NULL
            CONSTRAINT DF_personnel_action_type_ShowProposedSituation DEFAULT (1);
END
GO

-- Valor inicial según la categoría actual (solo la primera vez: si ya hay valores distintos de 1 no se pisan)
IF NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type WHERE ShowProposedSituation = 0)
BEGIN
    UPDATE HR.tbl_personnel_action_type
       SET ShowProposedSituation = CASE WHEN ActionCategory IN (N'ENTRY', N'MOVEMENT', N'SALARY_CHANGE') THEN 1 ELSE 0 END;

    -- Subrogación (MOVEMENT) imprime solo el nombre de la acción
    UPDATE HR.tbl_personnel_action_type
       SET ShowProposedSituation = 0
     WHERE Code IN (N'SUBROGACION', N'FINALIZACION_SUBROGACION', N'SUBROGACION_GRUPO');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
                WHERE major_id = OBJECT_ID('HR.tbl_personnel_action_type')
                  AND minor_id = COLUMNPROPERTY(OBJECT_ID('HR.tbl_personnel_action_type'), 'ShowProposedSituation', 'ColumnId')
                  AND name = N'MS_Description')
    EXEC sys.sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'1 = el documento de la accion imprime la grilla completa de SITUACION PROPUESTA; 0 = imprime solo el nombre de la accion. Independiente de ActionCategory (que decide movimientos, historial laboral y SIIES). Se configura en Tipos de Accion de Personal.',
        @level0type = N'SCHEMA', @level0name = N'HR',
        @level1type = N'TABLE',  @level1name = N'tbl_personnel_action_type',
        @level2type = N'COLUMN', @level2name = N'ShowProposedSituation';
GO

-- Verificación
SELECT Code, Name, ActionCategory, ShowProposedSituation
  FROM HR.tbl_personnel_action_type
 ORDER BY ShowProposedSituation DESC, ActionCategory, Code;
