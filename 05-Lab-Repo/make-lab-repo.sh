#!/usr/bin/env bash
# Builds the Day 2 training repository from AssetDesk-Lab/.
#   main             = AssetDesk-Lab/, one commit, tagged start
#   reference-build  = start + the finished app as AssetDesk/ (the fallback)
# Usage, from the package root:  bash 05-Lab-Repo/make-lab-repo.sh [target-dir]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
src="$here/AssetDesk-Lab"
ref="$here/../01-Instructor/Lab-Solutions/reference-build/AssetDesk"
target="${1:-./assetdesk-lab-out}"

[ -f "$src/SPEC.md" ] || { echo "Missing $src/SPEC.md"; exit 1; }
[ -f "$ref/AssetDesk.csproj" ] || { echo "Missing reference build at $ref"; exit 1; }
[ -e "$target" ] && { echo "$target already exists. Delete it or pass another path."; exit 1; }

# Copy a folder, dotfiles included, without build output, databases, MCP leftovers or Finder litter.
copy() { mkdir -p "$2"; (cd "$1" && tar cf - --exclude bin --exclude obj --exclude '*.db' \
  --exclude '*.db-shm' --exclude '*.db-wal' --exclude .DS_Store --exclude .mcp.json \
  --exclude .playwright-mcp .) | (cd "$2" && tar xf -); }

copy "$src" "$target"
cd "$target"
git init -q -b main
git add -A
git commit -q -m "Initial lab state"
git tag start

git switch -q -c reference-build
copy "$ref" AssetDesk
git add -A
git commit -q -m "Reference build (fallback)"
git switch -q main

echo "Created $(pwd)"
git log --oneline --all --decorate
echo
echo "Push it to an empty GitHub repository:"
echo "  cd $(pwd)"
echo "  git remote add origin <url>"
echo "  git push -u origin main reference-build --tags"
