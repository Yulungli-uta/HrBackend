from geo_common import *
import os
adm1, adm2 = load_geonames()
provmap = json.load(open(G + "province_map.json"))
c = connect(); cur = c.cursor()
cur.execute("SELECT COUNT(*) FROM HR.tbl_Cantons WHERE Source = 'GEONAMES'")
if cur.fetchone()[0] > 0:
    sys.exit("Los cantones ya fueron cargados (hay filas con Source = GEONAMES). Esta herramienta es de una sola vez: use los .sql ya generados.")
cur.execute("SELECT CantonID, ProvinceID, CantonName FROM HR.tbl_Cantons")
legacy = collections.defaultdict(list); allids = set()
for r in cur.fetchall(): legacy[r[1]].append(r); allids.add(r[0])
by = collections.defaultdict(list)
for iso, rows in adm2.items():
    if iso == "EC": continue
    for a1, a2, nm, asc in rows: by[(iso, a1)].append((a2, nm, asc))
upd, ins = [], []; stats = collections.Counter(); width3 = 0
for (iso, a1), rows in sorted(by.items()):
    pid = provmap.get(f"{iso}.{a1}")
    if not pid: stats["no_parent"] += 1; continue
    cand = {}
    for a2, nm, asc in rows:
        for key in (norm2(nm), norm2(asc)): cand.setdefault(key, a2)
    used = set()
    for kid, kpid, kname in legacy.get(pid, []):
        k = match_best(kname, cand)
        if k and cand[k] not in used:
            used.add(cand[k]); upd.append(f"UPDATE HR.tbl_Cantons SET IsoCode = N'{iso}.{a1}.{cand[k]}' WHERE CantonID = N'{kid}';"); stats["matched"] += 1
        else: stats["legacy_kept"] += 1
    todo = [x for x in rows if x[0] not in used]
    total = len(legacy.get(pid, [])) + len(todo)
    seqlens = [len(i) - len(pid) for i in allids if i.startswith(pid)]
    width = 3 if (total > 99 or any(l >= 3 for l in seqlens)) else 2
    width3 += width == 3
    seqs = [int(i[len(pid):]) for i in allids if i.startswith(pid) and i[len(pid):].isdigit()]
    nxt = (max(seqs) if seqs else 0) + 1
    for a2, nm, asc in sorted(todo):
        while f"{pid}{nxt:0{width}d}" in allids: nxt += 1
        kid = f"{pid}{nxt:0{width}d}"; assert len(kid) <= 10; allids.add(kid); nxt += 1
        name = display(nm, asc).replace("'", "''"); assert len(name) <= 100
        ins.append(f"(N'{kid}', N'{pid}', N'{name}', SYSDATETIME(), N'{iso}.{a1}.{a2}', N'GEONAMES', 1)"); stats["inserted"] += 1
cur.execute("SELECT COUNT(*) FROM HR.tbl_Cantons"); before = cur.fetchone()[0]
sql = ["-- Catálogo geográfico: CANTONES (segundo nivel, GeoNames admin2: municipios, condados, departamentos...). Los heredados que coinciden",
       "-- por nombre reciben su código ISO y conservan su ID; los demás heredados no se tocan. Ecuador (INEC) no recibe filas nuevas.",
       "SET XACT_ABORT ON;", "BEGIN TRAN;"] + upd
for i in range(0, len(ins), 1000):
    sql.append("INSERT INTO HR.tbl_Cantons (CantonID, ProvinceID, CantonName, CreatedAt, IsoCode, Source, IsActive) VALUES\n" + ",\n".join(ins[i:i+1000]) + ";")
sql += [f"IF (SELECT COUNT(*) FROM HR.tbl_Cantons) <> {before + len(ins)} BEGIN ROLLBACK; THROW 50000, 'El conteo final de cantones no coincide', 1; END", "COMMIT;",
        "SELECT (SELECT COUNT(*) FROM HR.tbl_Cantons) cantones, (SELECT COUNT(*) FROM HR.tbl_Cantons WHERE IsoCode IS NOT NULL) conIso;"]
p = os.path.join(OUT, "60_geo_load_cantons.sql")
open(p, "w", encoding="utf-8").write("\n".join(sql) + "\n")
print(dict(stats), "| provinces with 3-digit width:", width3, "| before", before, "| expected after", before + len(ins), "| file MB", round(os.path.getsize(p) / 1e6, 1))
