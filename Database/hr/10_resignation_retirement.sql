-- ============================================================
-- RENUNCIA Y JUBILACION : esquema [HR]
-- Generado: 2026-07-06
-- Solicitudes de renuncia/jubilacion registradas por el propio
-- empleado autenticado y revisadas por Recursos Humanos.
-- Los documentos adjuntos reutilizan HR.TBL_StoredFile +
-- HR.TBL_DirectoryParameters (DirectoryCode='HR_RESIGNATION_RETIREMENT'),
-- no se crea tabla de archivos nueva.
-- ============================================================

SET NOCOUNT ON;
GO

-- [tbl_ResignationRetirementRequests]

IF OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]') IS NULL
CREATE TABLE [HR].[tbl_ResignationRetirementRequests] (
    [RequestID]              INT IDENTITY(1,1) NOT NULL,
    [EmployeeID]              INT NOT NULL,
    [RequestType]             NVARCHAR(20) NOT NULL,
    [RequestDate]             DATE NOT NULL DEFAULT (CAST(GETDATE() AS DATE)),
    [ProposedExitDate]        DATE NOT NULL,
    [Reason]                  NVARCHAR(1000) NULL,
    [AdditionalNotes]         NVARCHAR(1000) NULL,
    [Status]                  NVARCHAR(20) NOT NULL DEFAULT ('PENDIENTE'),
    -- Gancho reservado para la futura accion de personal de desvinculacion;
    -- no se crea automaticamente, solo se enlaza si RRHH la genera despues.
    [LinkedPersonnelActionID] INT NULL,
    [CreatedAt]               DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]               INT NOT NULL,
    [UpdatedAt]               DATETIME2 NULL,
    [UpdatedBy]               INT NULL,
    [ApprovedAt]              DATETIME2 NULL,
    [ApprovedBy]              INT NULL,
    [RejectedAt]              DATETIME2 NULL,
    [RejectedBy]              INT NULL,
    [CancelledAt]             DATETIME2 NULL,
    [CancelledBy]             INT NULL,
    [RowVersion]              ROWVERSION NOT NULL,
    CONSTRAINT [PK_ResignationRetirementRequests] PRIMARY KEY CLUSTERED ([RequestID]),
    CONSTRAINT [CHK_ResignationRetirement_RequestType] CHECK ([RequestType] IN ('RESIGNATION', 'RETIREMENT')),
    CONSTRAINT [CHK_ResignationRetirement_Status] CHECK ([Status] IN ('PENDIENTE', 'EN_REVISION', 'DEVUELTO', 'APROBADO', 'RECHAZADO', 'ANULADO'))
);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResignationRetirementRequests_Employee'
)
ALTER TABLE [HR].[tbl_ResignationRetirementRequests]
    ADD CONSTRAINT [FK_ResignationRetirementRequests_Employee]
    FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees] ([EmployeeID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResignationRetirementRequests_Employee'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]')
)
CREATE INDEX [IX_ResignationRetirementRequests_Employee]
    ON [HR].[tbl_ResignationRetirementRequests] ([EmployeeID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResignationRetirementRequests_Status'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]')
)
CREATE INDEX [IX_ResignationRetirementRequests_Status]
    ON [HR].[tbl_ResignationRetirementRequests] ([Status]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResignationRetirementRequests_RequestType'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]')
)
CREATE INDEX [IX_ResignationRetirementRequests_RequestType]
    ON [HR].[tbl_ResignationRetirementRequests] ([RequestType]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResignationRetirementRequests_RequestDate'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]')
)
CREATE INDEX [IX_ResignationRetirementRequests_RequestDate]
    ON [HR].[tbl_ResignationRetirementRequests] ([RequestDate]);
GO

-- Evita 2 solicitudes activas (PENDIENTE/EN_REVISION/DEVUELTO) del mismo
-- tipo para el mismo empleado. Indice unico filtrado.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_ResignationRetirement_ActiveByEmployeeType'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]')
)
CREATE UNIQUE INDEX [UQ_ResignationRetirement_ActiveByEmployeeType]
    ON [HR].[tbl_ResignationRetirementRequests] ([EmployeeID], [RequestType])
    WHERE [Status] IN ('PENDIENTE', 'EN_REVISION', 'DEVUELTO');
GO

-- [tbl_ResignationRetirementStatusHistory]

IF OBJECT_ID('[HR].[tbl_ResignationRetirementStatusHistory]') IS NULL
CREATE TABLE [HR].[tbl_ResignationRetirementStatusHistory] (
    [HistoryID]      INT IDENTITY(1,1) NOT NULL,
    [RequestID]      INT NOT NULL,
    [PreviousStatus] NVARCHAR(20) NULL,
    [NewStatus]      NVARCHAR(20) NOT NULL,
    [Action]         NVARCHAR(20) NOT NULL,
    [Observation]    NVARCHAR(1000) NULL,
    [CreatedAt]      DATETIME2 NOT NULL DEFAULT (getdate()),
    [CreatedBy]      INT NOT NULL,
    CONSTRAINT [PK_ResignationRetirementStatusHistory] PRIMARY KEY CLUSTERED ([HistoryID])
);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResignationRetirementStatusHistory_Request'
)
ALTER TABLE [HR].[tbl_ResignationRetirementStatusHistory]
    ADD CONSTRAINT [FK_ResignationRetirementStatusHistory_Request]
    FOREIGN KEY ([RequestID]) REFERENCES [HR].[tbl_ResignationRetirementRequests] ([RequestID]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResignationRetirementStatusHistory_Request'
      AND object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementStatusHistory]')
)
CREATE INDEX [IX_ResignationRetirementStatusHistory_Request]
    ON [HR].[tbl_ResignationRetirementStatusHistory] ([RequestID], [CreatedAt] DESC);
GO

-- Catalogo documental: reutiliza el motor existente de archivos.
-- DirectoryCode='HR_RESIGNATION_RETIREMENT', EntityType='RESIGNATION_RETIREMENT_REQUEST'.
IF NOT EXISTS (SELECT 1 FROM [HR].[TBL_DirectoryParameters] WHERE [Code] = 'HR_RESIGNATION_RETIREMENT')
INSERT INTO [HR].[TBL_DirectoryParameters] ([Code], [PhysicalPath], [RelativePath], [Description], [Extension], [MaxSizeMB], [Status])
VALUES ('HR_RESIGNATION_RETIREMENT', '\\nas11.uta.edu.ec\ArchUTA1\HR\resignation_retirement\', '\\nas11.uta.edu.ec\ArchUTA1\HR\resignation_retirement\', 'Documentos de solicitudes de renuncia y jubilacion', '.pdf', 25, 1);
GO

-- Catalogo de modulos para HR.tbl_UserAccessScopes (mismo patron que CONTRACTS/PERSONNEL_ACTIONS).
-- Sin esta fila, IUserAccessScopeService.GetAllowedDepartmentIdsAsync nunca encuentra el
-- ModuleTypeId de 'RESIGNATION_RETIREMENT_REQUESTS' y el chequeo de scope quedaria inerte
-- aunque RRHH asigne scopes despues.
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ACCESS_MODULE_TYPE' AND [Name] = 'RESIGNATION_RETIREMENT_REQUESTS')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [IsActive])
VALUES ('ACCESS_MODULE_TYPE', 'RESIGNATION_RETIREMENT_REQUESTS', 1);
GO

-- Catalogo de tipos de solicitud, consumido por el frontend via
-- TiposReferenciaAPI.byCategory(REF_TYPE_CATEGORIES.RESIGNATION_RETIREMENT_TYPE)
-- en vez de tener "Renuncia"/"Jubilacion" hardcodeado en los <SelectItem>.
-- Name = valor exacto que ya espera el backend (ResignationRetirementRequestType /
-- CHK_ResignationRetirement_RequestType) -- no cambia el contrato, solo la fuente.
-- Description = etiqueta visible en español.
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'RESIGNATION_RETIREMENT_TYPE' AND [Name] = 'RESIGNATION')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('RESIGNATION_RETIREMENT_TYPE', 'RESIGNATION', 'Renuncia', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'RESIGNATION_RETIREMENT_TYPE' AND [Name] = 'RETIREMENT')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('RESIGNATION_RETIREMENT_TYPE', 'RETIREMENT', 'Jubilación', 1);
GO

-- ============================================================
-- Documento de renuncia/jubilacion: generar plantilla -> descargar -> el
-- empleado sube el PDF firmado (reutiliza TBL_StoredFile + DirectoryCode
-- HR_RESIGNATION_RETIREMENT ya sembrado) -- RRHH lo revisa en su pantalla.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_ResignationRetirementRequests]') AND name = 'GeneratedDocumentID'
)
    ALTER TABLE [HR].[tbl_ResignationRetirementRequests] ADD [GeneratedDocumentID] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResignationRetirementRequests_GeneratedDocument')
ALTER TABLE [HR].[tbl_ResignationRetirementRequests]
    ADD CONSTRAINT [FK_ResignationRetirementRequests_GeneratedDocument]
    FOREIGN KEY ([GeneratedDocumentID]) REFERENCES [HR].[tbl_GeneratedDocuments] ([DocumentID]);
GO

-- CHK_GeneratedDocuments_EntityType (ver 11_employee_self_service.sql -- ya se amplio una vez
-- para 'CERTIFICATE') necesita tambien 'RESIGNATIONRETIREMENT' para el nuevo
-- DocumentEntityType.ResignationRetirement.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_GeneratedDocuments_EntityType' AND definition NOT LIKE '%RESIGNATIONRETIREMENT%'
)
    ALTER TABLE [HR].[tbl_GeneratedDocuments] DROP CONSTRAINT [CHK_GeneratedDocuments_EntityType];
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_GeneratedDocuments_EntityType')
    ALTER TABLE [HR].[tbl_GeneratedDocuments] WITH CHECK
        ADD CONSTRAINT [CHK_GeneratedDocuments_EntityType]
        CHECK ([EntityType]='OFICIO' OR [EntityType]='AGREEMENT' OR [EntityType]='PERSONNELACTION'
               OR [EntityType]='CONTRACT' OR [EntityType]='CERTIFICATE' OR [EntityType]='RESIGNATIONRETIREMENT');
GO

-- Plantilla generica: sirve tanto para Renuncia como para Jubilacion. REQUEST_TYPE_LABEL,
-- REASON, PROPOSED_EXIT_DATE y JOB_DESCRIPTION/DEPARTMENT_NAME son MANUAL (calculados en
-- ResignationRetirementService, mismo patron que EmployeeCertificateService).
IF NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateCode = 'CARTA_RENUNCIA_JUBILACION' AND Status = 'PUBLISHED')
BEGIN
    DECLARE @RrTemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType, TemplateCode, Name, Version, LayoutType, Status, HtmlContent, CreatedAt
    )
    VALUES (
        'CARTA_RENUNCIA_JUBILACION',
        'CARTA_RENUNCIA_JUBILACION',
        'Carta de Renuncia / Jubilación',
        '1.0',
        'FLOW_TEXT',
        'PUBLISHED',
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color:#000; width:210mm; margin:0 auto; padding:25mm; }
.header { display:flex; align-items:center; gap:10mm; border-bottom:2px solid #8B1A1A; padding-bottom:6mm; margin-bottom:10mm; }
.header img { height:20mm; }
.header .titles { text-align:center; flex:1; font-weight:bold; font-size:11pt; }
.doc-title { text-align:center; font-size:14pt; font-weight:bold; margin:8mm 0; text-decoration:underline; }
.body-text { line-height:1.9; text-align:justify; margin-bottom:8mm; }
.signature { margin-top:30mm; text-align:center; }
.signature .line { border-top:1px solid #000; width:70mm; margin:0 auto 2mm auto; }
.footer-date { margin-top:10mm; font-size:10pt; }
</style>
</head>
<body>
<div class="header">
<img src="{{LOGO_URL}}" alt="UTA"/>
<div class="titles">UNIVERSIDAD TÉCNICA DE AMBATO<br/>DIRECCIÓN DE TALENTO HUMANO</div>
</div>
<div class="doc-title">{{REQUEST_TYPE_LABEL}}</div>
<div class="body-text">Ambato, {{SYSTEM_DATE}}</div>
<div class="body-text">
Señores<br/>
DIRECCIÓN DE TALENTO HUMANO<br/>
Universidad Técnica de Ambato<br/>
Presente.-
</div>
<div class="body-text">
Yo, <b>{{EMPLOYEE_FULLNAME}}</b>, portador/a de la cédula de ciudadanía No. {{EMPLOYEE_IDCARD}},
quien desempeña el cargo de <b>{{JOB_DESCRIPTION}}</b> en la dependencia <b>{{DEPARTMENT_NAME}}</b>,
por medio de la presente presento mi {{REQUEST_TYPE_LABEL_LOWER}} a la institución, a partir del
<b>{{PROPOSED_EXIT_DATE}}</b>, por el siguiente motivo:
</div>
<div class="body-text">{{REASON}}</div>
<div class="body-text">
Sin otro particular, agradezco la atención brindada.
</div>
<div class="signature">
<div class="line"></div>
<div>{{EMPLOYEE_FULLNAME}}</div>
<div>C.C. {{EMPLOYEE_IDCARD}}</div>
</div>
</body>
</html>',
        GETDATE()
    );

    SET @RrTemplateID = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, IsRequired, SortOrder)
    VALUES
        (@RrTemplateID, 'LOGO_URL', 'Logo institucional', 'SYSTEM', 0, 1),
        (@RrTemplateID, 'SYSTEM_DATE', 'Fecha', 'SYSTEM', 0, 2),
        (@RrTemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo', 'EMPLOYEE', 1, 3),
        (@RrTemplateID, 'EMPLOYEE_IDCARD', 'Cédula', 'EMPLOYEE', 1, 4),
        (@RrTemplateID, 'JOB_DESCRIPTION', 'Cargo', 'MANUAL', 0, 5),
        (@RrTemplateID, 'DEPARTMENT_NAME', 'Dependencia', 'MANUAL', 0, 6),
        (@RrTemplateID, 'REQUEST_TYPE_LABEL', 'Tipo de solicitud', 'MANUAL', 1, 7),
        (@RrTemplateID, 'REQUEST_TYPE_LABEL_LOWER', 'Tipo de solicitud (minúscula)', 'MANUAL', 1, 8),
        (@RrTemplateID, 'PROPOSED_EXIT_DATE', 'Fecha propuesta de salida', 'MANUAL', 1, 9),
        (@RrTemplateID, 'REASON', 'Motivo', 'MANUAL', 0, 10);
END
GO

-- ============================================================
-- Elegibilidad de jubilacion (edad minima / anios de servicio minimos).
-- Parametrizado en HR.tbl_Parameters para que RRHH pueda ajustar los umbrales
-- sin requerir cambios de codigo. Se cumple con edad O anios de servicio
-- (cualquiera de las dos condiciones, no ambas). Ver ResignationRetirementService.
-- Es solo informativo/de advertencia -- no bloquea la creacion de la solicitud.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [HR].[tbl_Parameters] WHERE [name] = 'RETIREMENT_MIN_AGE')
INSERT INTO [HR].[tbl_Parameters] ([name], [Pvalues], [Description], [DataType], [IsActive])
VALUES ('RETIREMENT_MIN_AGE', '65', 'EDAD MINIMA PARA JUBILACION', 'NUMERO', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[tbl_Parameters] WHERE [name] = 'RETIREMENT_MIN_SERVICE_YEARS')
INSERT INTO [HR].[tbl_Parameters] ([name], [Pvalues], [Description], [DataType], [IsActive])
VALUES ('RETIREMENT_MIN_SERVICE_YEARS', '30', 'ANIOS DE SERVICIO MINIMOS PARA JUBILACION', 'NUMERO', 1);
GO

-- ============================================================
-- SPLIT (2026-07-16): plantillas separadas de carta de Renuncia y de
-- Jubilacion. Antes se usaba una sola plantilla generica
-- 'CARTA_RENUNCIA_JUBILACION' (arriba) que cambiaba su texto via
-- REQUEST_TYPE_LABEL/REQUEST_TYPE_LABEL_LOWER. Por pedido explicito de
-- negocio, ahora se usan dos plantillas independientes: 'CARTA_RENUNCIA'
-- y 'CARTA_JUBILACION', cada una con su propio texto fijo.
-- ResignationRetirementService.GenerateDocumentAsync elige el
-- TemplateCode segun RequestType.
--
-- La plantilla vieja 'CARTA_RENUNCIA_JUBILACION' NO se borra ni se
-- desactiva -- queda en la tabla por si algun documento ya generado la
-- referencia (auditoria), simplemente deja de usarse para nuevas
-- generaciones. Solo datos (catalogo), sin cambios de esquema.
-- ============================================================

-- ------------------------------------------------------------
-- CARTA_RENUNCIA
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateCode = 'CARTA_RENUNCIA' AND Status = 'PUBLISHED')
BEGIN
    DECLARE @ResignTemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType, TemplateCode, Name, Version, LayoutType, Status, HtmlContent, CreatedAt
    )
    VALUES (
        'CARTA_RENUNCIA',
        'CARTA_RENUNCIA',
        'Carta de Renuncia',
        '1.0',
        'FLOW_TEXT',
        'PUBLISHED',
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color:#000; width:210mm; margin:0 auto; padding:25mm; }
.header { display:flex; align-items:center; gap:10mm; border-bottom:2px solid #8B1A1A; padding-bottom:6mm; margin-bottom:10mm; }
.header img { height:20mm; }
.header .titles { text-align:center; flex:1; font-weight:bold; font-size:11pt; }
.doc-title { text-align:center; font-size:14pt; font-weight:bold; margin:8mm 0; text-decoration:underline; }
.body-text { line-height:1.9; text-align:justify; margin-bottom:8mm; }
.signature { margin-top:30mm; text-align:center; }
.signature .line { border-top:1px solid #000; width:70mm; margin:0 auto 2mm auto; }
.footer-date { margin-top:10mm; font-size:10pt; }
</style>
</head>
<body>
<div class="header">
<img src="{{LOGO_URL}}" alt="UTA"/>
<div class="titles">UNIVERSIDAD TÉCNICA DE AMBATO<br/>DIRECCIÓN DE TALENTO HUMANO</div>
</div>
<div class="doc-title">SOLICITUD DE RENUNCIA</div>
<div class="body-text">Ambato, {{SYSTEM_DATE}}</div>
<div class="body-text">
Señores<br/>
DIRECCIÓN DE TALENTO HUMANO<br/>
Universidad Técnica de Ambato<br/>
Presente.-
</div>
<div class="body-text">
Yo, <b>{{EMPLOYEE_FULLNAME}}</b>, portador/a de la cédula de ciudadanía No. {{EMPLOYEE_IDCARD}},
quien desempeña el cargo de <b>{{JOB_DESCRIPTION}}</b> en la dependencia <b>{{DEPARTMENT_NAME}}</b>,
por medio de la presente presento mi renuncia irrevocable a la institución, a partir del
<b>{{PROPOSED_EXIT_DATE}}</b>, por el siguiente motivo:
</div>
<div class="body-text">{{REASON}}</div>
<div class="body-text">
Sin otro particular, agradezco la atención brindada.
</div>
<div class="signature">
<div class="line"></div>
<div>{{EMPLOYEE_FULLNAME}}</div>
<div>C.C. {{EMPLOYEE_IDCARD}}</div>
</div>
</body>
</html>',
        GETDATE()
    );

    SET @ResignTemplateID = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, IsRequired, SortOrder)
    VALUES
        (@ResignTemplateID, 'LOGO_URL', 'Logo institucional', 'SYSTEM', 0, 1),
        (@ResignTemplateID, 'SYSTEM_DATE', 'Fecha', 'SYSTEM', 0, 2),
        (@ResignTemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo', 'EMPLOYEE', 1, 3),
        (@ResignTemplateID, 'EMPLOYEE_IDCARD', 'Cédula', 'EMPLOYEE', 1, 4),
        (@ResignTemplateID, 'JOB_DESCRIPTION', 'Cargo', 'MANUAL', 0, 5),
        (@ResignTemplateID, 'DEPARTMENT_NAME', 'Dependencia', 'MANUAL', 0, 6),
        (@ResignTemplateID, 'PROPOSED_EXIT_DATE', 'Fecha propuesta de salida', 'MANUAL', 1, 7),
        (@ResignTemplateID, 'REASON', 'Motivo', 'MANUAL', 0, 8);
END
GO

-- ------------------------------------------------------------
-- CARTA_JUBILACION
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateCode = 'CARTA_JUBILACION' AND Status = 'PUBLISHED')
BEGIN
    DECLARE @RetireTemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType, TemplateCode, Name, Version, LayoutType, Status, HtmlContent, CreatedAt
    )
    VALUES (
        'CARTA_JUBILACION',
        'CARTA_JUBILACION',
        'Carta de Jubilación',
        '1.0',
        'FLOW_TEXT',
        'PUBLISHED',
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color:#000; width:210mm; margin:0 auto; padding:25mm; }
.header { display:flex; align-items:center; gap:10mm; border-bottom:2px solid #8B1A1A; padding-bottom:6mm; margin-bottom:10mm; }
.header img { height:20mm; }
.header .titles { text-align:center; flex:1; font-weight:bold; font-size:11pt; }
.doc-title { text-align:center; font-size:14pt; font-weight:bold; margin:8mm 0; text-decoration:underline; }
.body-text { line-height:1.9; text-align:justify; margin-bottom:8mm; }
.signature { margin-top:30mm; text-align:center; }
.signature .line { border-top:1px solid #000; width:70mm; margin:0 auto 2mm auto; }
.footer-date { margin-top:10mm; font-size:10pt; }
</style>
</head>
<body>
<div class="header">
<img src="{{LOGO_URL}}" alt="UTA"/>
<div class="titles">UNIVERSIDAD TÉCNICA DE AMBATO<br/>DIRECCIÓN DE TALENTO HUMANO</div>
</div>
<div class="doc-title">SOLICITUD DE JUBILACIÓN</div>
<div class="body-text">Ambato, {{SYSTEM_DATE}}</div>
<div class="body-text">
Señores<br/>
DIRECCIÓN DE TALENTO HUMANO<br/>
Universidad Técnica de Ambato<br/>
Presente.-
</div>
<div class="body-text">
Yo, <b>{{EMPLOYEE_FULLNAME}}</b>, portador/a de la cédula de ciudadanía No. {{EMPLOYEE_IDCARD}},
quien desempeña el cargo de <b>{{JOB_DESCRIPTION}}</b> en la dependencia <b>{{DEPARTMENT_NAME}}</b>,
por medio de la presente comunico mi decisión de acogerme a mi jubilación, solicitando se dé
por terminada mi relación laboral con la institución a partir del <b>{{PROPOSED_EXIT_DATE}}</b>,
por el siguiente motivo:
</div>
<div class="body-text">{{REASON}}</div>
<div class="body-text">
Sin otro particular, agradezco la atención brindada.
</div>
<div class="signature">
<div class="line"></div>
<div>{{EMPLOYEE_FULLNAME}}</div>
<div>C.C. {{EMPLOYEE_IDCARD}}</div>
</div>
</body>
</html>',
        GETDATE()
    );

    SET @RetireTemplateID = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, IsRequired, SortOrder)
    VALUES
        (@RetireTemplateID, 'LOGO_URL', 'Logo institucional', 'SYSTEM', 0, 1),
        (@RetireTemplateID, 'SYSTEM_DATE', 'Fecha', 'SYSTEM', 0, 2),
        (@RetireTemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo', 'EMPLOYEE', 1, 3),
        (@RetireTemplateID, 'EMPLOYEE_IDCARD', 'Cédula', 'EMPLOYEE', 1, 4),
        (@RetireTemplateID, 'JOB_DESCRIPTION', 'Cargo', 'MANUAL', 0, 5),
        (@RetireTemplateID, 'DEPARTMENT_NAME', 'Dependencia', 'MANUAL', 0, 6),
        (@RetireTemplateID, 'PROPOSED_EXIT_DATE', 'Fecha propuesta de salida', 'MANUAL', 1, 7),
        (@RetireTemplateID, 'REASON', 'Motivo', 'MANUAL', 0, 8);
END
GO

-- ============================================================
-- SEED: tipo de acción de personal "Renuncia o Jubilación"
-- (RENUNCIA_JUBILACION) y transición de contrato VIGENTE→RENUNCIA.
-- Cierra el flujo de aprobación de Renuncia/Jubilación: al subir el
-- documento firmado, PersonnelActionService.UploadSignedDocumentAsync
-- ya dispara RequiresAdUserDisable=1 (bloquea la cuenta institucional
-- vía RepositoryUta) y, con este seed, también el nuevo efecto
-- colateral que cierra el contrato vigente a RENUNCIA (solo si la
-- acción tiene ContractId).
--
-- Reutiliza el mismo DefaultTemplateId=1 (formulario institucional
-- "Acción de Personal") que ya usan los otros 10 tipos existentes —
-- no se crea una plantilla nueva, evita el riesgo de placeholders
-- incompatibles de una plantilla ajena.
--
-- Solo datos (catálogo), sin cambios de esquema. Idempotente.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type WHERE Code = 'RENUNCIA_JUBILACION')
INSERT INTO HR.tbl_personnel_action_type
    (Name, Code, Description, NumberingPrefix, NumberingYear, NumberingLastSequence,
     DefaultTemplateId, IsActive, ActionCategory, ReachesVigente,
     RequiresAdUserCreation, RequiresAdUserDisable, RequiresAdGroupAssignment)
SELECT
    N'Renuncia o Jubilación', N'RENUNCIA_JUBILACION',
    N'Desvinculación del empleado por renuncia o jubilación. Al cargar el documento firmado, deshabilita la cuenta institucional y, si la acción tiene un contrato asociado, lo transiciona a RENUNCIA.',
    N'REN', DATEPART(YEAR, GETDATE()), 0,
    dt.TemplateID, 1, N'EXIT', 0,
    0, 1, 0
FROM HR.tbl_DocumentTemplates dt
WHERE dt.TemplateID = 1;
GO

-- ------------------------------------------------------------
-- Transición de contrato: VIGENTE → RENUNCIA (resuelto por nombre
-- contra ref_Types, no por ID fijo — los TypeID varían entre entornos).
-- ------------------------------------------------------------
INSERT INTO HR.tbl_contract_status_transitions (FromStatusTypeID, ToStatusTypeID, IsActive)
SELECT vig.TypeID, ren.TypeID, 1
FROM HR.ref_Types vig
CROSS JOIN HR.ref_Types ren
WHERE vig.Category = 'CONTRACT_STATUS' AND vig.Name = 'VIGENTE'
  AND ren.Category = 'CONTRACT_STATUS' AND ren.Name = 'RENUNCIA'
  AND NOT EXISTS (
    SELECT 1 FROM HR.tbl_contract_status_transitions t
    WHERE t.FromStatusTypeID = vig.TypeID AND t.ToStatusTypeID = ren.TypeID
  );
GO
