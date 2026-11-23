import os, sys; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dbconn import connect
c = connect(); cur = c.cursor()
cur.execute(open(sys.argv[1], encoding="utf-8").read())
while True:
    try:
        if cur.description: print([x[0] for x in cur.description], [list(r) for r in cur.fetchall()][:3])
    except Exception: pass
    if not cur.nextset(): break
print("OK")
