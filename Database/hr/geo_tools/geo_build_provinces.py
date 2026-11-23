from geo_common import *
import os
adm1, adm2 = load_geonames()
c = connect(); cur = c.cursor()
cur.execute("SELECT COUNT(*) FROM HR.tbl_Provinces WHERE Source = 'GEONAMES'")
if cur.fetchone()[0] > 0:
    sys.exit("Las provincias ya fueron cargadas (hay filas con Source = GEONAMES). Esta herramienta es de una sola vez: use los .sql ya generados.")
cur.execute("SELECT CountryID, IsoCode FROM HR.tbl_Countries WHERE IsoCode IS NOT NULL")
iso2cid = {r[1]: r[0] for r in cur.fetchall()}
cur.execute("SELECT ProvinceID, CountryID, ProvinceName FROM HR.tbl_Provinces ORDER BY ProvinceID")
legacy = collections.defaultdict(list); allids = set()
for r in cur.fetchall(): legacy[r[1]].append(r); allids.add(r[0])
upd, ins, provmap, review = [], [], {}, []
stats = collections.Counter()
for iso, cid in sorted(iso2cid.items()):
    A = adm1.get(iso, [])
    if not A: continue
    cand = {}
    for a1, nm, asc in A:
        for key in (norm2(nm), norm2(asc)): cand.setdefault(key, a1)
    used = set()
    for pid, ccid, pname in legacy.get(cid, []):
        k = match_best(pname, cand)
        if k and cand[k] not in used:
            a1 = cand[k]; used.add(a1); provmap[f"{iso}.{a1}"] = pid
            upd.append(f"UPDATE HR.tbl_Provinces SET IsoCode = N'{iso}.{a1}' WHERE ProvinceID = N'{pid}';"); stats["matched"] += 1
            if norm2(pname) != k: review.append((iso, pname, k))
        else: stats["legacy_kept"] += 1
    if iso == "EC": continue
    todo = [(a1, nm, asc) for a1, nm, asc in A if a1 not in used]
    total = len(legacy.get(cid, [])) + len(todo)
    seqlens = [len(i) - 3 for i in allids if i.startswith(cid)]
    width = 3 if (total > 99 or any(l >= 3 for l in seqlens)) else 2
    existing_seq = [int(i[3:]) for i in allids if i.startswith(cid) and i[3:].isdigit()]
    nxt = (max(existing_seq) if existing_seq else 0) + 1
    for a1, nm, asc in sorted(todo):
        while f"{cid}{nxt:0{width}d}" in allids: nxt += 1
        pid = f"{cid}{nxt:0{width}d}"; allids.add(pid); nxt += 1
        name = display(nm, asc).replace("'", "''"); assert len(name) <= 100
        ins.append(f"(N'{pid}', N'{cid}', N'{name}', SYSDATETIME(), N'{iso}.{a1}', N'GEONAMES', 1)"); provmap[f"{iso}.{a1}"] = pid; stats["inserted"] += 1
cur.execute("SELECT COUNT(*) FROM HR.tbl_Provinces"); before = cur.fetchone()[0]
sql = ["-- Catálogo geográfico: PROVINCIAS (primer nivel, GeoNames admin1). Las provincias heredadas que coinciden por nombre reciben su código ISO",
       "-- y conservan su ID (hay personas, direcciones e instituciones apuntando a ellas); las demás heredadas no se tocan. Ecuador no recibe filas nuevas.",
       "SET XACT_ABORT ON;", "BEGIN TRAN;"] + upd
for i in range(0, len(ins), 500):
    sql.append("INSERT INTO HR.tbl_Provinces (ProvinceID, CountryID, ProvinceName, CreatedAt, IsoCode, Source, IsActive) VALUES\n" + ",\n".join(ins[i:i+500]) + ";")
sql += [f"IF (SELECT COUNT(*) FROM HR.tbl_Provinces) <> {before + len(ins)} BEGIN ROLLBACK; THROW 50000, 'El conteo final de provincias no coincide', 1; END", "COMMIT;",
        "SELECT (SELECT COUNT(*) FROM HR.tbl_Provinces) provincias, (SELECT COUNT(*) FROM HR.tbl_Provinces WHERE IsoCode IS NOT NULL) conIso;"]
open(os.path.join(OUT, "59_geo_load_provinces.sql"), "w", encoding="utf-8").write("\n".join(sql) + "\n")
json.dump(provmap, open(G + "province_map.json", "w"))
print(dict(stats), "| before", before, "| expected after", before + len(ins))
print("fuzzy matches to review:", review[:25], len(review))
