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
#
# Skips PublishTrimmed (XmlSerializer reflection); see README for context.

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT="${SCRIPT_DIR}/Bee.DefineEditor.csproj"

if [[ ! -f "${PROJECT}" ]]; then
    echo "error: csproj not found at ${PROJECT}" >&2
    exit 1
fi

SELF_CONTAINED="false"
SINGLE_FILE="false"
RIDS=()
for arg in "$@"; do
    case "${arg}" in
        --self-contained) SELF_CONTAINED="true" ;;
        --single-file) SINGLE_FILE="true" ;;
        *) RIDS+=("${arg}") ;;
    esac
done

if [[ ${#RIDS[@]} -eq 0 ]]; then
    RIDS=(osx-arm64 osx-x64 win-x64 linux-x64)
fi

echo "Mode: $([[ "${SELF_CONTAINED}" == "true" ]] && echo "self-contained (includes .NET runtime)" || echo "framework-dependent (target machine needs .NET 10)")$([[ "${SINGLE_FILE}" == "true" ]] && echo " + single-file" || echo "")"
echo

for rid in "${RIDS[@]}"; do
    echo "=== publishing ${rid} ==="
    dotnet publish "${PROJECT}" -c Release -r "${rid}" \
        --self-contained "${SELF_CONTAINED}" \
        -p:PublishTrimmed=false \
        -p:PublishSingleFile="${SINGLE_FILE}"
done

echo
echo "Done. Outputs:"
for rid in "${RIDS[@]}"; do
    out="${SCRIPT_DIR}/bin/Release/net10.0/${rid}/publish"
    if [[ -d "${out}" ]]; then
        size=$(du -sh "${out}" | cut -f1)
        echo "  ${size}  ${out}"
    fi
done
