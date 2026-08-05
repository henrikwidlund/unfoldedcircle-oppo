#!/usr/bin/env bash
# Stamps driver.json with the version/date derived from the current tag.
set -euo pipefail

driver_json="${1:-src/UnfoldedCircle.OppoBluRay/driver.json}"

tag=$(git describe --tags --abbrev=0)
date=$(date -u +"%Y-%m-%d")
version="${tag//v}"

jq --arg version "$version" --arg date "$date" \
  '.version = $version | .release_date = $date' \
  "$driver_json" > "$driver_json.tmp" && mv "$driver_json.tmp" "$driver_json"

if [ -n "${GITHUB_ENV:-}" ]; then
  {
    echo "DRIVER_TAG=$tag"
    echo "DRIVER_VERSION=$version"
  } >> "$GITHUB_ENV"
fi
