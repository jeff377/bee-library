#!/usr/bin/env bash
# 公開文件不得引用 docs/plans/ —— 落地檢查（規範見 .claude/rules/public-docs.md）
#
# (1)~(5) 涵蓋三種引用型式（路徑 / 點名檔名 / 純文字）× 兩種範圍（markdown / 原始碼與建置檔），
# (6) 防「指向外部讀者開不了的檔案」，(7) 防「指向已不存在的 plan」。
# 每一道都是補出來的，對應過一批長期漏網；**不要自行縮減範圍或副檔名**。
#
# 預期輸出：(1) 只剩 docs/README.md / docs/README.zh-TW.md 對 plans/ 資料夾的性質說明；
#           (2)(4)(5)(6) 完全無輸出；(3)(7) 有已知誤報，須逐筆判讀（見規範文件的誤報表）。
set -uo pipefail
cd "$(dirname "$0")"

MD_ROOTS=(docs/ README.md README.zh-TW.md CHANGELOG.md CHANGELOG.zh-TW.md src/ samples/ apps/ tools/)
SRC_ROOTS=(src/ samples/ apps/ tools/)
SRC_EXT=(--include="*.cs" --include="*.axaml" --include="*.razor"
         --include="*.js" --include="*.ts" --include="*.html"
         --include="*.xml" --include="*.csproj" --include="*.props" --include="*.targets"
         --include="*.sh" --include="*.yml" --include="*.yaml" --include="*.json")

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

# (1)~(6) 問的都是「**該不該**引用」，母體限定在公開文件。這一道問的是另一件事：
# 「引用的**對象還在不在**」——它不看引用者是誰，掃全 repo，因此涵蓋 tests/、.claude/、
# 根目錄建置檔這些前六道刻意排除的地方。
#
# 死指標與「違規引用」是兩種不同的病：前者連維運文件、測試註解、agent 設定都會犯，
# 而封存 plan 一到期被清除，前一輪還合法的引用就集體變成死指標，**沒有任何機制會發現**。
#
# 實例：2026-09-06 清除 28 份到期封存 plan 時，全 repo 掃出 8 處指向早已不存在的 plan，
# 最舊的目標消失於好幾輪之前。其中 samples/Web.Js.Demo/form-renderer.js 那筆，
# 是出貨給使用者當範例讀的程式碼指著讀者永遠打不開的檔案。
#
# docs/plans/archive/ 排除在外：封存 plan 是凍結的歷史紀錄，它提到當時存在、後來被清除的
# 兄弟 plan 完全合理，不該報。active 的 docs/plans/*.md 則**要**掃——那是還會被人照著做的文件。
section 7 "全 repo — 指向不存在的 plan（死指標；有已知誤報，逐筆判讀）"
grep -rnoE "plan-[a-z0-9]+(-[a-z0-9.]+)+\.md" . \
    --exclude-dir=.git --exclude-dir=obj --exclude-dir=bin --exclude-dir=node_modules \
    --exclude-dir=archive 2>/dev/null \
  | while IFS= read -r hit; do
      name="${hit##*:}"
      [ -e "docs/plans/$name" ] || [ -e "docs/plans/archive/$name" ] || echo "$hit"
    done | sort -u

echo
