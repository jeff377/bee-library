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

# `.claude/` 依 rules/public-docs.md 是「給 agent 的工程規範，非產品文件」，與 docs/plans/
# 同屬公開文件不得指向的對象。這一道與上面五道方向不同：那些防「指向階段性文件」，
# 這道防「指向外部讀者根本開不了的檔案」——`~/.claude/...`（使用者家目錄）尤其如此。
#
# 實例：docs/api-method-reference 曾連向 .claude/rules/security.md 與一支 skill，
# adr-006 更指向 `~/.claude/rules/code-style.md`。這五處活到 2026-09-04 的盤點才被抓到，
# 因為前五道只掃 docs/plans/，不涵蓋這個方向。
section 6 "markdown — 指向 .claude/（預期無輸出）"
# CLAUDE.md 本身就是 agent 規範、不是公開文件（見 rules/public-docs.md 的分類表），
# 它指向 .claude/rules/ 完全合法，排除之。
grep -rn --include="*.md" -e "\.claude/" "${MD_ROOTS[@]}" 2>/dev/null \
  | grep -v "/CLAUDE\.md:" | exclude_md

echo
