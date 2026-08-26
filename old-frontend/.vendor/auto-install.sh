#!/usr/bin/env bash
# Repeatedly runs `npm install`, and whenever it fails with a 404 on a
# specific package tarball, vendors that exact package version from jsdelivr
# into .vendor/<pkg> and adds an npm "overrides" entry pointing at it, then
# retries. This works around a flaky registry proxy in this sandbox that
# 404s on a subset of transitive dependency tarballs.
set -uo pipefail
cd "$(dirname "$0")/.."
export npm_config_cache="$(pwd)/.npm-cache"

MAX_ITER=40
for i in $(seq 1 $MAX_ITER); do
  echo "=== install attempt $i ==="
  out=$(npm install 2>&1)
  status=$?
  if [ $status -eq 0 ]; then
    echo "$out" | tail -20
    echo "INSTALL SUCCEEDED"
    exit 0
  fi

  echo "$out" | tail -15

  # Extract "404 Not Found - GET .../registry/<pkg>/-/<pkg>-<ver>.tgz"
  line=$(echo "$out" | grep -m1 "404 Not Found - GET")
  if [ -z "$line" ]; then
    echo "No recognizable 404 pattern; giving up."
    exit 1
  fi

  tarball=$(echo "$line" | grep -oE '[^/]+\.tgz')
  # tarball like: pkgname-version.tgz or @scope-pkgname-version.tgz (scoped differs)
  pkgpath=$(echo "$line" | sed -E 's#.*registry/(.*)/-/[^/]+\.tgz.*#\1#')
  version=$(echo "$tarball" | sed -E 's/.*-([0-9]+\.[0-9]+\.[0-9]+[^.]*)\.tgz/\1/')

  echo "Detected missing package: '$pkgpath' version '$version'"

  if [ -z "$pkgpath" ] || [ -z "$version" ]; then
    echo "Could not parse package/version; giving up."
    exit 1
  fi

  python3 .vendor/fetch.py "$pkgpath" "$version" || { echo "vendoring failed"; exit 1; }

  # Add/update override in package.json
  python3 - "$pkgpath" <<'PYEOF'
import json, sys
pkg = sys.argv[1]
with open("package.json") as f:
    data = json.load(f)
overrides = data.setdefault("overrides", {})
overrides[pkg] = f"file:./.vendor/{pkg}"
with open("package.json", "w") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print(f"Added override for {pkg}")
PYEOF

done

echo "Exceeded max iterations without success."
exit 1
