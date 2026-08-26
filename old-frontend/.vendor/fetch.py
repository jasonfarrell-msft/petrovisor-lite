#!/usr/bin/env python3
"""Vendor a small npm package's files from jsdelivr into .vendor/<name>.
Used only as a workaround for a flaky npm registry proxy in this sandbox that
404s on a handful of specific tarballs. Downloads the full file tree listed
by jsdelivr's package metadata API.
"""
import json
import os
import sys
import urllib.request

def fetch(url, retries=3):
    last_err = None
    for _ in range(retries):
        try:
            with urllib.request.urlopen(url, timeout=20) as r:
                return r.read()
        except Exception as e:
            last_err = e
    raise last_err

def walk(files, base_url, base_path, url_prefix=""):
    for f in files:
        if f["type"] == "directory":
            sub_path = os.path.join(base_path, f["name"])
            os.makedirs(sub_path, exist_ok=True)
            walk(f["files"], base_url, sub_path, url_prefix + "/" + f["name"])
        else:
            rel = f["name"]
            out_path = os.path.join(base_path, rel)
            os.makedirs(os.path.dirname(out_path), exist_ok=True)
            import urllib.parse
            url = base_url + urllib.parse.quote(url_prefix + "/" + rel)
            try:
                data = fetch(url)
            except Exception as e:
                print(f"  SKIP {url_prefix}/{rel} ({e})")
                continue
            with open(out_path, "wb") as fh:
                fh.write(data)
            print("  wrote", out_path)

def main(pkg, version):
    meta_url = f"https://data.jsdelivr.com/v1/packages/npm/{pkg}@{version}"
    meta = json.loads(fetch(meta_url))
    out_dir = os.path.join(os.path.dirname(__file__), pkg)
    os.makedirs(out_dir, exist_ok=True)
    cdn_base = f"https://cdn.jsdelivr.net/npm/{pkg}@{version}"
    walk(meta["files"], cdn_base, out_dir)
    # Clean package.json of scripts/devDependencies that might trigger installs
    pkg_json_path = os.path.join(out_dir, "package.json")
    if os.path.exists(pkg_json_path):
        d = json.load(open(pkg_json_path))
        d.pop("scripts", None)
        d.pop("devDependencies", None)
        json.dump(d, open(pkg_json_path, "w"), indent=2)
    print(f"Vendored {pkg}@{version} -> {out_dir}")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
