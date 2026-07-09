-- Carga de plantillas: Técnico Docente y Técnico de Laboratorio (con/sin delegación)
-- Generado automáticamente. Ver Database/hr/ para scripts base.

DECLARE @TplDocenteCon INT, @TplDocenteSin INT, @TplLabCon INT, @TplLabSin INT;

INSERT INTO HR.tbl_DocumentTemplates (TemplateCode, Name, Description, TemplateType, Version, LayoutType, Status, HtmlContent, RequiresSignature, RequiresApproval, CreatedAt)
VALUES (
  'CONTRATO_TECNICO_DOCENTE_DELEGACION',
  'Contrato Técnico Docente (Delegación) - Universidad Técnica de Ambato',
  'Plantilla de contrato de Técnico Docente firmado por delegación del Decano.',
  'CONTRATO', '1.0', 'FLOW_TEXT', 'PUBLISHED',
  N'<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Contrato Técnico Docente UTA</title>
    <style>
        @page {
            size: A4;
            margin: 2.5cm;
        }
        body {
            font-family: ''Times New Roman'', Times, serif;
            font-size: 11pt;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 0;
        }
        .header {
            text-align: center;
            margin-bottom: 20pt;
        }
        .header img {
            max-width: 150px;
            height: auto;
        }
        .institution-name {
            font-weight: bold;
            font-size: 12pt;
            margin-top: 5pt;
            text-transform: uppercase;
        }
        .document-title {
            font-weight: bold;
            font-size: 11pt;
            margin-top: 10pt;
            text-transform: uppercase;
        }
        .document-number {
            font-weight: bold;
            margin-top: 15pt;
            margin-bottom: 20pt;
            text-align: center;
        }
        p {
            text-align: justify;
            margin-bottom: 10pt;
        }
        .clause-title {
            font-weight: bold;
            text-transform: uppercase;
        }
        .bold {
            font-weight: bold;
        }
        .signature-section {
            margin-top: 50pt;
            width: 100%;
        }
        .signature-table {
            width: 100%;
            border-collapse: collapse;
        }
        .signature-cell {
            width: 50%;
            text-align: center;
            vertical-align: top;
            padding-top: 40pt;
        }
        .signature-line {
            width: 80%;
            border-top: 1px solid #000;
            margin: 0 auto 5pt auto;
        }
        .footer-info {
            font-size: 9pt;
            margin-top: 20pt;
            font-style: italic;
        }
        .page-break {
            page-break-before: always;
        }
        .declaration-title {
            font-weight: bold;
            text-align: justify;
            margin-bottom: 30pt;
            margin-top: 20pt;
        }
        .dth-cert-title {
            font-weight: bold;
            margin-top: 40pt;
            margin-bottom: 10pt;
        }
    </style>
</head>
<body>
    <div class="header">
        <!-- Espacio para el logo institucional -->
        <div class="institution-name">Universidad Técnica de Ambato</div>
        <div class="document-title">Contrato Técnico Docente</div>
    </div>

    <div class="document-number">
        {{CONTRACT_CODE}}
    </div>

    <p>
        En la ciudad de Ambato, a los <span class="bold">{{DATE_DAY_WORDS}}</span> días del mes de <span class="bold">{{DATE_MONTH_NAME}}</span> de <span class="bold">{{DATE_YEAR_WORDS}}</span>, comparecen: por una parte la Universidad Técnica de Ambato, representada por el/la señor/a DECANO de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, por delegación de la señora Rectora de la indicada Institución, Dra. Sara Nidhya Camacho Estrada Ph.D, mediante <span class="bold">{{DELEGATION_RESOLUTION}}</span>, con fecha <span class="bold">{{DELEGATION_DATE}}</span>, a la que en adelante y para efectos del presente contrato se le podrá llamar como El Contratante, o La Universidad; y por otra parte <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien así mismo para efectos del presente contrato se le podrá invocar por sus propios nombres que son los que quedan ya señalados, o el/la TÉCNICO DOCENTE, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, capaces, libre y voluntariamente convienen en celebrar el presente Contrato de Prestación de Servicios de Técnico Docente, al amparo de lo previsto en el Art. 118 y 120 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior, así como también a lo estipulado en el Art. 83 literal m) de la Ley Orgánica de Servicio Público, al tenor de las siguientes cláusulas y estipulaciones que se determinan a continuación:
    </p>

    <p>
        <span class="clause-title">Primera.- Antecedentes.-</span> En el Estatuto de la Universidad Técnica de Ambato en su Art. 10.- Objetivos.- La Universidad Técnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a través de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sólidos conocimientos científicos, tecnológicos y culturales, que interpreten y comprendan la realidad socioeconómica del Ecuador, de Latinoamérica y del mundo, y que, emprendan de manera autónoma en iniciativas que propicien el desarrollo socioeconómico de la provincia, la región y el país. y, Art. 35 del Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato.
    </p>

    <p>
        2) La suscripción del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior en función de lo resuelto mediante Resolución Nro: <span class="bold">{{RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{RESOLUTION_DATE}}</span> por medio de la cual el Consejo Académico Universitario tuvo a bien aprobar el distributivo de trabajo del personal académico; concomitantemente, con memorando Nro. <span class="bold">{{MEMORANDUM_NUMBER}}</span>, con fecha: <span class="bold">{{MEMORANDUM_DATE}}</span> en la que la señora Rectora de la Universidad Técnica de Ambato autoriza el presente Contrato de Prestación de Servicios de TÉCNICO DOCENTE con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.
    </p>

    <p>
        <span class="clause-title">Segunda.-</span> Por los antecedentes que quedan expuestos, el/la señor/a DECANO de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, conforme queda señalado en líneas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente:
    </p>

    <!-- Espacio para la tabla dinámica de distributivo de trabajo -->
    <div id="distributivo-trabajo">
        {{DISTRIBUTIVO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Tercera.-</span> El presente contrato tendrá vigencia del <span class="bold">{{CONTRACT_STARTDATE}}</span> al <span class="bold">{{CONTRACT_ENDDATE}}</span>.
    </p>

    <p>
        Una vez cumplida la vigencia del presente contrato, automáticamente se da por terminado el mismo, sin que sea menester formalidad o notificación alguna.
    </p>

    <p>
        <span class="clause-title">Cuarta.-</span> La Universidad Técnica de Ambato, por su parte, pagará a el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneración por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de Norteamérica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) más beneficios de ley, pago que se efectuará en forma mensual. El egreso se aplicará a la partida presupuestaria N° <span class="bold">{{BUDGET_ITEM}}</span>.
    </p>

    <p>
        <span class="clause-title">Quinta.-</span> "El/la Profesional" desempeñará las actividades inherentes a TÉCNICO DOCENTE y que se hace referencia a la cláusula segunda del presente contrato:
    </p>

    <!-- Espacio para la tabla dinámica de horario -->
    <div id="horario-semanal">
        {{HORARIO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Sexta.- Naturaleza Jurídica del Contrato.-</span> El presente contrato estará sujeto a la Ley Orgánica de Educación Superior, Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior y Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificación y/o carga horaria en función de los requerimientos de las Unidades Académicas; se entenderá incorporado en el presente contrato.
    </p>

    <p>
        <span class="clause-title">Séptima.-</span> Conforme queda señalado en la cláusula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automáticamente se dará por terminado el mismo, sin que sea menester formalidad o notificación alguna, o se podrá dar por terminado anticipadamente mediante una notificación realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.
    </p>

    <p>
        <span class="clause-title">Octava.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte de el/la TÉCNICO DOCENTE contratado/a, el retraso o incumplimiento de sus obligaciones contractuales dará lugar al pago de la indemnización de los daños y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificación alguna.
    </p>

    <p>
        <span class="clause-title">Novena.- Controversia.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vía amigable y sobre la base de los principios de buena fe, las partes contratantes se someterán a la vía alternativa de solución de conflictos sea medición o arbitraje ante el Centro de Mediación y Arbitraje de la Procuraduría General del Estado.
    </p>

    <p>
        <span class="clause-title">Décima.- Protección de Datos.-</span> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:
    </p>

    <ul>
        <li>Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</li>
        <li>Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.</li>
        <li>Generar bases de datos de acceso público.</li>
    </ul>

    <p>
        El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.
    </p>

    <p>
        Para constancia de su total acuerdo y conformidad con todas y cada una de las cláusulas del presente contrato, las partes suscriben en original y dos copias del mismo tenor y efecto.
    </p>

    <div class="signature-section">
        <table class="signature-table">
            <tr>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span><br>
                    {{AUTHORITY_ID}}<br>
                    DECANO
                </td>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span><br>
                    {{EMPLOYEE_IDCARD}}<br>
                    TÉCNICO DOCENTE
                </td>
            </tr>
        </table>
    </div>

    <div class="footer-info">
        Elaborado por: {{ELABORATOR_NAME}}
    </div>

    <div class="page-break"></div>

    <div class="document-title" style="text-align: center; margin-bottom: 40pt;">
        CONTRATO TÉCNICO DOCENTE (DELEGACIÓN) N° {{CONTRACT_CODE}}
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÓN PÚBLICA, NI HE RECIBIDO INDEMNIZACIÓN POR VENTA DE RENUNCIA O POR SUPRESIÓN DE PUESTO DE TRABAJO EN EL SECTOR PÚBLICO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÑO EL PUESTO DE ......................................... EN ........................................., SEGÚN EL HORARIO ADJUNTO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO TENGO NINGÚN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÁXIMA AUTORIDAD DE LA UNIVERSIDAD TÉCNICA DE AMBATO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="dth-cert-title">
        DIRECCIÓN DE TALENTO HUMANO - UNIVERSIDAD TÉCNICA DE AMBATO
    </div>

    <p>
        Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registró el presente contrato con el N° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE}}</span>.
    </p>

    <div style="text-align: center; margin-top: 50pt;">
        AMBATO:<br><br><br>
        __________________________________<br>
        <span class="bold">{{DTH_DIRECTOR_NAME}}</span><br>
        DIRECTOR
    </div>

</body>
</html>',
  0, 1, GETDATE()
);
SET @TplDocenteCon = SCOPE_IDENTITY();

INSERT INTO HR.tbl_DocumentTemplates (TemplateCode, Name, Description, TemplateType, Version, LayoutType, Status, HtmlContent, RequiresSignature, RequiresApproval, CreatedAt)
VALUES (
  'CONTRATO_TECNICO_DOCENTE_SIN_DELEGACION',
  'Contrato Técnico Docente (Sin Delegación) - Universidad Técnica de Ambato',
  'Plantilla de contrato de Técnico Docente firmado directamente por la Rectora.',
  'CONTRATO', '1.0', 'FLOW_TEXT', 'PUBLISHED',
  N'<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Contrato Técnico Docente UTA (Sin Delegación)</title>
    <style>
        @page {
            size: A4;
            margin: 2.5cm;
        }
        body {
            font-family: ''Times New Roman'', Times, serif;
            font-size: 11pt;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 0;
        }
        .header {
            text-align: center;
            margin-bottom: 20pt;
        }
        .institution-name {
            font-weight: bold;
            font-size: 12pt;
            margin-top: 5pt;
            text-transform: uppercase;
        }
        .document-title {
            font-weight: bold;
            font-size: 11pt;
            margin-top: 10pt;
            text-transform: uppercase;
        }
        .document-number {
            font-weight: bold;
            margin-top: 15pt;
            margin-bottom: 20pt;
            text-align: center;
        }
        p {
            text-align: justify;
            margin-bottom: 10pt;
        }
        .clause-title {
            font-weight: bold;
            text-transform: uppercase;
        }
        .bold {
            font-weight: bold;
        }
        .signature-section {
            margin-top: 50pt;
            width: 100%;
        }
        .signature-table {
            width: 100%;
            border-collapse: collapse;
        }
        .signature-cell {
            width: 50%;
            text-align: center;
            vertical-align: top;
            padding-top: 40pt;
        }
        .signature-line {
            width: 80%;
            border-top: 1px solid #000;
            margin: 0 auto 5pt auto;
        }
        .footer-info {
            font-size: 9pt;
            margin-top: 20pt;
            font-style: italic;
        }
        .page-break {
            page-break-before: always;
        }
        .declaration-title {
            font-weight: bold;
            text-align: justify;
            margin-bottom: 30pt;
            margin-top: 20pt;
        }
        .dth-cert-title {
            font-weight: bold;
            margin-top: 40pt;
            margin-bottom: 10pt;
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="institution-name">Universidad Técnica de Ambato</div>
        <div class="document-title">Contrato Técnico Docente</div>
    </div>

    <div class="document-number">
        {{CONTRACT_CODE}}
    </div>

    <p>
        En la ciudad de Ambato, a los <span class="bold">{{DATE_DAY_WORDS}}</span> días del mes de <span class="bold">{{DATE_MONTH_NAME}}</span> de <span class="bold">{{DATE_YEAR_WORDS}}</span>, comparecen: por una parte la Universidad Técnica de Ambato, representada por la señora Rectora, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, a quien en adelante y para efectos del presente contrato se le podrá llamar como El Contratante, o La Universidad; y por otra parte <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien así mismo para efectos del presente contrato se le podrá invocar por sus propios nombres que son los que quedan ya señalados, o el/la TÉCNICO DOCENTE, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, capaces, libre y voluntariamente convienen en celebrar el presente Contrato de Prestación de Servicios de Técnico Docente, al amparo de lo previsto en el Art. 118 y 120 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior, así como también a lo estipulado en el Art. 83 literal m) de la Ley Orgánica de Servicio Público, al tenor de las siguientes cláusulas y estipulaciones que se determinan a continuación:
    </p>

    <p>
        <span class="clause-title">Primera.- Antecedentes.-</span> En el Estatuto de la Universidad Técnica de Ambato en su Art. 10.- Objetivos.- La Universidad Técnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a través de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sólidos conocimientos científicos, tecnológicos y culturales, que interpreten y comprendan la realidad socioeconómica del Ecuador, de Latinoamérica y del mundo, y que, emprendan de manera autónoma en iniciativas que propicien el desarrollo socioeconómico de la provincia, la región y el país. y, Art. 35 del Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato.
    </p>

    <p>
        2) La suscripción del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior en función de lo resuelto mediante Resolución Nro: <span class="bold">{{RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{RESOLUTION_DATE}}</span> por medio de la cual el Consejo Académico Universitario tuvo a bien aprobar el distributivo de trabajo del personal académico; concomitantemente, con memorando Nro. <span class="bold">{{MEMORANDUM_NUMBER}}</span>, con fecha: <span class="bold">{{MEMORANDUM_DATE}}</span> en la que la señora Rectora de la Universidad Técnica de Ambato autoriza el presente Contrato de Prestación de Servicios de TÉCNICO DOCENTE con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.
    </p>

    <p>
        <span class="clause-title">Segunda.-</span> Por los antecedentes que quedan expuestos, la señora Rectora, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, conforme queda señalado en líneas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente:
    </p>

    <div id="distributivo-trabajo">
        {{DISTRIBUTIVO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Tercera.-</span> El presente contrato tendrá vigencia del <span class="bold">{{CONTRACT_STARTDATE}}</span> al <span class="bold">{{CONTRACT_ENDDATE}}</span>.
    </p>

    <p>
        Una vez cumplida la vigencia del presente contrato, automáticamente se da por terminado el mismo, sin que sea menester formalidad o notificación alguna.
    </p>

    <p>
        <span class="clause-title">Cuarta.-</span> La Universidad Técnica de Ambato, por su parte, pagará a el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneración por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de Norteamérica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) más beneficios de ley, pago que se efectuará en forma mensual. El egreso se aplicará a la partida presupuestaria N° <span class="bold">{{BUDGET_ITEM}}</span>.
    </p>

    <p>
        <span class="clause-title">Quinta.-</span> "El/la Profesional" desempeñará las actividades inherentes a TÉCNICO DOCENTE y que se hace referencia a la cláusula segunda del presente contrato:
    </p>

    <div id="horario-semanal">
        {{HORARIO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Sexta.- Naturaleza Jurídica del Contrato.-</span> El presente contrato estará sujeto a la Ley Orgánica de Educación Superior, Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior y Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificación y/o carga horaria en función de los requerimientos de las Unidades Académicas; se entenderá incorporado en el presente contrato.
    </p>

    <p>
        <span class="clause-title">Séptima.-</span> Conforme queda señalado en la cláusula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automáticamente se dará por terminado el mismo, sin que sea menester formalidad o notificación alguna, o se podrá dar por terminado anticipadamente mediante una notificación realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.
    </p>

    <p>
        <span class="clause-title">Octava.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte de el/la TÉCNICO DOCENTE contratado/a, el retraso o incumplimiento de sus obligaciones contractuales dará lugar al pago de la indemnización de los daños y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificación alguna.
    </p>

    <p>
        <span class="clause-title">Novena.- Controversia.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vía amigable y sobre la base de los principios de buena fe, las partes contratantes se someterán a la vía alternativa de solución de conflictos sea medición o arbitraje ante el Centro de Mediación y Arbitraje de la Procuraduría General del Estado.
    </p>

    <p>
        <span class="clause-title">Décima.- Protección de Datos.-</span> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:
    </p>

    <ul>
        <li>Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</li>
        <li>Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.</li>
        <li>Generar bases de datos de acceso público.</li>
    </ul>

    <p>
        El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.
    </p>

    <p>
        Para constancia de su total acuerdo y conformidad con todas y cada una de las cláusulas del presente contrato, las partes suscriben en original y dos copias del mismo tenor y efecto.
    </p>

    <div class="signature-section">
        <table class="signature-table">
            <tr>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span><br>
                    RECTORA
                </td>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span><br>
                    {{EMPLOYEE_IDCARD}}<br>
                    TÉCNICO DOCENTE
                </td>
            </tr>
        </table>
    </div>

    <div class="footer-info">
        Elaborado por: {{ELABORATOR_NAME}}
    </div>

    <div class="page-break"></div>

    <div class="document-title" style="text-align: center; margin-bottom: 40pt;">
        CONTRATO TÉCNICO DOCENTE N° {{CONTRACT_CODE}}
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÓN PÚBLICA, NI HE RECIBIDO INDEMNIZACIÓN POR VENTA DE RENUNCIA O POR SUPRESIÓN DE PUESTO DE TRABAJO EN EL SECTOR PÚBLICO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÑO EL PUESTO DE ......................................... EN ........................................., SEGÚN EL HORARIO ADJUNTO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO TENGO NINGÚN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÁXIMA AUTORIDAD DE LA UNIVERSIDAD TÉCNICA DE AMBATO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="dth-cert-title">
        DIRECCIÓN DE TALENTO HUMANO - UNIVERSIDAD TÉCNICA DE AMBATO
    </div>

    <p>
        Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registró el presente contrato con el N° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE}}</span>.
    </p>

    <div style="text-align: center; margin-top: 50pt;">
        AMBATO:<br><br><br>
        __________________________________<br>
        <span class="bold">{{DTH_DIRECTOR_NAME}}</span><br>
        DIRECTOR
    </div>

</body>
</html>',
  0, 1, GETDATE()
);
SET @TplDocenteSin = SCOPE_IDENTITY();

INSERT INTO HR.tbl_DocumentTemplates (TemplateCode, Name, Description, TemplateType, Version, LayoutType, Status, HtmlContent, RequiresSignature, RequiresApproval, CreatedAt)
VALUES (
  'CONTRATO_TECNICO_LABORATORIO_DELEGACION',
  'Contrato Técnico de Laboratorio (Delegación) - Universidad Técnica de Ambato',
  'Plantilla de contrato de Técnico de Laboratorio firmado por delegación del Decano.',
  'CONTRATO', '1.0', 'FLOW_TEXT', 'PUBLISHED',
  N'<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Contrato Técnico de Laboratorio UTA (Con Delegación)</title>
    <style>
        @page {
            size: A4;
            margin: 2.5cm;
        }
        body {
            font-family: ''Times New Roman'', Times, serif;
            font-size: 11pt;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 0;
        }
        .header {
            text-align: center;
            margin-bottom: 20pt;
        }
        .institution-name {
            font-weight: bold;
            font-size: 12pt;
            margin-top: 5pt;
            text-transform: uppercase;
        }
        .document-title {
            font-weight: bold;
            font-size: 11pt;
            margin-top: 10pt;
            text-transform: uppercase;
        }
        .document-number {
            font-weight: bold;
            margin-top: 15pt;
            margin-bottom: 20pt;
            text-align: center;
        }
        p {
            text-align: justify;
            margin-bottom: 10pt;
        }
        .clause-title {
            font-weight: bold;
            text-transform: uppercase;
        }
        .bold {
            font-weight: bold;
        }
        .signature-section {
            margin-top: 50pt;
            width: 100%;
        }
        .signature-table {
            width: 100%;
            border-collapse: collapse;
        }
        .signature-cell {
            width: 50%;
            text-align: center;
            vertical-align: top;
            padding-top: 40pt;
        }
        .signature-line {
            width: 80%;
            border-top: 1px solid #000;
            margin: 0 auto 5pt auto;
        }
        .footer-info {
            font-size: 9pt;
            margin-top: 20pt;
            font-style: italic;
        }
        .page-break {
            page-break-before: always;
        }
        .declaration-title {
            font-weight: bold;
            text-align: justify;
            margin-bottom: 30pt;
            margin-top: 20pt;
        }
        .dth-cert-title {
            font-weight: bold;
            margin-top: 40pt;
            margin-bottom: 10pt;
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="institution-name">Universidad Técnica de Ambato</div>
        <div class="document-title">Contrato Técnico de Laboratorio</div>
    </div>

    <div class="document-number">
        {{CONTRACT_CODE}}
    </div>

    <p>
        En la ciudad de Ambato, a los <span class="bold">{{DATE_DAY_WORDS}}</span> días del mes de <span class="bold">{{DATE_MONTH_NAME}}</span> de <span class="bold">{{DATE_YEAR_WORDS}}</span>, comparecen: por una parte el/la señor/a DECANO de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, por delegación de la señora Rectora de la indicada Institución, Dra. Sara Nidhya Camacho Estrada Ph.D, mediante <span class="bold">{{DELEGATION_RESOLUTION}}</span>, con fecha <span class="bold">{{DELEGATION_DATE}}</span>, a quien en adelante y para efectos del presente contrato se le podrá llamar "El Contratante", o simplemente "La Universidad"; y por otra parte el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien para los mismos efectos ya señalados se le podrá llamar el/la TÉCNICO DE LABORATORIO, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, libre y voluntariamente convienen en celebrar el presente Contrato de Prestación de Servicios Ocasionales, al amparo de lo establecido en los Art. 3 y 4 del Reglamento de Carrera y Escalafón del Personal Académico de Educación Superior; y, al tenor de las cláusulas y estipulaciones que se determinan a continuación:
    </p>

    <p>
        <span class="clause-title">Primera.- Antecedentes.-</span> En el Estatuto de la Universidad Técnica de Ambato en su Art. 10.- Objetivos.- La Universidad Técnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a través de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sólidos conocimientos científicos, tecnológicos y culturales, que interpreten y comprendan la realidad socioeconómica del Ecuador, de Latinoamérica y del mundo y que emprendan de manera autónoma en iniciativas que propicien el desarrollo socioeconómico de la provincia, la región y el país.
    </p>

    <p>
        2) La suscripción del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior en función de lo resuelto mediante Resolución Nro: <span class="bold">{{RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{RESOLUTION_DATE}}</span> por medio de la cual el Consejo Académico Universitario tuvo a bien aprobar el distributivo de trabajo del personal académico; concomitantemente, con memorando Nro. <span class="bold">{{MEMORANDUM_NUMBER}}</span>, con fecha: <span class="bold">{{MEMORANDUM_DATE}}</span> en la que la señora Rectora de la Universidad Técnica de Ambato autoriza el presente Contrato de Prestación de Servicios de TÉCNICO DE LABORATORIO con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.
    </p>

    <p>
        <span class="clause-title">Segunda.-</span> Por los antecedentes que quedan expuestos, el/la señor/a DECANO de la Facultad de <span class="bold">{{FACULTY_NAME}}</span>, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, conforme queda señalado en líneas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente; "El/la Profesional" desempeñará las actividades inherentes a Técnico de Laboratorio:
    </p>

    <div id="distributivo-trabajo">
        {{DISTRIBUTIVO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Tercera.-</span> El presente contrato tendrá vigencia del <span class="bold">{{CONTRACT_STARTDATE}}</span> al <span class="bold">{{CONTRACT_ENDDATE}}</span>.
    </p>

    <p>
        Una vez cumplida la vigencia del presente contrato, automáticamente se da por terminado el mismo, sin que sea menester formalidad o notificación alguna.
    </p>

    <p>
        <span class="clause-title">Cuarta.-</span> La Universidad Técnica de Ambato, por su parte, pagará a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneración por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de Norteamérica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) más beneficios de ley, pago que se efectuará en forma mensual. El egreso se aplicará a la partida presupuestaria N° <span class="bold">{{BUDGET_ITEM}}</span>.
    </p>

    <p>
        <span class="clause-title">Quinta.-</span> "El/la Profesional" desempeñará las actividades inherentes a Técnico de Laboratorio y que se hace referencia a la cláusula segunda del presente contrato:
    </p>

    <div id="horario-semanal">
        {{HORARIO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Sexta.- Naturaleza Jurídica del Contrato.-</span> El presente contrato estará sujeto a la Ley Orgánica de Educación Superior, Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior y Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificación y/o carga horaria en función de los requerimientos de las Unidades Académicas; se entenderá incorporado en el presente contrato.
    </p>

    <p>
        <span class="clause-title">Séptima.-</span> Conforme queda señalado en la cláusula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automáticamente se dará por terminado el mismo, sin que sea menester formalidad o notificación alguna, o se podrá dar por terminado anticipadamente mediante una notificación realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.
    </p>

    <p>
        <span class="clause-title">Octava.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte del TÉCNICO DE LABORATORIO contratado/a, el retraso o incumplimiento de sus obligaciones contractuales dará lugar al pago de la indemnización de los daños y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificación alguna.
    </p>

    <p>
        <span class="clause-title">Novena.- Controversia.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vía amigable y sobre la base de los principios de buena fe, las partes contratantes se someterán a los jueces competentes de esta ciudad de Ambato, provincia del Tungurahua y se sujetará al trámite Sumario.
    </p>

    <p>
        <span class="clause-title">Décima.- Protección de Datos.-</span> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:
    </p>

    <ul>
        <li>Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</li>
        <li>Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.</li>
        <li>Generar bases de datos de acceso público.</li>
    </ul>

    <p>
        El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.
    </p>

    <p>
        Para constancia de su total acuerdo y conformidad con todas y cada una de las cláusulas del presente contrato, las partes suscriben en original y cuatro copias del mismo tenor y efecto.
    </p>

    <div class="signature-section">
        <table class="signature-table">
            <tr>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span><br>
                    {{AUTHORITY_ID}}<br>
                    DECANO
                </td>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span><br>
                    {{EMPLOYEE_IDCARD}}<br>
                    TÉCNICO DE LABORATORIO
                </td>
            </tr>
        </table>
    </div>

    <div class="footer-info">
        Elaborado por: {{ELABORATOR_NAME}}
    </div>

    <div class="page-break"></div>

    <div class="document-title" style="text-align: center; margin-bottom: 40pt;">
        CONTRATO TÉCNICO DE LABORATORIO (DELEGACIÓN) N° {{CONTRACT_CODE}}
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÓN PÚBLICA, NI HE RECIBIDO INDEMNIZACIÓN POR VENTA DE RENUNCIA O POR SUPRESIÓN DE PUESTO DE TRABAJO EN EL SECTOR PÚBLICO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÑO EL PUESTO DE ......................................... EN ........................................., SEGÚN EL HORARIO ADJUNTO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO TENGO NINGÚN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÁXIMA AUTORIDAD DE LA UNIVERSIDAD TÉCNICA DE AMBATO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="dth-cert-title">
        DIRECCIÓN DE TALENTO HUMANO - UNIVERSIDAD TÉCNICA DE AMBATO
    </div>

    <p>
        Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registró el presente contrato con el N° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE}}</span>.
    </p>

    <div style="text-align: center; margin-top: 50pt;">
        AMBATO:<br><br><br>
        __________________________________<br>
        <span class="bold">{{DTH_DIRECTOR_NAME}}</span><br>
        DIRECTOR
    </div>

</body>
</html>',
  0, 1, GETDATE()
);
SET @TplLabCon = SCOPE_IDENTITY();

INSERT INTO HR.tbl_DocumentTemplates (TemplateCode, Name, Description, TemplateType, Version, LayoutType, Status, HtmlContent, RequiresSignature, RequiresApproval, CreatedAt)
VALUES (
  'CONTRATO_TECNICO_LABORATORIO_SIN_DELEGACION',
  'Contrato Técnico de Laboratorio (Sin Delegación) - Universidad Técnica de Ambato',
  'Plantilla de contrato de Técnico de Laboratorio firmado directamente por la Rectora.',
  'CONTRATO', '1.0', 'FLOW_TEXT', 'PUBLISHED',
  N'<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Contrato Técnico de Laboratorio UTA (Sin Delegación)</title>
    <style>
        @page {
            size: A4;
            margin: 2.5cm;
        }
        body {
            font-family: ''Times New Roman'', Times, serif;
            font-size: 11pt;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 0;
        }
        .header {
            text-align: center;
            margin-bottom: 20pt;
        }
        .institution-name {
            font-weight: bold;
            font-size: 12pt;
            margin-top: 5pt;
            text-transform: uppercase;
        }
        .document-title {
            font-weight: bold;
            font-size: 11pt;
            margin-top: 10pt;
            text-transform: uppercase;
        }
        .document-number {
            font-weight: bold;
            margin-top: 15pt;
            margin-bottom: 20pt;
            text-align: center;
        }
        p {
            text-align: justify;
            margin-bottom: 10pt;
        }
        .clause-title {
            font-weight: bold;
            text-transform: uppercase;
        }
        .bold {
            font-weight: bold;
        }
        .signature-section {
            margin-top: 50pt;
            width: 100%;
        }
        .signature-table {
            width: 100%;
            border-collapse: collapse;
        }
        .signature-cell {
            width: 50%;
            text-align: center;
            vertical-align: top;
            padding-top: 40pt;
        }
        .signature-line {
            width: 80%;
            border-top: 1px solid #000;
            margin: 0 auto 5pt auto;
        }
        .footer-info {
            font-size: 9pt;
            margin-top: 20pt;
            font-style: italic;
        }
        .page-break {
            page-break-before: always;
        }
        .declaration-title {
            font-weight: bold;
            text-align: justify;
            margin-bottom: 30pt;
            margin-top: 20pt;
        }
        .dth-cert-title {
            font-weight: bold;
            margin-top: 40pt;
            margin-bottom: 10pt;
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="institution-name">Universidad Técnica de Ambato</div>
        <div class="document-title">Contrato Técnico de Laboratorio</div>
    </div>

    <div class="document-number">
        {{CONTRACT_CODE}}
    </div>

    <p>
        En la ciudad de Ambato, a los <span class="bold">{{DATE_DAY_WORDS}}</span> días del mes de <span class="bold">{{DATE_MONTH_NAME}}</span> de <span class="bold">{{DATE_YEAR_WORDS}}</span>, comparecen: por una parte la señora Rectora, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, a quien en adelante y para efectos del presente contrato se le podrá llamar "El Contratante", o simplemente "La Universidad"; y por otra parte el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, a quien para los mismos efectos ya señalados se le podrá llamar el/la TÉCNICO DE LABORATORIO, quienes por los derechos a los que se representa y por el suyo propio, respectivamente, libre y voluntariamente convienen en celebrar el presente Contrato de Prestación de Servicios Ocasionales, al amparo de lo establecido en los Art. 3 y 4 del Reglamento de Carrera y Escalafón del Personal Académico de Educación Superior; y, al tenor de las cláusulas y estipulaciones que se determinan a continuación:
    </p>

    <p>
        <span class="clause-title">Primera.- Antecedentes.-</span> En el Estatuto de la Universidad Técnica de Ambato en su Art. 10.- Objetivos.- La Universidad Técnica de Ambato tiene los siguientes objetivos: a) Formar talento humano de grado y posgrado a través de diferentes modalidades, con liderazgo, responsabilidad social y ambiental, con sólidos conocimientos científicos, tecnológicos y culturales, que interpreten y comprendan la realidad socioeconómica del Ecuador, de Latinoamérica y del mundo y que emprendan de manera autónoma en iniciativas que propicien el desarrollo socioeconómico de la provincia, la región y el país.
    </p>

    <p>
        2) La suscripción del presente contrato procede conforme a lo preceptuado en el Art. 52 del Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior en función de lo resuelto mediante Resolución Nro: <span class="bold">{{RESOLUTION_NUMBER}}</span>, con fecha <span class="bold">{{RESOLUTION_DATE}}</span> por medio de la cual el Consejo Académico Universitario tuvo a bien aprobar el distributivo de trabajo del personal académico; concomitantemente, con memorando Nro. <span class="bold">{{MEMORANDUM_NUMBER}}</span>, con fecha: <span class="bold">{{MEMORANDUM_DATE}}</span> en la que la señora Rectora de la Universidad Técnica de Ambato autoriza el presente Contrato de Prestación de Servicios de TÉCNICO DE LABORATORIO con el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>.
    </p>

    <p>
        <span class="clause-title">Segunda.-</span> Por los antecedentes que quedan expuestos, la señora Rectora, <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span>, conforme queda señalado en líneas anteriores, tiene a bien contratar a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, conforme al siguiente distributivo de trabajo del personal docente; "El/la Profesional" desempeñará las actividades inherentes a Técnico de Laboratorio:
    </p>

    <div id="distributivo-trabajo">
        {{DISTRIBUTIVO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Tercera.-</span> El presente contrato tendrá vigencia del <span class="bold">{{CONTRACT_STARTDATE}}</span> al <span class="bold">{{CONTRACT_ENDDATE}}</span>.
    </p>

    <p>
        Una vez cumplida la vigencia del presente contrato, automáticamente se da por terminado el mismo, sin que sea menester formalidad o notificación alguna.
    </p>

    <p>
        <span class="clause-title">Cuarta.-</span> La Universidad Técnica de Ambato, por su parte, pagará a <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span>, en concepto de remuneración por los servicios a prestar, la suma total de <span class="bold">{{SALARY_WORDS}}</span> DOLARES de los Estados Unidos de Norteamérica (USD <span class="bold">{{SALARY_AMOUNT}}</span>) más beneficios de ley, pago que se efectuará en forma mensual. El egreso se aplicará a la partida presupuestaria N° <span class="bold">{{BUDGET_ITEM}}</span>.
    </p>

    <p>
        <span class="clause-title">Quinta.-</span> "El/la Profesional" desempeñará las actividades inherentes a Técnico de Laboratorio y que se hace referencia a la cláusula segunda del presente contrato:
    </p>

    <div id="horario-semanal">
        {{HORARIO_TABLE_HTML}}
    </div>

    <p>
        <span class="clause-title">Sexta.- Naturaleza Jurídica del Contrato.-</span> El presente contrato estará sujeto a la Ley Orgánica de Educación Superior, Reglamento de Carrera y Escalafón del Personal Académico del Sistema de Educación Superior y Reglamento de Carrera y Escalafón del Personal Académico de la Universidad Técnica de Ambato. De existir modificaciones en el Distributivo relacionado con la planificación y/o carga horaria en función de los requerimientos de las Unidades Académicas; se entenderá incorporado en el presente contrato.
    </p>

    <p>
        <span class="clause-title">Séptima.-</span> Conforme queda señalado en la cláusula tercera del presente contrato y por lo mismo una vez vencido el plazo estipulado, automáticamente se dará por terminado el mismo, sin que sea menester formalidad o notificación alguna, o se podrá dar por terminado anticipadamente mediante una notificación realizada por el representante legal de la Universidad, o su delegado, o por solicitud expresa del contratado.
    </p>

    <p>
        <span class="clause-title">Octava.-</span> Salvo circunstancia de fuerza mayor o caso fortuito debidamente comprobados por parte del TÉCNICO DE LABORATORIO contratado/a, el retraso o incumplimiento de sus obligaciones contractuales dará lugar al pago de la indemnización de los daños y perjuicios ocasionados o que llegare a ocasionar a la Universidad, cuando aquello obedezca a causas que no tengan justificación alguna.
    </p>

    <p>
        <span class="clause-title">Novena.- Controversia.-</span> Para el evento de producirse controversias derivadas de la falta de cumplimiento del presente contrato, que no puedan o que no deban superarse por la vía amigable y sobre la base de los principios de buena fe, las partes contratantes se someterán a los jueces competentes de esta ciudad de Ambato, provincia del Tungurahua y se sujetará al trámite Sumario.
    </p>

    <p>
        <span class="clause-title">Décima.- Protección de Datos.-</span> En cumplimiento con la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que, la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades:
    </p>

    <ul>
        <li>Cumplir con obligaciones contractuales legales, tributarias y de seguridad social.</li>
        <li>Generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES.</li>
        <li>Generar bases de datos de acceso público.</li>
    </ul>

    <p>
        El titular de los datos personales autoriza expresamente, al momento de proporcionar su información, el tratamiento de los mismos en conformidad con la Ley Orgánica de Protección de Datos Personales en Ecuador. En caso de tratarse de datos sensibles, el consentimiento será solicitado y recabado de manera explícita y fehaciente.
    </p>

    <p>
        Para constancia de su total acuerdo y conformidad con todas y cada una de las cláusulas del presente contrato, las partes suscriben en original y cuatro copias del mismo tenor y efecto.
    </p>

    <div class="signature-section">
        <table class="signature-table">
            <tr>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{AUTHORITY_TITLE}} {{AUTHORITY_NAME}}</span><br>
                    RECTORA
                </td>
                <td class="signature-cell">
                    <div class="signature-line"></div>
                    <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span><br>
                    {{EMPLOYEE_IDCARD}}<br>
                    TÉCNICO DE LABORATORIO
                </td>
            </tr>
        </table>
    </div>

    <div class="footer-info">
        Elaborado por: {{ELABORATOR_NAME}}
    </div>

    <div class="page-break"></div>

    <div class="document-title" style="text-align: center; margin-bottom: 40pt;">
        CONTRATO TÉCNICO DE LABORATORIO N° {{CONTRACT_CODE}}
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO LABORO EN OTRA INSTITUCIÓN PÚBLICA, NI HE RECIBIDO INDEMNIZACIÓN POR VENTA DE RENUNCIA O POR SUPRESIÓN DE PUESTO DE TRABAJO EN EL SECTOR PÚBLICO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO QUE ADEMAS DEL CARGO PARA EL QUE ESTOY SIENDO DESIGNADO(A), DESEMPEÑO EL PUESTO DE ......................................... EN ........................................., SEGÚN EL HORARIO ADJUNTO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="declaration-title">
        DECLARO BAJO JURAMENTO QUE NO TENGO NINGÚN PARENTESCO HASTA EL CUARTO GRADO DE CONSANGUINIDAD, NI HASTA EL SEGUNDO GRADO DE AFINIDAD CON LA MÁXIMA AUTORIDAD DE LA UNIVERSIDAD TÉCNICA DE AMBATO.
    </div>
    <div style="margin-left: 50pt; margin-bottom: 40pt;">
        f) __________________________________
    </div>

    <div class="dth-cert-title">
        DIRECCIÓN DE TALENTO HUMANO - UNIVERSIDAD TÉCNICA DE AMBATO
    </div>

    <p>
        Certifico que el/la <span class="bold">{{EMPLOYEE_TITLE}} {{EMPLOYEE_FULLNAME}}</span> registró el presente contrato con el N° <span class="bold">{{DTH_REGISTRY_NUMBER}}</span> el <span class="bold">{{DTH_REGISTRY_DATE}}</span>.
    </p>

    <div style="text-align: center; margin-top: 50pt;">
        AMBATO:<br><br><br>
        __________________________________<br>
        <span class="bold">{{DTH_DIRECTOR_NAME}}</span><br>
        DIRECTOR
    </div>

</body>
</html>',
  0, 1, GETDATE()
);
SET @TplLabSin = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────
-- Campos: comunes a las 4 plantillas (mismo set en CON y SIN delegación,
-- salvo los exclusivos de delegación: FACULTY_NAME, AUTHORITY_IDCARD,
-- DELEGATION_RESOLUTION, DELEGATION_DATE)
-- ─────────────────────────────────────────────────────────────────────────

-- Plantilla: Técnico Docente CON delegación
INSERT INTO HR.tbl_DocumentTemplateFields (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType, IsRequired, IsEditable, SortOrder)
VALUES
(@TplDocenteCon, 'CONTRACT_CODE',         'Código del contrato',                  'CONTRACT', 'Contract.ContractCode',  'TEXT', 1, 0, 10),
(@TplDocenteCon, 'DATE_DAY_WORDS',        'Día de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 20),
(@TplDocenteCon, 'DATE_MONTH_NAME',       'Mes de suscripción',                    'MANUAL',   NULL,                     'TEXT', 1, 0, 30),
(@TplDocenteCon, 'DATE_YEAR_WORDS',       'Año de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 40),
(@TplDocenteCon, 'FACULTY_NAME',          'Facultad del Decano delegado',          'MANUAL',   NULL,                     'TEXT', 1, 0, 50),
(@TplDocenteCon, 'AUTHORITY_TITLE',       'Título del firmante (Decano/Rectora)',  'SYSTEM',   'Authority.Title',        'TEXT', 1, 0, 60),
(@TplDocenteCon, 'AUTHORITY_NAME',        'Nombre del firmante (Decano/Rectora)',  'SYSTEM',   'Authority.Name',         'TEXT', 1, 0, 70),
(@TplDocenteCon, 'AUTHORITY_ID',          'Cédula del Decano delegado',            'MANUAL',   NULL,                     'TEXT', 1, 0, 80),
(@TplDocenteCon, 'DELEGATION_RESOLUTION', 'Resolución de delegación',              'MANUAL',   NULL,                     'TEXT', 1, 0, 90),
(@TplDocenteCon, 'DELEGATION_DATE',       'Fecha de la resolución de delegación',  'MANUAL',   NULL,                     'DATE', 1, 0, 100),
(@TplDocenteCon, 'EMPLOYEE_TITLE',        'Título académico del empleado',         'MANUAL',   NULL,                     'TEXT', 1, 1, 110),
(@TplDocenteCon, 'EMPLOYEE_FULLNAME',     'Nombre completo del empleado',          'EMPLOYEE', 'People.FullName',        'TEXT', 1, 0, 120),
(@TplDocenteCon, 'EMPLOYEE_IDCARD',       'Cédula del empleado',                   'EMPLOYEE', 'People.IdCard',          'TEXT', 1, 0, 130),
(@TplDocenteCon, 'RESOLUTION_NUMBER',     'Número de resolución CAU',              'MANUAL',   NULL,                     'TEXT', 1, 1, 140),
(@TplDocenteCon, 'RESOLUTION_DATE',       'Fecha de resolución CAU',               'MANUAL',   NULL,                     'DATE', 1, 1, 150),
(@TplDocenteCon, 'MEMORANDUM_NUMBER',     'Número de memorando del Rectorado',     'MANUAL',   NULL,                     'TEXT', 1, 1, 160),
(@TplDocenteCon, 'MEMORANDUM_DATE',       'Fecha del memorando del Rectorado',     'MANUAL',   NULL,                     'DATE', 1, 1, 170),
(@TplDocenteCon, 'DISTRIBUTIVO_TABLE_HTML','Tabla del distributivo de trabajo',    'MANUAL',   NULL,                     'TEXT', 1, 1, 180),
(@TplDocenteCon, 'CONTRACT_STARTDATE',    'Fecha de inicio de vigencia',           'CONTRACT', 'Contract.StartDate',     'DATE', 1, 0, 190),
(@TplDocenteCon, 'CONTRACT_ENDDATE',      'Fecha de fin de vigencia',              'CONTRACT', 'Contract.EndDate',       'DATE', 1, 0, 200),
(@TplDocenteCon, 'SALARY_WORDS',          'Remuneración en palabras',              'MANUAL',   NULL,                     'TEXT', 1, 0, 210),
(@TplDocenteCon, 'SALARY_AMOUNT',         'Remuneración en números',               'MANUAL',   NULL,                     'CURRENCY', 1, 0, 220),
(@TplDocenteCon, 'BUDGET_ITEM',           'Partida presupuestaria',                'MANUAL',   NULL,                     'TEXT', 1, 0, 230),
(@TplDocenteCon, 'HORARIO_TABLE_HTML',    'Tabla del horario semanal',             'MANUAL',   NULL,                     'TEXT', 1, 1, 240),
(@TplDocenteCon, 'ELABORATOR_NAME',       'Nombre del elaborador',                 'SYSTEM',   'Config.ElaboratorName',  'TEXT', 1, 0, 250),
(@TplDocenteCon, 'DTH_REGISTRY_NUMBER',   'Número de registro DTH',                'MANUAL',   NULL,                     'TEXT', 1, 1, 260),
(@TplDocenteCon, 'DTH_REGISTRY_DATE',     'Fecha de registro DTH',                 'MANUAL',   NULL,                     'DATE', 1, 0, 270),
(@TplDocenteCon, 'DTH_DIRECTOR_NAME',     'Nombre del Director de Talento Humano', 'SYSTEM',   'Authority.HumanResource','TEXT', 1, 0, 280);

-- Plantilla: Técnico Docente SIN delegación (mismo set, sin los 4 campos de delegación)
INSERT INTO HR.tbl_DocumentTemplateFields (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType, IsRequired, IsEditable, SortOrder)
VALUES
(@TplDocenteSin, 'CONTRACT_CODE',         'Código del contrato',                  'CONTRACT', 'Contract.ContractCode',  'TEXT', 1, 0, 10),
(@TplDocenteSin, 'DATE_DAY_WORDS',        'Día de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 20),
(@TplDocenteSin, 'DATE_MONTH_NAME',       'Mes de suscripción',                    'MANUAL',   NULL,                     'TEXT', 1, 0, 30),
(@TplDocenteSin, 'DATE_YEAR_WORDS',       'Año de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 40),
(@TplDocenteSin, 'AUTHORITY_TITLE',       'Título de la Rectora',                  'SYSTEM',   'Authority.Title',        'TEXT', 1, 0, 50),
(@TplDocenteSin, 'AUTHORITY_NAME',        'Nombre de la Rectora',                  'SYSTEM',   'Authority.Name',         'TEXT', 1, 0, 60),
(@TplDocenteSin, 'EMPLOYEE_TITLE',        'Título académico del empleado',         'MANUAL',   NULL,                     'TEXT', 1, 1, 70),
(@TplDocenteSin, 'EMPLOYEE_FULLNAME',     'Nombre completo del empleado',          'EMPLOYEE', 'People.FullName',        'TEXT', 1, 0, 80),
(@TplDocenteSin, 'EMPLOYEE_IDCARD',       'Cédula del empleado',                   'EMPLOYEE', 'People.IdCard',          'TEXT', 1, 0, 90),
(@TplDocenteSin, 'RESOLUTION_NUMBER',     'Número de resolución CAU',              'MANUAL',   NULL,                     'TEXT', 1, 1, 100),
(@TplDocenteSin, 'RESOLUTION_DATE',       'Fecha de resolución CAU',               'MANUAL',   NULL,                     'DATE', 1, 1, 110),
(@TplDocenteSin, 'MEMORANDUM_NUMBER',     'Número de memorando del Rectorado',     'MANUAL',   NULL,                     'TEXT', 1, 1, 120),
(@TplDocenteSin, 'MEMORANDUM_DATE',       'Fecha del memorando del Rectorado',     'MANUAL',   NULL,                     'DATE', 1, 1, 130),
(@TplDocenteSin, 'DISTRIBUTIVO_TABLE_HTML','Tabla del distributivo de trabajo',    'MANUAL',   NULL,                     'TEXT', 1, 1, 140),
(@TplDocenteSin, 'CONTRACT_STARTDATE',    'Fecha de inicio de vigencia',           'CONTRACT', 'Contract.StartDate',     'DATE', 1, 0, 150),
(@TplDocenteSin, 'CONTRACT_ENDDATE',      'Fecha de fin de vigencia',              'CONTRACT', 'Contract.EndDate',       'DATE', 1, 0, 160),
(@TplDocenteSin, 'SALARY_WORDS',          'Remuneración en palabras',              'MANUAL',   NULL,                     'TEXT', 1, 0, 170),
(@TplDocenteSin, 'SALARY_AMOUNT',         'Remuneración en números',               'MANUAL',   NULL,                     'CURRENCY', 1, 0, 180),
(@TplDocenteSin, 'BUDGET_ITEM',           'Partida presupuestaria',                'MANUAL',   NULL,                     'TEXT', 1, 0, 190),
(@TplDocenteSin, 'HORARIO_TABLE_HTML',    'Tabla del horario semanal',             'MANUAL',   NULL,                     'TEXT', 1, 1, 200),
(@TplDocenteSin, 'ELABORATOR_NAME',       'Nombre del elaborador',                 'SYSTEM',   'Config.ElaboratorName',  'TEXT', 1, 0, 210),
(@TplDocenteSin, 'DTH_REGISTRY_NUMBER',   'Número de registro DTH',                'MANUAL',   NULL,                     'TEXT', 1, 1, 220),
(@TplDocenteSin, 'DTH_REGISTRY_DATE',     'Fecha de registro DTH',                 'MANUAL',   NULL,                     'DATE', 1, 0, 230),
(@TplDocenteSin, 'DTH_DIRECTOR_NAME',     'Nombre del Director de Talento Humano', 'SYSTEM',   'Authority.HumanResource','TEXT', 1, 0, 240);

-- Plantilla: Técnico de Laboratorio CON delegación (mismo set que Docente Con)
INSERT INTO HR.tbl_DocumentTemplateFields (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType, IsRequired, IsEditable, SortOrder)
VALUES
(@TplLabCon, 'CONTRACT_CODE',         'Código del contrato',                  'CONTRACT', 'Contract.ContractCode',  'TEXT', 1, 0, 10),
(@TplLabCon, 'DATE_DAY_WORDS',        'Día de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 20),
(@TplLabCon, 'DATE_MONTH_NAME',       'Mes de suscripción',                    'MANUAL',   NULL,                     'TEXT', 1, 0, 30),
(@TplLabCon, 'DATE_YEAR_WORDS',       'Año de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 40),
(@TplLabCon, 'FACULTY_NAME',          'Facultad del Decano delegado',          'MANUAL',   NULL,                     'TEXT', 1, 0, 50),
(@TplLabCon, 'AUTHORITY_TITLE',       'Título del firmante (Decano/Rectora)',  'SYSTEM',   'Authority.Title',        'TEXT', 1, 0, 60),
(@TplLabCon, 'AUTHORITY_NAME',        'Nombre del firmante (Decano/Rectora)',  'SYSTEM',   'Authority.Name',         'TEXT', 1, 0, 70),
(@TplLabCon, 'AUTHORITY_ID',          'Cédula del Decano delegado',            'MANUAL',   NULL,                     'TEXT', 1, 0, 80),
(@TplLabCon, 'DELEGATION_RESOLUTION', 'Resolución de delegación',              'MANUAL',   NULL,                     'TEXT', 1, 0, 90),
(@TplLabCon, 'DELEGATION_DATE',       'Fecha de la resolución de delegación',  'MANUAL',   NULL,                     'DATE', 1, 0, 100),
(@TplLabCon, 'EMPLOYEE_TITLE',        'Título académico del empleado',         'MANUAL',   NULL,                     'TEXT', 1, 1, 110),
(@TplLabCon, 'EMPLOYEE_FULLNAME',     'Nombre completo del empleado',          'EMPLOYEE', 'People.FullName',        'TEXT', 1, 0, 120),
(@TplLabCon, 'EMPLOYEE_IDCARD',       'Cédula del empleado',                   'EMPLOYEE', 'People.IdCard',          'TEXT', 1, 0, 130),
(@TplLabCon, 'RESOLUTION_NUMBER',     'Número de resolución CAU',              'MANUAL',   NULL,                     'TEXT', 1, 1, 140),
(@TplLabCon, 'RESOLUTION_DATE',       'Fecha de resolución CAU',               'MANUAL',   NULL,                     'DATE', 1, 1, 150),
(@TplLabCon, 'MEMORANDUM_NUMBER',     'Número de memorando del Rectorado',     'MANUAL',   NULL,                     'TEXT', 1, 1, 160),
(@TplLabCon, 'MEMORANDUM_DATE',       'Fecha del memorando del Rectorado',     'MANUAL',   NULL,                     'DATE', 1, 1, 170),
(@TplLabCon, 'DISTRIBUTIVO_TABLE_HTML','Tabla del distributivo de trabajo',    'MANUAL',   NULL,                     'TEXT', 1, 1, 180),
(@TplLabCon, 'CONTRACT_STARTDATE',    'Fecha de inicio de vigencia',           'CONTRACT', 'Contract.StartDate',     'DATE', 1, 0, 190),
(@TplLabCon, 'CONTRACT_ENDDATE',      'Fecha de fin de vigencia',              'CONTRACT', 'Contract.EndDate',       'DATE', 1, 0, 200),
(@TplLabCon, 'SALARY_WORDS',          'Remuneración en palabras',              'MANUAL',   NULL,                     'TEXT', 1, 0, 210),
(@TplLabCon, 'SALARY_AMOUNT',         'Remuneración en números',               'MANUAL',   NULL,                     'CURRENCY', 1, 0, 220),
(@TplLabCon, 'BUDGET_ITEM',           'Partida presupuestaria',                'MANUAL',   NULL,                     'TEXT', 1, 0, 230),
(@TplLabCon, 'HORARIO_TABLE_HTML',    'Tabla del horario semanal',             'MANUAL',   NULL,                     'TEXT', 1, 1, 240),
(@TplLabCon, 'ELABORATOR_NAME',       'Nombre del elaborador',                 'SYSTEM',   'Config.ElaboratorName',  'TEXT', 1, 0, 250),
(@TplLabCon, 'DTH_REGISTRY_NUMBER',   'Número de registro DTH',                'MANUAL',   NULL,                     'TEXT', 1, 1, 260),
(@TplLabCon, 'DTH_REGISTRY_DATE',     'Fecha de registro DTH',                 'MANUAL',   NULL,                     'DATE', 1, 0, 270),
(@TplLabCon, 'DTH_DIRECTOR_NAME',     'Nombre del Director de Talento Humano', 'SYSTEM',   'Authority.HumanResource','TEXT', 1, 0, 280);

-- Plantilla: Técnico de Laboratorio SIN delegación (mismo set que Docente Sin)
INSERT INTO HR.tbl_DocumentTemplateFields (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType, IsRequired, IsEditable, SortOrder)
VALUES
(@TplLabSin, 'CONTRACT_CODE',         'Código del contrato',                  'CONTRACT', 'Contract.ContractCode',  'TEXT', 1, 0, 10),
(@TplLabSin, 'DATE_DAY_WORDS',        'Día de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 20),
(@TplLabSin, 'DATE_MONTH_NAME',       'Mes de suscripción',                    'MANUAL',   NULL,                     'TEXT', 1, 0, 30),
(@TplLabSin, 'DATE_YEAR_WORDS',       'Año de suscripción en palabras',        'MANUAL',   NULL,                     'TEXT', 1, 0, 40),
(@TplLabSin, 'AUTHORITY_TITLE',       'Título de la Rectora',                  'SYSTEM',   'Authority.Title',        'TEXT', 1, 0, 50),
(@TplLabSin, 'AUTHORITY_NAME',        'Nombre de la Rectora',                  'SYSTEM',   'Authority.Name',         'TEXT', 1, 0, 60),
(@TplLabSin, 'EMPLOYEE_TITLE',        'Título académico del empleado',         'MANUAL',   NULL,                     'TEXT', 1, 1, 70),
(@TplLabSin, 'EMPLOYEE_FULLNAME',     'Nombre completo del empleado',          'EMPLOYEE', 'People.FullName',        'TEXT', 1, 0, 80),
(@TplLabSin, 'EMPLOYEE_IDCARD',       'Cédula del empleado',                   'EMPLOYEE', 'People.IdCard',          'TEXT', 1, 0, 90),
(@TplLabSin, 'RESOLUTION_NUMBER',     'Número de resolución CAU',              'MANUAL',   NULL,                     'TEXT', 1, 1, 100),
(@TplLabSin, 'RESOLUTION_DATE',       'Fecha de resolución CAU',               'MANUAL',   NULL,                     'DATE', 1, 1, 110),
(@TplLabSin, 'MEMORANDUM_NUMBER',     'Número de memorando del Rectorado',     'MANUAL',   NULL,                     'TEXT', 1, 1, 120),
(@TplLabSin, 'MEMORANDUM_DATE',       'Fecha del memorando del Rectorado',     'MANUAL',   NULL,                     'DATE', 1, 1, 130),
(@TplLabSin, 'DISTRIBUTIVO_TABLE_HTML','Tabla del distributivo de trabajo',    'MANUAL',   NULL,                     'TEXT', 1, 1, 140),
(@TplLabSin, 'CONTRACT_STARTDATE',    'Fecha de inicio de vigencia',           'CONTRACT', 'Contract.StartDate',     'DATE', 1, 0, 150),
(@TplLabSin, 'CONTRACT_ENDDATE',      'Fecha de fin de vigencia',              'CONTRACT', 'Contract.EndDate',       'DATE', 1, 0, 160),
(@TplLabSin, 'SALARY_WORDS',          'Remuneración en palabras',              'MANUAL',   NULL,                     'TEXT', 1, 0, 170),
(@TplLabSin, 'SALARY_AMOUNT',         'Remuneración en números',               'MANUAL',   NULL,                     'CURRENCY', 1, 0, 180),
(@TplLabSin, 'BUDGET_ITEM',           'Partida presupuestaria',                'MANUAL',   NULL,                     'TEXT', 1, 0, 190),
(@TplLabSin, 'HORARIO_TABLE_HTML',    'Tabla del horario semanal',             'MANUAL',   NULL,                     'TEXT', 1, 1, 200),
(@TplLabSin, 'ELABORATOR_NAME',       'Nombre del elaborador',                 'SYSTEM',   'Config.ElaboratorName',  'TEXT', 1, 0, 210),
(@TplLabSin, 'DTH_REGISTRY_NUMBER',   'Número de registro DTH',                'MANUAL',   NULL,                     'TEXT', 1, 1, 220),
(@TplLabSin, 'DTH_REGISTRY_DATE',     'Fecha de registro DTH',                 'MANUAL',   NULL,                     'DATE', 1, 0, 230),
(@TplLabSin, 'DTH_DIRECTOR_NAME',     'Nombre del Director de Talento Humano', 'SYSTEM',   'Authority.HumanResource','TEXT', 1, 0, 240);

-- ─────────────────────────────────────────────────────────────────────────
-- Vincular tipos de contrato existentes a las plantillas nuevas y desactivar
-- los duplicados "(DELEGACIÓN)" que ya no se usarán como tipos independientes
-- (no se elimina ningún registro, solo se cambia Status a Inactivo 'I').
-- ─────────────────────────────────────────────────────────────────────────

-- Técnico Docente: ContractTypeID 23 (delegación, duplicado, ahora inactivo) / 52 (sin delegación, tipo único)
UPDATE HR.tbl_contract_type SET DefaultTemplateId = @TplDocenteSin, DelegationTemplateId = @TplDocenteCon WHERE ContractTypeID = 52;
UPDATE HR.tbl_contract_type SET Status = 'I' WHERE ContractTypeID = 23;

-- Técnico Laboratorio: ContractTypeID 47 (delegación, duplicado, ahora inactivo) / 53 (sin delegación, tipo único)
UPDATE HR.tbl_contract_type SET DefaultTemplateId = @TplLabSin, DelegationTemplateId = @TplLabCon WHERE ContractTypeID = 53;
UPDATE HR.tbl_contract_type SET Status = 'I' WHERE ContractTypeID = 47;

SELECT @TplDocenteCon AS TplDocenteCon, @TplDocenteSin AS TplDocenteSin, @TplLabCon AS TplLabCon, @TplLabSin AS TplLabSin;
