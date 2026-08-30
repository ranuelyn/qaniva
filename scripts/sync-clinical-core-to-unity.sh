#!/usr/bin/env bash
# Build the pure-C# clinical engine and vendor it into the Unity project as a DLL.
# Also copies demo case JSON into Unity Resources. Both destinations are git-ignored.
#
# After running this, set the scripting define QANIVA_HAS_CLINICAL_CORE in
# Unity (Project Settings -> Player) to switch from StubClinicalRuntime to the
# real ClinicalRuntime. See unity/QanivaSimulation/README.md.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CORE_PROJ="$ROOT/clinical-core/Qaniva.Clinical.Core/Qaniva.Clinical.Core.csproj"
PLUGIN_DIR="$ROOT/unity/QanivaSimulation/Assets/Qaniva/Plugins/ClinicalCore"
CASES_DIR="$ROOT/unity/QanivaSimulation/Assets/Qaniva/Resources/Qaniva/Cases"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "error: dotnet SDK not found on PATH" >&2
  exit 1
fi

echo "==> building Qaniva.Clinical.Core (Release, netstandard2.1)"
dotnet publish "$CORE_PROJ" -c Release -o "$PLUGIN_DIR" --nologo

echo "==> pruning non-DLL publish output"
find "$PLUGIN_DIR" -type f ! -name '*.dll' -delete
# Newtonsoft is provided by the com.unity.nuget.newtonsoft-json package; drop the
# copy here to avoid a duplicate-assembly error. Keep System.Text.Json.dll and its
# System.* dependencies — Unity's .NET Standard 2.1 profile does not include them.
# If Unity reports an assembly conflict, disable "Assembly Version Validation" in
# Player settings or manage System.Text.Json via NuGetForUnity instead.
rm -f "$PLUGIN_DIR/Newtonsoft.Json.dll" 2>/dev/null || true

echo "==> copying demo case JSON into Unity Resources"
mkdir -p "$CASES_DIR"
while IFS= read -r -d '' case_file; do
  id="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1]))['id'])" "$case_file")"
  cp "$case_file" "$CASES_DIR/$id.json"
  echo "    $id.json"
done < <(find "$ROOT/packages/case-schema/fixtures" -name 'case.json' -print0)

echo "==> done. Set QANIVA_HAS_CLINICAL_CORE in Unity Player settings to enable the real engine."
