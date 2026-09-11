#!/usr/bin/env bash
# markdown 相對連結檢查：連結指向的檔案或資料夾必須存在。
#
# 為什麼要有：編譯器不看文件、測試不跑它，文件搬移或改名後連結集體失效，沒有任何機制會發現。
#
# 範圍：git 追蹤中、以及未追蹤但未被忽略的 .md。gitignored 的 docs/blogs、docs/internal、
# bin、obj 因此自然排除。另外排除 docs/plans/archive/：封存 plan 是凍結的歷史紀錄，
# 它指向當時存在、後來被移走的檔案是正常的。active 的 docs/plans/*.md 要掃，那些還會有人照著做。
#
# 抓的型式：inline 連結與圖片的 `](target)`。fenced code block 與 inline code 內的不算，
# 規則文件裡示範「錯誤寫法」的樣本就放在那裡。外部 scheme（http:、mailto: 等）與純錨點略過。
# fence 的開合依 CommonMark 判定：收尾 fence 不帶 info string，且長度不短於開頭。
# 只看「行首是 ```」就切換的話，fence 內的 ```bash 會被當成收尾，之後整份檔案的判定全部錯位。
#
# 行號後綴（`](path/Foo.cs:120)`）是桌面 app 可點擊的慣例，plan 裡常用：去掉後綴再驗檔案，行號本身不驗。
#
# IMPORTANT: 存在與否以 git 的路徑清單比對，不用 `[[ -e ]]`。macOS 的檔案系統不分大小寫，
# `-e` 會放過大小寫寫錯的連結，而那種連結在 GitHub 與 Linux CI 上是 404。
# 副作用是連向 gitignored 檔案的連結也會被報出來，這是對的：外部讀者開不到那些檔案。
#
# 已知限制：
# - 不驗錨點。中文與標點的 heading slug 規則各 renderer 不同，驗了誤報會很多。
# - 不抓 HTML 的 <a href> / <img src>，也不抓 reference-style 定義（[id]: path）。
# - 反引號裸路徑（`docs/x.md`）不是連結，這支抓不到。
#
# 預期輸出：無輸出、exit 0。有死連結時逐筆列出「檔案:行號: 連結目標」並 exit 1。
set -uo pipefail
cd "$(dirname "$0")"

known_paths="$(mktemp)"
trap 'rm -f "$known_paths"' EXIT

git ls-files --cached --others --exclude-standard > "$known_paths"

git ls-files -z --cached --others --exclude-standard -- '*.md' ':!docs/plans/archive/' \
  | LC_ALL=C xargs -0 awk -v known="$known_paths" '
    function normalize(p,    n, parts, out, i, m, r) {
      n = split(p, parts, "/")
      m = 0
      for (i = 1; i <= n; i++) {
        if (parts[i] == "" || parts[i] == ".") continue
        if (parts[i] == "..") {
          if (m == 0) return ""
          m--
          continue
        }
        out[++m] = parts[i]
      }
      if (m == 0) return "."
      r = out[1]
      for (i = 2; i <= m; i++) r = r "/" out[i]
      return r
    }

    function check(raw,    t, path, dir, resolved) {
      t = raw
      sub(/^[ \t]+/, "", t)
      if (substr(t, 1, 1) == "<") {
        sub(/^</, "", t)
        sub(/>.*$/, "", t)
      } else {
        sub(/[ \t].*$/, "", t)
      }
      path = t
      sub(/#.*$/, "", path)
      sub(/\?.*$/, "", path)
      # The line suffix goes first: `Foo.cs:120` would otherwise match the scheme pattern and be skipped.
      sub(/:[0-9]+(-[0-9]+)?$/, "", path)
      if (path == "") return
      if (path ~ /^[A-Za-z][A-Za-z0-9+.-]*:/) return
      gsub(/%20/, " ", path)

      if (substr(path, 1, 1) == "/") {
        resolved = normalize(substr(path, 2))
      } else {
        dir = FILENAME
        if (!sub(/\/[^\/]*$/, "", dir)) dir = "."
        resolved = normalize(dir "/" path)
      }

      # An empty result means the link climbs above the repo root, which cannot be verified here.
      if (resolved == "" || !(resolved in exists)) {
        printf "%s:%d: %s\n", FILENAME, FNR, raw
        bad = 1
      }
    }

    function run_length(s, ch,    n) {
      n = 0
      while (substr(s, n + 1, 1) == ch) n++
      return n
    }

    BEGIN {
      exists["."] = 1
      while ((getline p < known) > 0) {
        exists[p] = 1
        while (sub(/\/[^\/]*$/, "", p)) exists[p] = 1
      }
      close(known)
    }

    FNR == 1 { fence_char = "" }

    {
      line = $0
      lead = line
      sub(/^[ \t]+/, "", lead)
      if (fence_char != "") {
        n = run_length(lead, fence_char)
        if (n >= fence_size && substr(lead, n + 1) ~ /^[ \t]*$/) fence_char = ""
        next
      }
      c = substr(lead, 1, 1)
      if ((c == "`" || c == "~") && run_length(lead, c) >= 3) {
        fence_char = c
        fence_size = run_length(lead, c)
        next
      }

      gsub(/`[^`]*`/, "", line)
      while (match(line, /\]\([^)]*\)/)) {
        check(substr(line, RSTART + 2, RLENGTH - 3))
        line = substr(line, RSTART + RLENGTH)
      }
    }

    END { exit bad }
  ' || exit 1
