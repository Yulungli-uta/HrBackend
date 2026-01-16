namespace WsUtaSystem.Application.Interfaces.Auditable
{
    /// <summary>
    /// Interfaz para entidades que requieren auditoría de creación
    /// </summary>
    public interface ICreationAuditable
    {
        /// <summary>
        /// Fecha y hora de creación del registro
        /// </summary>
        DateTime? CreatedAt { get; set; }

        /// <summary>
        /// ID del usuario que creó el registro
        /// </summary>
        int? CreatedBy { get; set; }
    }

    /// <summary>
    /// Interfaz para entidades que requieren auditoría de modificación
    /// </summary>
    public interface IModificationAuditable
    {
        /// <summary>
        /// Fecha y hora de la última actualización
        /// </summary>
        DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID del usuario que realizó la última actualización
        /// </summary>
        int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Interfaz completa de auditoría que combina creación y modificación
    /// Usar cuando la entidad requiere auditoría completa
    /// </summary>
    public interface IAuditable : ICreationAuditable, IModificationAuditable
    { 
    }
}
