#!/usr/bin/env bash
# Install Unity headlessly and run the whole milestone pipeline in one go.
#
# Written for a Claude Code cloud session whose environment has been given
# network access to Unity's domains. It also works on any Ubuntu box.
#
#   ./Tools/bootstrap_unity.sh                 # install, then run everything
#   ./Tools/bootstrap_unity.sh --install-only
#   ./Tools/bootstrap_unity.sh --skip-bake     # much faster, worse lighting
#
# Licensing
#   Unity will not run unlicensed. This script never asks for, stores or
#   transmits credentials. Supply your own licence one of two ways:
#
#     UNITY_LICENSE   the full contents of a Unity_lic.ulf file
#     UNITY_ULF_PATH  a path to one
#
#   To produce a .ulf: on a machine where you have Unity, activate it, then copy
#   ~/.local/share/unity3d/Unity/Unity_lic.ulf (Linux) or the equivalent under
#   %ProgramData%\Unity on Windows.
#
#   A .ulf is tied to your Unity account. Cloud environment variables are
#   readable by anyone who can use that environment and there is no secrets
#   store, so prefer UNITY_ULF_PATH pointing at a file you place in the session.
#
# Rendering
#   Cloud sessions have no GPU. This script installs Mesa's lavapipe so Unity
#   has a software Vulkan device, which is what lets the screenshot pass run at
#   all. Expect it to be slow, and expect the profiling numbers to be worthless
#   - a software rasteriser says nothing about whether the build hits 60 fps.

set -uo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.0.58f1}"
UNITY_CHANGESET="${UNITY_CHANGESET:-}"
INSTALL_ROOT="${UNITY_INSTALL_ROOT:-/opt/unity}"
EDITOR="$INSTALL_ROOT/Editor/Unity"
LOG_DIR="$PROJECT_ROOT/Builds/bootstrap"

INSTALL_ONLY=0
SKIP_BAKE=0
for arg in "$@"; do
  case "$arg" in
    --install-only) INSTALL_ONLY=1 ;;
    --skip-bake)    SKIP_BAKE=1 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

mkdir -p "$LOG_DIR"

say()  { printf '\n\033[1m== %s\033[0m\n' "$*"; }
warn() { printf '\033[33m!! %s\033[0m\n' "$*" >&2; }
die()  { printf '\033[31mxx %s\033[0m\n' "$*" >&2; exit 1; }

# ---------------------------------------------------------------------------
# Preflight
# ---------------------------------------------------------------------------

say "Preflight"

AVAIL_GB=$(df -BG --output=avail "$PROJECT_ROOT" | tail -1 | tr -dc '0-9')
echo "disk available: ${AVAIL_GB} GB"
if [[ "${AVAIL_GB:-0}" -lt 20 ]]; then
  warn "Under 20 GB free. The editor is ~8 GB, the Windows module ~2 GB, and"
  warn "the HDRP Library folder several more. This will probably run out."
fi

for host in download.unity3d.com; do
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 15 "https://$host" || echo 000)
  echo "$host -> HTTP $code"
  if [[ "$code" == "000" || "$code" == "403" ]]; then
    cat >&2 <<'EOF'

Unity's download servers are not reachable from this session.

In a Claude Code cloud session this is the environment's network policy, and
you can change it yourself:

  1. Go to claude.ai/code
  2. In the row above the message box, click the cloud icon showing the
     environment's name
  3. Hover the environment, click the settings (gear) icon
  4. Set Network access to Custom, tick "Also include default list of common
     package managers", and add:

         download.unity3d.com
         *.unity3d.com
         *.unity.com
         unity.com

     (Or set it to Full, which allows any domain.)
  5. Save. Changing allowed hosts rebuilds the environment cache, so the next
     session starts fresh.

Then start a new session and run this script again.
EOF
    exit 1
  fi
done

# ---------------------------------------------------------------------------
# Licence
# ---------------------------------------------------------------------------

ULF=""
if [[ -n "${UNITY_ULF_PATH:-}" && -f "${UNITY_ULF_PATH}" ]]; then
  ULF="$UNITY_ULF_PATH"
elif [[ -n "${UNITY_LICENSE:-}" ]]; then
  ULF="$LOG_DIR/Unity_lic.ulf"
  printf '%s' "$UNITY_LICENSE" > "$ULF"
elif [[ -f "$HOME/.local/share/unity3d/Unity/Unity_lic.ulf" ]]; then
  ULF="$HOME/.local/share/unity3d/Unity/Unity_lic.ulf"
fi

if [[ -z "$ULF" ]]; then
  die "No Unity licence found. Set UNITY_ULF_PATH or UNITY_LICENSE (see the header of this script). This script will not ask for your Unity credentials."
fi
echo "licence file: $ULF"

# ---------------------------------------------------------------------------
# Host dependencies, including a software Vulkan device
# ---------------------------------------------------------------------------

say "Installing host dependencies"

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >"$LOG_DIR/apt.log" 2>&1 || warn "apt update reported errors"
apt-get install -y -qq --no-install-recommends \
  xvfb libvulkan1 mesa-vulkan-drivers vulkan-tools \
  libgl1 libglu1-mesa libgconf-2-4 libxss1 libnss3 libasound2t64 \
  libgtk-3-0 libxtst6 xz-utils ca-certificates \
  >>"$LOG_DIR/apt.log" 2>&1 || warn "some packages failed; see $LOG_DIR/apt.log"

# lavapipe is Mesa's CPU Vulkan implementation. Without pinning the ICD, Unity
# finds no device at all and every rendering call fails.
LVP_ICD=$(ls /usr/share/vulkan/icd.d/lvp_icd*.json 2>/dev/null | head -1 || true)
if [[ -n "$LVP_ICD" ]]; then
  export VK_ICD_FILENAMES="$LVP_ICD"
  echo "software Vulkan ICD: $LVP_ICD"
else
  warn "lavapipe not found. The screenshot pass will almost certainly fail."
fi
export LIBGL_ALWAYS_SOFTWARE=1
export GALLIUM_DRIVER=llvmpipe

# ---------------------------------------------------------------------------
# Unity
# ---------------------------------------------------------------------------

if [[ -x "$EDITOR" ]]; then
  say "Unity already installed at $EDITOR"
else
  say "Resolving Unity $UNITY_VERSION"

  if [[ -z "$UNITY_CHANGESET" ]]; then
    # The changeset is part of every download URL. It is in ProjectVersion.txt
    # when the project was made by a real editor; ours was written by hand, so
    # fall back to Unity's release feed.
    FROM_PROJECT=$(sed -n 's/.*(\([0-9a-f]\{12\}\)).*/\1/p' \
      "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt" 2>/dev/null | head -1)
    if [[ -n "$FROM_PROJECT" ]]; then
      UNITY_CHANGESET="$FROM_PROJECT"
      echo "changeset from ProjectVersion.txt: $UNITY_CHANGESET"
    else
      UNITY_CHANGESET=$(curl -s --max-time 30 \
        "https://services.api.unity.com/unity/editor/release/v1/releases?limit=200&stream=LTS" \
        | grep -o "\"version\":\"$UNITY_VERSION\"[^}]*\"shortRevision\":\"[0-9a-f]*\"" \
        | grep -o '"shortRevision":"[0-9a-f]*"' | head -1 | cut -d'"' -f4 || true)
      [[ -n "$UNITY_CHANGESET" ]] && echo "changeset from release feed: $UNITY_CHANGESET"
    fi
  fi

  [[ -n "$UNITY_CHANGESET" ]] || die "Could not determine the changeset for $UNITY_VERSION. Set UNITY_CHANGESET explicitly - it is the 12-character hash in the download URL on Unity's release page."

  BASE="https://download.unity3d.com/download_unity/$UNITY_CHANGESET"
  mkdir -p "$INSTALL_ROOT"

  say "Downloading the editor (about 5 GB)"
  curl -fL --retry 4 --retry-delay 5 -o /tmp/unity-editor.tar.xz \
    "$BASE/LinuxEditorInstaller/Unity.tar.xz" \
    || die "Editor download failed. Check the version and changeset."
  tar -xf /tmp/unity-editor.tar.xz -C "$INSTALL_ROOT" || die "Editor extract failed"
  rm -f /tmp/unity-editor.tar.xz

  say "Downloading Windows Build Support (Mono)"
  MODULE="UnitySetup-Windows-Mono-Support-for-Editor-$UNITY_VERSION.tar.xz"
  if curl -fL --retry 4 --retry-delay 5 -o /tmp/unity-win.tar.xz \
       "$BASE/LinuxEditorTargetInstaller/$MODULE"; then
    tar -xf /tmp/unity-win.tar.xz -C "$INSTALL_ROOT" || warn "Windows module extract failed"
    rm -f /tmp/unity-win.tar.xz
  else
    warn "Windows module download failed. Everything except the build will still run."
  fi

  [[ -x "$EDITOR" ]] || die "Editor binary not found at $EDITOR after install"
fi

"$EDITOR" -version 2>/dev/null | head -1 || true

# ---------------------------------------------------------------------------
# Activate
# ---------------------------------------------------------------------------

say "Activating the licence"
mkdir -p "$HOME/.local/share/unity3d/Unity"
cp -f "$ULF" "$HOME/.local/share/unity3d/Unity/Unity_lic.ulf" 2>/dev/null || true
# This command exits non-zero even when it succeeds, so its status is ignored
# and the real check is whether the next editor run gets past licensing.
"$EDITOR" -batchmode -nographics -quit -logFile "$LOG_DIR/activate.log" \
  -manualLicenseFile "$ULF" >/dev/null 2>&1 || true
grep -qi "license" "$LOG_DIR/activate.log" 2>/dev/null && echo "see $LOG_DIR/activate.log"

if [[ $INSTALL_ONLY -eq 1 ]]; then
  say "Install complete. Re-run without --install-only to build the scene."
  exit 0
fi

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

# Everything that renders runs under a virtual display with a software Vulkan
# device. Everything that does not gets -nographics, which is far faster.
run_headless() {
  local name="$1"; shift
  say "$name"
  "$EDITOR" -batchmode -nographics -projectPath "$PROJECT_ROOT" \
    -logFile "$LOG_DIR/$name.log" "$@"
  local status=$?
  [[ $status -eq 0 ]] || warn "$name exited $status (see $LOG_DIR/$name.log)"
  return $status
}

run_rendering() {
  local name="$1"; shift
  say "$name (software rendering, slow)"
  xvfb-run -a -s "-screen 0 1920x1080x24" \
    "$EDITOR" -batchmode -force-vulkan -projectPath "$PROJECT_ROOT" \
    -logFile "$LOG_DIR/$name.log" "$@"
  local status=$?
  [[ $status -eq 0 ]] || warn "$name exited $status (see $LOG_DIR/$name.log)"
  return $status
}

FAILED=()

run_headless "01-configure" -quit \
  -executeMethod Freedome.EditorTools.Build.ProjectConfigurator.Configure \
  || FAILED+=("configure")

run_headless "02-generate" -quit \
  -executeMethod Freedome.EditorTools.Generation.ShedSceneGenerator.GenerateEverythingBatch \
  || FAILED+=("generate")

if [[ $SKIP_BAKE -eq 0 ]]; then
  run_headless "03-bake" -quit \
    -executeMethod Freedome.EditorTools.Build.WindowsBuild.BakeOnly \
    || FAILED+=("bake")
fi

run_headless "04-tests-editmode" -runTests -testPlatform EditMode \
  -testResults "$LOG_DIR/EditMode.xml" || FAILED+=("EditMode tests")

run_rendering "05-tests-playmode" -runTests -testPlatform PlayMode \
  -testResults "$LOG_DIR/PlayMode.xml" || FAILED+=("PlayMode tests")

run_rendering "06-screenshots" -quit \
  -executeMethod Freedome.EditorTools.Validation.ScreenshotCapture.CaptureAllFromBatch \
  || FAILED+=("screenshots")

run_headless "07-build" -quit \
  -executeMethod Freedome.EditorTools.Build.WindowsBuild.PerformBuild \
  || FAILED+=("Windows build")

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

say "Result"
echo "logs:        $LOG_DIR"
echo "screenshots: $(ls "$PROJECT_ROOT"/docs/screenshots/*.png 2>/dev/null | wc -l) captured"
echo "build:       $([[ -f "$PROJECT_ROOT/Builds/Windows/ShedRoomDemo/ShedRoomDemo.exe" ]] \
                     && echo present || echo missing)"

if [[ ${#FAILED[@]} -gt 0 ]]; then
  warn "Steps that did not succeed: ${FAILED[*]}"
  warn "Read the matching log before believing any of the outputs."
  exit 1
fi

say "All steps completed"
