#!/usr/bin/env bash
# Local (in-network) deploy to Mezga, run from WSL. Alternative to .github/workflows/ahk-web-deploy.yaml
# for when you're already inside the BME network and don't want to pay the VPN round-trip.
#
# Usage (from a WSL shell, or via scripts\deploy-local.cmd on Windows):
#   wsl bash scripts/deploy-local.sh [--force-full]
#
# --force-full ignores the deployment manifest stored on the share and re-copies everything. Use this
# after any manual/out-of-band change on the server, since the manifest only reflects "what a deploy
# script last wrote", not "what is actually on disk right now".
#
# The manifest this script reads/writes (.deploy-manifest.sha256 at the share root) is byte-compatible
# with the one ahk-web-deploy.yaml produces: same sha256sum invocation, same relative-path convention.
# Either deploy path can pick up where the other left off.
set -euo pipefail

# ---- Config ----
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# Relative to ahk-backend/ (the publish step below cd's there first) — that CWD is what lets the .NET
# SDK's own global.json discovery (a directory walk-up) find ahk-backend/global.json and pin the exact
# SDK, matching ahk-web-deploy.yaml's working-directory: ahk-backend. Without it, dotnet.exe (Windows
# interop) would use whatever SDK happens to be on PATH, which can silently drift from what CI uses.
BACKEND_PROJECT="Ahk.Web.Server/Ahk.Web.Server.csproj"
FRONTEND_DIR="ahk-frontend"
OFFLINE_PAGE="ahk-backend/Ahk.Web.Server/app_offline.htm"
PUBLISH_DIR="publish-local"
MOUNT_POINT="/mnt/ahk-deploy-local"
MANIFEST_NAME=".deploy-manifest.sha256"
SECRETS_FILE="deploy.local.json"

FORCE_FULL=false
if [ "${1:-}" = "--force-full" ]; then
  FORCE_FULL=true
fi

cd "$REPO_ROOT"

# ---- Preflight ----
missing=()
command -v dotnet.exe >/dev/null 2>&1 || missing+=("dotnet.exe (the .NET SDK, reached via Windows-PATH interop)")
command -v npm >/dev/null 2>&1 || missing+=("npm (Node, reached via Windows-PATH interop)")
command -v jq >/dev/null 2>&1 || missing+=("jq (install with: sudo apt-get install -y jq)")
command -v rsync >/dev/null 2>&1 || missing+=("rsync")
command -v curl >/dev/null 2>&1 || missing+=("curl")

if [ "${#missing[@]}" -gt 0 ]; then
  echo "Missing required tool(s):"
  for m in "${missing[@]}"; do echo "  - $m"; done
  exit 1
fi

if [ ! -f "$SECRETS_FILE" ]; then
  echo "Missing $SECRETS_FILE. Copy deploy.local.json.example to $SECRETS_FILE and fill in your values."
  exit 1
fi

DEPLOYMENT_PATH=$(jq -r '.deploymentPath' "$SECRETS_FILE")
DATABASE_CONNECTIONSTRING=$(jq -r '.databaseConnectionString' "$SECRETS_FILE")
OIDC_CLIENTSECRET=$(jq -r '.oidcClientSecret' "$SECRETS_FILE")

if [ -z "$DEPLOYMENT_PATH" ] || [ "$DEPLOYMENT_PATH" = "null" ]; then
  echo "deploy.local.json is missing deploymentPath."
  exit 1
fi

# ---- Git state visibility (informational only — a local deploy of WIP is a legitimate use case) ----
echo "Deploying branch $(git rev-parse --abbrev-ref HEAD), commit $(git rev-parse --short HEAD)"
if [ -n "$(git status --porcelain)" ]; then
  echo "NOTE: working tree has uncommitted changes — deploying them along with everything else."
fi

# ---- Build ----
echo "== Publishing backend (self-contained win-x64 via Mezga profile) =="
rm -rf "$PUBLISH_DIR"
(cd ahk-backend && dotnet.exe publish "$BACKEND_PROJECT" -p:PublishProfile=Mezga -o "../$PUBLISH_DIR")

echo "== Building frontend (production) =="
(cd "$FRONTEND_DIR" && npm ci && npx ng build --configuration production)

echo "== Copying SPA into backend wwwroot =="
mkdir -p "$PUBLISH_DIR/wwwroot"
cp -r "$FRONTEND_DIR/dist/ahk-frontend/browser/." "$PUBLISH_DIR/wwwroot/"

# ---- Inject production config (never committed) ----
echo "== Injecting connection string and OIDC client secret =="
jq --arg cs "$DATABASE_CONNECTIONSTRING" --arg secret "$OIDC_CLIENTSECRET" \
  '.ConnectionStrings.Default = $cs | .Authentication.Oidc.ClientSecret = $secret' \
  "$PUBLISH_DIR/appsettings.json" > "$PUBLISH_DIR/appsettings.json.tmp"
mv "$PUBLISH_DIR/appsettings.json.tmp" "$PUBLISH_DIR/appsettings.json"

# Last content-mutating step before the diff — identical invocation to the GitHub workflow, so the
# manifest either produces is byte-for-byte comparable.
echo "== Computing local deployment manifest =="
(cd "$PUBLISH_DIR" && find . -type f -printf '%P\0' | sort -z | xargs -0 sha256sum) > new.manifest

# ---- Mount the share, using the current Windows session's own access ----
# drvfs proxies through the Windows kernel's SMB redirector, so whatever credentials Windows already
# has cached for this UNC path (from Explorer or a prior `net use`) are reused automatically — no
# separate Linux-side username/password, unlike the GitHub runner's mount -t cifs.
sudo mkdir -p "$MOUNT_POINT"
if ! mountpoint -q "$MOUNT_POINT"; then
  echo "== Mounting $DEPLOYMENT_PATH =="
  if ! sudo mount -t drvfs "$DEPLOYMENT_PATH" "$MOUNT_POINT" -o "uid=$(id -u),gid=$(id -g)"; then
    echo "::error::Could not mount $DEPLOYMENT_PATH."
    echo "Windows needs to already have access to this share. From Windows, either:"
    echo "  - browse to $DEPLOYMENT_PATH in Explorer once and accept 'remember my credentials', or"
    echo "  - run: net use $DEPLOYMENT_PATH"
    echo "then re-run this script."
    exit 1
  fi
fi

cleanup() {
  sudo umount "$MOUNT_POINT" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# ---- Fetch previous deployment manifest ----
if [ "$FORCE_FULL" = "true" ]; then
  echo "--force-full requested — treating the stored manifest as empty (full re-copy)."
  : > old.manifest
elif [ -f "$MOUNT_POINT/$MANIFEST_NAME" ]; then
  cp "$MOUNT_POINT/$MANIFEST_NAME" old.manifest
else
  echo "No manifest found on the share yet — treating this as a first-time full deploy."
  : > old.manifest
fi

# ---- Diff manifests (pure local text processing — no network cost) ----
sort old.manifest -o old.sorted
sort new.manifest -o new.sorted

# Changed or added: whole lines (hash+path) present in the new build but not the old one.
comm -13 old.sorted new.sorted | cut -c 67- > changed.list

# Removed: paths that existed before and are entirely absent from the new build — correctly prunes
# Angular's abandoned content-hashed chunk files, unlike a size comparison.
cut -c 67- old.manifest | sort -u > old.paths
cut -c 67- new.manifest | sort -u > new.paths
comm -23 old.paths new.paths > removed.list

changed=$(wc -l < changed.list)
removed=$(wc -l < removed.list)
echo "Changed/added: $changed, removed: $removed"

if [ "$changed" -eq 0 ] && [ "$removed" -eq 0 ]; then
  echo "Nothing changed — skipping the deploy."
else
  echo "== Stopping site (app_offline.htm) =="
  cp "$OFFLINE_PAGE" "$MOUNT_POINT/app_offline.htm"
  sleep 5

  if [ -s changed.list ]; then
    echo "== Copying changed files =="
    ok=false
    for i in $(seq 1 10); do
      if rsync -R --files-from=changed.list --ignore-times --whole-file --inplace \
          --no-perms --no-owner --no-group --no-times "$PUBLISH_DIR/" "$MOUNT_POINT/"; then
        ok=true
        break
      fi
      echo "rsync attempt $i failed (likely a locked file); retrying..."
      sleep 2
    done
    if [ "$ok" != "true" ]; then
      echo "::error::Copying changed files failed after retries"
      exit 1
    fi
  fi

  if [ -s removed.list ]; then
    echo "== Removing stale files =="
    while IFS= read -r path; do
      rm -f "$MOUNT_POINT/$path"
    done < removed.list
  fi

  echo "== Waking site (removing app_offline.htm) =="
  rm -f "$MOUNT_POINT/app_offline.htm"

  echo "== Warming up and verifying =="
  curl --fail --show-error --retry 5 --retry-delay 3 https://ahk.aut.bme.hu/

  # Last content-mutating step, only reached after everything above succeeded — a failed run leaves the
  # old manifest in place, so the next run still sees these files as "changed" and retries them.
  echo "== Publishing new deployment manifest =="
  cp new.manifest "$MOUNT_POINT/$MANIFEST_NAME"
fi

echo "Done."
