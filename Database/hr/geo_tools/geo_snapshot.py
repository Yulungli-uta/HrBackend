import os, sys, json; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dbconn import connect
def snap():
    c = connect(); cur = c.cursor(); s = {}
    for t in ['tbl_Countries','tbl_Provinces','tbl_Cantons']:
        cur.execute(f"SELECT COUNT(*) FROM HR.{t}"); s[t] = cur.fetchone()[0]
    refs = {'tbl_People':['CountryID','ProvinceID','CantonID'],'tbl_Addresses':['CountryID','ProvinceID','CantonID'],'tbl_Institutions':['CountryID','ProvinceID','CantonID'],
            'tbl_Trainings':['CountryID'],'tbl_Books':['CountryID'],'tbl_Languages':['CountryID'],'tbl_WorkExperiences':['CountryID']}
    for t, cols in refs.items():
        cur.execute(f"SELECT COUNT(*) FROM HR.{t}"); s[t+'.rows'] = cur.fetchone()[0]
        for col in cols:
            cur.execute(f"SELECT COUNT({col}) FROM HR.{t}"); s[f'{t}.{col}.notnull'] = cur.fetchone()[0]
    cur.execute("SELECT COUNT(*) FROM HR.tbl_Provinces WHERE CountryID='001'"); s['EC.prov'] = cur.fetchone()[0]
    cur.execute("SELECT COUNT(*) FROM HR.tbl_Cantons k JOIN HR.tbl_Provinces p ON p.ProvinceID=k.ProvinceID WHERE p.CountryID='001'"); s['EC.cant'] = cur.fetchone()[0]
    return s
if __name__ == "__main__":
    s = snap(); json.dump(s, open(sys.argv[1], "w")); print(json.dumps(s, indent=0))
