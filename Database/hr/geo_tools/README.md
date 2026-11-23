# Carga del catálogo geográfico (países, provincias, cantones)

Herramientas que generaron los scripts `57` a `61` de `Database/hr`. **Son de una sola vez**: los `.sql` ya generados contienen
los datos y son lo que se aplica en cada ambiente. Estas herramientas existen para auditar cómo se produjeron o para repetir la carga
desde cero en una base distinta (con la misma situación inicial que desarrollo el 2026-11-23).

## Scripts que se aplican (en este orden)

| Script | Qué hace |
|---|---|
| `57_geo_catalog_schema.sql` | Columnas `IsoCode`, `Source`, `IsActive`; índices únicos por código; tabla `HR.tbl_GeoCleanupLog`. |
| `58_geo_countries_cleanup.sql` | Fusiona duplicados, quita los países de QA (dejando sin país las capacitaciones/libros que los usaban) y agrega los países ISO que faltaban. Registra cada fila modificada en `HR.tbl_GeoCleanupLog`. |
| `59_geo_load_provinces.sql` | Primer nivel (GeoNames admin1). Las heredadas que coinciden por nombre reciben su código ISO y conservan su ID. |
| `60_geo_load_cantons.sql` | Segundo nivel (GeoNames admin2). Ecuador (INEC) no recibe filas nuevas. |
| `61_geo_clean_names.sql` | Quita palabras administrativas en inglés al final de los nombres cargados ("Quindío Department" → "QUINDÍO"). |

Cada script va en una transacción y valida el conteo esperado antes de confirmar. Orden de despliegue: scripts → backend → frontend
(el frontend nuevo ya no descarga el catálogo completo).

## Fuentes (descargadas el 2026-11-23)

- Países: ISO 3166-1 con nombres en español (paquete `pycountry`, traducciones gettext) + Kosovo (`XK`, código de uso común).
- Provincias y cantones: GeoNames, licencia **CC BY 4.0** (se debe citar la fuente):
  - https://download.geonames.org/export/dump/admin1CodesASCII.txt
  - https://download.geonames.org/export/dump/admin2Codes.txt
  - https://download.geonames.org/export/dump/countryInfo.txt

Los archivos crudos no se versionan.

## Reglas de la carga

- **Nada que ya existía se borra ni se renumera**, salvo las fusiones y los países de QA de `58`. Las provincias y cantones heredados que no
  coinciden por nombre se conservan (hay personas apuntando a ellos) con `Source = 'LEGACY'` y sin código ISO.
- `Source`: `LEGACY` (ya existía), `ISO` (país agregado), `GEONAMES` (provincia/cantón cargado).
- IDs: país de 3 dígitos; provincia = país + secuencia; cantón = provincia + secuencia. La secuencia es de 2 dígitos y de 3 si el padre
  tiene 99 hijos o más (55 provincias y Eslovenia). **Los IDs se tratan siempre como texto**: no convertirlos a número.
- Escocia e Inglaterra se fusionaron en Reino Unido, Tíbet en China y Dubái en Emiratos Árabes. Las personas de esos países salen en los
  reportes SIIES con el código del país canónico.
- Los 44 países agregados no tienen nacionalidad ni código SIIES (la fuente no los trae).

## Herramientas (Python 3 con `pyodbc` y `pycountry`)

Variables de entorno: `GEO_DATA_DIR` (carpeta con los archivos de GeoNames), `GEO_APPSETTINGS` (ruta a un `appsettings` con
`SqlServerConn`; por defecto `appsettings.Development.json`), `GEO_OUT_DIR` (dónde escribir los `.sql`; por defecto `Database/hr`).

| Archivo | Uso |
|---|---|
| `geo_build_countries.py` | Genera `58_...`. Imprime el plan de fusiones. |
| `geo_build_provinces.py` | Genera `59_...`. |
| `geo_build_cantons.py` | Genera `60_...` (necesita `province_map.json`, que deja `geo_build_provinces.py` en `GEO_DATA_DIR`). |
| `geo_build_names.py` | Genera `61_...`. |
| `geo_snapshot.py <salida.json>` | Foto de conteos (tablas geográficas y referencias) para comparar antes y después. |
| `run_sql_file.py <archivo.sql>` | Ejecuta un script completo y muestra los resultados. |

Los generadores **se niegan a correr si el catálogo ya fue cargado** (filas con `Source = 'ISO'` o `'GEONAMES'`), porque no son idempotentes.

## Cómo revertir

`HR.tbl_GeoCleanupLog` guarda, por cada fila modificada en `58`, la tabla, la clave, la columna, el valor anterior y el nuevo. Las filas
agregadas por `59` y `60` se identifican con `Source = 'GEONAMES'`; las de `58`, con `Source = 'ISO'`. Los países fusionados y los de QA
se eliminaron: para restaurarlos hay que volver a insertarlos desde una copia de la base.
