"""Conexión a SQL Server leyendo SqlServerConn de appsettings (no imprime credenciales). Ruta: variable GEO_APPSETTINGS."""
import os, re, pyodbc
def connect(autocommit=True):
    t = open(os.environ.get("GEO_APPSETTINGS", os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "appsettings.Development.json")), encoding="utf-8-sig").read()
    cs = re.search(r'"SqlServerConn"\s*:\s*"([^"]*)"', t).group(1)
    parts = dict(p.split("=", 1) for p in cs.split(";") if "=" in p)
    srv = parts.get("Server") or parts.get("Data Source")
    db = parts.get("Database") or parts.get("Initial Catalog")
    uid = parts.get("User Id") or parts.get("UID") or parts.get("User ID")
    pwd = next((v for k, v in parts.items() if k.strip().lower() in ("password", "pwd")), None)
    return pyodbc.connect(f"DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={srv};DATABASE={db};UID={uid};PWD={pwd};TrustServerCertificate=yes;Encrypt=yes", timeout=30, autocommit=autocommit)
