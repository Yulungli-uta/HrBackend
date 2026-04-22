namespace WsUtaSystem.Application.Common.Enums;

/// <summary>
/// Tipo de diseño de la plantilla documental.
/// Controla cómo el motor renderiza el contenido HTML.
/// </summary>
public enum LayoutType
{
    /// <summary>Texto continuo con párrafos y saltos de línea (contratos, oficios).</summary>
    FlowText,

    /// <summary>Formulario estructurado con campos etiquetados (acción de personal).</summary>
    StructuredForm,

    /// <summary>Combinación de texto libre y secciones estructuradas.</summary>
    Hybrid
}

/// <summary>
/// Estado del ciclo de vida de una plantilla documental.
/// </summary>
public enum DocumentTemplateStatus
{
    /// <summary>Plantilla en elaboración, no disponible para generación.</summary>
    Draft,

    /// <summary>Plantilla activa y disponible para generar documentos.</summary>
    Published,

    /// <summary>Plantilla retirada, no disponible pero conservada para auditoría.</summary>
    Archived
}

/// <summary>
/// Origen de los datos que resuelve un campo de la plantilla.
/// Determina qué repositorio o fuente usa <c>DocumentFieldResolver</c>.
/// </summary>
public enum FieldSourceType
{
    /// <summary>Campo calculado por el sistema (fecha actual, número de documento, etc.).</summary>
    System,

    /// <summary>Campo proveniente de la entidad <c>Employees</c> / <c>People</c>.</summary>
    Employee,

    /// <summary>Campo proveniente de la entidad <c>Contracts</c> / <c>ContractType</c>.</summary>
    Contract,

    /// <summary>Campo proveniente de la entidad <c>PersonnelMovements</c>.</summary>
    Movement,

    /// <summary>Valor ingresado manualmente por el usuario al momento de generar.</summary>
    Manual
}

/// <summary>
/// Tipo de entidad de negocio al que está asociado un documento generado.
/// </summary>
public enum DocumentEntityType
{
    /// <summary>Documento asociado a un contrato laboral.</summary>
    Contract,

    /// <summary>Documento de acción de personal (traslado, encargo, licencia, etc.).</summary>
    PersonnelAction,

    /// <summary>Acuerdo o convenio institucional.</summary>
    Agreement,

    /// <summary>Oficio institucional.</summary>
    Oficio
}
