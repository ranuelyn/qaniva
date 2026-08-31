#!/usr/bin/env bash
# Export the Unity iOS project and build UnityFramework.framework for the RN host.
#
#   1. dotnet-publish the clinical core DLL into Unity (sync script)
#   2. Unity batchmode: enable QANIVA_HAS_CLINICAL_CORE, create the minimal scene,
#      export the iOS Xcode project to unity/QanivaSimulation/build/ios
#   3. xcodebuild the UnityFramework scheme (Release, device + simulator not unified:
#      pass SIM=1 for a simulator-arch build)
#   4. copy UnityFramework.framework into apps/mobile/unity-frameworks/ios/
#      (git-ignored; the Podfile embeds it when present)
#
# Usage:  scripts/export-unity-ios.sh [SIM=1]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/unity/QanivaSimulation"
EXPORT_DIR="$PROJECT/build/ios"
DEST="$ROOT/apps/mobile/unity-frameworks/ios"

UNITY_BIN="${UNITY_BIN:-}"
if [ -z "$UNITY_BIN" ]; then
  UNITY_BIN="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1 || true)"
fi
if [ -z "$UNITY_BIN" ] || [ ! -x "$UNITY_BIN" ]; then
  echo "error: Unity Editor not found. Install Unity 6 (6000.x) with iOS Build Support," >&2
  echo "       or set UNITY_BIN=/path/to/Unity.app/Contents/MacOS/Unity" >&2
  exit 1
fi
echo "==> Unity: $UNITY_BIN"

echo "==> syncing clinical core DLL into Unity"
"$ROOT/scripts/sync-clinical-core-to-unity.sh"

run_unity() {
  local method="$1"; shift
  "$UNITY_BIN" -batchmode -nographics -quit \
    -projectPath "$PROJECT" \
    -executeMethod "$method" \
    -logFile - "$@" 2>&1 | tail -40
}

echo "==> enabling $0 scripting define + minimal scene"
run_unity Qaniva.EditorTools.QanivaBuild.EnableClinicalCoreDefine
run_unity Qaniva.EditorTools.QanivaBuild.CreateMinimalScene

echo "==> exporting iOS Xcode project -> $EXPORT_DIR"
run_unity Qaniva.EditorTools.QanivaBuild.ExportIos -exportPath "$EXPORT_DIR"

echo "==> building UnityFramework.framework"
DERIVED="$PROJECT/build/DerivedData"
if [ "${SIM:-0}" = "1" ]; then
  xcodebuild -project "$EXPORT_DIR/Unity-iPhone.xcodeproj" \
    -scheme UnityFramework -configuration Release \
    -destination 'generic/platform=iOS Simulator' \
    -derivedDataPath "$DERIVED" \
    CODE_SIGNING_ALLOWED=NO build | tail -5
  PRODUCT="$DERIVED/Build/Products/Release-iphonesimulator/UnityFramework.framework"
else
  xcodebuild -project "$EXPORT_DIR/Unity-iPhone.xcodeproj" \
    -scheme UnityFramework -configuration Release \
    -destination 'generic/platform=iOS' \
    -derivedDataPath "$DERIVED" \
    CODE_SIGNING_ALLOWED=NO build | tail -5
  PRODUCT="$DERIVED/Build/Products/Release-iphoneos/UnityFramework.framework"
fi

if [ ! -d "$PRODUCT" ]; then
  echo "error: UnityFramework.framework not produced at $PRODUCT" >&2
  exit 1
fi

echo "==> installing framework -> $DEST"
rm -rf "$DEST/UnityFramework.framework"
mkdir -p "$DEST"
cp -R "$PRODUCT" "$DEST/"

# Unity data must live inside the framework so setDataBundleId("com.unity3d.framework") finds it.
if [ -d "$EXPORT_DIR/Data" ] && [ ! -d "$DEST/UnityFramework.framework/Data" ]; then
  cp -R "$EXPORT_DIR/Data" "$DEST/UnityFramework.framework/Data"
fi

echo "==> done. Re-run 'pod install' in apps/mobile/ios to embed the framework."
