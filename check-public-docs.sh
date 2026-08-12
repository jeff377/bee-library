#!/usr/bin/env bash
# 公開文件不得引用 docs/plans/ —— 落地檢查（規範見 .claude/rules/public-docs.md）
#
# 五道檢查涵蓋三種引用型式（路徑 / 點名檔名 / 純文字）× 兩種範圍（markdown / 原始碼與建置檔）。
# 每一道都是補出來的，對應過一批長期漏網；**不要自行縮減範圍或副檔名**。
#
# 預期輸出：(1) 只剩 docs/README.md / docs/README.zh-TW.md 對 plans/ 資料夾的性質說明；
#           (2)(4)(5) 完全無輸出；(3) 有已知誤報，須逐筆判讀（見規範文件的誤報表）。
set -uo pipefail
cd "$(dirname "$0")"

MD_ROOTS=(docs/ README.md README.zh-TW.md CHANGELOG.md CHANGELOG.zh-TW.md src/ samples/ apps/ tools/)
SRC_ROOTS=(src/ samples/ apps/ tools/)
SRC_EXT=(--include="*.cs" --include="*.axaml" --include="*.razor"
         --include="*.xml" --include="*.csproj" --include="*.props" --include="*.targets")

# docs/repo-ops 是維運文件、不是公開文件，引用 plan 合法，故排除
exclude_md() {
  grep -v "^docs/plans/" | grep -v "^docs/internal/" | grep -v "^docs/blogs/" | grep -v "^docs/repo-ops/"
}
exclude_build() { grep -v "/obj/" | grep -v "/bin/"; }

section() { printf '\n=== (%s) %s ===\n' "$1" "$2"; }

section 1 "markdown — 路徑 / 連結型引用"
grep -rn --include="*.md" -e "plans/" -e "](plan-" "${MD_ROOTS[@]}" 2>/dev/null | exclude_md

section 2 "markdown — 點名 plan 檔名（預期無輸出）"
grep -rnE --include="*.md" "plan-[a-z0-9]+(-[a-z0-9]+)+" "${MD_ROOTS[@]}" 2>/dev/null | exclude_md

section 3 "markdown — 純文字提及（有已知誤報，逐筆判讀）"
grep -rnE --include="*.md" "見 plan|本 plan|plan (的|內|各)|(the|migration|integration) plan" \
  "${MD_ROOTS[@]}" 2>/dev/null | exclude_md

section 4 "原始碼與建置檔 — 路徑型引用（預期無輸出）"
grep -rn "docs/plans" "${SRC_ROOTS[@]}" "${SRC_EXT[@]}" 2>/dev/null | exclude_build

section 5 "原始碼與建置檔 — 點名 plan 檔名（預期無輸出）"
grep -rnE "plan-[a-z0-9]+(-[a-z0-9]+)+" "${SRC_ROOTS[@]}" "${SRC_EXT[@]}" 2>/dev/null | exclude_build

echo
