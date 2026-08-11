using System.Reflection;
using System.Text.Json;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Arma el diff campo-por-campo (antes/después) de una corrección manual sobre una entidad
/// ya persistida, y lo deja registrado en HR.Audit con Action="CORRECTION". Reutiliza la
/// tabla HR.Audit existente (hoy solo poblada en DELETE por AuditSaveChangesInterceptor);
/// no crea tablas nuevas. Opera sobre snapshots tomados explícitamente por el caller (antes
/// y después de aplicar los cambios en memoria), no sobre el ChangeTracker de EF.
/// </summary>
public static class AuditSnapshotHelper
{
    /// <summary>Captura los valores actuales de las propiedades públicas de lectura de la entidad.</summary>
    public static Dictionary<string, object?> Snapshot(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p.GetValue(entity));
    }

    /// <summary>Compara dos snapshots de la misma entidad y retorna solo los campos cuyo valor cambió.</summary>
    public static List<AuditFieldChange> Diff(
        Dictionary<string, object?> before,
        Dictionary<string, object?> after)
    {
        var changes = new List<AuditFieldChange>();

        foreach (var (field, newValue) in after)
        {
            before.TryGetValue(field, out var oldValue);

            var oldText = oldValue?.ToString();
            var newText = newValue?.ToString();

            if (!string.Equals(oldText, newText, StringComparison.Ordinal))
                changes.Add(new AuditFieldChange(field, oldText, newText));
        }

        return changes;
    }

    /// <summary>
    /// Registra una corrección manual en HR.Audit (Action="CORRECTION") con el motivo y el
    /// diff campo-por-campo. No inserta fila si no hubo ningún cambio real.
    /// </summary>
    public static async Task WriteCorrectionAuditAsync(
        AppDbContext db,
        string tableName,
        string recordId,
        string reason,
        Dictionary<string, object?> before,
        Dictionary<string, object?> after,
        string? userName,
        CancellationToken ct)
    {
        var changes = Diff(before, after);
        if (changes.Count == 0) return;

        var details = JsonSerializer.Serialize(new { Reason = reason, Changes = changes });

        db.Set<Audit>().Add(new Audit
        {
            TableName = tableName,
            Action = "CORRECTION",
            RecordId = recordId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "unknown" : userName,
            DateTime = DateTime.Now,
            Details = details,
        });

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Un campo modificado por una corrección manual: valor anterior y nuevo, como texto.</summary>
public sealed record AuditFieldChange(string Field, string? OldValue, string? NewValue);
