namespace WsUtaSystem.Models
{
    /// <summary>
    /// Versión de un archivo (<see cref="StoredFile"/>) dentro de una ranura de un registro
    /// (HR.tbl_StoredFileVersions). Reemplazar un archivo crea una versión nueva y deja la anterior inactiva.
    /// </summary>
    public class StoredFileVersion
    {
        public int VersionId { get; set; }

        /// <summary>Tipo del registro dueño, con los mismos valores que <see cref="StoredFile.EntityType"/>.</summary>
        public string EntityType { get; set; } = null!;

        /// <summary>Id del registro dueño, con los mismos valores que <see cref="StoredFile.EntityId"/>.</summary>
        public string EntityId { get; set; } = null!;

        /// <summary>Ranura lógica dentro del registro (ver StoredFileSlots), ej. SIGNED.</summary>
        public string SlotCode { get; set; } = null!;

        public int VersionNo { get; set; }
        public int StoredFileId { get; set; }
        public bool IsCurrent { get; set; }

        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public int? ReplacedBy { get; set; }
        public DateTime? ReplacedAt { get; set; }

        /// <summary>Motivo del reemplazo; obligatorio desde la versión 2.</summary>
        public string? Reason { get; set; }
    }
}
