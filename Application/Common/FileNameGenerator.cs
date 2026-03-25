using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static QuestPDF.Helpers.Colors;

namespace WsUtaSystem.Application.Common
{
    public static class FileNameGenerator
    {

        //prefix      hrcontract  Identifica el módulo / DirectoryCode
        //yyyyMMdd	  20260108	  Fecha(ordenable)
        //HHmmss	  143322	  Hora exacta
        //shortGuid   a9f3c7e2    Garantiza unicidad
        //extension   .pdf        Tipo real del archivo

        private static readonly Regex InvalidChars =
            new(@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled);

        public static string Generate(
            string originalFileName,
            string prefix)
        {
            var extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".bin";

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var shortGuid = Guid.NewGuid().ToString("N")[..8];

            var safePrefix = Sanitize(prefix);

            return $"{safePrefix}_{timestamp}_{shortGuid}{extension.ToLowerInvariant()}";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "file";

            value = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                var category = Char.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            value = sb.ToString().Normalize(NormalizationForm.FormC);

            value = InvalidChars.Replace(value, "_");
            return value.ToLowerInvariant();
        }
    }
}
