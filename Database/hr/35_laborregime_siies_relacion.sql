/* ============================================================================
   35_laborregime_siies_relacion.sql
   HR.tbl_EmployeeLaborRegime.SiiesRelacionIesTypeId: RELACION_IES de SIIES guardada POR PERSONA/RÉGIMEN.

   Motivo: la relación se derivaba del tipo de contrato o de acción, pero tipos como "Otros", "Traslado" o
   "Cambio Administrativo" pueden ser nombramiento o contrato; no se pueden clasificar por tipo. Si la columna tiene valor
   manda sobre la derivación (vw_SiiesFuncionarios, vw_SiiesProfesores y sus consultas por rango); NULL = se sigue derivando.
   Apunta a ref_Types (Category = 'SIIES_RELACION_IES': 2131 Nombramiento, 2132/2133 Contrato, 2134 Prometeo).

   Aditivo e idempotente. Ejecutar con sqlcmd -f 65001.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_EmployeeLaborRegime') AND name = 'SiiesRelacionIesTypeId')
    ALTER TABLE HR.tbl_EmployeeLaborRegime ADD SiiesRelacionIesTypeId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeLaborRegime_SiiesRelacionIes')
    ALTER TABLE HR.tbl_EmployeeLaborRegime ADD CONSTRAINT FK_EmployeeLaborRegime_SiiesRelacionIes
        FOREIGN KEY (SiiesRelacionIesTypeId) REFERENCES HR.ref_Types (TypeID);
GO
