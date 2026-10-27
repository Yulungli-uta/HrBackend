namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Cómo reparte un reporte sus columnas en las hojas del PDF. Cada reporte declara la suya en
/// <see cref="ReportDefinition.PdfPagination"/>; si no declara nada se usa <see cref="FitA4"/> (el comportamiento histórico),
/// de modo que ningún reporte cambia de formato sin pedirlo.
/// </summary>
public enum PdfPagination
{
    /// <summary>Todas las columnas en una hoja A4, con los anchos relativos de <see cref="ReportColumn.Width"/>. Es el valor por defecto.</summary>
    FitA4 = 0,

    /// <summary>
    /// Si las columnas caben legibles en una A4 se comporta como <see cref="FitA4"/>; si no, las parte en bloques de columnas,
    /// cada bloque en su propia(s) hoja(s) A4 y repitiendo las columnas fijas (<see cref="ReportDefinition.PinnedColumnKeys"/>).
    /// Pensado para reportes anchos que se imprimen en A4. Con muchas columnas fijas deja pocas por bloque (SIIES quedaba en
    /// 12 bloques y más de 1.000 hojas), por eso las matrices SIIES usan <see cref="Expand"/>.
    /// </summary>
    Blocks = 1,

    /// <summary>
    /// Una sola hoja tan ancha como haga falta (misma altura que la A4). Todas las columnas quedan en una línea por fila,
    /// pero la hoja no se imprime en A4: solo sirve para revisar en pantalla con zoom. Es la política de las matrices SIIES.
    /// </summary>
    Expand = 2,
}
