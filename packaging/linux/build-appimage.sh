#!/usr/bin/env bash
# Builds a self-contained Linux AppImage.
#
#   ./packaging/linux/build-appimage.sh [output-dir]
#
# appimagetool is downloaded on demand if it is not already on PATH, which is
# how CI gets it too -- it is not packaged by most distributions.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${1:-${REPO_ROOT}/artifacts}"
RID="linux-x64"
WORK="$(mktemp -d)"
trap 'rm -rf "${WORK}"' EXIT

echo "==> Publishing self-contained ${RID}"
dotnet publish "${REPO_ROOT}/src/CpuEmulator.App" \
    --configuration Release \
    --runtime "${RID}" \
    --self-contained true \
    --output "${WORK}/publish"

echo "==> Staging AppDir"
APPDIR="${WORK}/AppDir"
mkdir -p "${APPDIR}/usr/bin"
cp -r "${WORK}/publish/." "${APPDIR}/usr/bin/"
cp "${REPO_ROOT}/packaging/linux/AppRun" "${APPDIR}/AppRun"
chmod +x "${APPDIR}/AppRun"
cp "${REPO_ROOT}/packaging/linux/cpuemulator.desktop" "${APPDIR}/cpuemulator.desktop"
cp "${REPO_ROOT}/packaging/assets/cpuemulator.png" "${APPDIR}/cpuemulator.png"

if command -v appimagetool >/dev/null 2>&1; then
    APPIMAGETOOL="appimagetool"
else
    echo "==> Fetching appimagetool"
    APPIMAGETOOL="${WORK}/appimagetool"
    curl -fsSL -o "${APPIMAGETOOL}" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "${APPIMAGETOOL}"
fi

echo "==> Building AppImage"
mkdir -p "${OUT_DIR}"
# --appimage-extract-and-run avoids needing FUSE on the *build* machine;
# running the resulting AppImage still wants FUSE on the target.
ARCH=x86_64 "${APPIMAGETOOL}" --appimage-extract-and-run \
    "${APPDIR}" "${OUT_DIR}/CpuEmulator-x86_64.AppImage"

echo "==> Done: ${OUT_DIR}/CpuEmulator-x86_64.AppImage"
du -h "${OUT_DIR}/CpuEmulator-x86_64.AppImage"
