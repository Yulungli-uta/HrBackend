using System.Text.Json;
using WsUtaSystem.Application.DTOs.UserPreferences;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IUserPreferenceRepository _repo;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(IUserPreferenceRepository repo, ILogger<UserPreferencesService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserPreferencesResponseDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var row = await _repo.GetByUserAsync(userId, track: false, ct);
        if (row is null)
            return new UserPreferencesResponseDto { SchemaVersion = UserPreferencesValidator.CurrentSchemaVersion };

        return new UserPreferencesResponseDto
        {
            SchemaVersion = row.SchemaVersion,
            Preferences = Deserialize(row.PreferencesJson, userId),
            UpdatedAt = row.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<UserPreferencesResponseDto> SaveAsync(Guid userId, UserPreferencesDto dto, CancellationToken ct)
    {
        var errors = UserPreferencesValidator.Validate(dto);
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors));

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var now = DateTime.UtcNow;

        var row = await _repo.GetByUserAsync(userId, track: true, ct);
        if (row is null)
        {
            row = new UserPreference { UserId = userId, CreatedAt = now };
            await _repo.AddAsync(row, ct);
        }
        row.SchemaVersion = UserPreferencesValidator.CurrentSchemaVersion;
        row.PreferencesJson = json;
        row.UpdatedAt = now;

        await _repo.SaveChangesAsync(ct);

        return new UserPreferencesResponseDto
        {
            SchemaVersion = row.SchemaVersion,
            Preferences = Deserialize(json, userId),
            UpdatedAt = row.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task ResetAsync(Guid userId, CancellationToken ct)
    {
        await _repo.DeleteByUserAsync(userId, ct);
    }

    // Un JSON corrupto nunca debe romper la sesión del usuario: se devuelven valores por defecto.
    private UserPreferencesDto Deserialize(string json, Guid userId)
    {
        try
        {
            return JsonSerializer.Deserialize<UserPreferencesDto>(json, JsonOptions) ?? new UserPreferencesDto();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Preferencias con JSON inválido para el usuario {UserId}; se usan valores por defecto.", userId);
            return new UserPreferencesDto();
        }
    }
}
