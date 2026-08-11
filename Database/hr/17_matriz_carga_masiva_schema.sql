-- ============================================================
-- Extensión de esquema: carga masiva MATRIZ SISTEMA INTEGRADO
-- UNIFICADO cruzada 28 julio (665 registros, análisis Fase 1
-- aprobado por el usuario 2026-08-04).
--
-- 1) HR.tbl_Employees.BudgetUnitTypeId: partida presupuestaria /
--    unidad de pago del sueldo ("Codigo_Facultad (UNIDAD A LA QUE
--    PERTENECE)" del Excel) — es un dato POR EMPLEADO, distinto de
--    Dependencia (unidad organizacional real). Confirmado que NO
--    es jerarquía derivable de HR.tbl_Departments.ParentID: un
--    mismo Dependencia aparece con varias partidas distintas.
-- 2) Categoría ref_Types AP_NIVEL_GESTION (ya existía, con 2 valores
--    placeholder de prueba "GESTION 1"/"GESTION 2", desactivados aquí):
--    se le agregan 38 valores reales de partida presupuestaria, tras
--    fusionar 3 pares que eran variantes de tipeo del mismo valor.
-- 3) HR.tbl_Contracts.BaseSalary: sueldo real individual para los
--    contratos (Tipo='CONTRATO' en el Excel, tbl_PersonnelActions
--    ya tenía NewRmu/PreviousRmu, no hacía falta tocarlo).
-- 4) HR.tbl_jobs.ReferenceSalary: sueldo de referencia del cargo
--    (moda de los sueldos reales encontrados para ese cargo en la
--    carga). NO es el sueldo real de ninguna persona en particular
--    — ver HR.vw_JobSalaryDiscrepancy para trazabilidad.
-- 5) HR.vw_JobSalaryDiscrepancy: vista viva (no una foto de un solo
--    momento) para que RRHH pueda revisar en cualquier momento qué
--    empleados activos tienen un sueldo distinto al de referencia
--    de su cargo, y corregirlo caso por caso.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar. Ninguna
-- columna existente se elimina, renombra ni cambia de tipo.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) tbl_Employees.BudgetUnitTypeId ------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_Employees]') AND name = 'BudgetUnitTypeId')
    ALTER TABLE [HR].[tbl_Employees] ADD [BudgetUnitTypeId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbl_Employees_BudgetUnitTypeId')
    ALTER TABLE [HR].[tbl_Employees]
        ADD CONSTRAINT [FK_tbl_Employees_BudgetUnitTypeId]
        FOREIGN KEY ([BudgetUnitTypeId]) REFERENCES [HR].[ref_Types]([TypeID]);
GO

-- 2) Catálogo AP_NIVEL_GESTION (38 valores nuevos, ya deduplicado) ----------
UPDATE [HR].[ref_Types] SET [IsActive] = 0 WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] IN (N'GESTION 1', N'GESTION 2');
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION ADMINISTRATIVA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION ADMINISTRATIVA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN INGENIERIA EN SISTEMAS ELECTRONICA E INDUSTRIAL')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN INGENIERIA EN SISTEMAS ELECTRONICA E INDUSTRIAL', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CONTABILIDAD Y AUDITORIA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CONTABILIDAD Y AUDITORIA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN INGENIERIA CIVIL Y MECANICA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN INGENIERIA CIVIL Y MECANICA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIA E INGENIERIA EN ALIMENTOS Y BIOTECNOLOGIA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIA E INGENIERIA EN ALIMENTOS Y BIOTECNOLOGIA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS ADMINISTRATIVAS')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS ADMINISTRATIVAS', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION FINANCIERA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION FINANCIERA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS HUMANAS Y DE LA EDUCACION')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS HUMANAS Y DE LA EDUCACION', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS AGROPECUARIAS')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN CIENCIAS AGROPECUARIAS', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN JURISPRUDENCIA Y CIENCIAS SOCIALES')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN JURISPRUDENCIA Y CIENCIAS SOCIALES', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE TECNOLOGIA DE INFORMACION Y COMUNICACION')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE TECNOLOGIA DE INFORMACION Y COMUNICACION', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE INVESTIGACION Y DESARROLLO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE INVESTIGACION Y DESARROLLO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION COMPLEMENTARIA DE ESTUDIANTES CENTRO DE IDIOMAS Y CULTURA FISICA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION COMPLEMENTARIA DE ESTUDIANTES CENTRO DE IDIOMAS Y CULTURA FISICA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE BIENESTAR UNIVERSITARIO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE BIENESTAR UNIVERSITARIO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE TALENTO HUMANO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE TALENTO HUMANO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN DISENO Y ARQUITECTURA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'FORMACION Y ESPECIALIZACION DE PROFESIONALES EN DISENO Y ARQUITECTURA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE SECRETARIA GENERAL')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE SECRETARIA GENERAL', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE COMUNICACION Y RELACIONES PUBLICAS')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE COMUNICACION Y RELACIONES PUBLICAS', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE INFRAESTRUCTURA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE INFRAESTRUCTURA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE PLANIFICACION Y EVALUACION')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE PLANIFICACION Y EVALUACION', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DEL VICERRECTORADO ACADEMICO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DEL VICERRECTORADO ACADEMICO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE INNOVACION Y EMPRENDIMIENTO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE INNOVACION Y EMPRENDIMIENTO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE EDUCACION CONTINUA A DISTANCIA Y VIRTUAL')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE EDUCACION CONTINUA A DISTANCIA Y VIRTUAL', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION ACADEMICA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION ACADEMICA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'RECTORADO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'RECTORADO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE GESTION DE CALIDAD')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE GESTION DE CALIDAD', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE RIESGOS')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE RIESGOS', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE CULTURA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE CULTURA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE RELACIONES NACIONALES E INTERNACIONALES')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE RELACIONES NACIONALES E INTERNACIONALES', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DEL CENTRO DE POSGRADOS')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DEL CENTRO DE POSGRADOS', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE TALENTO HUMANO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE TALENTO HUMANO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE PLANIFICACION EVALUACION Y ASEGURAMIENTO DE LA CALIDAD')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE PLANIFICACION EVALUACION Y ASEGURAMIENTO DE LA CALIDAD', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DEL VICERRECTORADO ADMINISTRATIVO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DEL VICERRECTORADO ADMINISTRATIVO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION CENTRAL DE INVESTIGACION Y DESARROLLO')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION CENTRAL DE INVESTIGACION Y DESARROLLO', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA PROCURADURIA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA PROCURADURIA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION CENTRAL DE LA ACADEMIA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION CENTRAL DE LA ACADEMIA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE VINCULACION CON LA SOCIEDAD')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE VINCULACION CON LA SOCIEDAD', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'AP_NIVEL_GESTION' AND [Name] = N'GESTION DE LA DIRECCION DE EDUCACION A DISTANCIA Y VIRTUAL')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('AP_NIVEL_GESTION', N'GESTION DE LA DIRECCION DE EDUCACION A DISTANCIA Y VIRTUAL', 1, GETDATE());
GO

-- 3) tbl_Contracts.BaseSalary -------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_Contracts]') AND name = 'BaseSalary')
    ALTER TABLE [HR].[tbl_Contracts] ADD [BaseSalary] DECIMAL(10,2) NULL;
GO

-- 4) tbl_jobs.ReferenceSalary ---------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_jobs]') AND name = 'ReferenceSalary')
    ALTER TABLE [HR].[tbl_jobs] ADD [ReferenceSalary] DECIMAL(10,2) NULL;
GO

-- 5) Vista de discrepancias de sueldo (viva, consultar cuando haga falta) ----
IF OBJECT_ID('[HR].[vw_JobSalaryDiscrepancy]', 'V') IS NOT NULL
    DROP VIEW [HR].[vw_JobSalaryDiscrepancy];
GO
CREATE VIEW [HR].[vw_JobSalaryDiscrepancy] AS
WITH CurrentSalary AS (
    SELECT
        e.EmployeeID,
        e.JobID,
        COALESCE(pa.NewRmu, c.BaseSalary) AS CurrentSalary
    FROM [HR].[tbl_Employees] e
    OUTER APPLY (
        SELECT TOP 1 p.NewRmu
        FROM [HR].[tbl_PersonnelActions] p
        WHERE p.EmployeeID = e.EmployeeID AND p.IsDeleted = 0 AND p.NewRmu IS NOT NULL
        ORDER BY p.EffectiveDate DESC, p.ActionDate DESC, p.ActionID DESC
    ) pa
    OUTER APPLY (
        SELECT TOP 1 co.BaseSalary
        FROM [HR].[tbl_Contracts] co
        WHERE co.PersonID = e.PersonID AND co.IsDeleted = 0 AND co.BaseSalary IS NOT NULL
        ORDER BY co.startdate DESC, co.ContractID DESC
    ) c
    WHERE e.IsActive = 1 AND e.IsDeleted = 0
)
SELECT
    j.JobID,
    j.Description AS JobDescription,
    j.ReferenceSalary,
    cs.EmployeeID,
    p.FirstName + ' ' + p.LastName AS EmployeeName,
    cs.CurrentSalary,
    cs.CurrentSalary - j.ReferenceSalary AS Deviation
FROM CurrentSalary cs
JOIN [HR].[tbl_jobs] j ON j.JobID = cs.JobID
JOIN [HR].[tbl_Employees] e ON e.EmployeeID = cs.EmployeeID
JOIN [HR].[tbl_People] p ON p.PersonID = e.PersonID
WHERE j.ReferenceSalary IS NOT NULL
  AND cs.CurrentSalary IS NOT NULL
  AND cs.CurrentSalary <> j.ReferenceSalary;
GO
