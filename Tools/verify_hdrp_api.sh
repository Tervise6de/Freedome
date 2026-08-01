#!/usr/bin/env bash
#
# Checks every HDRP symbol this project uses against HDRP's published source.
#
# The compile check (Tools/compile_check.sh) deliberately cannot do this: its
# HDRP stubs assert what the API ought to look like, so they agree with the
# project by construction. This closes that gap by reading the actual package
# source from Unity's public mirror.
#
#   ./Tools/verify_hdrp_api.sh
#
# Caveat that matters when you read the output: the public mirror's tags stop
# at HDRP 10, so this checks against the tip of `master`. At the time of
# writing that is 17.6.0 while the project pins 17.0.4 - same major, so the
# surface is nearly identical, but the signal is asymmetric:
#
#   symbol ABSENT  in master -> almost certainly broken in 17.0.4 too. Act on it.
#   symbol PRESENT in master -> good, but it could have been added after 17.0.4.
#
# It found one hard compile error: AmbientOcclusion was renamed to
# ScreenSpaceAmbientOcclusion in 2022.2, and the shell left behind under the
# old name is not a VolumeComponent.

set -euo pipefail

REPO="https://github.com/Unity-Technologies/Graphics"
WORK="${TMPDIR:-/tmp}/hdrp-verify"
HD="$WORK/Graphics/Packages/com.unity.render-pipelines.high-definition/Runtime"

if [ ! -d "$HD" ]; then
    echo "Fetching HDRP source (blobless sparse clone, ~55 MB)..."
    mkdir -p "$WORK"
    git clone --filter=blob:none --sparse --depth 1 "$REPO" "$WORK/Graphics"
    git -C "$WORK/Graphics" sparse-checkout set \
        Packages/com.unity.render-pipelines.high-definition/Runtime \
        Packages/com.unity.render-pipelines.core/Runtime
fi

echo "HDRP version in the checkout: $(grep -m1 '"version"' \
    "$WORK/Graphics/Packages/com.unity.render-pipelines.high-definition/package.json")"
echo

fail=0

# Searches every partial of a type, since HDRP splits most of these across
# a main file and a .Migration.cs.
chk() {
    local type="$1" member="$2"
    local files
    files=$(grep -rlE "(class|struct|enum) $type\b" "$HD" --include=*.cs 2>/dev/null || true)

    if [ -z "$files" ]; then
        printf '  %-28s %-28s TYPE NOT IN HDRP\n' "$type" "$member"
        return
    fi
    if echo "$files" | xargs grep -hE "\b$member\b" 2>/dev/null | grep -qv '^\s*//'; then
        printf '  %-28s %-28s ok\n' "$type" "$member"
    else
        printf '  %-28s %-28s ** MISSING **\n' "$type" "$member"
        fail=1
    fi
}

echo "Volume overrides"
chk VisualEnvironment skyType
chk VisualEnvironment cloudType
chk PhysicallyBasedSky type
chk PhysicallyBasedSky groundTint
chk Fog enabled
for m in mode meteringMode limitMin limitMax \
         adaptationSpeedDarkToLight adaptationSpeedLightToDark histogramPercentages; do
    chk Exposure "$m"
done
for m in enable length opacity; do chk ContactShadows "$m"; done
for m in intensity radius directLightingStrength; do chk ScreenSpaceAmbientOcclusion "$m"; done
chk ScreenSpaceReflection enabled
chk MicroShadowing enable
chk MicroShadowing opacity
chk Tonemapping mode
for m in postExposure contrast saturation; do chk ColorAdjustments "$m"; done
chk Bloom intensity
chk Bloom scatter
chk MotionBlur intensity

echo
echo "Components"
for m in SetIntensity angularDiameter EnableShadows shadowUpdateMode \
         affectsVolumetric SetShadowResolution shapeRadius innerSpotPercent; do
    chk HDAdditionalLightData "$m"
done
chk HDMaterial ValidateMaterial

echo
echo "Enum members"
chk SkyType PhysicallyBased
chk PhysicallyBasedSkyModel EarthSimple
chk ExposureMode AutomaticHistogram
chk MeteringMode CenterWeighted
chk TonemappingMode Neutral
chk ShadowUpdateMode OnEnable

echo
echo "Obsolete, but only a warning - the project still uses these knowingly:"
echo "  HDAdditionalLightData.SetIntensity   deprecated #from(2023.3)"
echo "  HDAdditionalLightData.shapeRadius    deprecated, (UnityUpgradable)"
echo "  HDAdditionalLightData.innerSpotPercent  deprecated #from(6000.3), so NOT"
echo "                                          yet deprecated in the pinned 17.0.4"
echo
echo "Note: LightUnit is UnityEngine.Rendering.LightUnit - it comes from the"
echo "engine, not from either SRP package. LightingBuilder imports"
echo "UnityEngine.Rendering, so it resolves."

exit $fail
