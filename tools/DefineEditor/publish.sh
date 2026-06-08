#!/usr/bin/env bash
# Publishes framework-dependent builds of Bee.DefineEditor for 4 RIDs.
# Target machine must have .NET 10 runtime installed; .NET 10 ASP.NET Core
# bundle is NOT required. Output:
#   tools/DefineEditor/bin/Release/net10.0/<rid>/publish/
#
# Usage:
#   ./publish.sh                   # all 4 RIDs
#   ./publish.sh osx-arm64         # one RID
#   ./publish.sh --self-contained  # bundle the runtime (much larger, no .NET dep)
#   ./publish.sh --single-file     # produce a single executable + a few native libs
#   ./publish.sh --app-bundle      # wrap macOS RIDs as Bee.DefineEditor.app (double-clickable)
#
# Skips PublishTrimmed (XmlSerializer reflection); see README for context.

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT="${SCRIPT_DIR}/Bee.DefineEditor.csproj"
REPO_ROOT="$( cd "${SCRIPT_DIR}/../.." && pwd )"

if [[ ! -f "${PROJECT}" ]]; then
    echo "error: csproj not found at ${PROJECT}" >&2
    exit 1
fi

SELF_CONTAINED="false"
SINGLE_FILE="false"
APP_BUNDLE="false"
RIDS=()
for arg in "$@"; do
    case "${arg}" in
        --self-contained) SELF_CONTAINED="true" ;;
        --single-file) SINGLE_FILE="true" ;;
        --app-bundle) APP_BUNDLE="true" ;;
        *) RIDS+=("${arg}") ;;
    esac
done

if [[ ${#RIDS[@]} -eq 0 ]]; then
    RIDS=(osx-arm64 osx-x64 win-x64 linux-x64)
fi

VERSION="$(grep -E '<Version>' "${REPO_ROOT}/src/Directory.Build.props" | head -1 | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/')"

build_app_bundle() {
    local rid="$1"
    local publish_dir="${SCRIPT_DIR}/bin/Release/net10.0/${rid}/publish"
    local app_dir="${publish_dir}/Bee.DefineEditor.app"

    echo "  -> packaging Bee.DefineEditor.app (${rid}, v${VERSION})"

    rm -rf "${app_dir}"
    mkdir -p "${app_dir}/Contents/MacOS"
    mkdir -p "${app_dir}/Contents/Resources"

    # Move payload into MacOS/. The Info.plist's CFBundleExecutable points to
    # Bee.DefineEditor, so macOS launches that binary on double-click; the
    # dylibs need to sit beside it so the dynamic loader resolves them via
    # @rpath / @loader_path relative to the executable.
    mv "${publish_dir}/Bee.DefineEditor" "${app_dir}/Contents/MacOS/"
    mv "${publish_dir}"/*.dylib "${app_dir}/Contents/MacOS/"
    # PDB / XML docs are non-essential but harmless inside the bundle; keep
    # them so managed exception stack traces still resolve file/line info.
    mv "${publish_dir}"/*.pdb "${app_dir}/Contents/MacOS/" 2>/dev/null || true
    mv "${publish_dir}"/*.xml "${app_dir}/Contents/MacOS/" 2>/dev/null || true

    # App icon — committed as Assets/AppIcon.icns; rebuilt from
    # scripts/build-icon.swift when the design changes. CFBundleIconFile in
    # Info.plist below points at "AppIcon" (extension implied by macOS).
    local icon_src="${SCRIPT_DIR}/Assets/AppIcon.icns"
    if [[ -f "${icon_src}" ]]; then
        cp "${icon_src}" "${app_dir}/Contents/Resources/AppIcon.icns"
    fi

    cat > "${app_dir}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>          <string>Bee.DefineEditor</string>
  <key>CFBundleIdentifier</key>          <string>tw.bee.defineeditor</string>
  <key>CFBundleName</key>                <string>Bee.DefineEditor</string>
  <key>CFBundleDisplayName</key>         <string>Bee.DefineEditor</string>
  <key>CFBundleVersion</key>             <string>${VERSION}</string>
  <key>CFBundleShortVersionString</key>  <string>${VERSION}</string>
  <key>CFBundlePackageType</key>         <string>APPL</string>
  <key>CFBundleSignature</key>           <string>????</string>
  <key>CFBundleIconFile</key>            <string>AppIcon</string>
  <key>LSMinimumSystemVersion</key>      <string>11.0</string>
  <key>NSHighResolutionCapable</key>     <true/>
  <key>LSApplicationCategoryType</key>   <string>public.app-category.developer-tools</string>
</dict>
</plist>
PLIST
}

echo "Mode: $([[ "${SELF_CONTAINED}" == "true" ]] && echo "self-contained (includes .NET runtime)" || echo "framework-dependent (target machine needs .NET 10)")$([[ "${SINGLE_FILE}" == "true" ]] && echo " + single-file" || echo "")$([[ "${APP_BUNDLE}" == "true" ]] && echo " + app-bundle (macOS)" || echo "")"
echo

for rid in "${RIDS[@]}"; do
    echo "=== publishing ${rid} ==="
    dotnet publish "${PROJECT}" -c Release -r "${rid}" \
        --self-contained "${SELF_CONTAINED}" \
        -p:PublishTrimmed=false \
        -p:PublishSingleFile="${SINGLE_FILE}"

    if [[ "${APP_BUNDLE}" == "true" && "${rid}" =~ ^osx- ]]; then
        build_app_bundle "${rid}"
    fi
done

echo
echo "Done. Outputs:"
for rid in "${RIDS[@]}"; do
    out="${SCRIPT_DIR}/bin/Release/net10.0/${rid}/publish"
    if [[ -d "${out}" ]]; then
        if [[ "${APP_BUNDLE}" == "true" && "${rid}" =~ ^osx- ]]; then
            app="${out}/Bee.DefineEditor.app"
            if [[ -d "${app}" ]]; then
                size=$(du -sh "${app}" | cut -f1)
                echo "  ${size}  ${app}"
                continue
            fi
        fi
        size=$(du -sh "${out}" | cut -f1)
        echo "  ${size}  ${out}"
    fi
done
