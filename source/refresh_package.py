from pathlib import Path
import hashlib,json
p=Path(__file__).resolve().parent.parent/"package"
m=json.loads((p/"package.json").read_text(encoding="utf-8"))
for e in m["files"]:e["hash"]=hashlib.sha256((p/"payload"/e["path"]).read_bytes()).hexdigest()
(p/"package.json").write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding="utf-8")
print("Package hashes refreshed")
