#!/usr/bin/env bash
#
# Type-checks the whole project and runs the part of the test suite that does
# not need Unity.
#
# This exists because the project has never been opened in Unity and probably
# will not be for a while. It is not a substitute for a real Unity compile -
# see Tools/CompileCheck/README.md for exactly what it does and does not prove -
# but it catches everything that is wrong with the C# on its own terms, which
# turned out to be a lot.
#
#   ./Tools/compile_check.sh          # build everything, then run the tests
#   ./Tools/compile_check.sh build    # build only
#   ./Tools/compile_check.sh test     # test only
#
# Needs the .NET SDK 8 (apt install dotnet-sdk-8.0). No Unity, no GPU, no
# network beyond the first NuGet restore.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CHECK="$ROOT/Tools/CompileCheck"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet not found. Install the .NET SDK 8:" >&2
    echo "  sudo apt-get install -y dotnet-sdk-8.0" >&2
    exit 1
fi

mode="${1:-all}"

# Mirrors the four asmdefs. Runtime is built without the UnityEditor stubs on
# purpose, so editor APIs leaking into runtime code fail here.
PROJECTS=(
    "Freedome.Runtime"
    "Freedome.Editor"
    "Freedome.Tests.EditMode"
    "Freedome.Tests.PlayMode"
)

build() {
    local failed=0
    for p in "${PROJECTS[@]}"; do
        printf '%-26s ' "$p"
        if dotnet build "$CHECK/Projects/$p.csproj" -v q --nologo >/tmp/cc.$$ 2>&1; then
            echo "ok"
        else
            echo "FAILED"
            sed "s|$ROOT/||" /tmp/cc.$$ | grep -E 'error' | sort -u || true
            failed=1
        fi
    done
    rm -f /tmp/cc.$$
    return $failed
}

test_run() {
    dotnet test "$CHECK/Projects/Freedome.Tests.Run.csproj" -v q --nologo
}

case "$mode" in
    build) build ;;
    test)  test_run ;;
    all)   build && test_run ;;
    *)     echo "usage: $0 [build|test|all]" >&2; exit 2 ;;
esac
