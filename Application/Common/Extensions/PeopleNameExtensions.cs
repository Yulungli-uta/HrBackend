using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Common.Extensions;

public static class PeopleNameExtensions
{
    public static string GetFullName(this People? person) =>
        person is null ? string.Empty : $"{person.LastName} {person.FirstName}".Trim();
}
