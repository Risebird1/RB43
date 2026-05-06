#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="${ROOT_DIR}/dist"
ZIP_NAME="chrome-proctor-extension-v2-$(date +%Y%m%d-%H%M%S).zip"
ZIP_PATH="${DIST_DIR}/${ZIP_NAME}"

mkdir -p "${DIST_DIR}"

cd "${ROOT_DIR}"
rm -f "${ZIP_PATH}"

zip -r "${ZIP_PATH}" \
  manifest.json \
  service-worker.js \
  content-monitor.js \
  popup.html \
  popup.js \
  popup.css \
  rules \
  README.md \
  ARCHITECTURE.md \
  BLUEPRINT.md \
  DISTRIBUTION.md \
  DELTA_CHANGELOG.md

echo "Built extension package: ${ZIP_PATH}"
