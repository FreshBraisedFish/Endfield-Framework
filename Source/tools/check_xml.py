import sys, xml.etree.ElementTree as ET
import os
root = sys.argv[1]
bad = []
count = 0
for base, dirs, files in os.walk(root):
    for f in files:
        if f.lower().endswith('.xml'):
            p = os.path.join(base, f)
            count += 1
            try:
                ET.parse(p)
            except Exception as e:
                bad.append((p, str(e)[:160]))
print("PARSED", count, "BAD", len(bad))
for p, e in bad:
    print("ERR", p, e)