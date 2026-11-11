-- ============================================================================
-- 39_personnel_action_leave_series.sql
-- Licencias con series por prefijo (tipo padre/hijos), motivo de licencia, bloque de notificación y firma del servidor
-- en la Acción de Personal, y tipo de acción en negrilla en el documento (plantilla 20).
--
-- 1) tbl_personnel_action_type: ParentTypeId (un solo nivel), NumberingFormat (NULL = formato actual "{prefijo}-{año}-{secuencia}")
--    y NumberingResetsYearly (1 = comportamiento actual). Los tipos existentes no cambian.
-- 2) Hijos de "Licencia" = series del legado (LPE, MAT, PAT, CRN, LCR, LSR): prefijo propio, numeración acumulada con el formato
--    del legado "DTH-UTA-{PREFIX}-{SEQ}-{YEAR}". Se crean INACTIVOS: se activan al corte con el legado, tras sincronizar la semilla
--    (NumberingLastSequence = último número del legado; se calculó en 12.3 el 2026-11-11 como el mayor entre secuence_acc, el número del
--    código y el registro secuence_). Heredan del padre plantilla, categoría y atributos SIIES.
-- 3) tbl_PersonnelActions: LeaveReasonId (motivo, ref_Types) y bloque de notificación/firma (5 columnas) con restricciones de integridad.
-- 4) ref_Types: categoría PERSONNEL_ACTION_LEAVE_REASON (motivos de LCR/LCRA/LSR; "types" en Metadata indica a qué series aplica).
-- 5) Plantilla 20: etiqueta de cada casilla del tipo de acción en negrilla cuando está marcada (clase .lbl.X); los campos de notificación y
--    firma pasan de SYSTEM a MANUAL (los valores salen de la acción, no de la hora de generación).
-- Idempotente. Reversión: ver el final del archivo.
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) tbl_personnel_action_type ---------------------------------------------------
IF COL_LENGTH('HR.tbl_personnel_action_type', 'ParentTypeId') IS NULL
    ALTER TABLE HR.tbl_personnel_action_type ADD ParentTypeId INT NULL;
IF COL_LENGTH('HR.tbl_personnel_action_type', 'NumberingFormat') IS NULL
    ALTER TABLE HR.tbl_personnel_action_type ADD NumberingFormat NVARCHAR(60) NULL;
IF COL_LENGTH('HR.tbl_personnel_action_type', 'NumberingResetsYearly') IS NULL
    ALTER TABLE HR.tbl_personnel_action_type ADD NumberingResetsYearly BIT NOT NULL
        CONSTRAINT DF_personnel_action_type_NumberingResetsYearly DEFAULT (1);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_personnel_action_type_Parent')
    ALTER TABLE HR.tbl_personnel_action_type ADD CONSTRAINT FK_personnel_action_type_Parent
        FOREIGN KEY (ParentTypeId) REFERENCES HR.tbl_personnel_action_type (PersonnelActionTypeId);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_personnel_action_type_NotSelfParent')
    ALTER TABLE HR.tbl_personnel_action_type ADD CONSTRAINT CK_personnel_action_type_NotSelfParent
        CHECK (ParentTypeId IS NULL OR ParentTypeId <> PersonnelActionTypeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_personnel_action_type_ParentTypeId' AND object_id = OBJECT_ID('HR.tbl_personnel_action_type'))
    CREATE INDEX IX_personnel_action_type_ParentTypeId ON HR.tbl_personnel_action_type (ParentTypeId) WHERE ParentTypeId IS NOT NULL;
GO

-- 2) Hijos de Licencia (series del legado) ---------------------------------------
-- Semillas = último número emitido por la serie en el legado (12.3) al 2026-11-11. Re-sincronizar al corte.
-- La serie LCRA (con remuneración, "alcance") no se crea: solo duplicaba a LCR; sus acciones antiguas pasan a LCR en el relleno histórico.
DECLARE @parentId INT = (SELECT PersonnelActionTypeId FROM HR.tbl_personnel_action_type WHERE Code = N'LICENCIA');
IF @parentId IS NULL THROW 50001, 'No existe el tipo de acción padre LICENCIA.', 1;

INSERT INTO HR.tbl_personnel_action_type
    (Name, Code, Description, NumberingPrefix, NumberingYear, NumberingLastSequence, DefaultTemplateId, IsActive, CreatedAt,
     RequiresAdUserCreation, RequiresAdUserDisable, RequiresAdGroupAssignment, ActionCategory, ReachesVigente, SiiesRelacionIesTypeId,
     ParentTypeId, NumberingFormat, NumberingResetsYearly)
SELECT v.Name, v.Code, v.Descr, v.Prefix, YEAR(SYSDATETIME()), v.Seed, p.DefaultTemplateId, 0, SYSDATETIME(),
       p.RequiresAdUserCreation, p.RequiresAdUserDisable, p.RequiresAdGroupAssignment, p.ActionCategory, p.ReachesVigente, p.SiiesRelacionIesTypeId,
       p.PersonnelActionTypeId, N'DTH-UTA-{PREFIX}-{SEQ}-{YEAR}', 0
FROM (VALUES
    (N'Licencia por enfermedad',                         N'LICENCIA_ENFERMEDAD',          N'Serie LPE del legado.',  N'LPE', 2056),
    (N'Licencia por maternidad',                         N'LICENCIA_MATERNIDAD',          N'Serie MAT del legado.',  N'MAT',  565),
    (N'Licencia por paternidad',                         N'LICENCIA_PATERNIDAD',          N'Serie PAT del legado.',  N'PAT',  335),
    (N'Licencia para el cuidado del recién nacido',      N'LICENCIA_RECIEN_NACIDO',       N'Serie CRN del legado.',  N'CRN',  453),
    (N'Licencia con remuneración',                       N'LICENCIA_CON_REMUNERACION',    N'Serie LCR del legado; requiere motivo.',  N'LCR',  565),
    (N'Licencia sin remuneración',                       N'LICENCIA_SIN_REMUNERACION',    N'Serie LSR del legado; requiere motivo.',  N'LSR',  447)
) v (Name, Code, Descr, Prefix, Seed)
CROSS JOIN HR.tbl_personnel_action_type p
WHERE p.PersonnelActionTypeId = @parentId
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type x WHERE x.Code = v.Code);
GO

-- 3) tbl_PersonnelActions: motivo y bloque de notificación / firma del servidor ---
IF COL_LENGTH('HR.tbl_PersonnelActions', 'LeaveReasonId') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD LeaveReasonId INT NULL;
IF COL_LENGTH('HR.tbl_PersonnelActions', 'NotificationEnabled') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD NotificationEnabled BIT NOT NULL
        CONSTRAINT DF_PersonnelActions_NotificationEnabled DEFAULT (0);
IF COL_LENGTH('HR.tbl_PersonnelActions', 'NotificationDate') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD NotificationDate DATE NULL;
IF COL_LENGTH('HR.tbl_PersonnelActions', 'NotificationTime') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD NotificationTime TIME(0) NULL;
IF COL_LENGTH('HR.tbl_PersonnelActions', 'EmployeeSignatureDate') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD EmployeeSignatureDate DATE NULL;
IF COL_LENGTH('HR.tbl_PersonnelActions', 'EmployeeSignatureTime') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD EmployeeSignatureTime TIME(0) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PersonnelActions_LeaveReason')
    ALTER TABLE HR.tbl_PersonnelActions ADD CONSTRAINT FK_PersonnelActions_LeaveReason
        FOREIGN KEY (LeaveReasonId) REFERENCES HR.ref_Types (TypeID);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PersonnelActions_NotificationPair')
    ALTER TABLE HR.tbl_PersonnelActions ADD CONSTRAINT CK_PersonnelActions_NotificationPair
        CHECK ((NotificationDate IS NULL AND NotificationTime IS NULL) OR (NotificationDate IS NOT NULL AND NotificationTime IS NOT NULL));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PersonnelActions_SignaturePair')
    ALTER TABLE HR.tbl_PersonnelActions ADD CONSTRAINT CK_PersonnelActions_SignaturePair
        CHECK ((EmployeeSignatureDate IS NULL AND EmployeeSignatureTime IS NULL) OR (EmployeeSignatureDate IS NOT NULL AND EmployeeSignatureTime IS NOT NULL));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PersonnelActions_NotificationOff')
    ALTER TABLE HR.tbl_PersonnelActions ADD CONSTRAINT CK_PersonnelActions_NotificationOff
        CHECK (NotificationEnabled = 1 OR (NotificationDate IS NULL AND NotificationTime IS NULL
                                           AND EmployeeSignatureDate IS NULL AND EmployeeSignatureTime IS NULL));
GO

-- 4) Catálogo de motivos de licencia (ref_Types) ---------------------------------
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder, Metadata)
SELECT N'PERSONNEL_ACTION_LEAVE_REASON', v.Name, v.Descr, 1, SYSDATETIME(), v.SortOrder, v.Meta
FROM (VALUES
    (N'Calamidad doméstica',                                 N'Licencia con remuneración (LOSEP art. 27; a validar con Talento Humano).', 10, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Matrimonio o unión de hecho',                         N'Licencia con remuneración.', 20, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Fallecimiento de familiar',                           N'Licencia con remuneración.', 30, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Cuidado de familiar con enfermedad grave',            N'Licencia con remuneración.', 40, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Estudios regulares o capacitación',                   N'Licencia con remuneración.', 50, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Otra causa (con remuneración)',                       N'Licencia con remuneración; detallar en observaciones.', 90, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Asuntos particulares',                                N'Licencia sin remuneración (LOSEP art. 28; a validar con Talento Humano).', 110, N'{"types":["LSR"],"remuneration":"WITHOUT"}'),
    (N'Estudios regulares (sin remuneración)',               N'Licencia sin remuneración.', 120, N'{"types":["LSR"],"remuneration":"WITHOUT"}'),
    (N'Cuidado de familiar (sin remuneración)',              N'Licencia sin remuneración.', 130, N'{"types":["LSR"],"remuneration":"WITHOUT"}'),
    (N'Cargo de elección popular o servicio militar',        N'Licencia sin remuneración.', 140, N'{"types":["LSR"],"remuneration":"WITHOUT"}'),
    (N'Otra causa (sin remuneración)',                       N'Licencia sin remuneración; detallar en observaciones.', 190, N'{"types":["LSR"],"remuneration":"WITHOUT"}')
) v (Name, Descr, SortOrder, Meta)
WHERE NOT EXISTS (SELECT 1 FROM HR.ref_Types r WHERE r.Category = N'PERSONNEL_ACTION_LEAVE_REASON' AND r.Name = v.Name);
GO

-- 5) Plantilla 20: tipo de acción en negrilla y campos de notificación como MANUAL -
DECLARE @h NVARCHAR(MAX) = (SELECT HtmlContent FROM HR.tbl_DocumentTemplates WHERE TemplateID = 20);
IF @h IS NOT NULL AND @h NOT LIKE N'%class="lbl %'
BEGIN
    SET @h = REPLACE(@h, N'<span class="cb {{CB_INGRESO}}">{{CB_INGRESO}}</span>INGRESO</span>', N'<span class="cb {{CB_INGRESO}}">{{CB_INGRESO}}</span><span class="lbl {{CB_INGRESO}}">INGRESO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_REINGRESO}}">{{CB_REINGRESO}}</span>REINGRESO</span>', N'<span class="cb {{CB_REINGRESO}}">{{CB_REINGRESO}}</span><span class="lbl {{CB_REINGRESO}}">REINGRESO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_RESTITUCION}}">{{CB_RESTITUCION}}</span>RESTITUCIÓN</span>', N'<span class="cb {{CB_RESTITUCION}}">{{CB_RESTITUCION}}</span><span class="lbl {{CB_RESTITUCION}}">RESTITUCIÓN</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_REINGRESO2}}">{{CB_REINGRESO2}}</span>REINGRESO</span>', N'<span class="cb {{CB_REINGRESO2}}">{{CB_REINGRESO2}}</span><span class="lbl {{CB_REINGRESO2}}">REINGRESO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_ASCENSO}}">{{CB_ASCENSO}}</span>ASCENSO</span>', N'<span class="cb {{CB_ASCENSO}}">{{CB_ASCENSO}}</span><span class="lbl {{CB_ASCENSO}}">ASCENSO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_TRASLADO}}">{{CB_TRASLADO}}</span>TRASLADO</span>', N'<span class="cb {{CB_TRASLADO}}">{{CB_TRASLADO}}</span><span class="lbl {{CB_TRASLADO}}">TRASLADO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_TRASPASO}}">{{CB_TRASPASO}}</span>TRASPASO</span>', N'<span class="cb {{CB_TRASPASO}}">{{CB_TRASPASO}}</span><span class="lbl {{CB_TRASPASO}}">TRASPASO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_CAMBIO_ADMIN}}">{{CB_CAMBIO_ADMIN}}</span>CAMBIO ADMINISTRATIVO</span>', N'<span class="cb {{CB_CAMBIO_ADMIN}}">{{CB_CAMBIO_ADMIN}}</span><span class="lbl {{CB_CAMBIO_ADMIN}}">CAMBIO ADMINISTRATIVO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_INTERCAMBIO}}">{{CB_INTERCAMBIO}}</span>INTERCAMBIO VOLUNTARIO</span>', N'<span class="cb {{CB_INTERCAMBIO}}">{{CB_INTERCAMBIO}}</span><span class="lbl {{CB_INTERCAMBIO}}">INTERCAMBIO VOLUNTARIO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_LICENCIA}}">{{CB_LICENCIA}}</span>LICENCIA</span>', N'<span class="cb {{CB_LICENCIA}}">{{CB_LICENCIA}}</span><span class="lbl {{CB_LICENCIA}}">LICENCIA</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_COMISION}}">{{CB_COMISION}}</span>COMISIÓN DE SERVICIOS</span>', N'<span class="cb {{CB_COMISION}}">{{CB_COMISION}}</span><span class="lbl {{CB_COMISION}}">COMISIÓN DE SERVICIOS</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_SANCIONES}}">{{CB_SANCIONES}}</span>SANCIONES</span>', N'<span class="cb {{CB_SANCIONES}}">{{CB_SANCIONES}}</span><span class="lbl {{CB_SANCIONES}}">SANCIONES</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_INCREMENTO_RMU}}">{{CB_INCREMENTO_RMU}}</span>INCREMENTO RMU</span>', N'<span class="cb {{CB_INCREMENTO_RMU}}">{{CB_INCREMENTO_RMU}}</span><span class="lbl {{CB_INCREMENTO_RMU}}">INCREMENTO RMU</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_SUBROGACION}}">{{CB_SUBROGACION}}</span>SUBROGACIÓN</span>', N'<span class="cb {{CB_SUBROGACION}}">{{CB_SUBROGACION}}</span><span class="lbl {{CB_SUBROGACION}}">SUBROGACIÓN</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_ENCARGO}}">{{CB_ENCARGO}}</span>ENCARGO</span>', N'<span class="cb {{CB_ENCARGO}}">{{CB_ENCARGO}}</span><span class="lbl {{CB_ENCARGO}}">ENCARGO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_CESACION}}">{{CB_CESACION}}</span>CESACIÓN DE FUNCIONES</span>', N'<span class="cb {{CB_CESACION}}">{{CB_CESACION}}</span><span class="lbl {{CB_CESACION}}">CESACIÓN DE FUNCIONES</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_DESTITUCION}}">{{CB_DESTITUCION}}</span>DESTITUCIÓN</span>', N'<span class="cb {{CB_DESTITUCION}}">{{CB_DESTITUCION}}</span><span class="lbl {{CB_DESTITUCION}}">DESTITUCIÓN</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_VACACIONES}}">{{CB_VACACIONES}}</span>VACACIONES</span>', N'<span class="cb {{CB_VACACIONES}}">{{CB_VACACIONES}}</span><span class="lbl {{CB_VACACIONES}}">VACACIONES</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_REVISION_CLASI}}">{{CB_REVISION_CLASI}}</span>REVISIÓN CLASI. PUESTO</span>', N'<span class="cb {{CB_REVISION_CLASI}}">{{CB_REVISION_CLASI}}</span><span class="lbl {{CB_REVISION_CLASI}}">REVISIÓN CLASI. PUESTO</span></span>');
    SET @h = REPLACE(@h, N'<span class="cb {{CB_OTRO}}">{{CB_OTRO}}</span>OTRO (DETALLAR):</span>', N'<span class="cb {{CB_OTRO}}">{{CB_OTRO}}</span><span class="lbl {{CB_OTRO}}">OTRO (DETALLAR):</span></span>');
    -- La clase "X" (valor de la casilla marcada) pone la etiqueta en negrilla; sin marca la clase queda vacía.
    SET @h = REPLACE(@h, N'</style>', N'.lbl.X { font-weight: bold; }' + CHAR(10) + N'</style>');
    UPDATE HR.tbl_DocumentTemplates
       SET HtmlContent = @h, Version = CAST(ISNULL(TRY_CAST(Version AS INT), 1) + 1 AS NVARCHAR(20)), UpdatedAt = SYSDATETIME()
     WHERE TemplateID = 20;
END
GO

UPDATE HR.tbl_DocumentTemplateFields
   SET SourceType = N'MANUAL', SourceProperty = NULL
 WHERE TemplateID = 20 AND SourceType = N'SYSTEM'
   AND FieldName IN (N'EMPLOYEE_SIGNATURE_DATE', N'EMPLOYEE_SIGNATURE_HOUR', N'NOTIFICATION_DATE', N'NOTIFICATION_HOUR');
GO

-- ============================================================================
-- Reversión (solo si ninguna acción usa aún las columnas nuevas):
--   DELETE de los hijos (WHERE ParentTypeId IS NOT NULL) y de los motivos (Category = 'PERSONNEL_ACTION_LEAVE_REASON');
--   DROP de los CK/FK/DF y de las columnas ParentTypeId, NumberingFormat, NumberingResetsYearly (tipo) y LeaveReasonId,
--   NotificationEnabled, NotificationDate, NotificationTime, EmployeeSignatureDate, EmployeeSignatureTime (acción);
--   restaurar HtmlContent de la plantilla 20 desde el respaldo (quitar las etiquetas <span class="lbl ..."> y la regla .lbl.X).
-- ============================================================================
