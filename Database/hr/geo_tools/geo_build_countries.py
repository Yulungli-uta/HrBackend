from geo_common import *
import os
c = connect(); cur = c.cursor()
cur.execute("SELECT COUNT(*) FROM HR.tbl_Countries WHERE Source = 'ISO'")
if cur.fetchone()[0] > 0:
    sys.exit("Los paises ya fueron completados (hay filas con Source = ISO). Esta herramienta es de una sola vez: use los .sql ya generados.")
cur.execute("SELECT CountryID, CountryName, Nationality, NationalityCode, AuxSIITH, AuxCEAACES FROM HR.tbl_Countries ORDER BY CountryID")
rows = cur.fetchall()
REFS = {"tbl_People":"CountryID","tbl_Addresses":"CountryID","tbl_Institutions":"CountryID","tbl_Trainings":"CountryID","tbl_Books":"CountryID","tbl_Languages":"CountryID","tbl_WorkExperiences":"CountryID","tbl_Provinces":"CountryID"}
pk = {}
for t in REFS:
    cur.execute("SELECT c.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_primary_key=1 AND i.object_id=OBJECT_ID(?)", f"HR.{t}")
    cols = [r[0] for r in cur.fetchall()]; assert len(cols) == 1, (t, cols); pk[t] = cols[0]
refc = collections.Counter()
for t in REFS:
    if t == "tbl_Provinces": continue
    cur.execute(f"SELECT CountryID, COUNT(*) FROM HR.{t} WHERE CountryID IS NOT NULL GROUP BY CountryID")
    for cid, n in cur.fetchall(): refc[cid] += n
isoN = {norm(n): k for k, n in ISO.items()}
grp = collections.defaultdict(list); qa = []
for r in rows:
    n = norm(r[1])
    if NOT_COUNTRY_QA(n): qa.append(r[0]); continue
    k = LEGACY.get(n) or isoN.get(n); assert k, r
    grp[k].append(r)
PREFER = {"GB": "162", "CN": "043", "AE": "060", "NL": "151"}
canon = {}; merges = []
for k, g in grp.items():
    ids = [x[0] for x in g]
    best = PREFER.get(k) or max(g, key=lambda x: (x[3] is not None, refc[x[0]], -int(x[0])))[0]
    canon[k] = best
    for x in g:
        if x[0] != best: merges.append((x[0], x[1], best, k))
newc = sorted(set(ISO) - set(grp))
nextid = max(int(r[0]) for r in rows) + 1
sql = ["-- Catálogo geográfico: limpieza y completado de PAÍSES (ISO 3166-1).",
       "-- 1) Fusiona duplicados y entradas que no son países, repuntando TODAS las referencias (queda registro en HR.tbl_GeoCleanupLog).",
       "-- 2) Quita el país de prueba QA de capacitaciones y libros reales (sin borrarlos) y elimina los países y provincias de QA.",
       "-- 3) Marca el código ISO de cada país y agrega los que faltaban.",
       "SET XACT_ABORT ON;", "BEGIN TRAN;"]
def log_update(t, col, old, new, reason):
    p = pk[t]
    return [f"INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'{t}', CAST({p} AS nvarchar(50)), N'{col}', {col}, {('N' + repr(new)) if new else 'NULL'}, N'{reason}' FROM HR.{t} WHERE {col} = N'{old}';",
            f"UPDATE HR.{t} SET {col} = {('N' + repr(new)) if new else 'NULL'} WHERE {col} = N'{old}';"]
for x, xname, y, k in merges:
    sql.append(f"-- {x} {xname} -> {y} ({k})")
    sql.append(f"UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'{x}' WHERE c.CountryID = N'{y}';")
    for t, col in REFS.items(): sql += log_update(t, col, x, y, f"Fusion pais {x}->{y}")
    sql.append(f"DELETE FROM HR.tbl_Countries WHERE CountryID = N'{x}';")
sql.append("-- Países de prueba QA")
qin = ",".join(f"N'{q}'" for q in qa)
for t in ["tbl_Trainings", "tbl_Books"]:
    p = pk[t]
    sql.append(f"INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'{t}', CAST({p} AS nvarchar(50)), N'CountryID', CountryID, NULL, N'Pais de prueba QA' FROM HR.{t} WHERE CountryID IN ({qin});")
    sql.append(f"UPDATE HR.{t} SET CountryID = NULL WHERE CountryID IN ({qin});")
sql.append(f"IF EXISTS (SELECT 1 FROM HR.tbl_People WHERE CountryID IN ({qin})) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE CountryID IN ({qin})) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE CountryID IN ({qin})) OR EXISTS (SELECT 1 FROM HR.tbl_Languages WHERE CountryID IN ({qin})) OR EXISTS (SELECT 1 FROM HR.tbl_WorkExperiences WHERE CountryID IN ({qin})) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a un pais de QA', 1; END")
sql.append(f"IF EXISTS (SELECT 1 FROM HR.tbl_Provinces p WHERE p.CountryID IN ({qin}) AND (EXISTS (SELECT 1 FROM HR.tbl_People WHERE ProvinceID = p.ProvinceID) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE ProvinceID = p.ProvinceID) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE ProvinceID = p.ProvinceID))) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a una provincia de QA', 1; END")
sql.append(f"IF EXISTS (SELECT 1 FROM HR.tbl_Cantons k JOIN HR.tbl_Provinces p ON p.ProvinceID = k.ProvinceID WHERE p.CountryID IN ({qin}) AND (EXISTS (SELECT 1 FROM HR.tbl_People WHERE CantonID = k.CantonID) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE CantonID = k.CantonID) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE CantonID = k.CantonID))) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a un canton de QA', 1; END")
sql.append(f"DELETE k FROM HR.tbl_Cantons k JOIN HR.tbl_Provinces p ON p.ProvinceID = k.ProvinceID WHERE p.CountryID IN ({qin});")
sql.append(f"DELETE FROM HR.tbl_Provinces WHERE CountryID IN ({qin});")
sql.append(f"DELETE FROM HR.tbl_Countries WHERE CountryID IN ({qin});")
sql.append("-- Código ISO de los países existentes")
for k, cid in sorted(canon.items()): sql.append(f"UPDATE HR.tbl_Countries SET IsoCode = N'{k}' WHERE CountryID = N'{cid}';")
sql.append("-- Países que faltaban (nombre en español ISO; sin nacionalidad: no existe en la fuente)")
vals = []
for i, k in enumerate(newc):
    name = (SHORT.get(k) or ISO[k]).upper().replace("'", "''")
    vals.append(f"(N'{nextid + i:03d}', N'{name}', NULL, NULL, NULL, NULL, SYSDATETIME(), N'{k}', N'ISO', 1)")
sql.append("INSERT INTO HR.tbl_Countries (CountryID, CountryName, Nationality, NationalityCode, AuxSIITH, AuxCEAACES, CreatedAt, IsoCode, Source, IsActive) VALUES\n" + ",\n".join(vals) + ";")
exp = 233 - len(merges) - len(qa) + len(newc)
sql.append(f"IF (SELECT COUNT(*) FROM HR.tbl_Countries) <> {exp} BEGIN ROLLBACK; THROW 50000, 'El conteo final de paises no coincide con lo esperado', 1; END")
sql.append("COMMIT;")
sql.append("SELECT (SELECT COUNT(*) FROM HR.tbl_Countries) paises, (SELECT COUNT(*) FROM HR.tbl_Countries WHERE IsoCode IS NOT NULL) conIso, (SELECT COUNT(*) FROM HR.tbl_GeoCleanupLog) registrosLog;")
open(os.path.join(OUT, "58_geo_countries_cleanup.sql"), "w", encoding="utf-8").write("\n".join(sql) + "\n")
print("merges", len(merges), "| QA", len(qa), "| new", len(newc), "| expected countries", exp)
print("MERGES:", [(m[0], m[1], "->", m[2]) for m in merges])
json.dump({"canon": canon, "newc": {k: f"{nextid + i:03d}" for i, k in enumerate(newc)}}, open(G + "country_plan.json", "w"))
