"""Utilidades comunes de la carga del catálogo geográfico (ISO 3166 + GeoNames). Variables: GEO_DATA_DIR, GEO_APPSETTINGS, GEO_OUT_DIR."""
import os, sys, unicodedata, gettext, collections, json
import pycountry
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dbconn import connect
G = os.environ.get("GEO_DATA_DIR", "geodata").rstrip("/\\") + "/"
def norm(s):
    s = unicodedata.normalize("NFKD", s).encode("ascii","ignore").decode().upper()
    for ch in "().,-/'": s = s.replace(ch, " ")
    return " ".join(s.split())
tr = gettext.translation('iso3166-1', pycountry.LOCALES_DIR, languages=['es'], fallback=True)
ISO = {c.alpha_2: tr.gettext(c.name) for c in pycountry.countries}
ISO["XK"] = "Kosovo"
# Nombres cortos en español para mostrar (el ISO trae nombres formales: "Bolivia, Estado plurinacional de")
SHORT = {"BO":"BOLIVIA","VE":"VENEZUELA","IR":"IRAN","MD":"MOLDAVIA","KP":"COREA DEL NORTE","KR":"COREA DEL SUR","RU":"RUSIA","SY":"SIRIA","TZ":"TANZANIA","LA":"LAOS","CD":"REPUBLICA DEMOCRATICA DEL CONGO","CG":"CONGO","FM":"MICRONESIA","PS":"PALESTINA","TW":"TAIWAN","VA":"VATICANO","BN":"BRUNEI","MK":"MACEDONIA DEL NORTE","CV":"CABO VERDE","VG":"ISLAS VIRGENES BRITANICAS","VI":"ISLAS VIRGENES DE ESTADOS UNIDOS","FK":"ISLAS MALVINAS","MM":"MYANMAR","CZ":"CHEQUIA","NL":"PAISES BAJOS","AE":"EMIRATOS ARABES UNIDOS","XK":"KOSOVO"}
# Alias de nombres heredados -> ISO (los nombres formales de ISO no coinciden con los del catálogo SIES)
LEGACY = {"ARGELIA":"DZ","BERMUDAS":"BM","BOLIVIA":"BO","BOSNIA":"BA","HERZEGOVINA":"BA","BOTSWANA":"BW","BRUNEI":"BN","BURKINA FASO":"BF","COMORAS":"KM",
 "COREA DEL NORTE":"KP","REPUBLICA DE COREA":"KR","DUBAI":"AE","EMIRATOS ARABES UNID":"AE","ESCOCIA":"GB","INGLATERRA":"GB","GUINEA BISSAU":"GW","HOLANDA":"NL",
 "IRAN":"IR","ISLAS PITCAIRN":"PN","KAZAJSTAN":"KZ","KOSOVO":"XK","LAOS":"LA","LESOTHO":"LS","MACEDONIA":"MK","MALAWI":"MW","MALDIVAS":"MV",
 "MARIANAS DEL NORTE":"MP","MICRONESIA":"FM","MOLDAVIA":"MD","MYANMAR BIRMANIA":"MM","PALAU":"PW","PALESTINA":"PS","QATAR":"QA","REPUB CENTROAFRICANA":"CF",
 "REPUBLICA DEL CONGO":"CG","REP DEMOCRATICA CONGO ZAIRE":"CD","RUSIA":"RU","SAINT KITTS NEVIS":"KN","SAMOA AMERICANA":"AS","SANTA LUCIA PAIS":"LC","SIRIA":"SY",
 "SRI LANKA ANTES CEILAN":"LK","SUAZILANDIA":"SZ","TAIWAN REPUBLICA CHINA":"TW","TANZANIA":"TZ","TIBET BAJO SOBERANIA CHINA":"CN","TIMOR ORIENTAL INDONESIA":"TL",
 "UZBEQUISTAN":"UZ","VATICANO":"VA","VENEZUELA":"VE","REPUBLICA CHECA":"CZ","CHEQUIA":"CZ","CONGO":"CG","BELGICA":"BE","SUDAFRICA":"ZA","PERU":"PE"}
NOT_COUNTRY_QA = lambda n: n.startswith("PAIS DE PRUEBA") or n.startswith("PAIS QA")
def load_geonames():
    adm1 = collections.defaultdict(list); adm2 = collections.defaultdict(list)
    for l in open(G+"admin1CodesASCII.txt", encoding="utf-8"):
        code, name, ascii_, gid = l.rstrip("\n").split("\t"); cc, a1 = code.split("."); adm1[cc].append((a1, name, ascii_))
    for l in open(G+"admin2Codes.txt", encoding="utf-8"):
        code, name, ascii_, gid = l.rstrip("\n").split("\t"); cc, a1, a2 = code.split("."); adm2[cc].append((a1, a2, name, ascii_))
    return adm1, adm2
import re, difflib
PREFIX = re.compile(r"^(DEPARTAMENTO|PROVINCIA|REGION|ESTADO|MUNICIPIO|DISTRITO|COMUNA|CANTON|PREFECTURA|STATE|PROVINCE|DEPARTMENT|COUNTY|DISTRICT|MUNICIPALITY|GOVERNORATE|REGION)( DE LA| DE LOS| DE LAS| DEL| DE| OF THE| OF)? ")
SUFFIX = re.compile(r" (PROVINCE|DEPARTMENT|REGION|STATE|COUNTY|DISTRICT|MUNICIPALITY|GOVERNORATE|PREFECTURE)$")
def norm2(s):
    n = norm(s)
    n = PREFIX.sub("", n); n = SUFFIX.sub("", n)
    return n
def latin(s): return all(ord(ch) < 0x250 for ch in s)
def display(name, ascii_):
    n = name if latin(name) else ascii_
    return n.upper().strip()
def match_best(name, cand):  # cand: dict key(norm2)->code
    n = norm2(name)
    if n in cand: return n
    hit = difflib.get_close_matches(n, list(cand), n=1, cutoff=0.86)
    return hit[0] if hit else None
def child_id(parent, existing_ids, width3):
    prefix_seqs = [int(i[len(parent):]) for i in existing_ids if i.startswith(parent) and i[len(parent):].isdigit()]
    return prefix_seqs

OUT = os.environ.get("GEO_OUT_DIR", os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
