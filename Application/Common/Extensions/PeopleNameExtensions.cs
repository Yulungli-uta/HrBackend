using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Common.Extensions;

public static class PeopleNameExtensions
{
    // 2026-08-18: PreferredDenomination existe pero deliberadamente NO se aplica aquí —
    // es un helper compartido usado en varios lugares sin relación con firmas de
    // documentos (ej. GuardVacationService). La denominación solo debe afectar los
    // campos de responsables/firmantes de Acciones de Personal y Contratos — ver
    // PersonnelActionRepository.ResolveEmployeeAsync, que la calcula aparte.
    public static string GetFullName(this People? person) =>
        person is null ? string.Empty : $"{person.LastName} {person.FirstName}".Trim();
}
