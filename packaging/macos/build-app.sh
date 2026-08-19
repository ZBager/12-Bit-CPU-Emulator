#!/usr/bin/env bash
# Builds an UNSIGNED macOS .app bundle.
#
#   ./packaging/macos/build-app.sh <osx-arm64|osx-x64> [output-dir]
#
# A .app is only a directory layout plus an Info.plist, so this runs on Linux
# too -- but a cross-built bundle is untested by definition. Prefer a macOS
# runner when it matters.
#
# The result is unsigned. macOS quarantines unsigned apps downloaded from the
# internet and reports them as "damaged", which is misleading -- see the README
# for the xattr incantation users need.
set -euo pipefail

RID="${1:?usage: build-app.sh <osx-arm64|osx-x64> [output-dir]}"
case "${RID}" in
    osx-arm64|osx-x64) ;;
    *) echo "error: runtime must be osx-arm64 or osx-x64, got '${RID}'" >&2; exit 2 ;;
esac

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${2:-${REPO_ROOT}/artifacts}"
APP="${OUT_DIR}/CpuEmulator-${RID}/CPU Emulator.app"

echo "==> Publishing self-contained ${RID}"
rm -rf "${OUT_DIR}/CpuEmulator-${RID}"
mkdir -p "${APP}/Contents/MacOS" "${APP}/Contents/Resources"

dotnet publish "${REPO_ROOT}/src/CpuEmulator.App" \
    --configuration Release \
    --runtime "${RID}" \
    --self-contained true \
    --output "${APP}/Contents/MacOS"

cp "${REPO_ROOT}/packaging/macos/Info.plist" "${APP}/Contents/Info.plist"

# iconutil only exists on macOS; the bundle is valid without an icon, so a
# Linux cross-build just skips it rather than failing.
if command -v iconutil >/dev/null 2>&1 && command -v sips >/dev/null 2>&1; then
    echo "==> Building icon"
    ICONSET="$(mktemp -d)/cpuemulator.iconset"
    mkdir -p "${ICONSET}"
    SRC="${REPO_ROOT}/packaging/assets/cpuemulator.png"
    # A complete iconset needs both the logical size and its @2x retina variant.
    # The master render is 1024px square so every one of these downscales cleanly.
    for pair in "16 32" "32 64" "128 256" "256 512" "512 1024"; do
        set -- ${pair}
        logical=$1; retina=$2
        sips -z ${logical} ${logical} "${SRC}" \
            --out "${ICONSET}/icon_${logical}x${logical}.png" >/dev/null
        sips -z ${retina} ${retina} "${SRC}" \
            --out "${ICONSET}/icon_${logical}x${logical}@2x.png" >/dev/null
    done
    iconutil -c icns "${ICONSET}" -o "${APP}/Contents/Resources/cpuemulator.icns"
else
    echo "==> Skipping icon (iconutil/sips unavailable -- not running on macOS)"
fi

chmod +x "${APP}/Contents/MacOS/CpuEmulator"

echo "==> Done: ${APP}"
du -sh "${APP}"
