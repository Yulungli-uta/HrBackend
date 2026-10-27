using System.Globalization;
using System.Text;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Separa los apellidos de una persona (tbl_People.LastName guarda "PRIMER SEGUNDO" en un solo campo) para las matrices
/// SIIES de pasaporte, que piden PRIMER_APELLIDO y SEGUNDO_APELLIDO por separado. Un apellido compuesto no se parte:
/// "DE LEON NICARETTA" es primer apellido "DE LEON" y segundo "NICARETTA", no "DE" y "LEON NICARETTA".
/// </summary>
public static class SiiesNameSplitter
{
    /// <summary>Partículas que forman parte del apellido que las sigue (DE, DEL, DE LA, DE LOS, SAN, VAN DER, etc.).</summary>
    private static readonly HashSet<string> Particles = new(StringComparer.Ordinal)
    {
        "DE", "DEL", "LA", "LAS", "LOS", "Y", "E",
        "DA", "DAS", "DO", "DOS", "DI", "DU", "LE",
        "VAN", "VON", "DER", "DEN", "TER", "TEN",
        "SAN", "SANTA", "SANTO", "MC", "MAC",
    };

    /// <summary>
    /// Devuelve (primer apellido, segundo apellido). El primero absorbe las partículas iniciales y el apellido que las sigue;
    /// el segundo es el resto, también con sus partículas ("DE LA CRUZ", "DEL VALLE") intactas.
    /// </summary>
    public static (string PrimerApellido, string SegundoApellido) Split(string? lastName)
    {
        var parts = (lastName ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (string.Empty, string.Empty);

        var index = 0;
        // Partículas iniciales (mientras quede al menos una palabra para el apellido propiamente dicho)
        while (index < parts.Length - 1 && IsParticle(parts[index])) index++;
        // Apellido propiamente dicho
        index++;

        var primer = string.Join(' ', parts.Take(index));
        var segundo = string.Join(' ', parts.Skip(index));
        return (primer, segundo);
    }

    private static bool IsParticle(string word) => Particles.Contains(Normalize(word));

    /// <summary>Mayúsculas sin tildes, para comparar con la lista de partículas.</summary>
    private static string Normalize(string word)
    {
        var decomposed = word.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().ToUpperInvariant();
    }
}
