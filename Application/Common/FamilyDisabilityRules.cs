namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas de la discapacidad de una carga familiar: con tipo de discapacidad el porcentaje es obligatorio, mayor que 0 y hasta 100;
/// sin tipo, el porcentaje debe estar vacío (el 0 se guarda como vacío). El formulario de HrFrontend aplica la misma regla.
/// </summary>
public static class FamilyDisabilityRules
{
    /// <summary>
    /// Valida y devuelve los valores a guardar. Lanza <see cref="BusinessRuleException"/> con un mensaje para el usuario si incumplen la regla.
    /// </summary>
    public static (int? TypeId, decimal? Percentage) ValidateAndNormalize(int? typeId, decimal? percentage)
    {
        if (typeId is null)
        {
            if (percentage is > 0)
                throw new BusinessRuleException("No se puede registrar un porcentaje de discapacidad si no se indica el tipo de discapacidad.");
            return (null, null);
        }

        if (percentage is null or <= 0)
            throw new BusinessRuleException("El porcentaje de discapacidad es obligatorio y debe ser mayor que 0 cuando se indica el tipo de discapacidad.");
        if (percentage > 100)
            throw new BusinessRuleException("El porcentaje de discapacidad no puede ser mayor a 100.");

        return (typeId, percentage);
    }

    /// <summary>
    /// true si el tipo o el porcentaje cambiaron (vacío y 0 en el porcentaje son lo mismo). Sirve para no exigir la regla a quien guarda
    /// otros datos de una carga familiar cuya discapacidad anterior ya estaba incompleta.
    /// </summary>
    public static bool HasChanged(int? oldTypeId, decimal? oldPercentage, int? newTypeId, decimal? newPercentage) =>
        oldTypeId != newTypeId || (oldPercentage ?? 0m) != (newPercentage ?? 0m);
}
