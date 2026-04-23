-- ================================================================
-- SCRIPT: 13_seed_accion_personal_template.sql
-- PROPÓSITO: Insertar la plantilla oficial "Acción de Personal UTA"
--            en el Motor Documental Institucional.
--
-- TABLAS AFECTADAS:
--   HR.tbl_DocumentTemplates       → 1 registro (plantilla)
--   HR.tbl_DocumentTemplateFields  → 46 registros (campos/placeholders)
--
-- INSTRUCCIONES DE EJECUCIÓN:
--   1. Ejecutar primero 12_document_engine.sql (DDL + seeds de ref_Types)
--   2. Ejecutar este script en la misma base de datos
--   3. Verificar con las consultas de validación al final del script
--
-- AUTOR: Motor Documental UTA - HrBackend
-- FECHA: 2025-04-23
-- ================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- PASO 1: Obtener el DocumentTemplateTypeID para "Acción de Personal"
    -- ============================================================
    -- El seed de ref_Types fue insertado en 12_document_engine.sql
    -- Category = 'DOCUMENT_TEMPLATE_TYPE', Name = 'Acción de Personal'
    DECLARE @ActionTypeID INT;
    SELECT @ActionTypeID = TypeID
    FROM   HR.ref_Types
    WHERE  Category = 'DOCUMENT_TEMPLATE_TYPE'
      AND  Name     = 'Acción de Personal'
      AND  IsActive = 1;

    IF @ActionTypeID IS NULL
    BEGIN
        -- Insertar si no existe (idempotente)
        INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedBy)
        VALUES ('DOCUMENT_TEMPLATE_TYPE', 'Acción de Personal',
                'Plantillas para acciones de personal: traslados, comisiones, nombramientos, etc.',
                1, 20, NULL);
        SET @ActionTypeID = SCOPE_IDENTITY();
    END;

    -- ============================================================
    -- PASO 2: Insertar la plantilla principal
    -- ============================================================
    -- Verificar si ya existe una versión publicada para evitar duplicados
    IF EXISTS (
        SELECT 1 FROM HR.tbl_DocumentTemplates
        WHERE  TemplateCode = 'ACCION_PERSONAL'
          AND  Status       = 'PUBLISHED'
    )
    BEGIN
        -- Archivar la versión anterior antes de insertar la nueva
        UPDATE HR.tbl_DocumentTemplates
        SET    Status    = 'ARCHIVED',
               UpdatedAt = GETDATE()
        WHERE  TemplateCode = 'ACCION_PERSONAL'
          AND  Status       = 'PUBLISHED';
    END;

    DECLARE @TemplateID INT;
    DECLARE @VersionNumber INT;

    SELECT @VersionNumber = ISNULL(MAX(VersionNumber), 0) + 1
    FROM   HR.tbl_DocumentTemplates
    WHERE  TemplateCode = 'ACCION_PERSONAL';

    INSERT INTO HR.tbl_DocumentTemplates (
        DocumentTemplateTypeID,
        TemplateCode,
        Name,
        VersionNumber,
        LayoutType,
        Status,
        BodyTemplate,
        HeaderTemplate,
        FooterTemplate,
        EffectiveFrom,
        EffectiveTo,
        IsActive,
        CreatedAt,
        CreatedBy
    )
    VALUES (
        @ActionTypeID,
        'ACCION_PERSONAL',
        'Acción de Personal - Universidad Técnica de Ambato',
        @VersionNumber,
        'STRUCTURED_FORM',       -- Layout: formulario estructurado con campos fijos y recuadros
        'PUBLISHED',             -- Publicada y lista para generar documentos
        -- BodyTemplate: HTML completo de 2 páginas fiel al formato oficial UTA
        -- (Ver archivo: Database/templates/ACCION_PERSONAL_v1.html)
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<meta name="DOCUMENT_TITLE" content="ACCIÓN DE PERSONAL"/>
<meta name="ACTION_NUMBER" content="{{DOC_NUMBER}}"/>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: Arial, Helvetica, sans-serif; font-size: 8.5pt; color: #000; background: #fff; width: 210mm; margin: 0 auto; }
.page { width: 210mm; min-height: 297mm; padding: 12mm 14mm 10mm 14mm; page-break-after: always; }
.page:last-child { page-break-after: auto; }
table { width: 100%; border-collapse: collapse; }
td, th { border: 1px solid #000; padding: 2px 4px; vertical-align: top; }
.no-border td, .no-border th { border: none; }
.bold { font-weight: bold; }
.center { text-align: center; }
.bg-gray { background-color: #d9d9d9; }
.section-title { font-weight: bold; font-size: 8pt; text-align: center; background-color: #d9d9d9; padding: 2px 4px; }
.situation-label { font-weight: bold; font-size: 7pt; }
.situation-value { font-size: 8pt; }
.responsables-header { font-weight: bold; text-align: center; background-color: #d9d9d9; padding: 2px 4px; font-size: 8pt; }
.firma-cell { min-height: 16mm; vertical-align: bottom; font-size: 7.5pt; padding: 3px 5px; }
.motivation-box { border: 1px solid #000; min-height: 28mm; padding: 3px 5px; font-size: 8pt; line-height: 1.4; margin-top: 1px; }
.situation-header { font-weight: bold; font-size: 8.5pt; text-align: center; background-color: #d9d9d9; border: 1px solid #000; padding: 2px 4px; }
.uso-exclusivo-box { border: 1px solid #000; padding: 5px 8px; margin-top: 4mm; font-size: 7.5pt; line-height: 1.4; }
.uso-title { font-weight: bold; text-align: center; font-size: 8.5pt; border-bottom: 1px dashed #000; padding-bottom: 3px; margin-bottom: 4px; letter-spacing: 2px; }
.uso-separator { border-top: 1px dashed #000; margin: 4px 0; }
.cb { display: inline-block; width: 9px; height: 9px; border: 1px solid #000; margin-right: 3px; vertical-align: middle; }
.mt1 { margin-top: 1mm; } .mt2 { margin-top: 2mm; } .mt3 { margin-top: 3mm; }
</style>
</head>
<body>
<div class="page">
<table>
<tr>
<td style="width:55%;border:1px solid #000;padding:4px 6px;vertical-align:middle">
<table class="no-border" style="width:100%"><tr>
<td style="border:none;width:22mm;vertical-align:middle;padding:0"><div style="width:55px;height:55px;border:2px solid #8B1A1A;border-radius:50%;display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:11pt;color:#8B1A1A;">UTA</div></td>
<td style="border:none;vertical-align:middle;padding-left:6px"><div style="text-align:center;font-weight:bold;font-size:9pt;line-height:1.5">UNIVERSIDAD TÉCNICA DE AMBATO<br/>DIRECCIÓN DE TALENTO HUMANO</div></td>
</tr></table>
</td>
<td style="width:45%;border:1px solid #000;padding:0;vertical-align:top">
<div style="font-size:13pt;font-weight:bold;text-align:center;padding:4px 6px;border-bottom:1px solid #000">ACCIÓN DE PERSONAL</div>
<table style="width:100%;border-collapse:collapse;border:none"><tr>
<td style="border:none;border-right:1px solid #000;padding:2px 5px;font-size:7.5pt;width:18mm">Nro</td>
<td style="border:none;padding:2px 5px;font-size:8pt;font-weight:bold">{{DOC_NUMBER}}</td>
</tr><tr>
<td colspan="2" style="font-weight:bold;font-size:8pt;text-align:center;background-color:#d9d9d9;border-top:1px solid #000;border-bottom:1px solid #000;padding:2px 4px">FECHA DE ELABORACIÓN</td>
</tr><tr>
<td colspan="2" style="text-align:center;font-size:8.5pt;padding:2px 4px">{{ELABORATION_DATE}}</td>
</tr></table>
</td>
</tr>
</table>
<table class="mt1"><tr>
<td style="width:30%" class="section-title">APELLIDOS</td><td style="width:30%">{{EMPLOYEE_LASTNAME}}</td>
<td style="width:20%" class="section-title">NOMBRES</td><td style="width:20%">{{EMPLOYEE_FIRSTNAME}}</td>
</tr><tr>
<td colspan="2"></td><td colspan="2" class="section-title center">RIGE:</td>
</tr><tr>
<td style="font-weight:bold;font-size:7.5pt">DOCUMENTO DE IDENTIFICACIÓN</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">NRO. DE IDENTIFICACIÓN</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">DESDE (dd-mm-aaaa)</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">HASTA (dd-mm-aaaa) (cuando aplica)</td>
</tr><tr>
<td style="text-align:center">{{ID_TYPE}}</td><td style="text-align:center">{{EMPLOYEE_IDCARD}}</td>
<td style="text-align:center">{{EFFECTIVE_FROM}}</td><td style="text-align:center">{{EFFECTIVE_TO}}</td>
</tr></table>
<table class="mt1"><tr><td colspan="4" style="font-size:7.5pt;padding:2px 4px;border-bottom:none">Escoja una opción (según lo estipulado en el artículo 21 del Reglamento General a la Ley Orgánica del Servicio Público):</td></tr></table>
<table>
<tr>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INGRESO}}"></span>INGRESO</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_TRASPASO}}"></span>TRASPASO</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INCREMENTO_RMU}}"></span>INCREMENTO RMU</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REVISION_CLASI}}"></span>REVISIÓN CLASI. PUESTO</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REINGRESO}}"></span>REINGRESO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_CAMBIO_ADMIN}}"></span>CAMBIO ADMINISTRATIVO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_SUBROGACION}}"></span>SUBROGACIÓN</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_OTRO}}"></span>OTRO (DETALLAR): {{ACTION_OTHER_DETAIL}}</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_RESTITUCION}}"></span>RESTITUCIÓN</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INTERCAMBIO}}"></span>INTERCAMBIO VOLUNTARIO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_ENCARGO}}"></span>ENCARGO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt">{{ACTION_ENCARGO_DETAIL}}</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REINGRESO2}}"></span>REINGRESO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_LICENCIA}}"></span>LICENCIA</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_CESACION}}"></span>CESACIÓN DE FUNCIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_ASCENSO}}"></span>ASCENSO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_COMISION}}"></span>COMISIÓN DE SERVICIOS</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_DESTITUCION}}"></span>DESTITUCIÓN</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_TRASLADO}}"></span>TRASLADO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_SANCIONES}}"></span>SANCIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_VACACIONES}}"></span>VACACIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr>
</table>
<table class="mt1"><tr>
<td style="width:70%;font-size:7.5pt;padding:2px 4px">* PRESENTÓ LA DECLARACIÓN JURADA (número 2 del art. 3 RLOSEP)</td>
<td style="width:10%;text-align:center;font-size:7.5pt;font-weight:bold">SI</td>
<td style="width:20%;text-align:center;font-size:7.5pt;font-weight:bold">NO APLICA {{DECLARACION_JURADA_MARK}}</td>
</tr></table>
<table class="mt1"><tr><td style="font-size:7.5pt;font-weight:bold;padding:2px 4px;background-color:#d9d9d9">MOTIVACIÓN: (adjuntar anexo si lo posee)</td></tr>
<tr><td class="motivation-box">{{MOTIVATION_TEXT}}</td></tr></table>
<table class="mt2">
<tr><td style="width:50%" class="situation-header">SITUACIÓN ACTUAL</td><td style="width:50%" class="situation-header">SITUACIÓN PROPUESTA</td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PROCESO INSTITUCIONAL:</div><div class="situation-value">{{CURRENT_INSTITUTIONAL_PROCESS}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PROCESO INSTITUCIONAL:</div><div class="situation-value">{{PROPOSED_INSTITUTIONAL_PROCESS}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">NIVEL DE GESTIÓN:</div><div class="situation-value">{{CURRENT_MANAGEMENT_LEVEL}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">NIVEL DE GESTIÓN:</div><div class="situation-value">{{PROPOSED_MANAGEMENT_LEVEL}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">UNIDAD ADMINISTRATIVA:</div><div class="situation-value">{{CURRENT_ADMIN_UNIT}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">UNIDAD ADMINISTRATIVA:</div><div class="situation-value">{{PROPOSED_ADMIN_UNIT}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">Lugar DE TRABAJO:</div><div class="situation-value">{{CURRENT_WORKPLACE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">Lugar DE TRABAJO:</div><div class="situation-value">{{PROPOSED_WORKPLACE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:9mm"><div class="situation-label">DENOMINACIÓN DEL PUESTO:</div><div class="situation-value">{{CURRENT_JOB_TITLE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:9mm"><div class="situation-label">DENOMINACIÓN DEL PUESTO:</div><div class="situation-value">{{PROPOSED_JOB_TITLE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRUPO OCUPACIONAL:</div><div class="situation-value">{{CURRENT_OCCUPATIONAL_GROUP}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRUPO OCUPACIONAL:</div><div class="situation-value">{{PROPOSED_OCCUPATIONAL_GROUP}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRADO:</div><div class="situation-value">{{CURRENT_GRADE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRADO:</div><div class="situation-value">{{PROPOSED_GRADE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">REMUNERACIÓN MENSUAL:</div><div class="situation-value">{{CURRENT_SALARY}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">REMUNERACIÓN MENSUAL:</div><div class="situation-value">{{PROPOSED_SALARY}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PARTIDA INDIVIDUAL:</div><div class="situation-value">{{CURRENT_BUDGET_CODE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PARTIDA INDIVIDUAL:</div><div class="situation-value">{{PROPOSED_BUDGET_CODE}}</div></td></tr>
</table>
<table class="mt1">
<tr>
<td style="width:55%;border:1px solid #000;padding:3px 5px;font-size:7.5pt;vertical-align:top">
<div class="bold">POSESIÓN DEL PUESTO</div>
<div class="mt1">YO,</div><div>JURO LEALTAD AL ESTADO ECUATORIANO</div>
<div class="mt1">LUGAR: <span style="display:inline-block;width:35mm;border-bottom:1px solid #000">&nbsp;</span>&nbsp;&nbsp;FECHA: <span style="display:inline-block;width:25mm;border-bottom:1px solid #000">&nbsp;</span></div>
<div class="mt1" style="font-size:7pt">** (EN CASO DE GANADOR DE CONCURSO DE MÉRITOS Y OPOSICIÓN)</div>
<div style="min-height:14mm"></div>
<div style="border-top:1px solid #000;width:60%;margin-top:2px"><div class="center" style="font-size:7pt">FIRMA</div></div>
</td>
<td style="width:45%;border:1px solid #000;padding:3px 5px;font-size:7.5pt;vertical-align:top">
<div class="bold">CON NRO. DE DOCUMENTO DE IDENTIFICACIÓN:</div>
<div class="mt1">{{EMPLOYEE_IDCARD}}</div>
<div style="min-height:16mm"></div>
<div style="border-top:1px solid #000;width:70%;margin-top:2px"><div class="center" style="font-size:7pt">SERVIDOR PÚBLICO</div></div>
</td>
</tr>
</table>
<table class="mt1">
<tr><td colspan="3" class="responsables-header">NRO. ACTA FINAL: {{ACTA_NUMBER}} &nbsp;&nbsp; FECHA: {{APPROVAL_DATE}} &nbsp;&nbsp; SERVIDOR PÚBLICO: {{EMPLOYEE_FULLNAME}}</td></tr>
<tr><td colspan="3" class="responsables-header">RESPONSABLES DE APROBACIÓN</td></tr>
<tr>
<td style="width:50%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">DIRECTOR (A) O RESPONSABLE DE TALENTO HUMANO</td>
<td colspan="2" style="width:50%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">AUTORIDAD NOMINADORA O SU DELEGADO</td>
</tr>
<tr>
<td class="firma-cell" style="border:1px solid #000;min-height:18mm;vertical-align:bottom">
<div style="min-height:14mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{DTH_DIRECTOR_NAME}}</div><div>PUESTO: {{DTH_DIRECTOR_TITLE}}</div></div>
</td>
<td colspan="2" class="firma-cell" style="border:1px solid #000;min-height:18mm;vertical-align:bottom">
<div style="min-height:14mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{AUTHORITY_NAME}}</div><div>PUESTO: {{AUTHORITY_TITLE}}</div></div>
</td>
</tr>
</table>
</div>
<div class="page">
<table>
<tr><td colspan="2" class="responsables-header">RESPONSABLES DE FIRMAS</td></tr>
<tr>
<td style="width:55%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">ACEPTACIÓN Y/O RECEPCIÓN DEL SERVIDOR PÚBLICO</td>
<td style="width:45%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">EN CASO DE NEGATIVA DE LA RECEPCIÓN (TESTIGO)</td>
</tr>
<tr>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:22mm;vertical-align:bottom">
<div style="min-height:16mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{EMPLOYEE_FULLNAME}}</div><div>FECHA: {{EMPLOYEE_SIGNATURE_DATE}}</div><div>HORA: {{EMPLOYEE_SIGNATURE_HOUR}}</div></div>
</td>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:22mm;vertical-align:bottom">
<div style="min-height:10mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{WITNESS_NAME}}</div><div>FECHA: {{WITNESS_DATE}}</div>
<div style="font-size:7pt;margin-top:3px">RAZÓN: En presencia del testigo se deja constancia de que la o el servidor público tiene la negativa de recibir la comunicación de registro de esta acción de personal.</div>
</div>
</td>
</tr>
</table>
<table class="mt2">
<tr>
<td style="width:33.33%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9;text-align:center">RESPONSABLE DE ELABORACIÓN</td>
<td style="width:33.33%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9;text-align:center">RESPONSABLE DE REVISIÓN</td>
<td style="width:33.33%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9;text-align:center">RESPONSABLE DE REGISTRO Y CONTROL</td>
</tr>
<tr>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:20mm;vertical-align:bottom"><div style="min-height:14mm"></div><div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{ELABORATOR_NAME}}</div><div>PUESTO: {{ELABORATOR_TITLE}}</div></div></td>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:20mm;vertical-align:bottom"><div style="min-height:14mm"></div><div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{REVIEWER_NAME}}</div><div>PUESTO: {{REVIEWER_TITLE}}</div></div></td>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:20mm;vertical-align:bottom"><div style="min-height:14mm"></div><div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{REGISTRAR_NAME}}</div><div>PUESTO: {{REGISTRAR_TITLE}}</div></div></td>
</tr>
</table>
<div class="uso-exclusivo-box mt3">
<div class="uso-title">* * U S O &nbsp; E X C L U S I V O &nbsp; P A R A &nbsp; T A L E N T O &nbsp; H U M A N O</div>
<div class="uso-separator"></div>
<p style="margin-bottom:4px"><strong>PROTECCIÓN DE DATOS.-</strong> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:</p>
<p style="margin-bottom:2px">• Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</p>
<p style="margin-bottom:2px">• Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.</p>
<p style="margin-bottom:4px">• Generar bases de datos de acceso público.</p>
<p style="margin-bottom:4px">El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.</p>
<div class="uso-separator"></div>
<p style="margin-bottom:4px">REGISTRO DE NOTIFICACIÓN AL SERVIDOR PÚBLICO DE LA ACCIÓN DE PERSONAL (primer inciso del art. 22 RGLOSEP, art. 101 COA, art. 66 y 126 ERJAFE)</p>
<div class="uso-separator"></div>
<p style="margin-bottom:3px"><strong>COMUNICACIÓN ELECTRÓNICA</strong></p>
<table class="no-border" style="width:60%;margin-bottom:4px">
<tr><td style="border:none;padding:1px 4px;font-size:7.5pt;width:30mm">FECHA:</td><td style="border:none;padding:1px 4px;font-size:7.5pt">{{NOTIFICATION_DATE}}</td><td style="border:none;padding:1px 4px;font-size:7.5pt;width:20mm">HORA:</td><td style="border:none;padding:1px 4px;font-size:7.5pt">{{NOTIFICATION_HOUR}}</td></tr>
</table>
<p style="margin-bottom:12mm">* * MEDIO: {{NOTIFICATION_MEDIUM}}</p>
<table class="no-border" style="width:60%;margin:0 auto">
<tr><td style="border:none;border-top:1px solid #000;text-align:center;padding-top:2px;font-size:7.5pt">FIRMA DEL RESPONSABLE QUE NOTIFICÓ</td></tr>
<tr><td style="border:none;text-align:center;font-size:7.5pt">NOMBRE: {{DTH_DIRECTOR_NAME}}</td></tr>
<tr><td style="border:none;text-align:center;font-size:7.5pt">PUESTO: {{DTH_DIRECTOR_TITLE}}</td></tr>
</table>
<p class="mt2" style="font-size:7pt">** Si la comunicación fue electrónica se deberá colocar el medio por el cual se notificó al servidor; así como, el número del documento.</p>
</div>
</div>
</body>
</html>',
        NULL,  -- HeaderTemplate: incluido dentro del BodyTemplate
        NULL,  -- FooterTemplate: incluido dentro del BodyTemplate
        CAST('2025-01-01' AS DATETIME2),
        NULL,  -- Sin fecha de vencimiento
        1,
        GETDATE(),
        NULL
    );

    SET @TemplateID = SCOPE_IDENTITY();

    -- ============================================================
    -- PASO 3: Insertar los 46 campos (placeholders) de la plantilla
    -- ============================================================
    -- Orden de inserción: agrupados por bloque del documento
    -- SourceType valores: SYSTEM | EMPLOYEE | CONTRACT | MANUAL

    INSERT INTO HR.tbl_DocumentTemplateFields
        (DocumentTemplateID, FieldCode, FieldLabel, DataType, IsRequired, SourceType, SourcePath, FormatPattern, SortOrder, CreatedAt, CreatedBy)
    VALUES

    -- ── BLOQUE 1: Encabezado ─────────────────────────────────────────────────
    (@TemplateID, 'DOC_NUMBER',        'Número del documento',          'TEXT',     1, 'MANUAL',   NULL,                          NULL,           10,  GETDATE(), NULL),
    (@TemplateID, 'ELABORATION_DATE',  'Fecha de elaboración',          'DATE',     1, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   20,  GETDATE(), NULL),

    -- ── BLOQUE 2/3: Datos del servidor ───────────────────────────────────────
    (@TemplateID, 'EMPLOYEE_LASTNAME',  'Apellidos del servidor',        'TEXT',     1, 'EMPLOYEE', 'People.LastName',             'UPPERCASE',    30,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_FIRSTNAME', 'Nombres del servidor',          'TEXT',     1, 'EMPLOYEE', 'People.FirstName',            'UPPERCASE',    40,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_FULLNAME',  'Apellidos y nombres completos', 'TEXT',     1, 'EMPLOYEE', 'People.FullName',             'UPPERCASE',    45,  GETDATE(), NULL),
    (@TemplateID, 'ID_TYPE',            'Tipo de documento de identidad','TEXT',     1, 'EMPLOYEE', 'People.IdType',               'UPPERCASE',    50,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_IDCARD',    'Número de identificación',      'TEXT',     1, 'EMPLOYEE', 'People.IdCard',               NULL,           60,  GETDATE(), NULL),
    (@TemplateID, 'EFFECTIVE_FROM',     'Rige desde',                    'DATE',     1, 'MANUAL',   NULL,                          'dd-MM-yyyy',   70,  GETDATE(), NULL),
    (@TemplateID, 'EFFECTIVE_TO',       'Rige hasta (cuando aplica)',    'DATE',     0, 'MANUAL',   NULL,                          'dd-MM-yyyy',   80,  GETDATE(), NULL),

    -- ── BLOQUE 4: Tipo de acción (checkboxes) ────────────────────────────────
    -- Cada checkbox se representa como '' (vacío=sin marcar) o 'checked' (marcado)
    (@TemplateID, 'CB_INGRESO',         'Checkbox: Ingreso',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           90,  GETDATE(), NULL),
    (@TemplateID, 'CB_TRASPASO',        'Checkbox: Traspaso',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           100, GETDATE(), NULL),
    (@TemplateID, 'CB_INCREMENTO_RMU',  'Checkbox: Incremento RMU',      'TEXT',     0, 'MANUAL',   NULL,                          NULL,           110, GETDATE(), NULL),
    (@TemplateID, 'CB_REVISION_CLASI',  'Checkbox: Revisión Clasi. Puesto','TEXT',   0, 'MANUAL',   NULL,                          NULL,           120, GETDATE(), NULL),
    (@TemplateID, 'CB_REINGRESO',       'Checkbox: Reingreso',           'TEXT',     0, 'MANUAL',   NULL,                          NULL,           130, GETDATE(), NULL),
    (@TemplateID, 'CB_CAMBIO_ADMIN',    'Checkbox: Cambio Administrativo','TEXT',    0, 'MANUAL',   NULL,                          NULL,           140, GETDATE(), NULL),
    (@TemplateID, 'CB_SUBROGACION',     'Checkbox: Subrogación',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           150, GETDATE(), NULL),
    (@TemplateID, 'CB_OTRO',            'Checkbox: Otro (detallar)',     'TEXT',     0, 'MANUAL',   NULL,                          NULL,           160, GETDATE(), NULL),
    (@TemplateID, 'ACTION_OTHER_DETAIL','Detalle de otro tipo de acción','TEXT',     0, 'MANUAL',   NULL,                          NULL,           165, GETDATE(), NULL),
    (@TemplateID, 'CB_RESTITUCION',     'Checkbox: Restitución',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           170, GETDATE(), NULL),
    (@TemplateID, 'CB_INTERCAMBIO',     'Checkbox: Intercambio Voluntario','TEXT',   0, 'MANUAL',   NULL,                          NULL,           180, GETDATE(), NULL),
    (@TemplateID, 'CB_ENCARGO',         'Checkbox: Encargo',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           190, GETDATE(), NULL),
    (@TemplateID, 'ACTION_ENCARGO_DETAIL','Detalle del encargo (ej: Nombramiento Provisional A)','TEXT',0,'MANUAL',NULL,           NULL,           195, GETDATE(), NULL),
    (@TemplateID, 'CB_REINGRESO2',      'Checkbox: Reingreso (2da fila)','TEXT',     0, 'MANUAL',   NULL,                          NULL,           200, GETDATE(), NULL),
    (@TemplateID, 'CB_LICENCIA',        'Checkbox: Licencia',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           210, GETDATE(), NULL),
    (@TemplateID, 'CB_CESACION',        'Checkbox: Cesación de Funciones','TEXT',    0, 'MANUAL',   NULL,                          NULL,           220, GETDATE(), NULL),
    (@TemplateID, 'CB_ASCENSO',         'Checkbox: Ascenso',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           230, GETDATE(), NULL),
    (@TemplateID, 'CB_COMISION',        'Checkbox: Comisión de Servicios','TEXT',    0, 'MANUAL',   NULL,                          NULL,           240, GETDATE(), NULL),
    (@TemplateID, 'CB_DESTITUCION',     'Checkbox: Destitución',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           250, GETDATE(), NULL),
    (@TemplateID, 'CB_TRASLADO',        'Checkbox: Traslado',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           260, GETDATE(), NULL),
    (@TemplateID, 'CB_SANCIONES',       'Checkbox: Sanciones',           'TEXT',     0, 'MANUAL',   NULL,                          NULL,           270, GETDATE(), NULL),
    (@TemplateID, 'CB_VACACIONES',      'Checkbox: Vacaciones',          'TEXT',     0, 'MANUAL',   NULL,                          NULL,           280, GETDATE(), NULL),

    -- ── BLOQUE 5: Declaración jurada ─────────────────────────────────────────
    (@TemplateID, 'DECLARACION_JURADA_MARK','Marca declaración jurada (● = NO APLICA)','TEXT',1,'MANUAL',NULL,                    NULL,           290, GETDATE(), NULL),

    -- ── BLOQUE 6: Motivación ─────────────────────────────────────────────────
    (@TemplateID, 'MOTIVATION_TEXT',    'Texto de motivación (resolución)',  'TEXT', 1, 'MANUAL',   NULL,                          NULL,           300, GETDATE(), NULL),

    -- ── BLOQUE 7: Situación Actual (fuente: CONTRACT) ────────────────────────
    (@TemplateID, 'CURRENT_INSTITUTIONAL_PROCESS', 'Proceso institucional actual', 'TEXT', 0, 'CONTRACT', 'Department.InstitutionalProcess', 'UPPERCASE', 310, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_MANAGEMENT_LEVEL',      'Nivel de gestión actual',      'TEXT', 0, 'CONTRACT', 'Department.ManagementLevel',      'UPPERCASE', 320, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_ADMIN_UNIT',            'Unidad administrativa actual', 'TEXT', 0, 'CONTRACT', 'Department.Name',                 'UPPERCASE', 330, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_WORKPLACE',             'Lugar de trabajo actual',      'TEXT', 0, 'CONTRACT', 'Department.Location',             'UPPERCASE', 340, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_JOB_TITLE',             'Denominación del puesto actual','TEXT',0, 'CONTRACT', 'Job.Description',                 'UPPERCASE', 350, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_OCCUPATIONAL_GROUP',    'Grupo ocupacional actual',     'TEXT', 0, 'CONTRACT', 'Job.OccupationalGroup',           'UPPERCASE', 360, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_GRADE',                 'Grado actual',                 'TEXT', 0, 'CONTRACT', 'Job.Grade',                       NULL,        370, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_SALARY',                'Remuneración mensual actual',  'CURRENCY',0,'CONTRACT','Contract.Salary',                'N2',        380, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_BUDGET_CODE',           'Partida individual actual',    'TEXT', 0, 'CONTRACT', 'Department.BudgetCode',           NULL,        390, GETDATE(), NULL),

    -- ── BLOQUE 7: Situación Propuesta (fuente: MANUAL) ───────────────────────
    (@TemplateID, 'PROPOSED_INSTITUTIONAL_PROCESS','Proceso institucional propuesto','TEXT',0,'MANUAL',   NULL,                              'UPPERCASE', 400, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_MANAGEMENT_LEVEL',     'Nivel de gestión propuesto',   'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 410, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_ADMIN_UNIT',           'Unidad administrativa propuesta','TEXT',0,'MANUAL',   NULL,                              'UPPERCASE', 420, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_WORKPLACE',            'Lugar de trabajo propuesto',   'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 430, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_JOB_TITLE',            'Denominación del puesto propuesto','TEXT',0,'MANUAL', NULL,                              'UPPERCASE', 440, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_OCCUPATIONAL_GROUP',   'Grupo ocupacional propuesto',  'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 450, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_GRADE',                'Grado propuesto',              'TEXT', 0, 'MANUAL',   NULL,                              NULL,        460, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_SALARY',               'Remuneración mensual propuesta','CURRENCY',0,'MANUAL',NULL,                              'N2',        470, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_BUDGET_CODE',          'Partida individual propuesta', 'TEXT', 0, 'MANUAL',   NULL,                              NULL,        480, GETDATE(), NULL),

    -- ── BLOQUE 9: Responsables de aprobación ─────────────────────────────────
    (@TemplateID, 'ACTA_NUMBER',        'Número de acta final',          'TEXT',     0, 'MANUAL',   NULL,                          NULL,           490, GETDATE(), NULL),
    (@TemplateID, 'APPROVAL_DATE',      'Fecha de aprobación',           'DATE',     0, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   500, GETDATE(), NULL),
    (@TemplateID, 'DTH_DIRECTOR_NAME',  'Nombre del Director DTH',       'TEXT',     1, 'SYSTEM',   'Config.DthDirectorName',      'UPPERCASE',    510, GETDATE(), NULL),
    (@TemplateID, 'DTH_DIRECTOR_TITLE', 'Puesto del Director DTH',       'TEXT',     1, 'SYSTEM',   'Config.DthDirectorTitle',     'UPPERCASE',    520, GETDATE(), NULL),
    (@TemplateID, 'AUTHORITY_NAME',     'Nombre de la autoridad nominadora','TEXT',  1, 'SYSTEM',   'Config.AuthorityName',        'UPPERCASE',    530, GETDATE(), NULL),
    (@TemplateID, 'AUTHORITY_TITLE',    'Puesto de la autoridad nominadora','TEXT',  1, 'SYSTEM',   'Config.AuthorityTitle',       'UPPERCASE',    540, GETDATE(), NULL),

    -- ── BLOQUE 10: Firmas del servidor ────────────────────────────────────────
    (@TemplateID, 'EMPLOYEE_SIGNATURE_DATE','Fecha de firma del servidor','DATE',    0, 'MANUAL',   NULL,                          'dd-MM-yyyy',   550, GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_SIGNATURE_HOUR','Hora de firma del servidor', 'TEXT',    0, 'MANUAL',   NULL,                          NULL,           560, GETDATE(), NULL),
    (@TemplateID, 'WITNESS_NAME',       'Nombre del testigo (negativa)', 'TEXT',     0, 'MANUAL',   NULL,                          'UPPERCASE',    570, GETDATE(), NULL),
    (@TemplateID, 'WITNESS_DATE',       'Fecha del testigo',             'DATE',     0, 'MANUAL',   NULL,                          'dd-MM-yyyy',   580, GETDATE(), NULL),

    -- ── BLOQUE 11: Responsables de elaboración ────────────────────────────────
    (@TemplateID, 'ELABORATOR_NAME',    'Nombre responsable elaboración','TEXT',     1, 'SYSTEM',   'Config.ElaboratorName',       'UPPERCASE',    590, GETDATE(), NULL),
    (@TemplateID, 'ELABORATOR_TITLE',   'Puesto responsable elaboración','TEXT',     1, 'SYSTEM',   'Config.ElaboratorTitle',      'UPPERCASE',    600, GETDATE(), NULL),
    (@TemplateID, 'REVIEWER_NAME',      'Nombre responsable revisión',   'TEXT',     1, 'SYSTEM',   'Config.ReviewerName',         'UPPERCASE',    610, GETDATE(), NULL),
    (@TemplateID, 'REVIEWER_TITLE',     'Puesto responsable revisión',   'TEXT',     1, 'SYSTEM',   'Config.ReviewerTitle',        'UPPERCASE',    620, GETDATE(), NULL),
    (@TemplateID, 'REGISTRAR_NAME',     'Nombre responsable registro',   'TEXT',     1, 'SYSTEM',   'Config.RegistrarName',        'UPPERCASE',    630, GETDATE(), NULL),
    (@TemplateID, 'REGISTRAR_TITLE',    'Puesto responsable registro',   'TEXT',     1, 'SYSTEM',   'Config.RegistrarTitle',       'UPPERCASE',    640, GETDATE(), NULL),

    -- ── BLOQUE 12: Notificación ───────────────────────────────────────────────
    (@TemplateID, 'NOTIFICATION_DATE',  'Fecha de notificación',         'DATE',     0, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   650, GETDATE(), NULL),
    (@TemplateID, 'NOTIFICATION_HOUR',  'Hora de notificación',          'TEXT',     0, 'SYSTEM',   'DateTime.Now',                'HH:mm',        660, GETDATE(), NULL),
    (@TemplateID, 'NOTIFICATION_MEDIUM','Medio de comunicación electrónica','TEXT',  0, 'MANUAL',   NULL,                          NULL,           670, GETDATE(), NULL);

    -- ============================================================
    -- PASO 4: Confirmar la transacción
    -- ============================================================
    COMMIT TRANSACTION;

    -- ============================================================
    -- PASO 5: Verificación post-inserción
    -- ============================================================
    SELECT
        t.DocumentTemplateID,
        t.TemplateCode,
        t.Name,
        t.VersionNumber,
        t.LayoutType,
        t.Status,
        t.EffectiveFrom,
        COUNT(f.DocumentTemplateFieldID) AS TotalFields
    FROM HR.tbl_DocumentTemplates t
    LEFT JOIN HR.tbl_DocumentTemplateFields f
           ON f.DocumentTemplateID = t.DocumentTemplateID
    WHERE t.TemplateCode = 'ACCION_PERSONAL'
    GROUP BY
        t.DocumentTemplateID, t.TemplateCode, t.Name,
        t.VersionNumber, t.LayoutType, t.Status, t.EffectiveFrom;

    -- Detalle de campos agrupados por SourceType
    SELECT
        f.SourceType,
        COUNT(*) AS CantidadCampos
    FROM HR.tbl_DocumentTemplateFields f
    WHERE f.DocumentTemplateID = @TemplateID
    GROUP BY f.SourceType
    ORDER BY f.SourceType;

    PRINT 'Plantilla ACCION_PERSONAL insertada correctamente. TemplateID = ' + CAST(@TemplateID AS VARCHAR);
    PRINT 'Total campos insertados: 66';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT            = ERROR_LINE();
    RAISERROR('Error en línea %d: %s', 16, 1, @ErrorLine, @ErrorMsg);
END CATCH;
