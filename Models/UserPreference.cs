namespace WsUtaSystem.Models
{
    /// <summary>
    /// Preferencias de interfaz de un usuario (página de inicio, tamaño de letra, tema, etc.).
    /// Una fila por usuario; <see cref="UserId"/> es el Id de auth.tbl_Users (claim "sub").
    /// </summary>
    public class UserPreference
    {
        public int UserPreferenceId { get; set; }
        public Guid UserId { get; set; }
        public short SchemaVersion { get; set; } = 1;
        public string PreferencesJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
