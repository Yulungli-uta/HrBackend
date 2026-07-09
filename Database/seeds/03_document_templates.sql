-- ============================================================
-- SEED: Templates Documentales (Acción Personal y Contratos)
-- Fuentes: seed_accion_personal_template_v2.sql + seed_contrato_profesor_ocasional_template.sql
-- Generado: 2026-05-29
-- ============================================================

-- ---- Template documental - Acción de Personal v2 ----
SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- PASO 1: Asegurar seed de ref_Types para DOCUMENT_TEMPLATE_TYPE
    -- ============================================================
    -- NOTA: TemplateType en tbl_DocumentTemplates es NVARCHAR, NO FK a ref_Types.
    -- Este seed es solo para catÃ¡logos UI; el renderer usa el cÃ³digo 'ACCION_PERSONAL'.
    IF NOT EXISTS (
        SELECT 1 FROM HR.ref_Types
        WHERE  Category = 'DOCUMENT_TEMPLATE_TYPE'
          AND  Name     = 'AcciÃ³n de Personal'
    )
    BEGIN
        INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedBy)
        VALUES ('DOCUMENT_TEMPLATE_TYPE', 'AcciÃ³n de Personal',
                'Plantillas para acciones de personal: traslados, comisiones, nombramientos, etc.',
                1, 20, NULL);
    END;

    -- ============================================================
    -- PASO 2: Insertar la plantilla principal
    -- ============================================================
    IF EXISTS (
        SELECT 1 FROM HR.tbl_DocumentTemplates
        WHERE  TemplateCode = 'ACCION_PERSONAL'
          AND  Status       = 'PUBLISHED'
    )
    BEGIN
        UPDATE HR.tbl_DocumentTemplates
        SET    Status    = 'ARCHIVED',
               UpdatedAt = GETDATE()
        WHERE  TemplateCode = 'ACCION_PERSONAL'
          AND  Status       = 'PUBLISHED';
    END;

    DECLARE @TemplateID INT;
    DECLARE @Version    NVARCHAR(10);

    SELECT @Version = CAST(ISNULL(MAX(TRY_CAST(Version AS INT)), 0) + 1 AS NVARCHAR(10))
    FROM   HR.tbl_DocumentTemplates
    WHERE  TemplateCode = 'ACCION_PERSONAL';

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateType,
        TemplateCode,
        Name,
        Version,
        LayoutType,
        Status,
        HtmlContent,
        CreatedAt,
        CreatedBy
    )
    VALUES (
        'ACCION_PERSONAL',
        'ACCION_PERSONAL',
        'AcciÃ³n de Personal - Universidad TÃ©cnica de Ambato',
        @Version,
        'STRUCTURED_FORM',
        'PUBLISHED',
        -- HtmlContent: HTML completo de 2 pÃ¡ginas fiel al formato oficial UTA
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<meta name="DOCUMENT_TITLE" content="ACCIÃ“N DE PERSONAL"/>
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
<td style="border:none;vertical-align:middle;padding-left:6px"><div style="text-align:center;font-weight:bold;font-size:9pt;line-height:1.5">UNIVERSIDAD TÃ‰CNICA DE AMBATO<br/>DIRECCIÃ“N DE TALENTO HUMANO</div></td>
</tr></table>
</td>
<td style="width:45%;border:1px solid #000;padding:0;vertical-align:top">
<div style="font-size:13pt;font-weight:bold;text-align:center;padding:4px 6px;border-bottom:1px solid #000">ACCIÃ“N DE PERSONAL</div>
<table style="width:100%;border-collapse:collapse;border:none"><tr>
<td style="border:none;border-right:1px solid #000;padding:2px 5px;font-size:7.5pt;width:18mm">Nro</td>
<td style="border:none;padding:2px 5px;font-size:8pt;font-weight:bold">{{DOC_NUMBER}}</td>
</tr><tr>
<td colspan="2" style="font-weight:bold;font-size:8pt;text-align:center;background-color:#d9d9d9;border-top:1px solid #000;border-bottom:1px solid #000;padding:2px 4px">FECHA DE ELABORACIÃ“N</td>
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
<td style="font-weight:bold;font-size:7.5pt">DOCUMENTO DE IDENTIFICACIÃ“N</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">NRO. DE IDENTIFICACIÃ“N</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">DESDE (dd-mm-aaaa)</td>
<td style="font-weight:bold;font-size:7.5pt;text-align:center">HASTA (dd-mm-aaaa) (cuando aplica)</td>
</tr><tr>
<td style="text-align:center">{{ID_TYPE}}</td><td style="text-align:center">{{EMPLOYEE_IDCARD}}</td>
<td style="text-align:center">{{EFFECTIVE_FROM}}</td><td style="text-align:center">{{EFFECTIVE_TO}}</td>
</tr></table>
<table class="mt1"><tr><td colspan="4" style="font-size:7.5pt;padding:2px 4px;border-bottom:none">Escoja una opciÃ³n (segÃºn lo estipulado en el artÃ­culo 21 del Reglamento General a la Ley OrgÃ¡nica del Servicio PÃºblico):</td></tr></table>
<table>
<tr>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INGRESO}}"></span>INGRESO</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_TRASPASO}}"></span>TRASPASO</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INCREMENTO_RMU}}"></span>INCREMENTO RMU</td>
<td style="width:25%;border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REVISION_CLASI}}"></span>REVISIÃ“N CLASI. PUESTO</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REINGRESO}}"></span>REINGRESO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_CAMBIO_ADMIN}}"></span>CAMBIO ADMINISTRATIVO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_SUBROGACION}}"></span>SUBROGACIÃ“N</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_OTRO}}"></span>OTRO (DETALLAR): {{ACTION_OTHER_DETAIL}}</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_RESTITUCION}}"></span>RESTITUCIÃ“N</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_INTERCAMBIO}}"></span>INTERCAMBIO VOLUNTARIO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_ENCARGO}}"></span>ENCARGO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt">{{ACTION_ENCARGO_DETAIL}}</td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_REINGRESO2}}"></span>REINGRESO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_LICENCIA}}"></span>LICENCIA</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_CESACION}}"></span>CESACIÃ“N DE FUNCIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_ASCENSO}}"></span>ASCENSO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_COMISION}}"></span>COMISIÃ“N DE SERVICIOS</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_DESTITUCION}}"></span>DESTITUCIÃ“N</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr><tr>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_TRASLADO}}"></span>TRASLADO</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_SANCIONES}}"></span>SANCIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"><span class="cb {{CB_VACACIONES}}"></span>VACACIONES</td>
<td style="border:1px solid #000;padding:1px 4px;font-size:7.5pt"></td>
</tr>
</table>
<table class="mt1"><tr>
<td style="width:70%;font-size:7.5pt;padding:2px 4px">* PRESENTÃ“ LA DECLARACIÃ“N JURADA (nÃºmero 2 del art. 3 RLOSEP)</td>
<td style="width:10%;text-align:center;font-size:7.5pt;font-weight:bold">SI {{DECLARACION_JURADA_SI_MARK}}</td>
<td style="width:20%;text-align:center;font-size:7.5pt;font-weight:bold">NO APLICA {{DECLARACION_JURADA_MARK}}</td>
</tr></table>
<table class="mt1"><tr><td style="font-size:7.5pt;font-weight:bold;padding:2px 4px;background-color:#d9d9d9">MOTIVACIÃ“N: (adjuntar anexo si lo posee)</td></tr>
<tr><td class="motivation-box">{{MOTIVATION_TEXT}}</td></tr></table>
<table class="mt2">
<tr><td style="width:50%" class="situation-header">SITUACIÃ“N ACTUAL</td><td style="width:50%" class="situation-header">SITUACIÃ“N PROPUESTA</td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PROCESO INSTITUCIONAL:</div><div class="situation-value">{{CURRENT_INSTITUTIONAL_PROCESS}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PROCESO INSTITUCIONAL:</div><div class="situation-value">{{PROPOSED_INSTITUTIONAL_PROCESS}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">NIVEL DE GESTIÃ“N:</div><div class="situation-value">{{CURRENT_MANAGEMENT_LEVEL}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">NIVEL DE GESTIÃ“N:</div><div class="situation-value">{{PROPOSED_MANAGEMENT_LEVEL}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">UNIDAD ADMINISTRATIVA:</div><div class="situation-value">{{CURRENT_ADMIN_UNIT}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:8mm"><div class="situation-label">UNIDAD ADMINISTRATIVA:</div><div class="situation-value">{{PROPOSED_ADMIN_UNIT}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">Lugar DE TRABAJO:</div><div class="situation-value">{{CURRENT_WORKPLACE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">Lugar DE TRABAJO:</div><div class="situation-value">{{PROPOSED_WORKPLACE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:9mm"><div class="situation-label">DENOMINACIÃ“N DEL PUESTO:</div><div class="situation-value">{{CURRENT_JOB_TITLE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt;min-height:9mm"><div class="situation-label">DENOMINACIÃ“N DEL PUESTO:</div><div class="situation-value">{{PROPOSED_JOB_TITLE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRUPO OCUPACIONAL:</div><div class="situation-value">{{CURRENT_OCCUPATIONAL_GROUP}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRUPO OCUPACIONAL:</div><div class="situation-value">{{PROPOSED_OCCUPATIONAL_GROUP}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRADO:</div><div class="situation-value">{{CURRENT_GRADE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">GRADO:</div><div class="situation-value">{{PROPOSED_GRADE}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">REMUNERACIÃ“N MENSUAL:</div><div class="situation-value">{{CURRENT_SALARY}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">REMUNERACIÃ“N MENSUAL:</div><div class="situation-value">{{PROPOSED_SALARY}}</div></td></tr>
<tr><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PARTIDA INDIVIDUAL:</div><div class="situation-value">{{CURRENT_BUDGET_CODE}}</div></td><td style="border:1px solid #000;padding:2px 4px;font-size:7.5pt"><div class="situation-label">PARTIDA INDIVIDUAL:</div><div class="situation-value">{{PROPOSED_BUDGET_CODE}}</div></td></tr>
</table>
<table class="mt1">
<tr>
<td style="width:55%;border:1px solid #000;padding:3px 5px;font-size:7.5pt;vertical-align:top">
<div class="bold">POSESIÃ“N DEL PUESTO</div>
<div class="mt1">YO,</div><div>JURO LEALTAD AL ESTADO ECUATORIANO</div>
<div class="mt1">LUGAR: <span style="display:inline-block;width:35mm;border-bottom:1px solid #000">&nbsp;</span>&nbsp;&nbsp;FECHA: <span style="display:inline-block;width:25mm;border-bottom:1px solid #000">&nbsp;</span></div>
<div class="mt1" style="font-size:7pt">** (EN CASO DE GANADOR DE CONCURSO DE MÃ‰RITOS Y OPOSICIÃ“N)</div>
<div style="min-height:14mm"></div>
<div style="border-top:1px solid #000;width:60%;margin-top:2px"><div class="center" style="font-size:7pt">FIRMA</div></div>
</td>
<td style="width:45%;border:1px solid #000;padding:3px 5px;font-size:7.5pt;vertical-align:top">
<div class="bold">CON NRO. DE DOCUMENTO DE IDENTIFICACIÃ“N:</div>
<div class="mt1">{{EMPLOYEE_IDCARD}}</div>
<div style="min-height:16mm"></div>
<div style="border-top:1px solid #000;width:70%;margin-top:2px"><div class="center" style="font-size:7pt">SERVIDOR PÃšBLICO</div></div>
</td>
</tr>
</table>
<table class="mt1">
<tr><td colspan="3" class="responsables-header">NRO. ACTA FINAL: {{ACTA_NUMBER}} &nbsp;&nbsp; FECHA: {{APPROVAL_DATE}} &nbsp;&nbsp; SERVIDOR PÃšBLICO: {{EMPLOYEE_FULLNAME}}</td></tr>
<tr><td colspan="3" class="responsables-header">RESPONSABLES DE APROBACIÃ“N</td></tr>
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
<td style="width:55%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">ACEPTACIÃ“N Y/O RECEPCIÃ“N DEL SERVIDOR PÃšBLICO</td>
<td style="width:45%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9">EN CASO DE NEGATIVA DE LA RECEPCIÃ“N (TESTIGO)</td>
</tr>
<tr>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:22mm;vertical-align:bottom">
<div style="min-height:16mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{EMPLOYEE_FULLNAME}}</div><div>FECHA: {{EMPLOYEE_SIGNATURE_DATE}}</div><div>HORA: {{EMPLOYEE_SIGNATURE_HOUR}}</div></div>
</td>
<td style="border:1px solid #000;padding:3px 5px;font-size:7.5pt;min-height:22mm;vertical-align:bottom">
<div style="min-height:10mm"></div>
<div style="border-top:1px solid #000;padding-top:2px"><div>FIRMA</div><div>NOMBRE: {{WITNESS_NAME}}</div><div>FECHA: {{WITNESS_DATE}}</div>
<div style="font-size:7pt;margin-top:3px">RAZÃ“N: En presencia del testigo se deja constancia de que la o el servidor pÃºblico tiene la negativa de recibir la comunicaciÃ³n de registro de esta acciÃ³n de personal.</div>
</div>
</td>
</tr>
</table>
<table class="mt2">
<tr>
<td style="width:33.33%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9;text-align:center">RESPONSABLE DE ELABORACIÃ“N</td>
<td style="width:33.33%;border:1px solid #000;padding:2px 4px;font-size:7.5pt;font-weight:bold;background-color:#d9d9d9;text-align:center">RESPONSABLE DE REVISIÃ“N</td>
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
<p style="margin-bottom:4px"><strong>PROTECCIÃ“N DE DATOS.-</strong> En cumplimiento con la Ley OrgÃ¡nica de ProtecciÃ³n de Datos Personales y su normativa conexa, la Universidad TÃ©cnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la informaciÃ³n proporcionada a la InstituciÃ³n serÃ¡ objeto de tratamiento con las siguientes finalidades:</p>
<p style="margin-bottom:2px">â€¢ Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</p>
<p style="margin-bottom:2px">â€¢ GeneraciÃ³n de reportes especÃ­ficos internos o que sean solicitados por una instituciÃ³n pÃºblica que rige a esta IES.</p>
<p style="margin-bottom:4px">â€¢ Generar bases de datos de acceso pÃºblico.</p>
<p style="margin-bottom:4px">El titular de los datos personales autoriza expresamente, al momento de proporcionar su informaciÃ³n, el tratamiento de los mismos en conformidad con la Ley OrgÃ¡nica de ProtecciÃ³n de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento serÃ¡ solicitado y recabado de manera explÃ­cita y fehaciente.</p>
<div class="uso-separator"></div>
<p style="margin-bottom:4px">REGISTRO DE NOTIFICACIÃ“N AL SERVIDOR PÃšBLICO DE LA ACCIÃ“N DE PERSONAL (primer inciso del art. 22 RGLOSEP, art. 101 COA, art. 66 y 126 ERJAFE)</p>
<div class="uso-separator"></div>
<p style="margin-bottom:3px"><strong>COMUNICACIÃ“N ELECTRÃ“NICA</strong></p>
<table class="no-border" style="width:60%;margin-bottom:4px">
<tr><td style="border:none;padding:1px 4px;font-size:7.5pt;width:30mm">FECHA:</td><td style="border:none;padding:1px 4px;font-size:7.5pt">{{NOTIFICATION_DATE}}</td><td style="border:none;padding:1px 4px;font-size:7.5pt;width:20mm">HORA:</td><td style="border:none;padding:1px 4px;font-size:7.5pt">{{NOTIFICATION_HOUR}}</td></tr>
</table>
<p style="margin-bottom:12mm">* * MEDIO: {{NOTIFICATION_MEDIUM}}</p>
<table class="no-border" style="width:60%;margin:0 auto">
<tr><td style="border:none;border-top:1px solid #000;text-align:center;padding-top:2px;font-size:7.5pt">FIRMA DEL RESPONSABLE QUE NOTIFICÃ“</td></tr>
<tr><td style="border:none;text-align:center;font-size:7.5pt">NOMBRE: {{DTH_DIRECTOR_NAME}}</td></tr>
<tr><td style="border:none;text-align:center;font-size:7.5pt">PUESTO: {{DTH_DIRECTOR_TITLE}}</td></tr>
</table>
<p class="mt2" style="font-size:7pt">** Si la comunicaciÃ³n fue electrÃ³nica se deberÃ¡ colocar el medio por el cual se notificÃ³ al servidor; asÃ­ como, el nÃºmero del documento.</p>
</div>
</div>
</body>
</html>',
        GETDATE(),
        NULL
    );

    SET @TemplateID = SCOPE_IDENTITY();

    -- ============================================================
    -- PASO 3: Insertar los 66 campos (placeholders) de la plantilla
    -- ============================================================
    -- SourceType valores: SYSTEM | EMPLOYEE | CONTRACT | MOVEMENT | MANUAL

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, DataType, IsRequired, SourceType, SourceProperty, FormatPattern, SortOrder, CreatedAt, CreatedBy)
    VALUES

    -- â”€â”€ BLOQUE 1: Encabezado â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'DOC_NUMBER',        'NÃºmero del documento',          'TEXT',     1, 'MANUAL',   NULL,                          NULL,           10,  GETDATE(), NULL),
    (@TemplateID, 'ELABORATION_DATE',  'Fecha de elaboraciÃ³n',          'DATE',     1, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   20,  GETDATE(), NULL),

    -- â”€â”€ BLOQUE 2/3: Datos del servidor â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'EMPLOYEE_LASTNAME',  'Apellidos del servidor',        'TEXT',     1, 'EMPLOYEE', 'People.LastName',             'UPPERCASE',    30,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_FIRSTNAME', 'Nombres del servidor',          'TEXT',     1, 'EMPLOYEE', 'People.FirstName',            'UPPERCASE',    40,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_FULLNAME',  'Apellidos y nombres completos', 'TEXT',     1, 'EMPLOYEE', 'People.FullName',             'UPPERCASE',    45,  GETDATE(), NULL),
    (@TemplateID, 'ID_TYPE',            'Tipo de documento de identidad','TEXT',     1, 'EMPLOYEE', 'People.IdType',               'UPPERCASE',    50,  GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_IDCARD',    'NÃºmero de identificaciÃ³n',      'TEXT',     1, 'EMPLOYEE', 'People.IdCard',               NULL,           60,  GETDATE(), NULL),
    (@TemplateID, 'EFFECTIVE_FROM',     'Rige desde',                    'DATE',     1, 'MANUAL',   NULL,                          'dd-MM-yyyy',   70,  GETDATE(), NULL),
    (@TemplateID, 'EFFECTIVE_TO',       'Rige hasta (cuando aplica)',    'DATE',     0, 'MANUAL',   NULL,                          'dd-MM-yyyy',   80,  GETDATE(), NULL),

    -- â”€â”€ BLOQUE 4: Tipo de acciÃ³n (checkboxes) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    -- Valor: '' (sin marcar) o 'checked' (marcado)
    (@TemplateID, 'CB_INGRESO',         'Checkbox: Ingreso',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           90,  GETDATE(), NULL),
    (@TemplateID, 'CB_TRASPASO',        'Checkbox: Traspaso',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           100, GETDATE(), NULL),
    (@TemplateID, 'CB_INCREMENTO_RMU',  'Checkbox: Incremento RMU',      'TEXT',     0, 'MANUAL',   NULL,                          NULL,           110, GETDATE(), NULL),
    (@TemplateID, 'CB_REVISION_CLASI',  'Checkbox: RevisiÃ³n Clasi. Puesto','TEXT',   0, 'MANUAL',   NULL,                          NULL,           120, GETDATE(), NULL),
    (@TemplateID, 'CB_REINGRESO',       'Checkbox: Reingreso',           'TEXT',     0, 'MANUAL',   NULL,                          NULL,           130, GETDATE(), NULL),
    (@TemplateID, 'CB_CAMBIO_ADMIN',    'Checkbox: Cambio Administrativo','TEXT',    0, 'MANUAL',   NULL,                          NULL,           140, GETDATE(), NULL),
    (@TemplateID, 'CB_SUBROGACION',     'Checkbox: SubrogaciÃ³n',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           150, GETDATE(), NULL),
    (@TemplateID, 'CB_OTRO',            'Checkbox: Otro (detallar)',     'TEXT',     0, 'MANUAL',   NULL,                          NULL,           160, GETDATE(), NULL),
    (@TemplateID, 'ACTION_OTHER_DETAIL','Detalle de otro tipo de acciÃ³n','TEXT',     0, 'MANUAL',   NULL,                          NULL,           165, GETDATE(), NULL),
    (@TemplateID, 'CB_RESTITUCION',     'Checkbox: RestituciÃ³n',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           170, GETDATE(), NULL),
    (@TemplateID, 'CB_INTERCAMBIO',     'Checkbox: Intercambio Voluntario','TEXT',   0, 'MANUAL',   NULL,                          NULL,           180, GETDATE(), NULL),
    (@TemplateID, 'CB_ENCARGO',         'Checkbox: Encargo',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           190, GETDATE(), NULL),
    (@TemplateID, 'ACTION_ENCARGO_DETAIL','Detalle del encargo (ej: Nombramiento Provisional A)','TEXT',0,'MANUAL',NULL,           NULL,           195, GETDATE(), NULL),
    (@TemplateID, 'CB_REINGRESO2',      'Checkbox: Reingreso (2da fila)','TEXT',     0, 'MANUAL',   NULL,                          NULL,           200, GETDATE(), NULL),
    (@TemplateID, 'CB_LICENCIA',        'Checkbox: Licencia',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           210, GETDATE(), NULL),
    (@TemplateID, 'CB_CESACION',        'Checkbox: CesaciÃ³n de Funciones','TEXT',    0, 'MANUAL',   NULL,                          NULL,           220, GETDATE(), NULL),
    (@TemplateID, 'CB_ASCENSO',         'Checkbox: Ascenso',             'TEXT',     0, 'MANUAL',   NULL,                          NULL,           230, GETDATE(), NULL),
    (@TemplateID, 'CB_COMISION',        'Checkbox: ComisiÃ³n de Servicios','TEXT',    0, 'MANUAL',   NULL,                          NULL,           240, GETDATE(), NULL),
    (@TemplateID, 'CB_DESTITUCION',     'Checkbox: DestituciÃ³n',         'TEXT',     0, 'MANUAL',   NULL,                          NULL,           250, GETDATE(), NULL),
    (@TemplateID, 'CB_TRASLADO',        'Checkbox: Traslado',            'TEXT',     0, 'MANUAL',   NULL,                          NULL,           260, GETDATE(), NULL),
    (@TemplateID, 'CB_SANCIONES',       'Checkbox: Sanciones',           'TEXT',     0, 'MANUAL',   NULL,                          NULL,           270, GETDATE(), NULL),
    (@TemplateID, 'CB_VACACIONES',      'Checkbox: Vacaciones',          'TEXT',     0, 'MANUAL',   NULL,                          NULL,           280, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 5: DeclaraciÃ³n jurada â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'DECLARACION_JURADA_SI_MARK','Marca declaraciÃ³n jurada (X = SI presentÃ³)','TEXT',1,'MANUAL',NULL,               NULL,           285, GETDATE(), NULL),
    (@TemplateID, 'DECLARACION_JURADA_MARK','Marca declaraciÃ³n jurada (â— = NO APLICA)','TEXT',1,'MANUAL',NULL,                    NULL,           290, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 6: MotivaciÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'MOTIVATION_TEXT',    'Texto de motivaciÃ³n (resoluciÃ³n)',  'TEXT', 1, 'MANUAL',   NULL,                          NULL,           300, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 7: SituaciÃ³n Actual (fuente: CONTRACT) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'CURRENT_INSTITUTIONAL_PROCESS', 'Proceso institucional actual', 'TEXT', 0, 'CONTRACT', 'Department.InstitutionalProcess', 'UPPERCASE', 310, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_MANAGEMENT_LEVEL',      'Nivel de gestiÃ³n actual',      'TEXT', 0, 'CONTRACT', 'Department.ManagementLevel',      'UPPERCASE', 320, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_ADMIN_UNIT',            'Unidad administrativa actual', 'TEXT', 0, 'CONTRACT', 'Department.Name',                 'UPPERCASE', 330, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_WORKPLACE',             'Lugar de trabajo actual',      'TEXT', 0, 'CONTRACT', 'Department.Location',             'UPPERCASE', 340, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_JOB_TITLE',             'DenominaciÃ³n del puesto actual','TEXT',0, 'CONTRACT', 'Job.Description',                 'UPPERCASE', 350, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_OCCUPATIONAL_GROUP',    'Grupo ocupacional actual',     'TEXT', 0, 'CONTRACT', 'Job.OccupationalGroup',           'UPPERCASE', 360, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_GRADE',                 'Grado actual',                 'TEXT', 0, 'CONTRACT', 'Job.Grade',                       NULL,        370, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_SALARY',                'RemuneraciÃ³n mensual actual',  'CURRENCY',0,'CONTRACT','Contract.Salary',                'N2',        380, GETDATE(), NULL),
    (@TemplateID, 'CURRENT_BUDGET_CODE',           'Partida individual actual',    'TEXT', 0, 'CONTRACT', 'Department.BudgetCode',           NULL,        390, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 8: SituaciÃ³n Propuesta (fuente: MANUAL) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'PROPOSED_INSTITUTIONAL_PROCESS','Proceso institucional propuesto','TEXT',0,'MANUAL',   NULL,                              'UPPERCASE', 400, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_MANAGEMENT_LEVEL',     'Nivel de gestiÃ³n propuesto',   'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 410, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_ADMIN_UNIT',           'Unidad administrativa propuesta','TEXT',0,'MANUAL',   NULL,                              'UPPERCASE', 420, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_WORKPLACE',            'Lugar de trabajo propuesto',   'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 430, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_JOB_TITLE',            'DenominaciÃ³n del puesto propuesto','TEXT',0,'MANUAL', NULL,                              'UPPERCASE', 440, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_OCCUPATIONAL_GROUP',   'Grupo ocupacional propuesto',  'TEXT', 0, 'MANUAL',   NULL,                              'UPPERCASE', 450, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_GRADE',                'Grado propuesto',              'TEXT', 0, 'MANUAL',   NULL,                              NULL,        460, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_SALARY',               'RemuneraciÃ³n mensual propuesta','CURRENCY',0,'MANUAL',NULL,                              'N2',        470, GETDATE(), NULL),
    (@TemplateID, 'PROPOSED_BUDGET_CODE',          'Partida individual propuesta', 'TEXT', 0, 'MANUAL',   NULL,                              NULL,        480, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 9: Responsables de aprobaciÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'ACTA_NUMBER',        'NÃºmero de acta final',          'TEXT',     0, 'MANUAL',   NULL,                          NULL,           490, GETDATE(), NULL),
    (@TemplateID, 'APPROVAL_DATE',      'Fecha de aprobaciÃ³n',           'DATE',     0, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   500, GETDATE(), NULL),
    (@TemplateID, 'DTH_DIRECTOR_NAME',  'Nombre del Director DTH',       'TEXT',     1, 'SYSTEM',   'Config.DthDirectorName',      'UPPERCASE',    510, GETDATE(), NULL),
    (@TemplateID, 'DTH_DIRECTOR_TITLE', 'Puesto del Director DTH',       'TEXT',     1, 'SYSTEM',   'Config.DthDirectorTitle',     'UPPERCASE',    520, GETDATE(), NULL),
    (@TemplateID, 'AUTHORITY_NAME',     'Nombre de la autoridad nominadora','TEXT',  1, 'SYSTEM',   'Config.AuthorityName',        'UPPERCASE',    530, GETDATE(), NULL),
    (@TemplateID, 'AUTHORITY_TITLE',    'Puesto de la autoridad nominadora','TEXT',  1, 'SYSTEM',   'Config.AuthorityTitle',       'UPPERCASE',    540, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 10: Firmas del servidor â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'EMPLOYEE_SIGNATURE_DATE','Fecha de firma del servidor','DATE',    0, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   550, GETDATE(), NULL),
    (@TemplateID, 'EMPLOYEE_SIGNATURE_HOUR','Hora de firma del servidor', 'TEXT',    0, 'SYSTEM',   'DateTime.Now',                'HH:mm',        560, GETDATE(), NULL),
    (@TemplateID, 'WITNESS_NAME',       'Nombre del testigo (negativa)', 'TEXT',     0, 'MANUAL',   NULL,                          'UPPERCASE',    570, GETDATE(), NULL),
    (@TemplateID, 'WITNESS_DATE',       'Fecha del testigo',             'DATE',     0, 'MANUAL',   NULL,                          'dd-MM-yyyy',   580, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 11: Responsables de elaboraciÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'ELABORATOR_NAME',    'Nombre responsable elaboraciÃ³n','TEXT',     1, 'SYSTEM',   'Config.ElaboratorName',       'UPPERCASE',    590, GETDATE(), NULL),
    (@TemplateID, 'ELABORATOR_TITLE',   'Puesto responsable elaboraciÃ³n','TEXT',     1, 'SYSTEM',   'Config.ElaboratorTitle',      'UPPERCASE',    600, GETDATE(), NULL),
    (@TemplateID, 'REVIEWER_NAME',      'Nombre responsable revisiÃ³n',   'TEXT',     1, 'SYSTEM',   'Config.ReviewerName',         'UPPERCASE',    610, GETDATE(), NULL),
    (@TemplateID, 'REVIEWER_TITLE',     'Puesto responsable revisiÃ³n',   'TEXT',     1, 'SYSTEM',   'Config.ReviewerTitle',        'UPPERCASE',    620, GETDATE(), NULL),
    (@TemplateID, 'REGISTRAR_NAME',     'Nombre responsable registro',   'TEXT',     1, 'SYSTEM',   'Config.RegistrarName',        'UPPERCASE',    630, GETDATE(), NULL),
    (@TemplateID, 'REGISTRAR_TITLE',    'Puesto responsable registro',   'TEXT',     1, 'SYSTEM',   'Config.RegistrarTitle',       'UPPERCASE',    640, GETDATE(), NULL),

    -- â”€â”€ BLOQUE 12: NotificaciÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (@TemplateID, 'NOTIFICATION_DATE',  'Fecha de notificaciÃ³n',         'DATE',     0, 'SYSTEM',   'DateTime.Now',                'dd-MM-yyyy',   650, GETDATE(), NULL),
    (@TemplateID, 'NOTIFICATION_HOUR',  'Hora de notificaciÃ³n',          'TEXT',     0, 'SYSTEM',   'DateTime.Now',                'HH:mm',        660, GETDATE(), NULL),
    (@TemplateID, 'NOTIFICATION_MEDIUM','Medio de comunicaciÃ³n electrÃ³nica','TEXT',  0, 'MANUAL',   NULL,                          NULL,           670, GETDATE(), NULL);

    -- ============================================================
    -- PASO 4: Confirmar la transacciÃ³n
    -- ============================================================
    COMMIT TRANSACTION;

    -- ============================================================
    -- PASO 5: VerificaciÃ³n post-inserciÃ³n
    -- ============================================================
    SELECT
        t.TemplateID,
        t.TemplateCode,
        t.Name,
        t.Version,
        t.LayoutType,
        t.Status,
        COUNT(f.FieldID) AS TotalFields
    FROM HR.tbl_DocumentTemplates t
    LEFT JOIN HR.tbl_DocumentTemplateFields f
           ON f.TemplateID = t.TemplateID
    WHERE t.TemplateCode = 'ACCION_PERSONAL'
    GROUP BY
        t.TemplateID, t.TemplateCode, t.Name,
        t.Version, t.LayoutType, t.Status;

    SELECT
        f.SourceType,
        COUNT(*) AS CantidadCampos
    FROM HR.tbl_DocumentTemplateFields f
    WHERE f.TemplateID = @TemplateID
    GROUP BY f.SourceType
    ORDER BY f.SourceType;

    PRINT 'Plantilla ACCION_PERSONAL insertada correctamente. TemplateID = ' + CAST(@TemplateID AS VARCHAR);
    PRINT 'Total campos insertados: 66';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT            = ERROR_LINE();
    RAISERROR('Error en lÃ­nea %d: %s', 16, 1, @ErrorLine, @ErrorMsg);
END CATCH;


-- ---- Template documental - Contrato Profesor Ocasional ----
-- ================================================================
-- SCRIPT: 14_seed_contrato_profesor_ocasional.sql
-- PROPÃ“SITO: Insertar la plantilla oficial "Contrato Profesor/a
--            Ocasional UTA" en el Motor Documental Institucional.
--
-- TABLAS AFECTADAS:
--   HR.tbl_DocumentTemplates       â†’ 1 registro (plantilla)
--   HR.tbl_DocumentTemplateFields  â†’ 32 registros (campos/placeholders)
--
-- ESTRUCTURA DE TABLAS (versiÃ³n corregida y alineada con EF Core):
--   tbl_DocumentTemplates  : TemplateID, TemplateCode, Name, Description,
--                            TemplateType, Version, LayoutType, Status,
--                            HtmlContent, CssStyles, MetaJson,
--                            RequiresSignature, RequiresApproval,
--                            CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
--
--   tbl_DocumentTemplateFields : FieldID, TemplateID, FieldName, Label,
--                                SourceType, SourceProperty, DataType,
--                                FormatPattern, DefaultValue, IsRequired,
--                                IsEditable, SortOrder, HelpText,
--                                CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
--
-- PREREQUISITOS:
--   - Las tablas deben existir (ejecutar DDL corregido primero)
--   - No depende de ref_Types (TemplateType es NVARCHAR directo)
--
-- IDEMPOTENCIA:
--   - Si ya existe una versiÃ³n PUBLISHED, la archiva antes de insertar
--   - Seguro de ejecutar mÃºltiples veces
--
-- AUTOR: Motor Documental UTA - HrBackend
-- FECHA: 2025-04-24
-- ================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- PASO 1: Archivar versiÃ³n anterior si existe
    -- ============================================================
    IF EXISTS (
        SELECT 1 FROM HR.tbl_DocumentTemplates
        WHERE  TemplateCode = 'CONTRATO_PROFESOR_OCASIONAL'
          AND  Status       = 'PUBLISHED'
    )
    BEGIN
        UPDATE HR.tbl_DocumentTemplates
        SET    Status    = 'ARCHIVED',
               UpdatedAt = GETDATE()
        WHERE  TemplateCode = 'CONTRATO_PROFESOR_OCASIONAL'
          AND  Status       = 'PUBLISHED';
    END;

    -- ============================================================
    -- PASO 2: Calcular siguiente versiÃ³n
    -- ============================================================
    DECLARE @NextVersion NVARCHAR(10);
    DECLARE @VersionCount INT;

    SELECT @VersionCount = COUNT(*)
    FROM   HR.tbl_DocumentTemplates
    WHERE  TemplateCode = 'CONTRATO_PROFESOR_OCASIONAL';

    SET @NextVersion = CAST((@VersionCount + 1) AS NVARCHAR(5)) + '.0';

    -- ============================================================
    -- PASO 3: Insertar la plantilla principal
    -- ============================================================
    DECLARE @TemplateID INT;

    INSERT INTO HR.tbl_DocumentTemplates (
        TemplateCode,
        Name,
        Description,
        TemplateType,
        Version,
        LayoutType,
        Status,
        HtmlContent,
        CssStyles,
        MetaJson,
        RequiresSignature,
        RequiresApproval,
        CreatedAt,
        CreatedBy
    )
    VALUES (
        'CONTRATO_PROFESOR_OCASIONAL',
        'Contrato Profesor/a Ocasional - Universidad TÃ©cnica de Ambato',
        'Contrato de PrestaciÃ³n de Servicios de Profesor/a Ocasional al amparo del Art. 147 y 153 LOES y Art. 83 literal m) LOSEP. Incluye distributivo de trabajo, horario semanal, clÃ¡usulas legales, firmas y declaraciones juradas.',
        'CONTRATO',           -- TemplateType: categorÃ­a del documento
        @NextVersion,         -- Version: calculada dinÃ¡micamente
        'FLOW_TEXT',          -- LayoutType: texto fluido (no formulario estructurado)
        'PUBLISHED',          -- Status: lista para generar documentos
        -- HtmlContent: plantilla completa (ver archivo CONTRATO_PROFESOR_OCASIONAL_v1.html)
        N'<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8"/>
<meta name="DOCUMENT_TITLE" content="CONTRATO PROFESOR/A OCASIONAL"/>
<meta name="CONTRACT_NUMBER" content="{{CONTRACT_NUMBER}}"/>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: "Times New Roman", Times, serif; font-size: 11pt; color: #000; background: #fff; width: 210mm; margin: 0 auto; }
.page { width: 210mm; min-height: 297mm; padding: 20mm 25mm 20mm 25mm; page-break-after: always; }
.page:last-child { page-break-after: auto; }
.logo-circle { width: 52px; height: 52px; border-radius: 50%; background: #8B1A1A; display: flex; align-items: center; justify-content: center; color: #fff; font-weight: bold; font-size: 14pt; font-family: Arial, sans-serif; border: 3px solid #fff; box-shadow: 0 0 0 2px #8B1A1A; }
.header-logo-container { display: flex; align-items: center; justify-content: center; padding: 4px 0 6px 0; border-bottom: 2px solid #8B1A1A; margin-bottom: 6px; }
.institution-name { text-align: center; font-family: "Times New Roman", serif; font-size: 13pt; letter-spacing: 3px; color: #000; margin-top: 4px; font-variant: small-caps; }
.doc-title { text-align: center; font-size: 12pt; font-weight: bold; margin-top: 18px; margin-bottom: 6px; font-family: Arial, Helvetica, sans-serif; }
.doc-number { text-align: center; font-size: 11pt; font-weight: bold; margin-bottom: 20px; font-family: Arial, Helvetica, sans-serif; }
p { text-align: justify; line-height: 1.5; margin-bottom: 12px; font-size: 11pt; }
.clause-title { font-weight: bold; display: inline; }
table { width: 100%; border-collapse: collapse; margin-bottom: 14px; font-size: 10pt; }
th { border: 1px solid #000; padding: 4px 6px; background-color: #f0f0f0; font-weight: bold; text-align: left; font-size: 10pt; }
td { border: 1px solid #000; padding: 3px 6px; vertical-align: top; font-size: 10pt; }
.signatures-row { display: flex; justify-content: space-between; margin-top: 50px; }
.signature-block { width: 45%; text-align: center; font-size: 10.5pt; line-height: 1.6; }
.signature-line { border-bottom: 1px solid #000; margin: 0 auto 4px auto; width: 80%; height: 1px; }
.elaborado-por { margin-top: 30px; font-size: 9.5pt; font-style: italic; }
.declaration-block { margin-bottom: 30px; }
.declaration-text { font-weight: bold; font-size: 11pt; line-height: 1.5; text-align: justify; margin-bottom: 20px; }
.firma-declaracion { margin-left: 35%; margin-bottom: 30px; }
.firma-declaracion-line { border-bottom: 1px solid #000; width: 60mm; height: 1px; margin-bottom: 2px; }
.firma-declaracion-label { font-size: 10pt; }
.dth-section { margin-top: 20px; }
.dth-title { font-weight: bold; font-size: 11pt; margin-bottom: 12px; }
.dth-signature { text-align: center; margin-top: 50px; font-size: 10.5pt; }
.dth-signature-line { border-bottom: 1px solid #000; width: 70mm; margin: 0 auto 4px auto; height: 1px; }
.mt20 { margin-top: 20px; } .center { text-align: center; } .bold { font-weight: bold; }
</style>
</head>
<body>
<div class="page">
<div style="background: linear-gradient(135deg, #8B1A1A 0%, #C0392B 50%, #8B1A1A 100%); height:16mm; width:100%; margin-bottom:0;"></div>
<div class="header-logo-container"><div class="logo-circle">UTA</div></div>
<div class="institution-name">Universidad TÃ©cnica de Ambato</div>
<div class="doc-title">CONTRATO PROFESOR/A OCASIONAL</div>
<div class="doc-number">{{CONTRACT_NUMBER}}</div>
<p>En la ciudad de Ambato, a los <span class="bold">{{CONTRACT_DATE_DAY_WORDS}}</span> dÃ­as del mes de <span class="bold">{{CONTRACT_DATE_MONTH}}</span> de <span class="bold">{{CONTRACT_DATE_YEAR_WORDS}}</span>, comparecen: por una parte la Universidad TÃ©cnica de Ambato, representada por el/la seÃ±or/a <span class="bold">{{FACULTY_ROLE}}</span> de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span>, por delegaciÃ³n de la seÃ±ora Rectora de la indicada InstituciÃ³n, <span class="bold">{{RECTOR_FULLNAME}}</span>, mediante <span class="bold">{{DELEGATION_RESOLUTION}}</span>, con fecha <span class="bold">{{DELEGATION_DATE}}</span>, a la que en adelante y para efectos del presente contrato se le podrÃ¡ llamar como El Contratante, o La Universidad; y por otra parte <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien asÃ­ mismo para efectos del presente contrato se le podrÃ¡ invocar por sus propios nombres que son los que quedan ya seÃ±alados, o el/la <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span>, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, capaces, libre y voluntariamente convienen en celebrar el presente Contrato de PrestaciÃ³n de Servicios de Profesor/a Ocasional, al amparo de lo previsto en el Art. 147 y 153, de la Ley OrgÃ¡nica de EducaciÃ³n Superior, en concordancia con el Art. 25 y Art. 26 del Reglamento de Carrera y EscalafÃ³n del Personal AcadÃ©mico del Sistema de EducaciÃ³n Superior, asÃ­ como tambiÃ©n a lo estipulado en el Art. 83 literal m) de la Ley OrgÃ¡nica de Servicio PÃºblico, al tenor de las siguientes clÃ¡usulas y estipulaciones que se determinan a continuaciÃ³n:</p>
<p><span class="clause-title">PRIMERA.- ANTECEDENTES.-</span> En el Estatuto de la Universidad TÃ©cnica de Ambato en su Art. 10.- Objetivos.- La Universidad TÃ©cnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a travÃ©s de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sÃ³lidos conocimientos cientÃ­ficos, tecnolÃ³gicos y culturales, que interpreten y comprendan la realidad socioeconÃ³mica del Ecuador, de LatinoamÃ©rica y del mundo, y que, emprendan de manera autÃ³noma en iniciativas que propicien el desarrollo socioeconÃ³mico de la provincia, la regiÃ³n y el paÃ­s. y, Art. 35 del Reglamento de Carrera y EscalafÃ³n del Personal AcadÃ©mico de la Universidad TÃ©cnica de Ambato.</p>
<p>2) La suscripciÃ³n del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y EscalafÃ³n del Personal AcadÃ©mico del Sistema de EducaciÃ³n Superior en funciÃ³n de lo resuelto mediante ResoluciÃ³n Nro: <span class="bold">{{CAU_RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{CAU_RESOLUTION_DATE}}</span> por medio de la cual el Consejo AcadÃ©mico Universitario tuvo a bien aprobar el distributivo de trabajo del personal acadÃ©mico; concomitantemente, con memorando Nro. <span class="bold">{{RECTOR_MEMO_NUMBER}}</span>, con fecha: <span class="bold">{{RECTOR_MEMO_DATE}}</span> en la que el Rectorado de la Universidad TÃ©cnica de Ambato autoriza el presente Contrato de PrestaciÃ³n de Servicios de <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.</p>
<p><span class="clause-title">SEGUNDA.-</span> Por los antecedentes que quedan expuestos, el/la seÃ±or/a <span class="bold">{{FACULTY_ROLE}}</span> de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span>, conforme queda seÃ±alado en lÃ­neas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente:</p>
{{WORK_DISTRIBUTION_TABLE}}
<p><span class="clause-title">TERCERA.-</span> El presente contrato tendrÃ¡ vigencia del <span class="bold">{{CONTRACT_START_DATE}}</span> al <span class="bold">{{CONTRACT_END_DATE}}</span>.</p>
<p>Una vez cumplida la vigencia del presente contrato, automÃ¡ticamente se da por terminado el mismo, sin que sea menester formalidad o notificaciÃ³n alguna.</p>
<p><span class="clause-title">CUARTA.-</span> La Universidad TÃ©cnica de Ambato, por su parte, pagarÃ¡ a el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneraciÃ³n por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de NorteamÃ©rica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) mÃ¡s beneficios de ley, pago que se efectuarÃ¡ en forma mensual. El egreso se aplicarÃ¡ a la partida presupuestaria NÂº <span class="bold">{{BUDGET_CODE}}</span>.</p>
<p><span class="clause-title">QUINTA.-</span> "El/la Profesional" desempeÃ±arÃ¡ las actividades inherentes a <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> y que se hace referencia a la clÃ¡usula segunda del presente contrato:</p>
{{SCHEDULE_TABLE}}
<p><span class="clause-title">SEXTA.- NATURALEZA JURÃDICA DEL CONTRATO.-</span> El presente contrato estarÃ¡ sujeto a la Ley OrgÃ¡nica de EducaciÃ³n Superior, Reglamento de Carrera y EscalafÃ³n del Personal AcadÃ©mico del Sistema de EducaciÃ³n Superior y Reglamento de Carrera y EscalafÃ³n del Personal AcadÃ©mico de la Universidad TÃ©cnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificaciÃ³n y/o carga horaria en funciÃ³n de los requerimientos de las Unidades AcadÃ©micas; se entenderÃ¡ incorporado en el presente contrato.</p>
<p><span class="clause-title">SÃ‰PTIMA.-</span> Conforme queda seÃ±alado en la clÃ¡usula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automÃ¡ticamente se darÃ¡ por terminado el mismo, sin que sea menester formalidad o notificaciÃ³n alguna, o se podrÃ¡ dar por terminado anticipadamente mediante una notificaciÃ³n realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.</p>
<p><span class="clause-title">OCTAVA.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte de el/la <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> contratado/a, el retraso o incumplimiento de sus obligaciones contractuales darÃ¡ lugar al pago de la indemnizaciÃ³n de los daÃ±os y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificaciÃ³n alguna.</p>
<p><span class="clause-title">NOVENA.- CONTROVERSIA.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vÃ­a amigable y sobre la base de los principios de buena fe, las partes contratantes se someterÃ¡n a la vÃ­a alternativa de soluciÃ³n de conflictos sea mediciÃ³n o arbitraje ante el Centro de MediaciÃ³n y Arbitraje de la ProcuradurÃ­a General del Estado.</p>
<p><span class="clause-title">DÃ‰CIMA.- PROTECCIÃ“N DE DATOS.-</span> En cumplimiento con la Ley OrgÃ¡nica de ProtecciÃ³n de Datos Personales y su normativa conexa, la Universidad TÃ©cnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la informaciÃ³n proporcionada a la InstituciÃ³n serÃ¡ objeto de tratamiento con las siguientes finalidades:</p>
<p style="margin-left:10mm">â€¢ Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.<br/>â€¢ GeneraciÃ³n de reportes especÃ­ficos internos o que sean solicitados por una instituciÃ³n pÃºblica que rige a esta IES.<br/>â€¢ Generar bases de datos de acceso pÃºblico.</p>
<p>El titular de los datos personales autoriza expresamente, al momento de proporcionar su informaciÃ³n, el tratamiento de los mismos en conformidad con la Ley OrgÃ¡nica de ProtecciÃ³n de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento serÃ¡ solicitado y recabado de manera explÃ­cita y fehaciente.</p>
<p>Para constancia de su total acuerdo y conformidad con todas y cada una de las clÃ¡usulas del presente contrato, las partes suscriben en original y dos copias del mismo tenor y efecto.</p>
<div style="margin-top:30px">
<div style="display:flex;justify-content:space-between;margin-top:50px">
<div style="width:45%;text-align:center;font-size:10.5pt;line-height:1.6"><div style="border-bottom:1px solid #000;margin:0 auto 4px auto;width:80%;height:1px"></div><div><span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span></div><div>{{AUTHORITY_IDCARD}}</div><div>{{AUTHORITY_ROLE}}</div></div>
<div style="width:45%;text-align:center;font-size:10.5pt;line-height:1.6"><div style="border-bottom:1px solid #000;margin:0 auto 4px auto;width:80%;height:1px"></div><div><span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span></div><div>{{EMPLOYEE_IDCARD}}</div><div>{{EMPLOYEE_CONTRACT_ROLE}}</div></div>
</div>
<div style="margin-top:30px;font-size:9.5pt;font-style:italic">Elaborado por: {{ELABORATOR_FULLNAME}}</div>
</div>
</div>
<div class="page">
<p class="bold" style="margin-bottom:20px">CONTRATO PROFESOR/A OCASIONAL (DELEGACIÃ“N) NÂº {{CONTRACT_NUMBER}}</p>
<div class="declaration-block"><p class="declaration-text">DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÃ“N PÃšBLICA, NI HE RECIBIDO INDEMNIZACIÃ“N POR VENTA DE RENUNCIA O POR SUPRESIÃ“N DE PUESTO DE TRABAJO EN EL SECTOR PÃšBLICO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="declaration-block"><p class="declaration-text">DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÃ‘O EL PUESTO DE....................................... EN ..............................., SEGÃšN EL HORARIO ADJUNTO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="declaration-block"><p class="declaration-text">DECLARO BAJO JURAMENTO QUE NO TENGO NINGÃšN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÃXIMA AUTORIDAD DE LA UNIVERSIDAD TÃ‰CNICA DE AMBATO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="dth-section"><p class="dth-title">DIRECCIÃ“N DE TALENTO HUMANO - UNIVERSIDAD TÃ‰CNICA DE AMBATO</p><p>Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registrÃ³ el presente contrato con el NÂ° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE_LONG}}</span>.</p><p class="center mt20">AMBATO:</p><div class="dth-signature"><div class="dth-signature-line"></div><div><span class="bold">{{DTH_DIRECTOR_FULLNAME}}</span></div><div>DIRECTOR</div></div></div>
</div>
</body>
</html>',
        -- CssStyles: NULL (incluido en HtmlContent)
        NULL,
        -- MetaJson: configuraciÃ³n de mÃ¡rgenes y formato de pÃ¡gina
        N'{"pageSize":"A4","marginTop":"20mm","marginBottom":"20mm","marginLeft":"25mm","marginRight":"25mm","fontFamily":"Times New Roman","fontSize":"11pt","language":"es","requiresSignature":false,"requiresApproval":true}',
        0,   -- RequiresSignature: firma fÃ­sica en el documento impreso
        1,   -- RequiresApproval: requiere aprobaciÃ³n antes de emitir
        GETDATE(),
        NULL
    );

    SET @TemplateID = SCOPE_IDENTITY();

    -- ============================================================
    -- PASO 4: Insertar los 32 campos (placeholders)
    -- ============================================================
    -- Columnas: FieldID(auto), TemplateID, FieldName, Label,
    --           SourceType, SourceProperty, DataType, FormatPattern,
    --           DefaultValue, IsRequired, IsEditable, SortOrder,
    --           HelpText, CreatedAt, CreatedBy

    INSERT INTO HR.tbl_DocumentTemplateFields
        (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType,
         FormatPattern, DefaultValue, IsRequired, IsEditable, SortOrder, HelpText, CreatedAt, CreatedBy)
    VALUES

    -- â”€â”€ ENCABEZADO DEL CONTRATO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'CONTRACT_NUMBER', 'NÃºmero del contrato',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 10,
        'NÃºmero oficial del contrato. Ej: FCAGP-DTH-021-2026',
        GETDATE(), NULL
    ),

    -- â”€â”€ FECHA DEL CONTRATO (partes del sistema) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'CONTRACT_DATE_DAY_WORDS', 'DÃ­a del contrato en palabras',
        'SYSTEM', 'DateTime.Now.DayInWords', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 20,
        'DÃ­a de suscripciÃ³n del contrato escrito en palabras. Ej: VEINTITRES',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CONTRACT_DATE_MONTH', 'Mes del contrato',
        'SYSTEM', 'DateTime.Now.MonthName', 'TEXT',
        'LOWERCASE', NULL, 1, 0, 30,
        'Nombre del mes de suscripciÃ³n del contrato. Ej: abril',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CONTRACT_DATE_YEAR_WORDS', 'AÃ±o del contrato en palabras',
        'SYSTEM', 'DateTime.Now.YearInWords', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 40,
        'AÃ±o de suscripciÃ³n del contrato escrito en palabras. Ej: DOS MIL VEINTISEIS',
        GETDATE(), NULL
    ),

    -- â”€â”€ REPRESENTANTE DE LA UNIVERSIDAD (AUTORIDAD) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'FACULTY_ROLE', 'Cargo del representante UTA',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 50,
        'Cargo del representante de la Universidad. Ej: DECANO, VICERRECTOR',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'FACULTY_NAME', 'Nombre de la facultad o unidad',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 60,
        'Nombre de la facultad o unidad acadÃ©mica. Ej: CIENCIAS AGROPECUARIAS',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_TITLE', 'TÃ­tulo acadÃ©mico del representante',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 70,
        'TÃ­tulo acadÃ©mico del representante UTA. Ej: PhD, Mg., Dr.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_FULLNAME', 'Nombre completo del representante UTA',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 80,
        'Apellidos y nombres completos del representante. Ej: VASQUEZ FREYTEZ CARLOS LUIS',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_IDCARD', 'CÃ©dula del representante UTA',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 90,
        'NÃºmero de cÃ©dula del representante UTA. Ej: 1758533747',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_ROLE', 'Rol del representante en el contrato',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 100,
        'Rol del representante en la firma del contrato. Ej: DECANO',
        GETDATE(), NULL
    ),

    -- â”€â”€ RECTORA (SISTEMA) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'RECTOR_FULLNAME', 'Nombre completo de la Rectora',
        'SYSTEM', 'Config.RectorFullName', 'TEXT',
        NULL, 'Dra. Sara Nidhya Camacho Estrada Ph.D', 1, 0, 110,
        'Nombre de la Rectora de la UTA. Se obtiene de la configuraciÃ³n del sistema.',
        GETDATE(), NULL
    ),

    -- â”€â”€ RESOLUCIÃ“N DE DELEGACIÃ“N â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'DELEGATION_RESOLUTION', 'NÃºmero de resoluciÃ³n de delegaciÃ³n',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 120,
        'NÃºmero de la resoluciÃ³n mediante la cual la Rectora delega. Ej: UTA-R-2026-0007',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'DELEGATION_DATE', 'Fecha de la resoluciÃ³n de delegaciÃ³n',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 130,
        'Fecha de la resoluciÃ³n de delegaciÃ³n. Ej: 20-02-2026',
        GETDATE(), NULL
    ),

    -- â”€â”€ DATOS DEL EMPLEADO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'EMPLOYEE_TITLE', 'TÃ­tulo acadÃ©mico del empleado',
        'EMPLOYEE', 'People.AcademicTitle', 'TEXT',
        NULL, NULL, 1, 0, 140,
        'TÃ­tulo acadÃ©mico del empleado. Ej: Ing. Mg., PhD, Lcda.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo del empleado',
        'EMPLOYEE', 'People.FullName', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 150,
        'Apellidos y nombres completos del empleado tal como constan en la cÃ©dula.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_IDCARD', 'NÃºmero de cÃ©dula del empleado',
        'EMPLOYEE', 'People.IdCard', 'TEXT',
        NULL, NULL, 1, 0, 160,
        'NÃºmero de cÃ©dula de ciudadanÃ­a del empleado.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_CONTRACT_ROLE', 'Rol del empleado en el contrato',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', 'PROFESOR/A OCASIONAL', 1, 1, 170,
        'DenominaciÃ³n del rol del empleado en el contrato. Ej: PROFESOR/A OCASIONAL',
        GETDATE(), NULL
    ),

    -- â”€â”€ RESOLUCIONES HABILITANTES â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'CAU_RESOLUTION_NUMBER', 'NÃºmero de resoluciÃ³n CAU',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 180,
        'NÃºmero de resoluciÃ³n del Consejo AcadÃ©mico Universitario. Ej: CAU-P-236-2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CAU_RESOLUTION_DATE', 'Fecha de resoluciÃ³n CAU',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 190,
        'Fecha de la resoluciÃ³n CAU. Ej: 17-04-2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'RECTOR_MEMO_NUMBER', 'NÃºmero de memorando del Rectorado',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 200,
        'NÃºmero del memorando de autorizaciÃ³n del Rectorado. Ej: UTA-R-2026-0682-M',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'RECTOR_MEMO_DATE', 'Fecha del memorando del Rectorado',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 210,
        'Fecha del memorando de autorizaciÃ³n del Rectorado. Ej: 22-04-2026',
        GETDATE(), NULL
    ),

    -- â”€â”€ TABLA DISTRIBUTIVO DE TRABAJO (HTML dinÃ¡mico) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'WORK_DISTRIBUTION_TABLE', 'Tabla del distributivo de trabajo',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 220,
        'HTML completo de la tabla de distributivo de trabajo. Columnas: FunciÃ³n Sustantiva | Asignatura/Actividad | Nro. Horas | Carrera. El backend genera este HTML antes de sustituir el placeholder.',
        GETDATE(), NULL
    ),

    -- â”€â”€ VIGENCIA DEL CONTRATO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'CONTRACT_START_DATE', 'Fecha de inicio de vigencia',
        'CONTRACT', 'Contract.StartDate', 'DATE',
        'dd-MM-yyyy', NULL, 1, 0, 230,
        'Fecha de inicio de vigencia del contrato. Proviene del registro del contrato.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CONTRACT_END_DATE', 'Fecha de fin de vigencia',
        'CONTRACT', 'Contract.EndDate', 'DATE',
        'dd-MM-yyyy', NULL, 1, 0, 240,
        'Fecha de fin de vigencia del contrato. Proviene del registro del contrato.',
        GETDATE(), NULL
    ),

    -- â”€â”€ REMUNERACIÃ“N â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'SALARY_WORDS', 'RemuneraciÃ³n en palabras',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 250,
        'Monto de la remuneraciÃ³n escrito en palabras. Ej: DOS MIL CON 00/100',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'SALARY_AMOUNT', 'RemuneraciÃ³n en nÃºmeros',
        'CONTRACT', 'Contract.Salary', 'CURRENCY',
        'N2', NULL, 1, 0, 260,
        'Monto de la remuneraciÃ³n en formato numÃ©rico. Ej: 2000,00',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'BUDGET_CODE', 'Partida presupuestaria',
        'CONTRACT', 'Contract.BudgetCode', 'TEXT',
        NULL, NULL, 1, 0, 270,
        'CÃ³digo de la partida presupuestaria del contrato.',
        GETDATE(), NULL
    ),

    -- â”€â”€ TABLA HORARIO SEMANAL (HTML dinÃ¡mico) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'SCHEDULE_TABLE', 'Tabla del horario semanal',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 280,
        'HTML completo de la tabla del horario semanal. Columnas: Horas | Lunes | Martes | MiÃ©rcoles | Jueves | Viernes. El backend genera este HTML antes de sustituir el placeholder.',
        GETDATE(), NULL
    ),

    -- â”€â”€ ELABORADOR DEL DOCUMENTO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'ELABORATOR_FULLNAME', 'Nombre del elaborador del contrato',
        'SYSTEM', 'Config.ElaboratorName', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 290,
        'Nombre completo del funcionario que elaborÃ³ el contrato. Se obtiene del usuario autenticado.',
        GETDATE(), NULL
    ),

    -- â”€â”€ CERTIFICACIÃ“N DTH â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (
        @TemplateID, 'DTH_REGISTRY_NUMBER', 'NÃºmero de registro DTH',
        'SYSTEM', 'Config.DthRegistryNumber', 'TEXT',
        NULL, NULL, 1, 0, 300,
        'NÃºmero de registro asignado por la DirecciÃ³n de Talento Humano. Ej: 012109-DTH-2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'DTH_REGISTRY_DATE_LONG', 'Fecha de registro DTH en texto largo',
        'SYSTEM', 'DateTime.Now.LongDateString', 'TEXT',
        'LOWERCASE', NULL, 1, 0, 310,
        'Fecha de registro en DTH escrita en texto largo. Ej: jueves, 23 de abril de 2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'DTH_DIRECTOR_FULLNAME', 'Nombre completo del Director DTH',
        'SYSTEM', 'Config.DthDirectorFullName', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 320,
        'Nombre completo del Director de Talento Humano con tÃ­tulo. Ej: Mg. ANDRADE PEÃ‘AHERRERA WILSON EDUARDO',
        GETDATE(), NULL
    );

    -- ============================================================
    -- PASO 5: Confirmar la transacciÃ³n
    -- ============================================================
    COMMIT TRANSACTION;

    -- ============================================================
    -- PASO 6: VerificaciÃ³n post-inserciÃ³n
    -- ============================================================
    -- Resumen de la plantilla insertada
    SELECT
        t.TemplateID,
        t.TemplateCode,
        t.Name,
        t.Version,
        t.LayoutType,
        t.Status,
        t.RequiresApproval,
        COUNT(f.FieldID) AS TotalFields
    FROM HR.tbl_DocumentTemplates t
    LEFT JOIN HR.tbl_DocumentTemplateFields f
           ON f.TemplateID = t.TemplateID
    WHERE t.TemplateCode = 'CONTRATO_PROFESOR_OCASIONAL'
    GROUP BY
        t.TemplateID, t.TemplateCode, t.Name,
        t.Version, t.LayoutType, t.Status, t.RequiresApproval;

    -- Campos agrupados por SourceType
    SELECT
        f.SourceType,
        COUNT(*) AS CantidadCampos
    FROM HR.tbl_DocumentTemplateFields f
    WHERE f.TemplateID = @TemplateID
    GROUP BY f.SourceType
    ORDER BY f.SourceType;

    -- Listado completo de campos en orden de apariciÃ³n
    SELECT
        f.SortOrder,
        f.FieldName,
        f.Label,
        f.SourceType,
        f.SourceProperty,
        f.DataType,
        f.IsRequired,
        f.IsEditable
    FROM HR.tbl_DocumentTemplateFields f
    WHERE f.TemplateID = @TemplateID
    ORDER BY f.SortOrder;

    PRINT 'âœ” Plantilla CONTRATO_PROFESOR_OCASIONAL insertada correctamente.';
    PRINT '  TemplateID = ' + CAST(@TemplateID AS VARCHAR);
    PRINT '  Version    = ' + @NextVersion;
    PRINT '  Campos     = 32';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT            = ERROR_LINE();
    RAISERROR('Error en lÃ­nea %d: %s', 16, 1, @ErrorLine, @ErrorMsg);
END CATCH;

