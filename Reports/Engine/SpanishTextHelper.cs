using System.Globalization;

namespace WsUtaSystem.Reports.Engine;

/// <summary>
/// Convierte números y fechas a su representación en palabras en español,
/// usado por las plantillas de contratos (ej. "TREINTA días", "DOS MIL VEINTISÉIS",
/// "SETECIENTOS TREINTA Y TRES CON 00/100 DOLARES").
/// </summary>
public static class SpanishTextHelper
{
    private static readonly string[] Unidades =
        ["", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE"];

    private static readonly string[] Decenas =
        ["", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"];

    private static readonly string[] Especiales =
    [
        "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"
    ];

    private static readonly string[] Centenas =
    [
        "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
        "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
    ];

    private static readonly string[] MesesEs =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    /// <summary>Convierte un entero (0-999,999,999) a palabras en mayúsculas, sin decimales.</summary>
    public static string NumberToWords(long number)
    {
        if (number == 0) return "CERO";
        if (number < 0) return "MENOS " + NumberToWords(-number);

        var parts = new List<string>();

        var millones = number / 1_000_000;
        var resto = number % 1_000_000;
        var miles = resto / 1000;
        var unidadesGrupo = resto % 1000;

        if (millones > 0)
            parts.Add(millones == 1 ? "UN MILLÓN" : $"{ThreeDigitsToWords(millones)} MILLONES");

        if (miles > 0)
            parts.Add(miles == 1 ? "MIL" : $"{ThreeDigitsToWords(miles)} MIL");

        if (unidadesGrupo > 0 || parts.Count == 0)
            parts.Add(ThreeDigitsToWords(unidadesGrupo));

        return string.Join(" ", parts).Trim();
    }

    private static string ThreeDigitsToWords(long n)
    {
        if (n == 0) return "";
        if (n == 100) return "CIEN";

        var centena = n / 100;
        var resto = n % 100;

        var parts = new List<string>();
        if (centena > 0) parts.Add(Centenas[centena]);
        if (resto > 0) parts.Add(TwoDigitsToWords(resto));

        return string.Join(" ", parts);
    }

    private static string TwoDigitsToWords(long n)
    {
        if (n < 10) return Unidades[n];
        if (n < 20) return Especiales[n - 10];

        var decena = n / 10;
        var unidad = n % 10;

        if (unidad == 0) return Decenas[decena];
        if (decena == 2) return $"VEINTI{Unidades[unidad]}";

        return $"{Decenas[decena]} Y {Unidades[unidad]}";
    }

    /// <summary>Convierte un monto decimal a palabras con formato "X CON YY/100" (ej. salarios).</summary>
    public static string AmountToWords(decimal amount)
    {
        var integerPart = (long)decimal.Truncate(amount);
        var cents = (int)Math.Round((amount - integerPart) * 100, MidpointRounding.AwayFromZero);
        return $"{NumberToWords(integerPart)} CON {cents:00}/100";
    }

    /// <summary>Día del mes en palabras (ej. "TREINTA").</summary>
    public static string DayToWords(DateTime date) => NumberToWords(date.Day);

    /// <summary>Nombre del mes en español, minúsculas (ej. "enero").</summary>
    public static string MonthName(DateTime date) => MesesEs[date.Month - 1];

    /// <summary>Año en palabras (ej. "DOS MIL VEINTISÉIS").</summary>
    public static string YearToWords(DateTime date) => NumberToWords(date.Year);

    /// <summary>Fecha corta dd/MM/yyyy, formato usado en las plantillas para fechas de resolución/memorando.</summary>
    public static string ShortDate(DateTime date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
