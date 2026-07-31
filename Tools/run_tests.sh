#!/usr/bin/env bash
# Runs the EditMode and PlayMode test suites headlessly and writes NUnit XML
# results into Builds/TestResults/.
#
#   ./Tools/run_tests.sh [editmode|playmode|all]
#
# Set UNITY_PATH if the editor is not in a standard Unity Hub location.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="$PROJECT_ROOT/Builds/TestResults"
MODE="${1:-all}"

find_unity() {
  if [[ -n "${UNITY_PATH:-}" ]]; then
    echo "$UNITY_PATH"
    return
  fi
  ls -d "$HOME"/Unity/Hub/Editor/*/Editor/Unity 2>/dev/null | sort -rV | head -1
}

UNITY_BIN="$(find_unity || true)"
if [[ -z "$UNITY_BIN" || ! -x "$UNITY_BIN" ]]; then
  echo "Could not find a Unity editor. Set UNITY_PATH." >&2
  exit 1
fi

mkdir -p "$RESULTS_DIR"

run_platform() {
  local platform="$1"
  echo "Running $platform tests..."

  set +e
  "$UNITY_BIN" \
    -runTests \
    -batchmode \
    -projectPath "$PROJECT_ROOT" \
    -testPlatform "$platform" \
    -testResults "$RESULTS_DIR/$platform.xml" \
    -logFile "$RESULTS_DIR/$platform.log"
  local status=$?
  set -e

  if [[ $status -ne 0 ]]; then
    echo "$platform tests failed (exit $status). See $RESULTS_DIR/$platform.log" >&2
  else
    echo "$platform tests passed."
  fi

  return $status
}

FAILED=0
case "$MODE" in
  editmode) run_platform EditMode || FAILED=1 ;;
  playmode) run_platform PlayMode || FAILED=1 ;;
  all)
    run_platform EditMode || FAILED=1
    run_platform PlayMode || FAILED=1
    ;;
  *) echo "Usage: $0 [editmode|playmode|all]" >&2; exit 2 ;;
esac

exit $FAILED
