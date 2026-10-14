#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_executable="${UNITY_EDITOR_PATH:-}"
if [[ -z "$unity_executable" || ! -x "$unity_executable" ]]; then
  echo 'Set UNITY_EDITOR_PATH to the licensed Unity 6000.3.26f1 executable with Android Build Support installed.' >&2
  exit 2
fi
mkdir -p "$project_root/Builds"
"$unity_executable" -batchmode -nographics -projectPath "$project_root" -runTests -testPlatform EditMode -testResults "$project_root/Builds/test-results.xml" -logFile "$project_root/Builds/tests.log"
# Unity may exit 0 for failed tests on some editor versions; parse the result.
python3 - "$project_root/Builds/test-results.xml" <<'PY'
import sys,xml.etree.ElementTree as ET
root=ET.parse(sys.argv[1]).getroot()
if root.attrib.get('result') not in ('Passed','Success') or int(root.attrib.get('failed','0')):
    raise SystemExit('Unity tests failed; APK build stopped.')
PY
"$unity_executable" -batchmode -nographics -quit -projectPath "$project_root" -buildTarget Android -executeMethod HabitGarden.Editor.ProjectSetup.BuildApk -logFile "$project_root/Builds/build.log"
test -s "$project_root/Builds/HabitGarden-test.apk"
echo "$project_root/Builds/HabitGarden-test.apk"
