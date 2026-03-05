using FluentValidation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WsUtaSystem.Application.DTOs.Docflow
{
    public sealed class CreateInstanceRequestValidator : AbstractValidator<CreateInstanceRequest>
    {
        public CreateInstanceRequestValidator()
        {
            RuleFor(x => x.InitialProcessId).GreaterThan(0);
            RuleFor(x => x.DynamicMetadata).MaximumLength(20000);
        }
    }

    public sealed class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
    {
        public CreateDocumentRequestValidator()
        {
            RuleFor(x => x.DocumentName).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Visibility).Must(v => v is null or 1 or 2)
                .WithMessage("Visibility debe ser 1 (público) o 2 (privado del departamento).");
        }
    }

    public sealed class CreateMovementRequestValidator : AbstractValidator<CreateMovementRequest>
    {
        public CreateMovementRequestValidator()
        {
            RuleFor(x => x.MovementType).NotEmpty().Must(t => t is DocflowMovementType.Forward or DocflowMovementType.Return);
            When(x => x.MovementType == DocflowMovementType.Return, () =>
            {
                RuleFor(x => x.Comments).NotEmpty().MaximumLength(2000);
            });
            RuleFor(x => x.Comments).MaximumLength(2000);
        }
    }

    /// <summary>
    /// Validador para esquema de campo dinámico.
    /// Valida nombre, tipo y compatibilidad de valor con el tipo.
    /// </summary>
    public sealed class DynamicFieldSchemaDtoValidator : AbstractValidator<DynamicFieldSchemaDto>
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "string", "number", "boolean", "date"
        };

        public DynamicFieldSchemaDtoValidator()
        {
            // ✅ MEJORADO: Permitir espacios y caracteres especiales comunes
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("El nombre del campo es obligatorio.")
                .MaximumLength(100)
                    .WithMessage("El nombre no puede exceder 100 caracteres.")
                .Must(n => !n.StartsWith("_") && !char.IsDigit(n[0]))
                    .WithMessage("El nombre no puede iniciar con número o guion bajo.")
                .Must(n => !Regex.IsMatch(n, @"[<>{}|\\^`\[\]""']"))
                    .WithMessage("El nombre contiene caracteres no permitidos: < > { } | \\ ^ ` [ ] \" '");

            RuleFor(x => x.Type)
                .NotEmpty()
                    .WithMessage("El tipo de campo es obligatorio.")
                .Must(t => AllowedTypes.Contains(t))
                    .WithMessage("Tipo permitido: string, number, boolean, date.");

            // ✅ MEJORADO: Validación robusta de compatibilidad de valor con tipo
            RuleFor(x => x.Value).Custom((val, ctx) =>
            {
                var field = (DynamicFieldSchemaDto)ctx.InstanceToValidate;
                if (val is null) return;

                var type = field.Type?.ToLowerInvariant();
                var fieldName = field.Name ?? "campo";

                switch (type)
                {
                    case "string":
                        // String acepta cualquier cosa
                        return;

                    case "number":
                        if (!IsValidNumber(val))
                        {
                            ctx.AddFailure("Value",
                                $"El valor de '{fieldName}' debe ser un número válido para type=number.");
                        }
                        return;

                    case "boolean":
                        if (!IsValidBoolean(val))
                        {
                            ctx.AddFailure("Value",
                                $"El valor de '{fieldName}' debe ser true/false para type=boolean.");
                        }
                        return;

                    case "date":
                        if (!IsValidDate(val))
                        {
                            ctx.AddFailure("Value",
                                $"El valor de '{fieldName}' debe ser una fecha ISO válida (yyyy-MM-dd) para type=date.");
                        }
                        return;
                }
            });
        }

        /// <summary>
        /// Valida si un valor es compatible con type=number.
        /// </summary>
        private static bool IsValidNumber(object? val)
        {
            if (val is null) return true;

            // Tipos numéricos nativos
            if (val is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
                return true;

            // JsonElement
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number) return true;
                if (je.ValueKind == JsonValueKind.String && decimal.TryParse(je.GetString(), out _)) return true;
                return false;
            }

            // String o cualquier otro tipo que se pueda parsear
            return decimal.TryParse(val.ToString(), out _);
        }

        /// <summary>
        /// Valida si un valor es compatible con type=boolean.
        /// </summary>
        private static bool IsValidBoolean(object? val)
        {
            if (val is null) return true;

            // Tipo nativo
            if (val is bool) return true;

            // JsonElement
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False) return true;
                if (je.ValueKind == JsonValueKind.String && bool.TryParse(je.GetString(), out _)) return true;
                return false;
            }

            // String
            return bool.TryParse(val.ToString(), out _);
        }

        /// <summary>
        /// Valida si un valor es compatible con type=date.
        /// Acepta ISO 8601 (yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, etc.)
        /// </summary>
        private static bool IsValidDate(object? val)
        {
            if (val is null) return true;

            // Tipo nativo
            if (val is DateTime) return true;

            // JsonElement
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.String && DateTime.TryParse(je.GetString(), out _)) return true;
                if (je.ValueKind == JsonValueKind.Null) return true;
                return false;
            }

            // String
            return DateTime.TryParse(val.ToString(), out _);
        }
    }

    /// <summary>
    /// Validador para actualización de campos dinámicos de un proceso.
    /// </summary>
    public sealed class UpdateProcessDynamicFieldsRequestValidator : AbstractValidator<UpdateProcessDynamicFieldsRequest>
    {
        public UpdateProcessDynamicFieldsRequestValidator()
        {
            RuleFor(x => x.DynamicFieldMetadata)
                .NotNull()
                    .WithMessage("La lista de campos dinámicos es obligatoria.");

            // Validar cada campo con el validador específico
            RuleForEach(x => x.DynamicFieldMetadata)
                .SetValidator(new DynamicFieldSchemaDtoValidator());

            // Validar que no haya nombres duplicados
            RuleFor(x => x.DynamicFieldMetadata)
                .Must(list => list.Select(f => f.Name?.Trim().ToLowerInvariant()).Distinct().Count() == list.Count)
                .WithMessage("No se permiten campos con nombres duplicados (sin importar mayúsculas/minúsculas).");

            // Validar que haya al menos un campo si se envía la lista
            RuleFor(x => x.DynamicFieldMetadata)
                .Must(list => list.Count > 0)
                .WithMessage("Debe haber al menos un campo dinámico.")
                .When(x => x.DynamicFieldMetadata != null);
        }
    }
}