#!/usr/bin/env bash
# Bumps the Oppo/UnfoldedCircle.OppoBluRay log categories to Trace for preview builds (a tag containing
# "-pre" after the version, e.g. v1.2.3-pre.1), so preview testers get more diagnostic detail without
# editing config by hand. No-op for a stable tag.
set -euo pipefail

appsettings_json="${1:-src/UnfoldedCircle.OppoBluRay/appsettings.json}"
tag="${DRIVER_TAG:-}"

if [[ "$tag" != *-pre* ]]; then
  echo "Not a preview tag ($tag), leaving log level as-is."
  exit 0
fi

echo "Preview tag detected ($tag), bumping log level to Trace for preview build."
jq '.Logging.LogLevel.Oppo = "Trace" | .Logging.LogLevel["UnfoldedCircle.OppoBluRay"] = "Trace"' \
  "$appsettings_json" > "$appsettings_json.tmp" && mv "$appsettings_json.tmp" "$appsettings_json"
