#!/usr/bin/env bash
# 公開文件的譯本同步檢查：docs/<語言>/ 下的譯本是否跟得上源文件。
#
# 為什麼要有：「改了源文件，譯本也要跟著改」原本沒有任何機制在執行。兩種語言時還能靠紀律，
# 語言一多一定會漂，而編譯器不看文件、測試不跑它，沒有人會發現。
#
# ---------------------------------------------------------------------------------------------
# 設定：語言清單與政策只寫在這裡。其他文件一律指路到本檔，不複寫。
# ---------------------------------------------------------------------------------------------
SOURCE_LANG="zh-TW"

# 譯本語言與政策，以空白分隔的「語言:政策」。語言切換列的順序是源語言第一，其餘依這裡的順序。
#   strict   每份源文件都要有譯本；缺譯、過期、檔頭錯誤都讓本腳本失敗。
#   partial  只要求必翻清單內的文件有譯本；過期與非必翻文件的缺譯只列報告、不失敗。
TRANSLATIONS="en:strict"

# partial 語言的必翻清單：變數名為 REQUIRED_<語言代碼，- 換成 _>，
# 值為 docs/<語言>/ 下的相對路徑，以空白分隔。例：
#   REQUIRED_ja="README.md getting-started.md"

# 語言切換列上的標示，一律用該語言的自稱。新增語言時，這裡與 TRANSLATIONS 都要加。
autonym() {
  case "$1" in
    en) echo "English" ;;
    zh-TW) echo "繁體中文" ;;
    zh-CN) echo "简体中文" ;;
    ja) echo "日本語" ;;
    *) return 1 ;;
  esac
}
# ---------------------------------------------------------------------------------------------
#
# 譯本檔頭：每份譯本的第一行
#   <!-- source: zh-TW/caching.md blob: <40 位 hex> -->
# - 記的是源文件的 blob hash（git hash-object），不是 commit hash。同一個 commit 同時改源文件
#   與譯本是常態，而 commit hash 要 commit 之後才算得出來；blob hash 只看檔案內容，commit 前就有。
#   也因此不依賴 git 歷史深度，不受 rebase 影響。
# - 追查譯本落後了什麼：git log --all --find-object=<blob> 找出當時的 commit，再 diff 到現在。
# - 源文件的任何改動（包括排版）都會讓譯本被判為過期。這是刻意的：要不要重翻由人判斷後
#   重新蓋章，不讓腳本去猜哪些改動「不重要」。
#
# 檢查項目：
#   1. docs/ 下出現形如語言代碼的資料夾，但上面的設定沒有宣告。
#   2. 源文件帶有譯本檔頭（翻譯方向寫反）。
#   3. 譯本缺少檔頭、檔頭格式錯誤、檔頭指向別的檔案，或指向的源文件不存在（孤兒譯本）。
#   4. 過期：檔頭記的 blob 與源文件目前的 blob 不同。
#   5. 缺譯。
#   6. 語言切換列：標題下第一個非空行開頭的語言連結，必須恰好列出這份文件實際存在的其他語言版本。
#      語言連結以「目標是 ../<語言代碼>/」判定，與標示文字無關，所以標示寫錯也抓得到。
#      必須由腳本管的理由：每加一種語言，所有語言的所有文件都要改；partial 語言沒翻的文件
#      又不能列（會是死連結），每份文件的切換列都不一樣，手改一定漂。
#
# 失敗政策：第 1、2、6 項一律失敗。第 3~5 項 strict 語言失敗；partial 語言的過期與
# 非必翻文件的缺譯只列「報告」、不失敗，其餘同 strict。
#
# 用法：
#   ./check-docs-i18n.sh                     檢查。無問題時無輸出、exit 0；有錯誤 exit 1。
#   ./check-docs-i18n.sh --stamp <譯本>...   把譯本檔頭更新成源文件目前的 blob（沒有檔頭就加上）。
#   ./check-docs-i18n.sh --fix-switch        依實際存在的譯本，重寫所有文件的語言切換列。
#
# IMPORTANT: 蓋章等於宣告「已經對照源文件更新過」。腳本不會、也無法驗證翻譯內容，
# 沒對照就蓋章，等於把這道檢查關掉。
#
# 範圍：git 追蹤中、以及未追蹤但未被忽略的檔案，與 check-md-links.sh 相同。
set -uo pipefail
cd "$(dirname "$0")"
export LC_ALL=C

SEP=" · "
fail=0

# NOTE: `read -d ''` instead of `$(cat <<'AWK' ...)`. The regexes below contain unbalanced
# parentheses, which bash 3.2 (the macOS default) mis-parses inside a command substitution.
read -r -d '' AWK_SWITCH <<'AWK'
function lang_link_len(s,    link, target) {
  if (!match(s, /^\[[^]]*\]\([^)]*\)/)) return 0
  link = substr(s, 1, RLENGTH)
  target = link
  sub(/^\[[^]]*\]\(/, "", target)
  sub(/\)$/, "", target)
  if (target ~ /^(\.\.\/)+[a-z][a-z](-[A-Za-z]+)?\//) return length(link)
  return 0
}

function split_switch(line,    n) {
  g_links = ""
  g_rest = line
  while ((n = lang_link_len(g_rest)) > 0) {
    g_links = (g_links == "" ? "" : g_links sep) substr(g_rest, 1, n)
    g_rest = substr(g_rest, n + 1)
    if (index(g_rest, sep) != 1) break
    g_rest = substr(g_rest, length(sep) + 1)
  }
}

BEGIN { sep = " · " }

{ lines[NR] = $0 }

END {
  title = 0
  for (i = 1; i <= NR; i++) if (substr(lines[i], 1, 2) == "# ") { title = i; break }
  sw = 0
  if (title) for (i = title + 1; i <= NR; i++) if (lines[i] != "") { sw = i; break }
  links = ""
  rest = ""
  if (sw) { split_switch(lines[sw]); links = g_links; rest = g_rest }

  if (mode == "get") { print links; exit }

  for (i = 1; i <= NR; i++) {
    if (title && i == sw && links != "") {
      out = want
      if (rest != "") out = (out == "" ? rest : out sep rest)
      if (out == "") {
        if (i < NR && lines[i + 1] == "") i++
        continue
      }
      print out
      continue
    }
    if (title && i == sw && want != "" && substr(lines[i], 1, 1) == "[") {
      print want sep lines[i]
      continue
    }
    print lines[i]
    if (title && i == title && want != "" && links == "" && !(sw && substr(lines[sw], 1, 1) == "[")) {
      print ""
      print want
      if (i < NR && lines[i + 1] != "") print ""
    }
  }
}
AWK

error() { printf '錯誤：%s\n' "$1"; fail=1; }
report() { printf '報告：%s\n' "$1"; }

langs_in_order() {
  local t
  echo "$SOURCE_LANG"
  for t in $TRANSLATIONS; do echo "${t%%:*}"; done
}

policy_of() {
  local t
  for t in $TRANSLATIONS; do
    if [ "${t%%:*}" = "$1" ]; then
      echo "${t#*:}"
      return 0
    fi
  done
  return 1
}

required_for() {
  local var
  var="REQUIRED_$(printf '%s' "$1" | tr '-' '_')"
  eval "printf '%s' \"\${$var:-}\""
}

listed() {
  git -c core.quotePath=false ls-files --cached --others --exclude-standard -- "$@"
}

# Paths relative to docs/<lang>/ of the markdown files in one language folder.
docs_of() {
  listed "docs/$1/*.md" | sed "s|^docs/$1/||" | sort -u
}

expected_switch() {
  local self="$1" rel="$2" prefix="../" rest="$2" out="" l
  while [ "${rest#*/}" != "$rest" ]; do
    prefix="../$prefix"
    rest="${rest#*/}"
  done
  for l in $(langs_in_order); do
    [ "$l" = "$self" ] && continue
    [ -f "docs/$l/$rel" ] || continue
    out="${out:+$out$SEP}[$(autonym "$l")](${prefix}${l}/${rel})"
  done
  printf '%s' "$out"
}

validate_config() {
  local l t
  for l in $(langs_in_order); do
    autonym "$l" > /dev/null || { echo "設定錯誤：語言 $l 沒有 autonym。" >&2; exit 2; }
  done
  for t in $TRANSLATIONS; do
    case "${t#*:}" in
      strict | partial) ;;
      *) echo "設定錯誤：$t 的政策只能是 strict 或 partial。" >&2; exit 2 ;;
    esac
  done
}

check_translation() {
  local lang="$1" rel="$2" first="$3" file="docs/$1/$2" policy expected_src src blob current msg
  policy=$(policy_of "$lang")
  expected_src="$SOURCE_LANG/$rel"
  case "$first" in
    "<!-- source: "*" blob: "*" -->") ;;
    *)
      error "$file：缺少譯本檔頭或格式錯誤。對照源文件翻譯完成後，以 ./check-docs-i18n.sh --stamp $file 蓋章。"
      return
      ;;
  esac
  src=${first#"<!-- source: "}
  src=${src%%" blob: "*}
  blob=${first##*" blob: "}
  blob=${blob%" -->"}
  if ! printf '%s' "$blob" | grep -Eq '^[0-9a-f]{40}$'; then
    error "$file：檔頭的 blob 不是 40 位 hex。"
    return
  fi
  if [ "$src" != "$expected_src" ]; then
    error "$file：檔頭指向 $src，應為 $expected_src。"
    return
  fi
  if [ ! -f "docs/$src" ]; then
    error "$file：源文件 docs/$src 不存在（孤兒譯本）。"
    return
  fi
  current=$(git hash-object "docs/$src")
  if [ "$blob" != "$current" ]; then
    msg="$file：過期，docs/$src 在蓋章之後改過。對照更新後以 --stamp 重新蓋章；蓋章時的版本可用 git log --all --find-object=$blob 找出。"
    if [ "$policy" = strict ]; then error "$msg"; else report "$msg"; fi
  fi
}

check() {
  local declared d l t lang policy required rel file want got first msg
  declared=" $(langs_in_order | tr '\n' ' ') "
  for d in $(listed docs | awk -F/ 'NF >= 3 { print $2 }' | sort -u); do
    printf '%s' "$d" | grep -Eq '^[a-z]{2}(-[A-Za-z]+)?$' || continue
    case "$declared" in
      *" $d "*) ;;
      *) error "docs/$d/ 看起來是語言資料夾，但 check-docs-i18n.sh 檔頭沒有宣告這個語言。" ;;
    esac
  done

  for l in $(langs_in_order); do
    for rel in $(docs_of "$l"); do
      file="docs/$l/$rel"
      want=$(expected_switch "$l" "$rel")
      got=$(awk -v mode=get "$AWK_SWITCH" "$file")
      if [ "$want" != "$got" ]; then
        error "$file：語言切換列應為「${want:-（無）}」，實際為「${got:-（無）}」。修法：./check-docs-i18n.sh --fix-switch"
      fi
      first=$(head -n 1 "$file")
      if [ "$l" = "$SOURCE_LANG" ]; then
        case "$first" in
          "<!-- source:"*) error "$file：源文件不應帶譯本檔頭（翻譯方向寫反了？）。" ;;
        esac
      else
        check_translation "$l" "$rel" "$first"
      fi
    done
  done

  for t in $TRANSLATIONS; do
    lang=${t%%:*}
    policy=${t#*:}
    required=" $(required_for "$lang") "
    for rel in $(docs_of "$SOURCE_LANG"); do
      [ -f "docs/$lang/$rel" ] && continue
      msg="docs/$lang/$rel：缺譯（源文件 docs/$SOURCE_LANG/$rel）。"
      if [ "$policy" = strict ]; then
        error "$msg"
      else
        case "$required" in
          *" $rel "*) error "$msg 這份在必翻清單內。" ;;
          *) report "$msg" ;;
        esac
      fi
    done
  done
}

stamp() {
  local path lang rel src blob tmp
  if [ "$#" -eq 0 ]; then
    echo "用法：./check-docs-i18n.sh --stamp <譯本路徑>..." >&2
    exit 2
  fi
  for path in "$@"; do
    path=${path#./}
    case "$path" in
      docs/*/*.md) ;;
      *) echo "不是 docs/<語言>/ 下的 .md：$path" >&2; exit 2 ;;
    esac
    lang=${path#docs/}
    lang=${lang%%/*}
    rel=${path#docs/"$lang"/}
    if ! policy_of "$lang" > /dev/null; then
      echo "$path：$lang 不是宣告的譯本語言，源文件不蓋章。" >&2
      exit 2
    fi
    [ -f "$path" ] || { echo "$path 不存在。" >&2; exit 2; }
    src="docs/$SOURCE_LANG/$rel"
    [ -f "$src" ] || { echo "$path：找不到源文件 $src。" >&2; exit 2; }
    blob=$(git hash-object "$src")
    tmp=$(mktemp)
    awk -v header="<!-- source: $SOURCE_LANG/$rel blob: $blob -->" \
      'NR == 1 { print header; if (index($0, "<!-- source:") == 1) next } { print }' "$path" > "$tmp"
    cat "$tmp" > "$path"
    rm -f "$tmp"
    echo "已蓋章：$path ← $src"
  done
}

fix_switch() {
  local l rel file want tmp
  for l in $(langs_in_order); do
    for rel in $(docs_of "$l"); do
      file="docs/$l/$rel"
      want=$(expected_switch "$l" "$rel")
      tmp=$(mktemp)
      awk -v mode=fix -v want="$want" "$AWK_SWITCH" "$file" > "$tmp"
      if ! cmp -s "$tmp" "$file"; then
        cat "$tmp" > "$file"
        echo "已更新語言切換列：$file"
      fi
      rm -f "$tmp"
    done
  done
}

validate_config
case "${1:-}" in
  "") check; exit "$fail" ;;
  --stamp) shift; stamp "$@" ;;
  --fix-switch) fix_switch ;;
  *) echo "用法：./check-docs-i18n.sh [--stamp <譯本路徑>... | --fix-switch]" >&2; exit 2 ;;
esac
