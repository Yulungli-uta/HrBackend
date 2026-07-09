-- ============================================================
-- AUTOSERVICIO DEL EMPLEADO : esquema [HR]
-- Generado: 2026-07-06
-- Certificados laborales y solicitudes internas del empleado
-- autenticado. Reutiliza el motor documental existente
-- (tbl_DocumentTemplates/tbl_GeneratedDocuments/TBL_StoredFile)
-- y el patron ya probado en Renuncia/Jubilacion (tabla+historial,
-- EmployeeId resuelto por ICurrentUserService, scope por
-- departamento via HR.tbl_UserAccessScopes).
-- ============================================================

SET NOCOUNT ON;
GO

-- [tbl_EmployeeCertificateRequests]

IF OBJECT_ID('[HR].[tbl_EmployeeCertificateRequests]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeCertificateRequests] (
    [RequestID]         INT IDENTITY(1,1) NOT NULL,
    [EmployeeID]        INT NOT NULL,
    -- Tipo de certificado (LABORAL, INGRESOS, ANTIGUEDAD, ...). Catalogo abierto,
    -- validado en backend contra una lista fija por ahora (ver ResignationRetirementRequestType
    -- para el mismo patron de constantes en vez de tabla de catalogo).
    [CertificateType]   NVARCHAR(30) NOT NULL DEFAULT ('LABORAL'),
    [Purpose]           NVARCHAR(300) NULL,
    [Status]            NVARCHAR(20) NOT NULL DEFAULT ('PENDIENTE'),
    [GeneratedDocumentID] INT NULL,
    [CreatedAt]         DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]         INT NOT NULL,
    [UpdatedAt]         DATETIME2 NULL,
    [UpdatedBy]         INT NULL,
    [IssuedAt]          DATETIME2 NULL,
    [IssuedBy]          INT NULL,
    [RejectedAt]        DATETIME2 NULL,
    [RejectedBy]        INT NULL,
    [RowVersion]        ROWVERSION NOT NULL,
    CONSTRAINT [PK_EmployeeCertificateRequests] PRIMARY KEY CLUSTERED ([RequestID]),
    CONSTRAINT [CHK_EmployeeCertificateRequests_Status] CHECK ([Status] IN ('PENDIENTE', 'EMITIDO', 'RECHAZADO', 'ANULADO'))
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeCertificateRequests_Employee')
ALTER TABLE [HR].[tbl_EmployeeCertificateRequests]
    ADD CONSTRAINT [FK_EmployeeCertificateRequests_Employee]
    FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeCertificateRequests_GeneratedDocument')
ALTER TABLE [HR].[tbl_EmployeeCertificateRequests]
    ADD CONSTRAINT [FK_EmployeeCertificateRequests_GeneratedDocument]
    FOREIGN KEY ([GeneratedDocumentID]) REFERENCES [HR].[tbl_GeneratedDocuments] ([DocumentID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeCertificateRequests_Employee'
      AND object_id = OBJECT_ID('[HR].[tbl_EmployeeCertificateRequests]')
)
CREATE INDEX [IX_EmployeeCertificateRequests_Employee]
    ON [HR].[tbl_EmployeeCertificateRequests] ([EmployeeID], [CreatedAt] DESC);
GO

-- [tbl_EmployeeCertificateStatusHistory]

IF OBJECT_ID('[HR].[tbl_EmployeeCertificateStatusHistory]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeCertificateStatusHistory] (
    [HistoryID]      INT IDENTITY(1,1) NOT NULL,
    [RequestID]      INT NOT NULL,
    [PreviousStatus] NVARCHAR(20) NULL,
    [NewStatus]      NVARCHAR(20) NOT NULL,
    [Action]         NVARCHAR(20) NOT NULL,
    [Observation]    NVARCHAR(1000) NULL,
    [CreatedAt]      DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]      INT NOT NULL,
    CONSTRAINT [PK_EmployeeCertificateStatusHistory] PRIMARY KEY CLUSTERED ([HistoryID])
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeCertificateStatusHistory_Request')
ALTER TABLE [HR].[tbl_EmployeeCertificateStatusHistory]
    ADD CONSTRAINT [FK_EmployeeCertificateStatusHistory_Request]
    FOREIGN KEY ([RequestID]) REFERENCES [HR].[tbl_EmployeeCertificateRequests] ([RequestID]);
GO

-- [tbl_EmployeeInternalRequests]
-- Estructura generica y reutilizable para solicitudes internas del empleado:
-- certificados (redundante con la tabla anterior se evita usando esta SOLO para
-- tipos que no tienen tabla propia), correccion de datos, documentos, informacion, otros.

IF OBJECT_ID('[HR].[tbl_EmployeeInternalRequests]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeInternalRequests] (
    [RequestID]      INT IDENTITY(1,1) NOT NULL,
    [EmployeeID]     INT NOT NULL,
    [RequestType]    NVARCHAR(30) NOT NULL,
    [Subject]        NVARCHAR(200) NOT NULL,
    [Description]    NVARCHAR(1500) NULL,
    [Status]         NVARCHAR(20) NOT NULL DEFAULT ('PENDIENTE'),
    [CreatedAt]      DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]      INT NOT NULL,
    [UpdatedAt]      DATETIME2 NULL,
    [UpdatedBy]      INT NULL,
    [ResolvedAt]     DATETIME2 NULL,
    [ResolvedBy]     INT NULL,
    [CancelledAt]    DATETIME2 NULL,
    [CancelledBy]    INT NULL,
    [RowVersion]     ROWVERSION NOT NULL,
    CONSTRAINT [PK_EmployeeInternalRequests] PRIMARY KEY CLUSTERED ([RequestID]),
    CONSTRAINT [CHK_EmployeeInternalRequests_Type] CHECK ([RequestType] IN
        ('ACTUALIZACION_DATOS', 'DOCUMENTO', 'INFORMACION', 'OTRO')),
    CONSTRAINT [CHK_EmployeeInternalRequests_Status] CHECK ([Status] IN
        ('PENDIENTE', 'EN_REVISION', 'DEVUELTO', 'APROBADO', 'RECHAZADO', 'ANULADO', 'COMPLETADO'))
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeInternalRequests_Employee')
ALTER TABLE [HR].[tbl_EmployeeInternalRequests]
    ADD CONSTRAINT [FK_EmployeeInternalRequests_Employee]
    FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeInternalRequests_Employee'
      AND object_id = OBJECT_ID('[HR].[tbl_EmployeeInternalRequests]')
)
CREATE INDEX [IX_EmployeeInternalRequests_Employee]
    ON [HR].[tbl_EmployeeInternalRequests] ([EmployeeID], [CreatedAt] DESC);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeInternalRequests_Status'
      AND object_id = OBJECT_ID('[HR].[tbl_EmployeeInternalRequests]')
)
CREATE INDEX [IX_EmployeeInternalRequests_Status]
    ON [HR].[tbl_EmployeeInternalRequests] ([Status]);
GO

-- [tbl_EmployeeInternalRequestStatusHistory]

IF OBJECT_ID('[HR].[tbl_EmployeeInternalRequestStatusHistory]') IS NULL
CREATE TABLE [HR].[tbl_EmployeeInternalRequestStatusHistory] (
    [HistoryID]      INT IDENTITY(1,1) NOT NULL,
    [RequestID]      INT NOT NULL,
    [PreviousStatus] NVARCHAR(20) NULL,
    [NewStatus]      NVARCHAR(20) NOT NULL,
    [Action]         NVARCHAR(20) NOT NULL,
    [Observation]    NVARCHAR(1000) NULL,
    [CreatedAt]      DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]      INT NOT NULL,
    CONSTRAINT [PK_EmployeeInternalRequestStatusHistory] PRIMARY KEY CLUSTERED ([HistoryID])
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeInternalRequestStatusHistory_Request')
ALTER TABLE [HR].[tbl_EmployeeInternalRequestStatusHistory]
    ADD CONSTRAINT [FK_EmployeeInternalRequestStatusHistory_Request]
    FOREIGN KEY ([RequestID]) REFERENCES [HR].[tbl_EmployeeInternalRequests] ([RequestID]);
GO

-- Catalogo documental: reutiliza el motor existente de archivos, mismo patron
-- que HR_RESIGNATION_RETIREMENT (ver Database/hr/10_resignation_retirement.sql).
IF NOT EXISTS (SELECT 1 FROM [HR].[TBL_DirectoryParameters] WHERE [Code] = 'HR_EMPLOYEE_REQUESTS')
INSERT INTO [HR].[TBL_DirectoryParameters] ([Code], [PhysicalPath], [RelativePath], [Description], [Extension], [MaxSizeMB], [Status])
VALUES ('HR_EMPLOYEE_REQUESTS', '\\nas11.uta.edu.ec\ArchUTA1\HR\employee_requests\', '\\nas11.uta.edu.ec\ArchUTA1\HR\employee_requests\', 'Adjuntos de solicitudes internas del autoservicio del empleado', '.pdf', 10, 1);
GO

-- Catalogo de modulos para scope por departamento de RRHH (HR.tbl_UserAccessScopes).
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ACCESS_MODULE_TYPE' AND [Name] = 'EMPLOYEE_CERTIFICATE_REQUESTS')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [IsActive])
VALUES ('ACCESS_MODULE_TYPE', 'EMPLOYEE_CERTIFICATE_REQUESTS', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ACCESS_MODULE_TYPE' AND [Name] = 'EMPLOYEE_INTERNAL_REQUESTS')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [IsActive])
VALUES ('ACCESS_MODULE_TYPE', 'EMPLOYEE_INTERNAL_REQUESTS', 1);
GO

-- ============================================================
-- Plantilla documental: Certificado Laboral
-- Reutiliza el motor existente (tbl_DocumentTemplates/Fields +
-- DocumentGenerationService + InstitutionalDocumentRenderer, sin
-- crear un renderer nuevo: DocumentRendererFactory ya cae por
-- default a InstitutionalDocumentRenderer para cualquier
-- TemplateType que no sea ACCION_PERSONAL/CONTRATO).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateCode = 'CERTIFICADO_LABORAL' AND Status = 'PUBLISHED')
BEGIN
    DECLARE @CertTemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType, TemplateCode, Name, Version, LayoutType, Status, HtmlContent, CreatedAt
    )
    VALUES (
        'CERTIFICADO_LABORAL',
        'CERTIFICADO_LABORAL',
        'Certificado Laboral',
        '1.0',
        'FLOW_TEXT',
        'PUBLISHED',
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color:#000; width:210mm; margin:0 auto; padding:20mm; }
.header { display:flex; align-items:center; gap:10mm; border-bottom:2px solid #8B1A1A; padding-bottom:6mm; margin-bottom:10mm; }
.header img { height:20mm; }
.header .titles { text-align:center; flex:1; font-weight:bold; font-size:11pt; }
.doc-title { text-align:center; font-size:14pt; font-weight:bold; margin:8mm 0; text-decoration:underline; }
.doc-number { text-align:right; font-size:9pt; margin-bottom:6mm; }
.body-text { line-height:1.8; text-align:justify; margin-bottom:10mm; }
.signature { margin-top:25mm; text-align:center; }
.signature .line { border-top:1px solid #000; width:70mm; margin:0 auto 2mm auto; }
.footer-date { margin-top:15mm; font-size:10pt; }
</style>
</head>
<body>
<div class="header">
<img src="{{LOGO_URL}}" alt="UTA"/>
<div class="titles">UNIVERSIDAD TÃ‰CNICA DE AMBATO<br/>DIRECCIÃ“N DE TALENTO HUMANO</div>
</div>
<div class="doc-number">No. {{DOC_NUMBER}}</div>
<div class="doc-title">CERTIFICADO LABORAL</div>
<div class="body-text">
La DirecciÃ³n de Talento Humano de la Universidad TÃ©cnica de Ambato certifica que <b>{{EMPLOYEE_FULLNAME}}</b>,
portador/a de la cÃ©dula de ciudadanÃ­a No. {{EMPLOYEE_IDCARD}}, labora en esta instituciÃ³n desde el
{{EMPLOYEE_HIREDATE}}, desempeÃ±ando el cargo de <b>{{JOB_DESCRIPTION}}</b> en la dependencia
<b>{{DEPARTMENT_NAME}}</b>.
</div>
<div class="body-text">
Se extiende el presente certificado a solicitud del/la interesado/a, para los fines que estime pertinentes.
</div>
<div class="footer-date">Ambato, {{SYSTEM_DATE}}</div>
<div class="signature">
<div class="line"></div>
<div>{{DTH_DIRECTOR_NAME}}</div>
<div>{{DTH_DIRECTOR_TITLE}}</div>
</div>
</body>
</html>',
        GETDATE()
    );

    SET @CertTemplateID = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, IsRequired, SortOrder)
    VALUES
        (@CertTemplateID, 'LOGO_URL', 'Logo institucional', 'SYSTEM', 0, 1),
        (@CertTemplateID, 'DOC_NUMBER', 'NÃºmero de documento', 'SYSTEM', 0, 2),
        (@CertTemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo', 'EMPLOYEE', 1, 3),
        (@CertTemplateID, 'EMPLOYEE_IDCARD', 'CÃ©dula', 'EMPLOYEE', 1, 4),
        (@CertTemplateID, 'EMPLOYEE_HIREDATE', 'Fecha de ingreso', 'EMPLOYEE', 1, 5),
        -- MANUAL: se resuelve en EmployeeCertificateService desde Employees.DepartmentId/JobId
        -- (evita la resolucion automatica CONTRACT_* de DocumentFieldResolver, que cae a
        -- "Contracts.Status = 1" -- convencion legada que no siempre coincide con el estado
        -- VIGENTE actual de multiples regimenes).
        (@CertTemplateID, 'JOB_DESCRIPTION', 'Cargo', 'MANUAL', 0, 6),
        (@CertTemplateID, 'DEPARTMENT_NAME', 'Dependencia', 'MANUAL', 0, 7),
        (@CertTemplateID, 'SYSTEM_DATE', 'Fecha de emisiÃ³n', 'SYSTEM', 0, 8),
        (@CertTemplateID, 'DTH_DIRECTOR_NAME', 'Director DTH', 'SYSTEM', 0, 9),
        (@CertTemplateID, 'DTH_DIRECTOR_TITLE', 'Cargo Director DTH', 'SYSTEM', 0, 10);
END
GO

-- CHK_GeneratedDocuments_EntityType (constraint existente fuera de estos scripts, igual que
-- CHK_PersonnelActions_Status en 02_constraints.sql) no permitia 'CERTIFICATE' -- se agrega
-- el nuevo valor del enum DocumentEntityType.Certificate usado por el autoservicio.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_GeneratedDocuments_EntityType' AND definition NOT LIKE '%CERTIFICATE%'
)
    ALTER TABLE [HR].[tbl_GeneratedDocuments] DROP CONSTRAINT [CHK_GeneratedDocuments_EntityType];
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_GeneratedDocuments_EntityType')
    ALTER TABLE [HR].[tbl_GeneratedDocuments] WITH CHECK
        ADD CONSTRAINT [CHK_GeneratedDocuments_EntityType]
        CHECK ([EntityType]='OFICIO' OR [EntityType]='AGREEMENT' OR [EntityType]='PERSONNELACTION'
               OR [EntityType]='CONTRACT' OR [EntityType]='CERTIFICATE');
GO

-- ============================================================
-- Certificados: tipo "actual" vs "historial laboral completo"
-- Catalogo consumido por el frontend via
-- TiposReferenciaAPI.byCategory(REF_TYPE_CATEGORIES.EMPLOYEE_CERTIFICATE_TYPE)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_CERTIFICATE_TYPE' AND [Name] = 'LABORAL')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('EMPLOYEE_CERTIFICATE_TYPE', 'LABORAL', 'Certificado de trabajo actual', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_CERTIFICATE_TYPE' AND [Name] = 'HISTORIAL_LABORAL')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('EMPLOYEE_CERTIFICATE_TYPE', 'HISTORIAL_LABORAL', 'Certificado de historial laboral completo', 1);
GO

-- Plantilla nueva: certificado de historial laboral completo. HISTORY_TABLE_HTML es MANUAL:
-- EmployeeCertificateService arma el fragmento HTML de la tabla (todos los contratos +
-- acciones de personal del empleado) y lo pasa como override -- no se modifica el motor
-- documental compartido (DocumentTemplateEngine/DocumentFieldResolver no soportan listas).
IF NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateCode = 'CERTIFICADO_HISTORIAL_LABORAL' AND Status = 'PUBLISHED')
BEGIN
    DECLARE @HistTemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType, TemplateCode, Name, Version, LayoutType, Status, HtmlContent, CreatedAt
    )
    VALUES (
        'CERTIFICADO_HISTORIAL_LABORAL',
        'CERTIFICADO_HISTORIAL_LABORAL',
        'Certificado de Historial Laboral',
        '1.0',
        'FLOW_TEXT',
        'PUBLISHED',
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 10pt; color:#000; width:210mm; margin:0 auto; padding:16mm; }
.header { display:flex; align-items:center; gap:10mm; border-bottom:2px solid #8B1A1A; padding-bottom:6mm; margin-bottom:8mm; }
.header img { height:20mm; }
.header .titles { text-align:center; flex:1; font-weight:bold; font-size:11pt; }
.doc-title { text-align:center; font-size:13pt; font-weight:bold; margin:6mm 0; text-decoration:underline; }
.doc-number { text-align:right; font-size:9pt; margin-bottom:4mm; }
.body-text { line-height:1.7; text-align:justify; margin-bottom:6mm; }
table.history { width:100%; border-collapse:collapse; margin-bottom:8mm; font-size:8.5pt; }
table.history th, table.history td { border:1px solid #000; padding:3px 5px; text-align:left; }
table.history th { background-color:#d9d9d9; font-weight:bold; }
.signature { margin-top:20mm; text-align:center; }
.signature .line { border-top:1px solid #000; width:70mm; margin:0 auto 2mm auto; }
.footer-date { margin-top:8mm; font-size:10pt; }
</style>
</head>
<body>
<div class="header">
<img src="{{LOGO_URL}}" alt="UTA"/>
<div class="titles">UNIVERSIDAD TÃ‰CNICA DE AMBATO<br/>DIRECCIÃ“N DE TALENTO HUMANO</div>
</div>
<div class="doc-number">No. {{DOC_NUMBER}}</div>
<div class="doc-title">CERTIFICADO DE HISTORIAL LABORAL</div>
<div class="body-text">
La DirecciÃ³n de Talento Humano de la Universidad TÃ©cnica de Ambato certifica que <b>{{EMPLOYEE_FULLNAME}}</b>,
portador/a de la cÃ©dula de ciudadanÃ­a No. {{EMPLOYEE_IDCARD}}, registra el siguiente historial laboral
en esta instituciÃ³n:
</div>
{{HISTORY_TABLE_HTML}}
<div class="body-text">
Se extiende el presente certificado a solicitud del/la interesado/a, para los fines que estime pertinentes.
</div>
<div class="footer-date">Ambato, {{SYSTEM_DATE}}</div>
<div class="signature">
<div class="line"></div>
<div>{{DTH_DIRECTOR_NAME}}</div>
<div>{{DTH_DIRECTOR_TITLE}}</div>
</div>
</body>
</html>',
        GETDATE()
    );

    SET @HistTemplateID = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, IsRequired, SortOrder)
    VALUES
        (@HistTemplateID, 'LOGO_URL', 'Logo institucional', 'SYSTEM', 0, 1),
        (@HistTemplateID, 'DOC_NUMBER', 'NÃºmero de documento', 'SYSTEM', 0, 2),
        (@HistTemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo', 'EMPLOYEE', 1, 3),
        (@HistTemplateID, 'EMPLOYEE_IDCARD', 'CÃ©dula', 'EMPLOYEE', 1, 4),
        (@HistTemplateID, 'HISTORY_TABLE_HTML', 'Tabla de historial laboral', 'MANUAL', 1, 5),
        (@HistTemplateID, 'SYSTEM_DATE', 'Fecha de emisiÃ³n', 'SYSTEM', 0, 6),
        (@HistTemplateID, 'DTH_DIRECTOR_NAME', 'Director DTH', 'SYSTEM', 0, 7),
        (@HistTemplateID, 'DTH_DIRECTOR_TITLE', 'Cargo Director DTH', 'SYSTEM', 0, 8);
END
GO
