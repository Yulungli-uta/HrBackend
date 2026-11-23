"""Genera 61_geo_clean_names.sql: quita palabras administrativas en inglés al final de los nombres cargados desde GeoNames."""
from geo_common import *
import os, re
SUF = r"( DEPARTMENT| PROVINCE| REGION| STATE| COUNTY| DISTRICT| MUNICIPALITY| GOVERNORATE| PREFECTURE| COMMUNE| CANTON| CITY| REPUBLIC| TERRITORY)$"
c = connect(); cur = c.cursor()
sql = ["-- Catálogo geográfico: quita palabras administrativas en inglés al final de los nombres cargados desde GeoNames (\"Quindío Department\" -> \"QUINDÍO\").",
       "-- Solo filas con Source = 'GEONAMES'; no se renombra si el resultado ya existe dentro del mismo padre.", "SET XACT_ABORT ON;", "BEGIN TRAN;"]
for t, idc, namec, par in [("tbl_Provinces", "ProvinceID", "ProvinceName", "CountryID"), ("tbl_Cantons", "CantonID", "CantonName", "ProvinceID")]:
    cur.execute(f"SELECT {idc},{par},{namec} FROM HR.{t} WHERE Source='GEONAMES'"); rows = cur.fetchall()
    cur.execute(f"SELECT {par},{namec} FROM HR.{t}"); taken = collections.defaultdict(set)
    for p, n in cur.fetchall(): taken[p].add(n)
    ch = []
    for i, p, n in rows:
        m = re.sub(SUF, "", n).strip()
        if m != n and m and m not in taken[p]: ch.append((i, m)); taken[p].add(m)
    for k in range(0, len(ch), 1000):
        vals = ",\n".join("(N'%s', N'%s')" % (i, m.replace("'", "''")) for i, m in ch[k:k + 1000])
        sql.append(f"UPDATE x SET x.{namec} = v.n FROM HR.{t} x JOIN (VALUES\n{vals}\n) v(id, n) ON v.id = x.{idc} WHERE x.Source = N'GEONAMES';")
sql += ["COMMIT;", "SELECT (SELECT COUNT(*) FROM HR.tbl_Provinces WHERE ProvinceName LIKE N'% DEPARTMENT') prov_restantes, (SELECT COUNT(*) FROM HR.tbl_Cantons WHERE CantonName LIKE N'% DISTRICT' AND Source=N'GEONAMES') cant_restantes;"]
open(os.path.join(OUT, "61_geo_clean_names.sql"), "w", encoding="utf-8").write("\n".join(sql) + "\n")
