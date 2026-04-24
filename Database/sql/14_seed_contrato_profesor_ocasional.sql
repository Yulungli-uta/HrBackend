-- ================================================================
-- SCRIPT: 14_seed_contrato_profesor_ocasional.sql
-- PROPÓSITO: Insertar la plantilla oficial "Contrato Profesor/a
--            Ocasional UTA" en el Motor Documental Institucional.
--
-- TABLAS AFECTADAS:
--   HR.tbl_DocumentTemplates       → 1 registro (plantilla)
--   HR.tbl_DocumentTemplateFields  → 32 registros (campos/placeholders)
--
-- ESTRUCTURA DE TABLAS (versión corregida y alineada con EF Core):
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
--   - Si ya existe una versión PUBLISHED, la archiva antes de insertar
--   - Seguro de ejecutar múltiples veces
--
-- AUTOR: Motor Documental UTA - HrBackend
-- FECHA: 2025-04-24
-- ================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- PASO 1: Archivar versión anterior si existe
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
    -- PASO 2: Calcular siguiente versión
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
        'Contrato Profesor/a Ocasional - Universidad Técnica de Ambato',
        'Contrato de Prestación de Servicios de Profesor/a Ocasional al amparo del Art. 147 y 153 LOES y Art. 83 literal m) LOSEP. Incluye distributivo de trabajo, horario semanal, cláusulas legales, firmas y declaraciones juradas.',
        'CONTRATO',           -- TemplateType: categoría del documento
        @NextVersion,         -- Version: calculada dinámicamente
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
<div class="institution-name">Universidad Técnica de Ambato</div>
<div class="doc-title">CONTRATO PROFESOR/A OCASIONAL</div>
<div class="doc-number">{{CONTRACT_NUMBER}}</div>
<p>En la ciudad de Ambato, a los <span class="bold">{{CONTRACT_DATE_DAY_WORDS}}</span> días del mes de <span class="bold">{{CONTRACT_DATE_MONTH}}</span> de <span class="bold">{{CONTRACT_DATE_YEAR_WORDS}}</span>, comparecen: por una parte la Universidad Técnica de Ambato, representada por el/la señor/a <span class="bold">{{FACULTY_ROLE}}</span> de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span>, por delegación de la señora Rectora de la indicada Institución, <span class="bold">{{RECTOR_FULLNAME}}</span>, mediante <span class="bold">{{DELEGATION_RESOLUTION}}</span>, con fecha <span class="bold">{{DELEGATION_DATE}}</span>, a la que en adelante y para efectos del presente contrato se le podrá llamar como El Contratante, o La Universidad; y por otra parte <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien así mismo para efectos del presente contrato se le podrá invocar por sus propios nombres que son los que quedan ya señalados, o el/la <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span>, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, capaces, libre y voluntariamente convienen en celebrar el presente Contrato de Prestación de Servicios de Profesor/a Ocasional, al amparo de lo previsto en el Art. 147 y 153, de la Ley Orgánica de Educación Superior, en concordancia con el Art. 25 y Art. 26 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior, así como también a lo estipulado en el Art. 83 literal m) de la Ley Orgánica de Servicio Público, al tenor de las siguientes cláusulas y estipulaciones que se determinan a continuación:</p>
<p><span class="clause-title">PRIMERA.- ANTECEDENTES.-</span> En el Estatuto de la Universidad Técnica de Ambato en su Art. 10.- Objetivos.- La Universidad Técnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a través de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sólidos conocimientos científicos, tecnológicos y culturales, que interpreten y comprendan la realidad socioeconómica del Ecuador, de Latinoamérica y del mundo, y que, emprendan de manera autónoma en iniciativas que propicien el desarrollo socioeconómico de la provincia, la región y el país. y, Art. 35 del Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato.</p>
<p>2) La suscripción del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior en función de lo resuelto mediante Resolución Nro: <span class="bold">{{CAU_RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{CAU_RESOLUTION_DATE}}</span> por medio de la cual el Consejo Académico Universitario tuvo a bien aprobar el distributivo de trabajo del personal académico; concomitantemente, con memorando Nro. <span class="bold">{{RECTOR_MEMO_NUMBER}}</span>, con fecha: <span class="bold">{{RECTOR_MEMO_DATE}}</span> en la que el Rectorado de la Universidad Técnica de Ambato autoriza el presente Contrato de Prestación de Servicios de <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.</p>
<p><span class="clause-title">SEGUNDA.-</span> Por los antecedentes que quedan expuestos, el/la señor/a <span class="bold">{{FACULTY_ROLE}}</span> de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span>, conforme queda señalado en líneas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente:</p>
{{WORK_DISTRIBUTION_TABLE}}
<p><span class="clause-title">TERCERA.-</span> El presente contrato tendrá vigencia del <span class="bold">{{CONTRACT_START_DATE}}</span> al <span class="bold">{{CONTRACT_END_DATE}}</span>.</p>
<p>Una vez cumplida la vigencia del presente contrato, automáticamente se da por terminado el mismo, sin que sea menester formalidad o notificación alguna.</p>
<p><span class="clause-title">CUARTA.-</span> La Universidad Técnica de Ambato, por su parte, pagará a el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneración por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de Norteamérica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) más beneficios de ley, pago que se efectuará en forma mensual. El egreso se aplicará a la partida presupuestaria Nº <span class="bold">{{BUDGET_CODE}}</span>.</p>
<p><span class="clause-title">QUINTA.-</span> "El/la Profesional" desempeñará las actividades inherentes a <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> y que se hace referencia a la cláusula segunda del presente contrato:</p>
{{SCHEDULE_TABLE}}
<p><span class="clause-title">SEXTA.- NATURALEZA JURÍDICA DEL CONTRATO.-</span> El presente contrato estará sujeto a la Ley Orgánica de Educación Superior, Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior y Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificación y/o carga horaria en función de los requerimientos de las Unidades Académicas; se entenderá incorporado en el presente contrato.</p>
<p><span class="clause-title">SÉPTIMA.-</span> Conforme queda señalado en la cláusula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automáticamente se dará por terminado el mismo, sin que sea menester formalidad o notificación alguna, o se podrá dar por terminado anticipadamente mediante una notificación realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.</p>
<p><span class="clause-title">OCTAVA.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte de el/la <span class="bold">{{EMPLOYEE_CONTRACT_ROLE}}</span> contratado/a, el retraso o incumplimiento de sus obligaciones contractuales dará lugar al pago de la indemnización de los daños y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificación alguna.</p>
<p><span class="clause-title">NOVENA.- CONTROVERSIA.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vía amigable y sobre la base de los principios de buena fe, las partes contratantes se someterán a la vía alternativa de solución de conflictos sea medición o arbitraje ante el Centro de Mediación y Arbitraje de la Procuraduría General del Estado.</p>
<p><span class="clause-title">DÉCIMA.- PROTECCIÓN DE DATOS.-</span> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:</p>
<p style="margin-left:10mm">• Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.<br/>• Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.<br/>• Generar bases de datos de acceso público.</p>
<p>El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.</p>
<p>Para constancia de su total acuerdo y conformidad con todas y cada una de las cláusulas del presente contrato, las partes suscriben en original y dos copias del mismo tenor y efecto.</p>
<div style="margin-top:30px">
<div style="display:flex;justify-content:space-between;margin-top:50px">
<div style="width:45%;text-align:center;font-size:10.5pt;line-height:1.6"><div style="border-bottom:1px solid #000;margin:0 auto 4px auto;width:80%;height:1px"></div><div><span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_FULLNAME}}</span></div><div>{{AUTHORITY_IDCARD}}</div><div>{{AUTHORITY_ROLE}}</div></div>
<div style="width:45%;text-align:center;font-size:10.5pt;line-height:1.6"><div style="border-bottom:1px solid #000;margin:0 auto 4px auto;width:80%;height:1px"></div><div><span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span></div><div>{{EMPLOYEE_IDCARD}}</div><div>{{EMPLOYEE_CONTRACT_ROLE}}</div></div>
</div>
<div style="margin-top:30px;font-size:9.5pt;font-style:italic">Elaborado por: {{ELABORATOR_FULLNAME}}</div>
</div>
</div>
<div class="page">
<p class="bold" style="margin-bottom:20px">CONTRATO PROFESOR/A OCASIONAL (DELEGACIÓN) Nº {{CONTRACT_NUMBER}}</p>
<div class="declaration-block"><p class="declaration-text">DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÓN PÚBLICA, NI HE RECIBIDO INDEMNIZACIÓN POR VENTA DE RENUNCIA O POR SUPRESIÓN DE PUESTO DE TRABAJO EN EL SECTOR PÚBLICO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="declaration-block"><p class="declaration-text">DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÑO EL PUESTO DE....................................... EN ..............................., SEGÚN EL HORARIO ADJUNTO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="declaration-block"><p class="declaration-text">DECLARO BAJO JURAMENTO QUE NO TENGO NINGÚN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÁXIMA AUTORIDAD DE LA UNIVERSIDAD TÉCNICA DE AMBATO.</p><div class="firma-declaracion"><div class="firma-declaracion-line"></div><div class="firma-declaracion-label">f)</div></div></div>
<div class="dth-section"><p class="dth-title">DIRECCIÓN DE TALENTO HUMANO - UNIVERSIDAD TÉCNICA DE AMBATO</p><p>Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registró el presente contrato con el N° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE_LONG}}</span>.</p><p class="center mt20">AMBATO:</p><div class="dth-signature"><div class="dth-signature-line"></div><div><span class="bold">{{DTH_DIRECTOR_FULLNAME}}</span></div><div>DIRECTOR</div></div></div>
</div>
</body>
</html>',
        -- CssStyles: NULL (incluido en HtmlContent)
        NULL,
        -- MetaJson: configuración de márgenes y formato de página
        N'{"pageSize":"A4","marginTop":"20mm","marginBottom":"20mm","marginLeft":"25mm","marginRight":"25mm","fontFamily":"Times New Roman","fontSize":"11pt","language":"es","requiresSignature":false,"requiresApproval":true}',
        0,   -- RequiresSignature: firma física en el documento impreso
        1,   -- RequiresApproval: requiere aprobación antes de emitir
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

    -- ── ENCABEZADO DEL CONTRATO ──────────────────────────────────────────────
    (
        @TemplateID, 'CONTRACT_NUMBER', 'Número del contrato',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 10,
        'Número oficial del contrato. Ej: FCAGP-DTH-021-2026',
        GETDATE(), NULL
    ),

    -- ── FECHA DEL CONTRATO (partes del sistema) ──────────────────────────────
    (
        @TemplateID, 'CONTRACT_DATE_DAY_WORDS', 'Día del contrato en palabras',
        'SYSTEM', 'DateTime.Now.DayInWords', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 20,
        'Día de suscripción del contrato escrito en palabras. Ej: VEINTITRES',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CONTRACT_DATE_MONTH', 'Mes del contrato',
        'SYSTEM', 'DateTime.Now.MonthName', 'TEXT',
        'LOWERCASE', NULL, 1, 0, 30,
        'Nombre del mes de suscripción del contrato. Ej: abril',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CONTRACT_DATE_YEAR_WORDS', 'Año del contrato en palabras',
        'SYSTEM', 'DateTime.Now.YearInWords', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 40,
        'Año de suscripción del contrato escrito en palabras. Ej: DOS MIL VEINTISEIS',
        GETDATE(), NULL
    ),

    -- ── REPRESENTANTE DE LA UNIVERSIDAD (AUTORIDAD) ──────────────────────────
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
        'Nombre de la facultad o unidad académica. Ej: CIENCIAS AGROPECUARIAS',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_TITLE', 'Título académico del representante',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 70,
        'Título académico del representante UTA. Ej: PhD, Mg., Dr.',
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
        @TemplateID, 'AUTHORITY_IDCARD', 'Cédula del representante UTA',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 90,
        'Número de cédula del representante UTA. Ej: 1758533747',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'AUTHORITY_ROLE', 'Rol del representante en el contrato',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 100,
        'Rol del representante en la firma del contrato. Ej: DECANO',
        GETDATE(), NULL
    ),

    -- ── RECTORA (SISTEMA) ────────────────────────────────────────────────────
    (
        @TemplateID, 'RECTOR_FULLNAME', 'Nombre completo de la Rectora',
        'SYSTEM', 'Config.RectorFullName', 'TEXT',
        NULL, 'Dra. Sara Nidhya Camacho Estrada Ph.D', 1, 0, 110,
        'Nombre de la Rectora de la UTA. Se obtiene de la configuración del sistema.',
        GETDATE(), NULL
    ),

    -- ── RESOLUCIÓN DE DELEGACIÓN ─────────────────────────────────────────────
    (
        @TemplateID, 'DELEGATION_RESOLUTION', 'Número de resolución de delegación',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 120,
        'Número de la resolución mediante la cual la Rectora delega. Ej: UTA-R-2026-0007',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'DELEGATION_DATE', 'Fecha de la resolución de delegación',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 130,
        'Fecha de la resolución de delegación. Ej: 20-02-2026',
        GETDATE(), NULL
    ),

    -- ── DATOS DEL EMPLEADO ───────────────────────────────────────────────────
    (
        @TemplateID, 'EMPLOYEE_TITLE', 'Título académico del empleado',
        'EMPLOYEE', 'People.AcademicTitle', 'TEXT',
        NULL, NULL, 1, 0, 140,
        'Título académico del empleado. Ej: Ing. Mg., PhD, Lcda.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_FULLNAME', 'Nombre completo del empleado',
        'EMPLOYEE', 'People.FullName', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 150,
        'Apellidos y nombres completos del empleado tal como constan en la cédula.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_IDCARD', 'Número de cédula del empleado',
        'EMPLOYEE', 'People.IdCard', 'TEXT',
        NULL, NULL, 1, 0, 160,
        'Número de cédula de ciudadanía del empleado.',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'EMPLOYEE_CONTRACT_ROLE', 'Rol del empleado en el contrato',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', 'PROFESOR/A OCASIONAL', 1, 1, 170,
        'Denominación del rol del empleado en el contrato. Ej: PROFESOR/A OCASIONAL',
        GETDATE(), NULL
    ),

    -- ── RESOLUCIONES HABILITANTES ────────────────────────────────────────────
    (
        @TemplateID, 'CAU_RESOLUTION_NUMBER', 'Número de resolución CAU',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 180,
        'Número de resolución del Consejo Académico Universitario. Ej: CAU-P-236-2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'CAU_RESOLUTION_DATE', 'Fecha de resolución CAU',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 190,
        'Fecha de la resolución CAU. Ej: 17-04-2026',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'RECTOR_MEMO_NUMBER', 'Número de memorando del Rectorado',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 200,
        'Número del memorando de autorización del Rectorado. Ej: UTA-R-2026-0682-M',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'RECTOR_MEMO_DATE', 'Fecha del memorando del Rectorado',
        'MANUAL', NULL, 'DATE',
        'dd-MM-yyyy', NULL, 1, 1, 210,
        'Fecha del memorando de autorización del Rectorado. Ej: 22-04-2026',
        GETDATE(), NULL
    ),

    -- ── TABLA DISTRIBUTIVO DE TRABAJO (HTML dinámico) ────────────────────────
    (
        @TemplateID, 'WORK_DISTRIBUTION_TABLE', 'Tabla del distributivo de trabajo',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 220,
        'HTML completo de la tabla de distributivo de trabajo. Columnas: Función Sustantiva | Asignatura/Actividad | Nro. Horas | Carrera. El backend genera este HTML antes de sustituir el placeholder.',
        GETDATE(), NULL
    ),

    -- ── VIGENCIA DEL CONTRATO ────────────────────────────────────────────────
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

    -- ── REMUNERACIÓN ─────────────────────────────────────────────────────────
    (
        @TemplateID, 'SALARY_WORDS', 'Remuneración en palabras',
        'MANUAL', NULL, 'TEXT',
        'UPPERCASE', NULL, 1, 1, 250,
        'Monto de la remuneración escrito en palabras. Ej: DOS MIL CON 00/100',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'SALARY_AMOUNT', 'Remuneración en números',
        'CONTRACT', 'Contract.Salary', 'CURRENCY',
        'N2', NULL, 1, 0, 260,
        'Monto de la remuneración en formato numérico. Ej: 2000,00',
        GETDATE(), NULL
    ),
    (
        @TemplateID, 'BUDGET_CODE', 'Partida presupuestaria',
        'CONTRACT', 'Contract.BudgetCode', 'TEXT',
        NULL, NULL, 1, 0, 270,
        'Código de la partida presupuestaria del contrato.',
        GETDATE(), NULL
    ),

    -- ── TABLA HORARIO SEMANAL (HTML dinámico) ────────────────────────────────
    (
        @TemplateID, 'SCHEDULE_TABLE', 'Tabla del horario semanal',
        'MANUAL', NULL, 'TEXT',
        NULL, NULL, 1, 1, 280,
        'HTML completo de la tabla del horario semanal. Columnas: Horas | Lunes | Martes | Miércoles | Jueves | Viernes. El backend genera este HTML antes de sustituir el placeholder.',
        GETDATE(), NULL
    ),

    -- ── ELABORADOR DEL DOCUMENTO ─────────────────────────────────────────────
    (
        @TemplateID, 'ELABORATOR_FULLNAME', 'Nombre del elaborador del contrato',
        'SYSTEM', 'Config.ElaboratorName', 'TEXT',
        'UPPERCASE', NULL, 1, 0, 290,
        'Nombre completo del funcionario que elaboró el contrato. Se obtiene del usuario autenticado.',
        GETDATE(), NULL
    ),

    -- ── CERTIFICACIÓN DTH ────────────────────────────────────────────────────
    (
        @TemplateID, 'DTH_REGISTRY_NUMBER', 'Número de registro DTH',
        'SYSTEM', 'Config.DthRegistryNumber', 'TEXT',
        NULL, NULL, 1, 0, 300,
        'Número de registro asignado por la Dirección de Talento Humano. Ej: 012109-DTH-2026',
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
        'Nombre completo del Director de Talento Humano con título. Ej: Mg. ANDRADE PEÑAHERRERA WILSON EDUARDO',
        GETDATE(), NULL
    );

    -- ============================================================
    -- PASO 5: Confirmar la transacción
    -- ============================================================
    COMMIT TRANSACTION;

    -- ============================================================
    -- PASO 6: Verificación post-inserción
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

    -- Listado completo de campos en orden de aparición
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

    PRINT '✔ Plantilla CONTRATO_PROFESOR_OCASIONAL insertada correctamente.';
    PRINT '  TemplateID = ' + CAST(@TemplateID AS VARCHAR);
    PRINT '  Version    = ' + @NextVersion;
    PRINT '  Campos     = 32';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT            = ERROR_LINE();
    RAISERROR('Error en línea %d: %s', 16, 1, @ErrorLine, @ErrorMsg);
END CATCH;
