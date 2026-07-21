-- ============================================================
-- PARAMETRIZACIÓN DE REQUISITOS DOCUMENTALES POR TRÁMITE : esquema [HR]
-- Generado: 2026-07-17
--
-- Catálogo de "qué documentos se requieren" por trámite, en dos niveles:
--   - ModuleTypeID  (obligatorio): nivel general, mismo catálogo que ya
--     filtra accesos de usuario (ref_Types Category='ACCESS_MODULE_TYPE':
--     CONTRACTS, PERSONNEL_ACTIONS, RESIGNATION_RETIREMENT_REQUESTS, ...).
--     Solo puede parametrizar un módulo quien tenga HR.tbl_UserAccessScopes
--     activo para ese ModuleTypeID.
--   - SpecificTypeID (opcional): override puntual dentro del módulo
--     (ej. ContractTypeID cuando ModuleTypeID=CONTRACTS). NULL = aplica a
--     todo el módulo. Es polimórfico (según ModuleTypeID apunta a distinta
--     tabla), por lo que NO lleva FK física — se valida en capa de aplicación.
--
-- No incluye mapeo "documento -> qué placeholder de plantilla alimenta":
-- eso ya vive en código (ContractsService.cs), resuelto por NOMBRE de
-- ref_Types, no por ID fijo. Esta tabla es solo el checklist de
-- obligatoriedad, para uso en validación antes de generar el documento.
--
-- Solo aditivo. Los 3 registros migrados de abajo quedan IsRequired=0
-- (informativos) para no alterar el comportamiento actual: hoy esos 3
-- documentos son opcionales (best-effort override si existen), no
-- bloqueantes. La obligatoriedad real se activa solo cuando alguien la
-- configura explícitamente desde la nueva pantalla.
-- ============================================================

SET NOCOUNT ON;
GO

IF OBJECT_ID('[HR].[tbl_TramiteRequirements]') IS NULL
CREATE TABLE [HR].[tbl_TramiteRequirements] (
    [RequirementID]  INT IDENTITY(1,1) NOT NULL,
    [ModuleTypeID]   INT NOT NULL,
    [SpecificTypeID] INT NULL,
    [DocumentTypeID] INT NOT NULL,
    [IsRequired]     BIT NOT NULL DEFAULT (0),
    [IsActive]       BIT NOT NULL DEFAULT (1),
    [CreatedAt]      DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]      INT NULL,
    [UpdatedAt]      DATETIME2 NULL,
    [UpdatedBy]      INT NULL,
    CONSTRAINT [PK_TramiteRequirements] PRIMARY KEY CLUSTERED ([RequirementID])
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TramiteRequirements_ModuleType')
ALTER TABLE [HR].[tbl_TramiteRequirements]
    ADD CONSTRAINT [FK_TramiteRequirements_ModuleType]
    FOREIGN KEY ([ModuleTypeID]) REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TramiteRequirements_DocumentType')
ALTER TABLE [HR].[tbl_TramiteRequirements]
    ADD CONSTRAINT [FK_TramiteRequirements_DocumentType]
    FOREIGN KEY ([DocumentTypeID]) REFERENCES [HR].[ref_Types] ([TypeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TramiteRequirements_Module_Specific' AND object_id = OBJECT_ID('[HR].[tbl_TramiteRequirements]'))
CREATE INDEX [IX_TramiteRequirements_Module_Specific] ON [HR].[tbl_TramiteRequirements] ([ModuleTypeID], [SpecificTypeID]);
GO

-- ------------------------------------------------------------
-- Migración informativa: reglas hoy implícitas en ContractsService.cs
-- (nivel general del módulo CONTRACTS, sin override por tipo específico).
-- Resueltas por NOMBRE, nunca por TypeID fijo.
-- ------------------------------------------------------------
INSERT INTO [HR].[tbl_TramiteRequirements] (ModuleTypeID, SpecificTypeID, DocumentTypeID, IsRequired, IsActive)
SELECT m.TypeID, NULL, d.TypeID, 0, 1
FROM HR.ref_Types m
CROSS JOIN HR.ref_Types d
WHERE m.Category = 'ACCESS_MODULE_TYPE' AND m.Name = 'CONTRACTS'
  AND d.Category = 'DOCUMENT_TYPE' AND d.Name IN ('RESOLUCION_CAU', 'MEMORANDO_RECTORADO', 'RESOLUCION_DELEGACION')
  AND NOT EXISTS (
    SELECT 1 FROM [HR].[tbl_TramiteRequirements] r
    WHERE r.ModuleTypeID = m.TypeID AND r.SpecificTypeID IS NULL AND r.DocumentTypeID = d.TypeID
  );
GO

-- ------------------------------------------------------------
-- Descripciones en español para los módulos (ref_Types
-- Category='ACCESS_MODULE_TYPE') consumidos por la pantalla
-- "Requisitos Documentales por Trámite". Estos registros existían sin
-- Description (CONTRACTS/PERSONNEL_ACTIONS del baseline; los otros 3
-- sembrados en 10_resignation_retirement.sql / 11_employee_self_service.sql).
-- Solo rellena donde Description es NULL, no pisa nada existente.
-- ------------------------------------------------------------
UPDATE [HR].[ref_Types]
SET [Description] = v.Description
FROM [HR].[ref_Types] r
INNER JOIN (VALUES
    ('CONTRACTS', N'Contratos'),
    ('PERSONNEL_ACTIONS', N'Acciones de Personal'),
    ('RESIGNATION_RETIREMENT_REQUESTS', N'Solicitudes de Renuncia/Jubilación'),
    ('EMPLOYEE_CERTIFICATE_REQUESTS', N'Solicitudes de Certificados de Empleado'),
    ('EMPLOYEE_INTERNAL_REQUESTS', N'Solicitudes Internas del Empleado')
) AS v(Name, Description) ON v.Name = r.Name
WHERE r.Category = 'ACCESS_MODULE_TYPE' AND r.Description IS NULL;
GO

-- ============================================================
-- ÍNDICES ÚNICOS: evita duplicar el mismo documento para el mismo
-- módulo/tipo específico en HR.tbl_TramiteRequirements.
-- Generado: 2026-07-20
--
-- SpecificTypeID es NULL para "aplica a todo el módulo". SQL Server NO
-- considera duplicados dos filas con NULL en un índice único simple, así
-- que se necesitan DOS índices únicos filtrados:
--   1) SpecificTypeID IS NOT NULL -> único por (Module, Specific, Document)
--   2) SpecificTypeID IS NULL     -> único por (Module, Document) general
--
-- Verificado contra la BD real antes de crear: 0 filas duplicadas hoy
-- (SELECT ... GROUP BY ... HAVING COUNT(*) > 1 no devolvió resultados).
-- Complementa (no reemplaza) la validación de aplicación ya existente en
-- TramiteRequirementsService.CreateAsync (chequeo previo + traducción de
-- SqlException 2601/2627 a error de negocio legible).
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TramiteRequirements_Module_Specific_Document'
      AND object_id = OBJECT_ID('[HR].[tbl_TramiteRequirements]')
)
CREATE UNIQUE INDEX [UX_TramiteRequirements_Module_Specific_Document]
    ON [HR].[tbl_TramiteRequirements] ([ModuleTypeID], [SpecificTypeID], [DocumentTypeID])
    WHERE [SpecificTypeID] IS NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TramiteRequirements_Module_Document_General'
      AND object_id = OBJECT_ID('[HR].[tbl_TramiteRequirements]')
)
CREATE UNIQUE INDEX [UX_TramiteRequirements_Module_Document_General]
    ON [HR].[tbl_TramiteRequirements] ([ModuleTypeID], [DocumentTypeID])
    WHERE [SpecificTypeID] IS NULL;
GO
