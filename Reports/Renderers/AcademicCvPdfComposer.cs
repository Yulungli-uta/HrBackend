using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.DTOs.AcademicCv;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Renderers;

/// <inheritdoc cref="IAcademicCvPdfComposer"/>
/// <remarks>
/// Colores de sección alineados con la misma paleta ya usada en las pestañas de Hoja de
/// Vida del frontend (HrFrontend), para que el PDF exportado se sienta visualmente
/// consistente con lo que la persona ve en pantalla.
/// </remarks>
public sealed class AcademicCvPdfComposer : IAcademicCvPdfComposer
{
    private readonly ReportConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IInstitutionalLogoService _logoService;

    private const string ColorCurrentStatus = "#0d9488";  // teal-600   (Situación Laboral Actual)
    private const string ColorEducation = "#0891b2";     // cyan-600   (Formación)
    private const string ColorExperience = "#f97316";    // uta-orange (Experiencia)
    private const string ColorPublications = "#1e40af";  // uta-blue   (Publicaciones)
    private const string ColorBooks = "#dc2626";          // red-600    (Libros)
    private const string ColorTrainings = "#a855f7";      // purple-500 (Capacitaciones)
    private const string ColorLanguages = "#6366f1";      // indigo-500 (Idiomas)
    private const string ColorKnowledgeAreas = "#16a34a"; // green-600  (Áreas de Conocimiento)

    public AcademicCvPdfComposer(
        ReportConfiguration config,
        IWebHostEnvironment env,
        IInstitutionalLogoService logoService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logoService = logoService ?? throw new ArgumentNullException(nameof(logoService));

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Compose(AcademicCvDto cv)
    {
        ArgumentNullException.ThrowIfNull(cv);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, cv));
                page.Content().Element(c => ComposeContent(c, cv));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    /// <summary>Encabezado corporativo que QuestPDF repite en TODAS las páginas — a
    /// propósito solo lleva logo/institución/título, nunca datos de la persona (ver
    /// <see cref="ComposePersonBlock"/>, que se renderiza una sola vez en el contenido).</summary>
    private void ComposeHeader(IContainer container, AcademicCvDto cv)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                var logoPath = _logoService.GetLogoFilePath();
                if (logoPath is not null)
                    row.ConstantItem(40).Height(40).Image(logoPath);

                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text("UNIVERSIDAD TÉCNICA DE AMBATO — Hoja de Vida Académica")
                        .FontSize(11).Bold().FontColor(_config.Colors.Primary);
                });

                row.ConstantItem(120).AlignRight().Text(
                    $"Generado: {DateTime.Now:dd/MM/yyyy}")
                    .FontSize(8).FontColor(_config.Colors.TextSecondary);
            });

            column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(_config.Colors.Primary);
        });
    }

    /// <summary>Foto, nombre, cédula, edad, correo y situación laboral resumida — se
    /// renderiza UNA sola vez al inicio del documento, no en el header repetido.</summary>
    private void ComposePersonBlock(IContainer container, AcademicCvDto cv)
    {
        container.Row(headerRow =>
        {
            if (cv.PhotoBytes is { Length: > 0 })
            {
                headerRow.ConstantItem(55).PaddingRight(10)
                    .Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Height(70).Width(55)
                    .Image(cv.PhotoBytes).FitArea();
            }

            headerRow.RelativeItem().Column(col =>
            {
                col.Item().Text(cv.FullName).FontSize(16).Bold().FontColor(_config.Colors.TextPrimary);

                var metaParts = new List<string> { $"Cédula: {cv.IdCard}" };
                if (cv.Age.HasValue) metaParts.Add($"Edad: {cv.Age} años");
                if (!string.IsNullOrWhiteSpace(cv.Email)) metaParts.Add(cv.Email!);
                col.Item().PaddingTop(2).Text(string.Join("   ·   ", metaParts))
                    .FontSize(9).FontColor(_config.Colors.TextSecondary);

                if (cv.LaborRegimes.Count > 0)
                {
                    var primary = cv.LaborRegimes[0];
                    var identityParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(primary.JobName)) identityParts.Add(primary.JobName!);
                    identityParts.Add(primary.RegimeName);
                    if (cv.YearsOfService.HasValue)
                        identityParts.Add($"{cv.YearsOfService} año{(cv.YearsOfService == 1 ? "" : "s")} de servicio");
                    col.Item().PaddingTop(3).Text(string.Join("  ·  ", identityParts))
                        .FontSize(10).Bold().FontColor(ColorCurrentStatus);
                }
            });
        });
    }

    private void ComposeContent(IContainer container, AcademicCvDto cv)
    {
        container.PaddingTop(10).Column(column =>
        {
            column.Spacing(10);

            column.Item().Element(c => ComposePersonBlock(c, cv));

            if (cv.LaborRegimes.Count > 0 || cv.TeacherStructures.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "SITUACIÓN LABORAL ACTUAL", ColorCurrentStatus, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(4);
                        foreach (var r in cv.LaborRegimes)
                        {
                            items.Item().Text(t =>
                            {
                                if (!string.IsNullOrWhiteSpace(r.JobName)) t.Span($"{r.JobName} — ").FontSize(9).Bold();
                                t.Span(r.RegimeName).FontSize(9);
                                if (!string.IsNullOrWhiteSpace(r.DepartmentName)) t.Span($" · {r.DepartmentName}").FontSize(9);
                                t.Span($" · {(r.IsIndefinite ? "Nombramiento" : "Contrato")} · Desde {r.SinceLabel}")
                                    .FontSize(9).FontColor(_config.Colors.TextSecondary);
                            });
                        }
                        foreach (var ts in cv.TeacherStructures)
                        {
                            items.Item().Text(t =>
                            {
                                t.Span(ts.LadderName ?? "Estructura docente").FontSize(9).Bold();
                                t.Span($" · {ts.DedicationName}").FontSize(9);
                                if (ts.WeeklyClassHours.HasValue) t.Span($" · {ts.WeeklyClassHours:0.#}h/semana").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(ts.DepartmentName)) t.Span($" · {ts.DepartmentName}").FontSize(9);
                                t.Span($" · Desde {ts.SinceLabel}").FontSize(9).FontColor(_config.Colors.TextSecondary);
                            });
                        }
                    });
                }));
            }

            if (cv.KnowledgeAreas.Count > 0)
            {
                column.Item().Element(c => ComposeSection(
                    c, "ÁREAS DE CONOCIMIENTO", ColorKnowledgeAreas,
                    cc => cc.Text(string.Join("  •  ", cv.KnowledgeAreas)).FontSize(9)));
            }

            if (cv.EducationLevels.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "FORMACIÓN ACADÉMICA", ColorEducation, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(6);
                        foreach (var e in cv.EducationLevels)
                        {
                            items.Item().Column(item =>
                            {
                                item.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(e.Title).FontSize(10).Bold();
                                    if (!string.IsNullOrWhiteSpace(e.DateRangeLabel))
                                        r.ConstantItem(90).AlignRight().Text(e.DateRangeLabel)
                                            .FontSize(8).FontColor(_config.Colors.TextSecondary);
                                });
                                if (!string.IsNullOrWhiteSpace(e.Specialty))
                                    item.Item().Text(e.Specialty!).FontSize(9).FontColor(_config.Colors.TextSecondary);
                                item.Item().Text(t =>
                                {
                                    if (!string.IsNullOrWhiteSpace(e.InstitutionName))
                                        t.Span(e.InstitutionName!).FontSize(9);
                                    if (!string.IsNullOrWhiteSpace(e.LevelName))
                                    {
                                        if (!string.IsNullOrWhiteSpace(e.InstitutionName)) t.Span("  •  ").FontSize(9);
                                        t.Span(e.LevelName!).FontSize(9).FontColor(ColorEducation);
                                    }
                                });
                                if (!string.IsNullOrWhiteSpace(e.SenescytRegistrationNumber))
                                    item.Item().Text($"Registro SENESCYT: {e.SenescytRegistrationNumber}")
                                        .FontSize(8).FontColor(_config.Colors.TextSecondary);
                            });
                        }
                    });
                }));
            }

            if (cv.WorkExperiences.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "EXPERIENCIA LABORAL", ColorExperience, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(6);
                        foreach (var e in cv.WorkExperiences)
                        {
                            items.Item().Column(item =>
                            {
                                item.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(e.Position).FontSize(10).Bold();
                                    r.ConstantItem(120).AlignRight().Text(e.DateRangeLabel)
                                        .FontSize(8).FontColor(_config.Colors.TextSecondary);
                                });
                                item.Item().Text(t =>
                                {
                                    t.Span(e.Company).FontSize(9);
                                    if (!string.IsNullOrWhiteSpace(e.ExperienceTypeName))
                                    {
                                        t.Span("  •  ").FontSize(9);
                                        t.Span(e.ExperienceTypeName!).FontSize(9).FontColor(ColorExperience);
                                    }
                                });
                            });
                        }
                    });
                }));
            }

            if (cv.Publications.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "PUBLICACIONES", ColorPublications, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(5);
                        foreach (var p in cv.Publications)
                        {
                            items.Item().Column(item =>
                            {
                                item.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(p.Title).FontSize(9.5f).Bold();
                                    if (!string.IsNullOrWhiteSpace(p.DateLabel))
                                        r.ConstantItem(70).AlignRight().Text(p.DateLabel!)
                                            .FontSize(8).FontColor(_config.Colors.TextSecondary);
                                });
                                if (!string.IsNullOrWhiteSpace(p.JournalName) || !string.IsNullOrWhiteSpace(p.PublicationTypeName))
                                {
                                    item.Item().Text(t =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(p.JournalName))
                                            t.Span(p.JournalName!).FontSize(8.5f).FontColor(_config.Colors.TextSecondary);
                                        if (!string.IsNullOrWhiteSpace(p.PublicationTypeName))
                                        {
                                            if (!string.IsNullOrWhiteSpace(p.JournalName)) t.Span("  •  ").FontSize(8.5f);
                                            t.Span(p.PublicationTypeName!).FontSize(8.5f).FontColor(ColorPublications);
                                        }
                                    });
                                }
                            });
                        }
                    });
                }));
            }

            if (cv.Books.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "LIBROS", ColorBooks, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(5);
                        foreach (var b in cv.Books)
                        {
                            items.Item().Column(item =>
                            {
                                item.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(b.Title).FontSize(9.5f).Bold();
                                    if (!string.IsNullOrWhiteSpace(b.DateLabel))
                                        r.ConstantItem(70).AlignRight().Text(b.DateLabel!)
                                            .FontSize(8).FontColor(_config.Colors.TextSecondary);
                                });
                                item.Item().Text(t =>
                                {
                                    if (!string.IsNullOrWhiteSpace(b.Publisher))
                                        t.Span(b.Publisher!).FontSize(8.5f).FontColor(_config.Colors.TextSecondary);
                                    if (b.PeerReviewed)
                                    {
                                        if (!string.IsNullOrWhiteSpace(b.Publisher)) t.Span("  •  ").FontSize(8.5f);
                                        t.Span("Revisado por pares").FontSize(8.5f).FontColor(ColorBooks);
                                    }
                                });
                            });
                        }
                    });
                }));
            }

            if (cv.Trainings.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "CAPACITACIONES", ColorTrainings, cc =>
                {
                    cc.Column(items =>
                    {
                        items.Spacing(5);
                        foreach (var t2 in cv.Trainings)
                        {
                            items.Item().Column(item =>
                            {
                                item.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(t2.Title).FontSize(9.5f).Bold();
                                    r.ConstantItem(120).AlignRight().Text(
                                        $"{t2.DateRangeLabel}" + (t2.Hours > 0 ? $"  ·  {t2.Hours}h" : ""))
                                        .FontSize(8).FontColor(_config.Colors.TextSecondary);
                                });
                                item.Item().Text(t2.Institution).FontSize(8.5f).FontColor(_config.Colors.TextSecondary);
                            });
                        }
                    });
                }));
            }

            if (cv.Languages.Count > 0)
            {
                column.Item().Element(c => ComposeSection(c, "IDIOMAS", ColorLanguages, cc =>
                {
                    cc.Row(row =>
                    {
                        row.Spacing(14);
                        foreach (var l in cv.Languages)
                        {
                            row.AutoItem().Text(t =>
                            {
                                t.Span(l.LanguageName).FontSize(9).Bold();
                                if (!string.IsNullOrWhiteSpace(l.LevelName))
                                    t.Span($" ({l.LevelName})").FontSize(8.5f).FontColor(ColorLanguages);
                            });
                        }
                    });
                }));
            }

            var hasAnyData = cv.EducationLevels.Count > 0 || cv.WorkExperiences.Count > 0
                || cv.Publications.Count > 0 || cv.Books.Count > 0 || cv.Trainings.Count > 0
                || cv.Languages.Count > 0 || cv.LaborRegimes.Count > 0 || cv.TeacherStructures.Count > 0;
            if (!hasAnyData)
            {
                column.Item().PaddingTop(20).AlignCenter()
                    .Text("No hay información académica registrada.")
                    .FontSize(10).Italic().FontColor(_config.Colors.TextSecondary);
            }
        });
    }

    /// <summary>Barra de color + título de sección (mismo lenguaje visual que las tarjetas
    /// con borde de acento de color usadas en el frontend), seguido del contenido.</summary>
    private static void ComposeSection(IContainer container, string title, string colorHex, Action<IContainer> content)
    {
        container.BorderLeft(3).BorderColor(colorHex).PaddingLeft(8).Column(col =>
        {
            col.Item().Text(title).FontSize(10.5f).Bold().FontColor(colorHex);
            col.Item().PaddingTop(4).Element(content);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(_config.Colors.Primary);
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text("Sistema HR — Universidad Técnica de Ambato")
                    .FontSize(8).FontColor(_config.Colors.TextSecondary);

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(8).FontColor(_config.Colors.TextSecondary);
                    text.CurrentPageNumber();
                    text.Span(" de ").FontSize(8).FontColor(_config.Colors.TextSecondary);
                    text.TotalPages();
                });
            });
        });
    }
}
