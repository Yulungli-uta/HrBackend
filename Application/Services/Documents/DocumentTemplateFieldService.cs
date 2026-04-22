using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services.Documents;

/// <summary>
/// Servicio de gestión de campos de plantillas documentales.
/// Permite agregar, actualizar y eliminar campos individuales de una plantilla.
/// </summary>
public sealed class DocumentTemplateFieldService : IDocumentTemplateFieldService
{
    private readonly IDocumentTemplateFieldRepository _fieldRepository;
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly ILogger<DocumentTemplateFieldService> _logger;

    public DocumentTemplateFieldService(
        IDocumentTemplateFieldRepository fieldRepository,
        IDocumentTemplateRepository templateRepository,
        ILogger<DocumentTemplateFieldService> logger)
    {
        _fieldRepository    = fieldRepository;
        _templateRepository = templateRepository;
        _logger             = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentTemplateFieldDto>> GetByTemplateIdAsync(
        int templateId,
        CancellationToken ct = default)
    {
        // Verificar que la plantilla existe
        _ = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");

        var entities = await _fieldRepository.GetByTemplateIdAsync(templateId, ct);

        return entities.Select(f => new DocumentTemplateFieldDto(
            FieldId:        f.FieldId,
            TemplateId:     f.TemplateId,
            FieldName:      f.FieldName,
            Label:          f.Label,
            SourceType:     f.SourceType,
            SourceProperty: f.SourceProperty,
            DataType:       f.DataType,
            FormatPattern:  f.FormatPattern,
            DefaultValue:   f.DefaultValue,
            IsRequired:     f.IsRequired,
            IsEditable:     f.IsEditable,
            SortOrder:      f.SortOrder,
            HelpText:       f.HelpText
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        int templateId,
        CreateDocumentTemplateFieldRequest request,
        int createdBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");

        if (template.Status == DocumentTemplateStatus.Archived)
            throw new InvalidOperationException("No se pueden agregar campos a una plantilla archivada.");

        var fieldNameNormalized = request.FieldName.ToUpperInvariant().Trim();

        var nameExists = await _fieldRepository.ExistsByNameAsync(
            templateId, fieldNameNormalized, ct: ct);

        if (nameExists)
            throw new InvalidOperationException(
                $"Ya existe un campo con el nombre '{request.FieldName}' en esta plantilla.");

        var field = new DocumentTemplateField
        {
            TemplateId     = templateId,
            FieldName      = fieldNameNormalized,
            Label          = request.Label.Trim(),
            SourceType     = request.SourceType,
            SourceProperty = request.SourceProperty?.Trim(),
            DataType       = request.DataType.Trim(),
            FormatPattern  = request.FormatPattern?.Trim(),
            DefaultValue   = request.DefaultValue?.Trim(),
            IsRequired     = request.IsRequired,
            IsEditable     = request.IsEditable,
            SortOrder      = request.SortOrder,
            HelpText       = request.HelpText?.Trim()
        };

        var fieldId = await _fieldRepository.CreateAsync(field, ct);

        _logger.LogInformation(
            "DocumentTemplateFieldService: campo '{Name}' creado en plantilla {TemplateId}.",
            fieldNameNormalized, templateId);

        return fieldId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int fieldId,
        UpdateDocumentTemplateFieldRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var field = await _fieldRepository.GetByIdAsync(fieldId, ct)
            ?? throw new KeyNotFoundException($"Campo {fieldId} no encontrado.");

        // UpdateDocumentTemplateFieldRequest no incluye FieldName (es inmutable)
        field.Label          = request.Label.Trim();
        field.SourceType     = request.SourceType;
        field.SourceProperty = request.SourceProperty?.Trim();
        field.DataType       = request.DataType.Trim();
        field.FormatPattern  = request.FormatPattern?.Trim();
        field.DefaultValue   = request.DefaultValue?.Trim();
        field.IsRequired     = request.IsRequired;
        field.IsEditable     = request.IsEditable;
        field.SortOrder      = request.SortOrder;
        field.HelpText       = request.HelpText?.Trim();

        await _fieldRepository.UpdateAsync(field, ct);

        _logger.LogInformation(
            "DocumentTemplateFieldService: campo {FieldId} actualizado.",
            fieldId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int fieldId, CancellationToken ct = default)
    {
        var field = await _fieldRepository.GetByIdAsync(fieldId, ct)
            ?? throw new KeyNotFoundException($"Campo {fieldId} no encontrado.");

        await _fieldRepository.DeleteAsync(fieldId, ct);

        _logger.LogInformation(
            "DocumentTemplateFieldService: campo '{Name}' (ID {FieldId}) eliminado.",
            field.FieldName, fieldId);
    }
}
