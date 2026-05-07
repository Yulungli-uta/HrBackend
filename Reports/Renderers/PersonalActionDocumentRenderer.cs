using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Renderers;

/// <summary>
/// Renderer específico para la plantilla institucional "Acción de Personal".
/// Consume un HTML con valores ya sustituidos y toma los metadatos
/// <meta name="..." content="..." /> para dibujar un formulario fijo
/// de dos páginas en QuestPDF.
/// </summary>
public sealed class PersonalActionDocumentRenderer : IDocumentRenderer
{
    private readonly ReportConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PersonalActionDocumentRenderer> _logger;

    private static readonly string BorderColor = "#444444";
    private static readonly string LightBorderColor = "#777777";
    private static readonly string HeaderFill = "#EFEFEF";
    private static readonly string TextColor = "#111111";

    public PersonalActionDocumentRenderer(
        ReportConfiguration config,
        IWebHostEnvironment env,
        ILogger<PersonalActionDocumentRenderer> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> RenderToPdfAsync(string htmlContent, string? cssStyles = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);

        var meta = MetaMap.Parse(htmlContent);
        var margin = PageMargin.FromMeta(meta);
        var model = PersonalActionModel.FromMeta(meta);

        _logger.LogInformation(
            "PersonalActionDocumentRenderer: generando PDF de Acción de Personal para {Employee} / {ActionNumber}.",
            model.EmployeeFullName,
            model.ActionNumber);

        _logger.LogInformation(
            "*************************************************" +
            "Acción Personal parsed model. ActionNumber={ActionNumber}, EmployeeFullName={EmployeeFullName}, ValidFrom={ValidFrom}, ValidTo={ValidTo}, CurrentJob={CurrentJob}, ProposedJob={ProposedJob}, CurrentRmu={CurrentRmu}, ProposedRmu={ProposedRmu}",
            model.ActionNumber,
            model.EmployeeFullName,
            model.ValidFrom,
            model.ValidTo,
            model.Current.JobTitle,
            model.Proposed.JobTitle,
            model.Current.MonthlyRmu,
            model.Proposed.MonthlyRmu);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(margin.Top, Unit.Centimetre);
                page.MarginRight(margin.Right, Unit.Centimetre);
                page.MarginBottom(margin.Bottom, Unit.Centimetre);
                page.MarginLeft(margin.Left, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(TextColor).FontFamily(Fonts.Arial));
                page.Content().Element(c => ComposeFirstPage(c, model));
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(margin.Top, Unit.Centimetre);
                page.MarginRight(margin.Right, Unit.Centimetre);
                page.MarginBottom(margin.Bottom, Unit.Centimetre);
                page.MarginLeft(margin.Left, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(TextColor).FontFamily(Fonts.Arial));
                page.Content().Element(c => ComposeSecondPage(c, model));
            });
        }).GeneratePdf();

        return Task.FromResult(pdfBytes);
    }

    private void ComposeFirstPage(IContainer container, PersonalActionModel m)
    {
        container.Column(col =>
        {
            col.Spacing(0);

            col.Item().Border(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.6f);
                });

                table.Cell().RowSpan(3).MinHeight(42).AlignCenter().AlignMiddle().Element(c => ComposeLogoCell(c));

                table.Cell().ColumnSpan(2).RowSpan(2).MinHeight(42).Padding(4).AlignCenter().AlignMiddle().Text(text =>
                {
                    text.Line(m.InstitutionName).Bold().FontSize(10);
                    text.Line(m.InstitutionDepartment).FontSize(9);
                });

                table.Cell().ColumnSpan(2).MinHeight(18)
                    .Background(HeaderFill)
                    .BorderBottom(1)
                    .BorderColor(BorderColor)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("ACCIÓN DE PERSONAL").Bold().FontSize(12);

                table.Cell().MinHeight(18).Padding(2).Text("Nro").Bold();
                table.Cell().MinHeight(18).Padding(2).Text(m.ActionNumber);

                table.Cell().ColumnSpan(2).MinHeight(18)
                    .Background(HeaderFill)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("FECHA DE ELABORACIÓN").Bold();

                table.Cell().ColumnSpan(2).MinHeight(18)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(m.ElaborationDate);
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                });

                HeaderRow(table, "APELLIDOS", m.EmployeeLastName, "NOMBRES", m.EmployeeFirstName);
                HeaderRow(table, "DOCUMENTO DE IDENTIFICACIÓN", "CÉDULA", "NRO. DE IDENTIFICACIÓN", m.EmployeeIdCard);
                HeaderRow(table, "DESDE (dd-mm-aaaa)", m.ValidFrom, "HASTA (dd-mm-aaaa)", m.ValidTo);
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(
                "Escoja una opción (según lo estipulado en el artículo 21 del Reglamento General a la Ley Orgánica del Servicio Público)")
                .FontSize(7);

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor)
                .PaddingHorizontal(4)
                .PaddingVertical(3)
                .Column(actions =>
                {
                    actions.Spacing(2);

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            ("INGRESO", m.CheckIngreso),
                            ("TRASPASO", m.CheckTraspaso),
                            ("INCREMENTO RMU", m.CheckIncrementoRmu),
                            ("REVISIÓN CLAS. PUESTO", m.CheckRevisionClasPuesto)
                        })).FontSize(7);
                    });

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            ("REINGRESO", m.CheckReingreso),
                            ("CAMBIO ADMINISTRATIVO", m.CheckCambioAdministrativo),
                            ("SUBROGACIÓN", m.CheckSubrogacion),
                            ("OTRO (DETALLAR)", m.CheckOtro)
                        })).FontSize(7);
                    });

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            ("RESTITUCIÓN", m.CheckRestitucion),
                            ("INTERCAMBIO VOLUNTARIO", m.CheckIntercambioVoluntario),
                            ("ENCARGO", m.CheckEncargo),
                            (string.IsNullOrWhiteSpace(m.OtherActionText) ? string.Empty : m.OtherActionText, false)
                        })).FontSize(7);
                    });

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            ("ASCENSO", m.CheckAscenso),
                            ("LICENCIA", m.CheckLicencia),
                            ("CESACIÓN DE FUNCIONES", m.CheckCesacionFunciones),
                            (string.Empty, false)
                        })).FontSize(7);
                    });

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            ("TRASLADO", m.CheckTraslado),
                            ("COMISIÓN DE SERVICIOS", m.CheckComisionServicios),
                            ("DESTITUCIÓN", m.CheckDestitucion),
                            (string.Empty, false)
                        })).FontSize(7);
                    });

                    actions.Item().Row(row =>
                    {
                        row.RelativeItem().Text(BuildActionRow(new[]
                        {
                            (string.Empty, false),
                            ("SANCIONES", m.CheckSanciones),
                            ("VACACIONES", m.CheckVacaciones),
                            (string.Empty, false)
                        })).FontSize(7);
                    });
                });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3.4f);
                    c.RelativeColumn(0.8f);
                    c.RelativeColumn(1.4f);
                });

                table.Cell().Padding(2)
                    .Text("* PRESENTÓ LA DECLARACIÓN JURADA (número 2 del art. 3 RLOSEP)")
                    .FontSize(7);

                table.Cell().Padding(2).AlignCenter()
                    .Text(m.HasSwornDeclaration ? "SI" : "NO")
                    .Bold()
                    .FontSize(7);

                table.Cell().Padding(2).AlignCenter()
                    .Text(m.HasSwornDeclaration ? string.Empty : "NO APLICA ●")
                    .FontSize(7);
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(3).Column(body =>
            {
                body.Spacing(2);
                body.Item().Text("MOTIVACIÓN (adjuntar anexo si lo posee)").Bold().FontSize(7);
                body.Item().MinHeight(92).Text(m.MotivationText).FontSize(7).Justify();
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Cell().Element(c => ComposeSituationPanel(c, "SITUACIÓN ACTUAL", m.Current, true));
                table.Cell().Element(c => ComposeSituationPanel(c, "SITUACIÓN PROPUESTA", m.Proposed, false));
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(3).Column(block =>
            {
                block.Spacing(2);
                block.Item().Text("POSESIÓN DEL PUESTO").Bold().FontSize(7);

                block.Item().Row(row =>
                {
                    row.RelativeItem().Text($"YO, {m.PossessionName}").FontSize(7);
                    row.RelativeItem().Text($"CON NRO. DE DOCUMENTO DE IDENTIFICACIÓN: {m.PossessionIdCard}").FontSize(7);
                });

                block.Item().Text("JURO LEALTAD AL ESTADO ECUATORIANO").FontSize(7);

                block.Item().Row(row =>
                {
                    row.RelativeItem().Text($"LUGAR: {m.PossessionLocation}").FontSize(7);
                    row.RelativeItem().Text($"FECHA: {m.PossessionDate}").FontSize(7);
                });

                block.Item().Text("** (EN CASO DE GANADOR DE CONCURSO DE MÉRITOS Y OPOSICIÓN)").FontSize(6);

                block.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Element(LineBox);
                    row.RelativeItem().Element(LineBox);
                });

                block.Item().Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("FIRMA").FontSize(7);
                    row.RelativeItem().AlignCenter().Text("SERVIDOR PÚBLICO").FontSize(7);
                });
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Cell().ColumnSpan(2)
                    .Background(HeaderFill)
                    .Padding(2)
                    .AlignCenter()
                    .Text("RESPONSABLES DE APROBACIÓN")
                    .Bold()
                    .FontSize(7);

                table.Cell().Padding(3).Element(c => ComposeApprovalBlock(
                    c,
                    "DIRECTOR (A) O RESPONSABLE DE TALENTO HUMANO",
                    m.HrResponsibleName,
                    m.HrResponsiblePosition,
                    m.ApprovalDate,
                    m.FinalRecordNumber));

                table.Cell().Padding(3).Element(c => ComposeApprovalBlock(
                    c,
                    "AUTORIDAD NOMINADORA O SU DELEGADO",
                    m.NominatingAuthorityName,
                    m.NominatingAuthorityPosition,
                    m.ApprovalDate,
                    string.Empty));
            });
        });
    }

    private void ComposeSecondPage(IContainer container, PersonalActionModel m)
    {
        container.Column(col =>
        {
            col.Spacing(0);

            col.Item().Border(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Cell().Background(HeaderFill).Padding(2).AlignCenter().Text("RESPONSABLES DE FIRMAS").Bold().FontSize(7);
                table.Cell().Background(HeaderFill).Padding(2).AlignCenter().Text("RESPONSABLES DE FIRMAS").Bold().FontSize(7);

                table.Cell().Padding(4).Element(c => ComposeReceptionBlock(
                    c,
                    "ACEPTACIÓN Y/O RECEPCIÓN DEL SERVIDOR PÚBLICO",
                    m.ReceptionSignatureName,
                    m.ReceptionDate,
                    m.ReceptionTime,
                    string.Empty));

                table.Cell().Padding(4).Element(c => ComposeWitnessBlock(
                    c,
                    "EN CASO DE NEGATIVA DE LA RECEPCIÓN (TESTIGO)",
                    m.WitnessName,
                    m.WitnessDate,
                    m.WitnessReason));
            });

            col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Cell().Padding(4).Element(c => ComposeSimpleSignatureBlock(
                    c,
                    "RESPONSABLE DE ELABORACIÓN",
                    m.ElaboratedByName,
                    m.ElaboratedByPosition));

                table.Cell().Padding(4).Element(c => ComposeSimpleSignatureBlock(
                    c,
                    "RESPONSABLE DE REVISIÓN",
                    m.ReviewedByName,
                    m.ReviewedByPosition));

                table.Cell().Padding(4).Element(c => ComposeSimpleSignatureBlock(
                    c,
                    "RESPONSABLE DE REGISTRO Y CONTROL",
                    m.ControlByName,
                    m.ControlByPosition));
            });

            col.Item().PaddingTop(18);

            col.Item().Border(1).BorderColor(BorderColor).Column(block =>
            {
                block.Item().PaddingHorizontal(3).PaddingTop(2)
                    .Text("** USO EXCLUSIVO PARA TALENTO HUMANO")
                    .Bold()
                    .FontSize(7);

                block.Item().PaddingHorizontal(3).PaddingTop(8)
                    .Text("PROTECCIÓN DE DATOS")
                    .Bold()
                    .FontSize(7);

                block.Item().PaddingHorizontal(3).PaddingTop(2)
                    .MinHeight(88)
                    .Text(m.DataProtectionText)
                    .FontSize(7)
                    .Justify();

                block.Item().PaddingHorizontal(3).PaddingTop(8)
                    .Text("REGISTRO DE NOTIFICACIÓN AL SERVIDOR PÚBLICO DE LA ACCIÓN DE PERSONAL (primer inciso del art. 22 RGLOSEP, art. 101 COA, art. 66 y 126 ERJAFE)")
                    .Bold()
                    .FontSize(7);

                block.Item().PaddingHorizontal(3).PaddingTop(10)
                    .Text("COMUNICACIÓN ELECTRÓNICA")
                    .FontSize(7);

                block.Item().PaddingHorizontal(3).PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text($"FECHA: {m.NotificationDate}").FontSize(7);
                    row.RelativeItem().Text($"HORA: {m.NotificationTime}").FontSize(7);
                });

                block.Item().PaddingHorizontal(3).PaddingTop(8)
                    .Text($"** MEDIO: {m.NotificationMedium}")
                    .FontSize(7);

                block.Item().PaddingTop(22).AlignCenter().Width(220).AlignMiddle().Element(LineBox);
                block.Item().AlignCenter().Text("FIRMA DEL RESPONSABLE QUE NOTIFICÓ").FontSize(7);
                block.Item().AlignCenter().PaddingTop(6).Text($"NOMBRE: {m.NotifiedByName}").FontSize(7);
                block.Item().AlignCenter().Text($"PUESTO: {m.NotifiedByPosition}").FontSize(7);

                block.Item().PaddingHorizontal(3).PaddingVertical(10)
                    .Text("** Si la comunicación fue electrónica se deberá colocar el medio por el cual se notificó al servidor; así como, el número del documento.")
                    .FontSize(6);
            });
        });
    }

    private void ComposeLogoCell(IContainer container)
    {
        var logoPath = Path.Combine(_env.WebRootPath ?? string.Empty, _config.Images.LogoPath ?? string.Empty);
        var hasLogo = !string.IsNullOrWhiteSpace(_config.Images.LogoPath) && File.Exists(logoPath);

        if (hasLogo)
        {
            container.Padding(2).Image(logoPath).FitArea();
            return;
        }

        container.AlignCenter().AlignMiddle().Text("UTA").Bold().FontSize(12);
    }

    private static void HeaderRow(TableDescriptor table, string label1, string value1, string label2, string value2)
    {
        table.Cell().Background(HeaderFill).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(2).AlignCenter().Text(label1).Bold().FontSize(7);
        table.Cell().BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(2).AlignCenter().Text(value1).FontSize(8);
        table.Cell().Background(HeaderFill).BorderRight(1).BorderBottom(1).BorderColor(BorderColor).Padding(2).AlignCenter().Text(label2).Bold().FontSize(7);
        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(2).AlignCenter().Text(value2).FontSize(8);
    }

    private static void ComposeSituationPanel(IContainer container, string title, SituationData s, bool addRightBorder)
    {
        var panel = addRightBorder
            ? container.BorderRight(1).BorderColor(BorderColor)
            : container;

        panel.Column(col =>
        {
            col.Item().Background(HeaderFill).Padding(2).AlignCenter().Text(title).Bold().FontSize(7);
            col.Item().BorderTop(1).BorderColor(BorderColor).Padding(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.15f);
                    c.RelativeColumn(1.35f);
                });

                AddFieldRow(table, "PROCESO INSTITUCIONAL:", s.InstitutionalProcess);
                AddFieldRow(table, "ADJETIVO:", s.Adjective);
                AddFieldRow(table, "NIVEL DE GESTIÓN:", s.ManagementLevel);
                AddFieldRow(table, "UNIDAD ADMINISTRATIVA:", s.AdminUnit);
                AddFieldRow(table, "LUGAR DE TRABAJO:", s.Workplace);
                AddFieldRow(table, "DENOMINACIÓN DEL PUESTO:", s.JobTitle);
                AddFieldRow(table, "GRUPO OCUPACIONAL:", s.OccupationalGroup);
                AddFieldRow(table, "GRADO:", s.Grade);
                AddFieldRow(table, "REMUNERACIÓN MENSUAL:", s.MonthlyRmu);
                AddFieldRow(table, "PARTIDA INDIVIDUAL:", s.BudgetItem);
            });
        });
    }

    private static void AddFieldRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(2).PaddingRight(3).Text(label).Bold().FontSize(7);
        table.Cell().PaddingVertical(2).Text(value).FontSize(7);
    }

    private static void ComposeApprovalBlock(IContainer container, string title, string name, string position, string approvalDate, string recordNumber)
    {
        container.Column(col =>
        {
            col.Spacing(3);
            col.Item().AlignCenter().Text(title).Bold().FontSize(7);

            if (!string.IsNullOrWhiteSpace(recordNumber) || !string.IsNullOrWhiteSpace(approvalDate))
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"NRO. ACTA FINAL: {recordNumber}").FontSize(6);
                    row.RelativeItem().Text($"FECHA: {approvalDate}").FontSize(6);
                });
            }

            col.Item().PaddingTop(10).Element(LineBox);
            col.Item().Text($"NOMBRE: {name}").FontSize(7);
            col.Item().Text($"PUESTO: {position}").FontSize(7);
        });
    }

    private static void ComposeReceptionBlock(IContainer container, string title, string name, string date, string time, string reason)
    {
        container.Column(col =>
        {
            col.Spacing(3);
            col.Item().Text(title).Bold().FontSize(7);
            col.Item().PaddingTop(16).Element(LineBox);
            col.Item().Text($"NOMBRE: {name}").FontSize(7);
            col.Item().Text($"FECHA: {date}").FontSize(7);
            col.Item().Text($"HORA: {time}").FontSize(7);

            if (!string.IsNullOrWhiteSpace(reason))
                col.Item().Text($"RAZÓN: {reason}").FontSize(7);
        });
    }

    private static void ComposeWitnessBlock(IContainer container, string title, string name, string date, string reason)
    {
        container.Column(col =>
        {
            col.Spacing(3);
            col.Item().Text(title).Bold().FontSize(7);
            col.Item().PaddingTop(16).Element(LineBox);
            col.Item().Text($"NOMBRE: {name}").FontSize(7);
            col.Item().Text($"FECHA: {date}").FontSize(7);
            col.Item().MinHeight(40).Text($"RAZÓN: {reason}").FontSize(7).Justify();
        });
    }

    private static void ComposeSimpleSignatureBlock(IContainer container, string title, string name, string position)
    {
        container.Column(col =>
        {
            col.Spacing(3);
            col.Item().AlignCenter().Text(title).Bold().FontSize(7);
            col.Item().PaddingTop(18).Element(LineBox);
            col.Item().Text($"NOMBRE: {name}").FontSize(7);
            col.Item().Text($"PUESTO: {position}").FontSize(7);
        });
    }

    private static IContainer LineBox(IContainer container)
        => container.BorderBottom(1).BorderColor(LightBorderColor).MinHeight(1);

    private static string BuildActionRow(IEnumerable<(string Label, bool Checked)> items)
    {
        return string.Join(
            "    ",
            items.Where(x => !string.IsNullOrWhiteSpace(x.Label))
                 .Select(x => $"{(x.Checked ? "●" : "○")} {x.Label}"));
    }

    private sealed record PageMargin(float Top, float Right, float Bottom, float Left)
    {
        public static PageMargin FromMeta(MetaMap meta)
        {
            return new PageMargin(
                meta.GetFloat("MARGIN_TOP_CM", 1.20f),
                meta.GetFloat("MARGIN_RIGHT_CM", 1.20f),
                meta.GetFloat("MARGIN_BOTTOM_CM", 1.20f),
                meta.GetFloat("MARGIN_LEFT_CM", 1.20f));
        }
    }

    private sealed class MetaMap
    {
        private static readonly Regex MetaRegex = new(
            @"<meta\s+name=""(?<name>[^""]+)""\s+content=""(?<value>[^""]*)""\s*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Dictionary<string, string> _values;

        private MetaMap(Dictionary<string, string> values)
        {
            _values = values;
        }

        public static MetaMap Parse(string html)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in MetaRegex.Matches(html))
            {
                var name = HtmlDecode(match.Groups["name"].Value.Trim());
                var value = HtmlDecode(match.Groups["value"].Value.Trim());
                values[name] = value;
            }

            return new MetaMap(values);
        }

        public string Get(string key, string defaultValue = "")
            => _values.TryGetValue(key, out var value) ? value : defaultValue;

        public bool GetBool(string key)
        {
            var raw = Get(key);
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("1", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("x", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("si", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("sí", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("ok", StringComparison.OrdinalIgnoreCase);
        }

        public float GetFloat(string key, float defaultValue)
        {
            var raw = Get(key);

            if (float.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
                return invariant;

            if (float.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-EC"), out var es))
                return es;

            return defaultValue;
        }

        private static string HtmlDecode(string value)
            => System.Net.WebUtility.HtmlDecode(value);
    }

    private sealed record SituationData(
        string InstitutionalProcess,
        string Adjective,
        string ManagementLevel,
        string AdminUnit,
        string Workplace,
        string JobTitle,
        string OccupationalGroup,
        string Grade,
        string MonthlyRmu,
        string BudgetItem);

    private sealed class PersonalActionModel
    {
        public string InstitutionName { get; init; } = string.Empty;
        public string InstitutionDepartment { get; init; } = string.Empty;
        public string ActionNumber { get; init; } = string.Empty;
        public string ElaborationDate { get; init; } = string.Empty;
        public string EmployeeLastName { get; init; } = string.Empty;
        public string EmployeeFirstName { get; init; } = string.Empty;
        public string EmployeeFullName { get; init; } = string.Empty;
        public string EmployeeIdCard { get; init; } = string.Empty;
        public string ValidFrom { get; init; } = string.Empty;
        public string ValidTo { get; init; } = string.Empty;
        public bool CheckIngreso { get; init; }
        public bool CheckReingreso { get; init; }
        public bool CheckRestitucion { get; init; }
        public bool CheckAscenso { get; init; }
        public bool CheckTraslado { get; init; }
        public bool CheckTraspaso { get; init; }
        public bool CheckCambioAdministrativo { get; init; }
        public bool CheckIntercambioVoluntario { get; init; }
        public bool CheckLicencia { get; init; }
        public bool CheckComisionServicios { get; init; }
        public bool CheckSanciones { get; init; }
        public bool CheckIncrementoRmu { get; init; }
        public bool CheckRevisionClasPuesto { get; init; }
        public bool CheckSubrogacion { get; init; }
        public bool CheckEncargo { get; init; }
        public bool CheckCesacionFunciones { get; init; }
        public bool CheckDestitucion { get; init; }
        public bool CheckVacaciones { get; init; }
        public bool CheckOtro { get; init; }
        public string OtherActionText { get; init; } = string.Empty;
        public bool HasSwornDeclaration { get; init; }
        public string MotivationText { get; init; } = string.Empty;
        public SituationData Current { get; init; } = new("", "", "", "", "", "", "", "", "", "");
        public SituationData Proposed { get; init; } = new("", "", "", "", "", "", "", "", "", "");
        public string PossessionName { get; init; } = string.Empty;
        public string PossessionIdCard { get; init; } = string.Empty;
        public string PossessionLocation { get; init; } = string.Empty;
        public string PossessionDate { get; init; } = string.Empty;
        public string FinalRecordNumber { get; init; } = string.Empty;
        public string ApprovalDate { get; init; } = string.Empty;
        public string HrResponsibleName { get; init; } = string.Empty;
        public string HrResponsiblePosition { get; init; } = string.Empty;
        public string NominatingAuthorityName { get; init; } = string.Empty;
        public string NominatingAuthorityPosition { get; init; } = string.Empty;
        public string ReceptionSignatureName { get; init; } = string.Empty;
        public string ReceptionDate { get; init; } = string.Empty;
        public string ReceptionTime { get; init; } = string.Empty;
        public string WitnessName { get; init; } = string.Empty;
        public string WitnessDate { get; init; } = string.Empty;
        public string WitnessReason { get; init; } = string.Empty;
        public string ElaboratedByName { get; init; } = string.Empty;
        public string ElaboratedByPosition { get; init; } = string.Empty;
        public string ReviewedByName { get; init; } = string.Empty;
        public string ReviewedByPosition { get; init; } = string.Empty;
        public string ControlByName { get; init; } = string.Empty;
        public string ControlByPosition { get; init; } = string.Empty;
        public string DataProtectionText { get; init; } = DefaultProtectionText;
        public string NotificationDate { get; init; } = string.Empty;
        public string NotificationTime { get; init; } = string.Empty;
        public string NotificationMedium { get; init; } = string.Empty;
        public string NotifiedByName { get; init; } = string.Empty;
        public string NotifiedByPosition { get; init; } = string.Empty;

        private const string DefaultProtectionText =
            "En cumplimiento a la Ley Orgánica de Protección de Datos Personales y su normativa conexa, la Universidad Técnica de Ambato, en calidad de responsable del tratamiento, informa al titular de los datos personales que la información proporcionada a la Institución será objeto de tratamiento con las siguientes finalidades: cumplir con obligaciones contractuales legales, tributarias y de seguridad social; generación de reportes específicos internos o que sean solicitados por una institución pública que rige a esta IES; y generar bases de datos de acceso público. El titular autoriza expresamente el tratamiento de los datos proporcionados.";

        public static PersonalActionModel FromMeta(MetaMap meta)
        {
            return new PersonalActionModel
            {
                InstitutionName = meta.Get("INSTITUTION_NAME", "Universidad Técnica de Ambato"),
                InstitutionDepartment = meta.Get("INSTITUTION_DEPARTMENT", "DIRECCIÓN DE TALENTO HUMANO"),
                ActionNumber = meta.Get("ACTION_NUMBER"),
                ElaborationDate = meta.Get("ELABORATION_DATE"),
                EmployeeLastName = meta.Get("EMPLOYEE_LASTNAME"),
                EmployeeFirstName = meta.Get("EMPLOYEE_FIRSTNAME"),
                EmployeeFullName = meta.Get("EMPLOYEE_FULLNAME"),
                EmployeeIdCard = meta.Get("EMPLOYEE_IDCARD"),
                ValidFrom = meta.Get("VALID_FROM"),
                ValidTo = meta.Get("VALID_TO"),
                CheckIngreso = meta.GetBool("CHK_INGRESO"),
                CheckReingreso = meta.GetBool("CHK_REINGRESO"),
                CheckRestitucion = meta.GetBool("CHK_RESTITUCION"),
                CheckAscenso = meta.GetBool("CHK_ASCENSO"),
                CheckTraslado = meta.GetBool("CHK_TRASLADO"),
                CheckTraspaso = meta.GetBool("CHK_TRASPASO"),
                CheckCambioAdministrativo = meta.GetBool("CHK_CAMBIO_ADMINISTRATIVO"),
                CheckIntercambioVoluntario = meta.GetBool("CHK_INTERCAMBIO_VOLUNTARIO"),
                CheckLicencia = meta.GetBool("CHK_LICENCIA"),
                CheckComisionServicios = meta.GetBool("CHK_COMISION_SERVICIOS"),
                CheckSanciones = meta.GetBool("CHK_SANCIONES"),
                CheckIncrementoRmu = meta.GetBool("CHK_INCREMENTO_RMU"),
                CheckRevisionClasPuesto = meta.GetBool("CHK_REVISION_CLAS_PUESTO"),
                CheckSubrogacion = meta.GetBool("CHK_SUBROGACION"),
                CheckEncargo = meta.GetBool("CHK_ENCARGO"),
                CheckCesacionFunciones = meta.GetBool("CHK_CESACION_FUNCIONES"),
                CheckDestitucion = meta.GetBool("CHK_DESTITUCION"),
                CheckVacaciones = meta.GetBool("CHK_VACACIONES"),
                CheckOtro = meta.GetBool("CHK_OTRO"),
                OtherActionText = meta.Get("OTHER_ACTION_TEXT"),
                HasSwornDeclaration = meta.GetBool("HAS_SWORN_DECLARATION"),
                MotivationText = meta.Get("MOTIVATION_TEXT"),
                Current = new SituationData(
                    meta.Get("CURRENT_INSTITUTIONAL_PROCESS"),
                    meta.Get("CURRENT_ADJECTIVE"),
                    meta.Get("CURRENT_MANAGEMENT_LEVEL"),
                    meta.Get("CURRENT_ADMIN_UNIT"),
                    meta.Get("CURRENT_WORKPLACE"),
                    meta.Get("CURRENT_JOB_TITLE"),
                    meta.Get("CURRENT_OCCUPATIONAL_GROUP"),
                    meta.Get("CURRENT_GRADE"),
                    meta.Get("CURRENT_MONTHLY_RMU"),
                    meta.Get("CURRENT_BUDGET_ITEM")),
                Proposed = new SituationData(
                    meta.Get("PROPOSED_INSTITUTIONAL_PROCESS"),
                    meta.Get("PROPOSED_ADJECTIVE"),
                    meta.Get("PROPOSED_MANAGEMENT_LEVEL"),
                    meta.Get("PROPOSED_ADMIN_UNIT"),
                    meta.Get("PROPOSED_WORKPLACE"),
                    meta.Get("PROPOSED_JOB_TITLE"),
                    meta.Get("PROPOSED_OCCUPATIONAL_GROUP"),
                    meta.Get("PROPOSED_GRADE"),
                    meta.Get("PROPOSED_MONTHLY_RMU"),
                    meta.Get("PROPOSED_BUDGET_ITEM")),
                PossessionName = meta.Get("POSSESSION_NAME"),
                PossessionIdCard = meta.Get("POSSESSION_IDCARD"),
                PossessionLocation = meta.Get("POSSESSION_LOCATION"),
                PossessionDate = meta.Get("POSSESSION_DATE"),
                FinalRecordNumber = meta.Get("FINAL_RECORD_NUMBER"),
                ApprovalDate = meta.Get("APPROVAL_DATE"),
                HrResponsibleName = meta.Get("HR_RESPONSIBLE_NAME"),
                HrResponsiblePosition = meta.Get("HR_RESPONSIBLE_POSITION"),
                NominatingAuthorityName = meta.Get("NOMINATING_AUTHORITY_NAME"),
                NominatingAuthorityPosition = meta.Get("NOMINATING_AUTHORITY_POSITION"),
                ReceptionSignatureName = meta.Get("RECEPTION_SIGNATURE_NAME"),
                ReceptionDate = meta.Get("RECEPTION_DATE"),
                ReceptionTime = meta.Get("RECEPTION_TIME"),
                WitnessName = meta.Get("WITNESS_NAME"),
                WitnessDate = meta.Get("WITNESS_DATE"),
                WitnessReason = meta.Get("WITNESS_REASON"),
                ElaboratedByName = meta.Get("ELABORATED_BY_NAME"),
                ElaboratedByPosition = meta.Get("ELABORATED_BY_POSITION"),
                ReviewedByName = meta.Get("REVIEWED_BY_NAME"),
                ReviewedByPosition = meta.Get("REVIEWED_BY_POSITION"),
                ControlByName = meta.Get("CONTROL_BY_NAME"),
                ControlByPosition = meta.Get("CONTROL_BY_POSITION"),
                DataProtectionText = meta.Get("DATA_PROTECTION_TEXT", DefaultProtectionText),
                NotificationDate = meta.Get("NOTIFICATION_DATE"),
                NotificationTime = meta.Get("NOTIFICATION_TIME"),
                NotificationMedium = meta.Get("NOTIFICATION_MEDIUM"),
                NotifiedByName = meta.Get("NOTIFIED_BY_NAME"),
                NotifiedByPosition = meta.Get("NOTIFIED_BY_POSITION")
            };
        }
    }
}