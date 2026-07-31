#!/usr/bin/env bash
# Builds the Windows 64-bit player headlessly.
#
#   ./Tools/build_windows.sh [--regenerate] [--bake] [--development]
#
# Requires a Unity 6 install with the "Windows Build Support" module. Set
# UNITY_PATH to point at the editor binary if it is not in one of the usual
# Hub locations.
#
# The build lands in Builds/Windows/ShedRoomDemo/ and the result is appended to
# docs/BUILD_REPORT.md by the build script itself.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$PROJECT_ROOT/Builds/Windows"
LOG_FILE="$LOG_DIR/build.log"

EXTRA_ARGS=()
for arg in "$@"; do
  case "$arg" in
    --regenerate)  EXTRA_ARGS+=("-regenerate") ;;
    --bake)        EXTRA_ARGS+=("-bakeLighting") ;;
    --development) EXTRA_ARGS+=("-development") ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

find_unity() {
  if [[ -n "${UNITY_PATH:-}" ]]; then
    echo "$UNITY_PATH"
    return
  fi

  local candidates=()
  # Unity Hub default locations, newest version first.
  while IFS= read -r line; do candidates+=("$line"); done < <(
    ls -d "$HOME"/Unity/Hub/Editor/*/Editor/Unity 2>/dev/null | sort -rV
    ls -d /opt/unity/editors/*/Editor/Unity 2>/dev/null | sort -rV
    ls -d "/Applications/Unity/Hub/Editor/"*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -rV
    ls -d "/c/Program Files/Unity/Hub/Editor/"*/Editor/Unity.exe 2>/dev/null | sort -rV
  )

  for candidate in "${candidates[@]}"; do
    if [[ -x "$candidate" ]]; then
      echo "$candidate"
      return
    fi
  done
}

UNITY_BIN="$(find_unity || true)"

if [[ -z "$UNITY_BIN" ]]; then
  cat >&2 <<'EOF'
Could not find a Unity editor.

Install Unity 6 with the "Windows Build Support (Mono)" module through Unity Hub,
then either add it to a standard Hub location or set UNITY_PATH, for example:

  UNITY_PATH="$HOME/Unity/Hub/Editor/6000.0.58f1/Editor/Unity" ./Tools/build_windows.sh
EOF
  exit 1
fi

echo "Unity:   $UNITY_BIN"
echo "Project: $PROJECT_ROOT"
echo "Options: ${EXTRA_ARGS[*]:-none}"

mkdir -p "$LOG_DIR"

set +e
"$UNITY_BIN" \
  -quit \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_ROOT" \
  -executeMethod Freedome.EditorTools.Build.WindowsBuild.PerformBuild \
  -logFile "$LOG_FILE" \
  "${EXTRA_ARGS[@]}"
STATUS=$?
set -e

if [[ $STATUS -ne 0 ]]; then
  echo "Build failed with exit code $STATUS. Last 60 lines of $LOG_FILE:" >&2
  tail -n 60 "$LOG_FILE" >&2 || true
  exit $STATUS
fi

echo
echo "Build succeeded: Builds/Windows/ShedRoomDemo/ShedRoomDemo.exe"
echo "Full log: $LOG_FILE"
