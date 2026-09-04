#!/usr/bin/env bash
# XML doc 散文中的 <c>識別字</c> 不得指向已不存在的型別 —— 落地檢查
# （規範見 ~/.claude/rules/code-style.md 的「XML 文件註解」段落）
#
# 為什麼需要這支：`<see cref>` 由編譯器把關（CS1574 + TreatWarningsAsErrors → 建置失敗），
# 但**散文裡的反引號 `<c>Foo</c>` 完全不受保護**。型別改名或刪除後，`<c>` 就成了指空，
# 而 XML doc 隨 NuGet 的 .xml 發佈、直接出現在消費端 IntelliSense。
# 實際漏網：`CacheInfo.Initialize` 的 doc 指向 `CacheBootstrapper`，該型別在靜態 facade
# 拆除時就沒了，卻活到 2026-08-15 的全 repo 盤點才被抓到。
#
# 正解優先序：能改用 `<see cref>` 的就改用（讓編譯器接手），這支只守「確實只能用 `<c>`」
# 的場合 —— 外部套件型別、SQL 關鍵字、刻意指涉的已移除型別，以及**跨組件向上指涉**
# （如 Bee.ObjectCaching 提及 Bee.Hosting 的 AddBeeFramework，相依方向反了，cref 解析不到）。
#
# 預期輸出：完全無輸出。有輸出時逐筆判斷是「真的指空」還是「該進 allowlist 的新例外」。
set -uo pipefail
cd "$(dirname "$0")"

# 刻意不受檢的識別字。新增時**必須註明歸類**，否則這份清單會退化成消音器。
ALLOWLIST=(
  # --- 外部套件 / BCL 型別（不在本 solution 內宣告）---
  AsyncLocal CoCreateInstance InternalsVisibleTo ToolStripMenuItem
  FormatterNotRegisteredException TypelessFormatter
  # --- 刻意指涉的已移除型別（原文即寫 used to / which is gone / the former）---
  SafeTypelessFormatter ItemsForSerialization NumberFormatPresets
  # --- 前瞻建議中的假想型別（尚未實作，原文為 "abstract this via …"）---
  IDescriptionSyncCommandBuilder
  # --- 文件用佔位符，非真實型別名 ---
  Cxxx SaveX IXxxRepository G
  # --- SQL 關鍵字 / 資料字典物件 / 欄位名 ---
  ALL_TABLES ALL_TAB_COMMENTS ALL_COL_COMMENTS ANDEC ANSI_QUOTES
  DO_SUM QUANTITY SIZE SQL_MODE USERNAME
)

is_allowed() {
  local needle="$1"
  for item in "${ALLOWLIST[@]}"; do
    [[ "$item" == "$needle" ]] && return 0
  done
  return 1
}

# 先擋掉會讓下面整套 grep 靜默失明的東西：原始碼裡的 NUL 位元組。
#
# grep 把含 NUL 的檔案當 binary，於是它對每一道 grep 都變成空的 —— 不是報錯，是**無聲跳過**。
# 實際踩過：SnapshotLanguageService.cs 的 `$"{lang}\x00{ns}"` 把 NUL 寫成了原始位元組而非 `\0`
# 逸出序列，該檔 5,432 bytes 的原始碼對下面的比對母體貢獻 0 bytes，它自己的 <c> 也永遠不被檢查。
# 2026-09-04 的框架體檢才發現，中間沒有任何機制會出聲。
#
# 行為完全等價的寫法是 `\0`（C# 逸出序列，編譯後位元組相同），所以這條沒有正當例外。
# 判定法用 `tr -d '\000'` 後與原檔比對：shell 變數存不住 NUL，所以不能靠 grep 樣式去找它。
NUL_HITS=$(
  find src tests tools apps samples -name '*.cs' \
       -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null \
  | while IFS= read -r f; do
      tr -d '\000' < "$f" | cmp -s - "$f" || echo "$f"
    done
)
if [[ -n "$NUL_HITS" ]]; then
  echo "原始碼含 NUL 位元組（grep 會把整個檔案當 binary 而無聲跳過，請改用 \\0 逸出序列）："
  echo "$NUL_HITS" | sed 's/^/    /'
  exit 1
fi

# 比對母體：全 solution 的**非 XML doc** 行。含 tests/ 等非發佈目錄是刻意的 ——
# 這支只問「這個名字還存在嗎」，不問「它在哪一層」。
CODE=$(mktemp)
trap 'rm -f "$CODE"' EXIT
find src tests tools apps samples -name '*.cs' \
     -not -path '*/bin/*' -not -path '*/obj/*' -print0 2>/dev/null \
  | xargs -0 grep -hv '^[[:space:]]*///' > "$CODE"

status=0
while IFS= read -r id; do
  is_allowed "$id" && continue
  grep -qw -- "$id" "$CODE" && continue
  status=1
  echo "指向不存在的型別：<c>${id}</c>"
  grep -rn --include='*.cs' --exclude-dir=bin --exclude-dir=obj "<c>${id}</c>" src \
    | sed 's/^/    /'
done < <(
  find src -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' -print0 \
    | xargs -0 grep -ho '<c>[A-Z][A-Za-z0-9_]*</c>' \
    | sed 's|<c>\(.*\)</c>|\1|' | sort -u
)

[[ $status -eq 0 ]] && echo "OK：src/ 的 XML doc 散文無指空的 <c> 識別字。"
exit $status
