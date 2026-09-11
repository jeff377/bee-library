# 計畫：公開文件改為語言資料夾結構，並建立譯本同步機制

**狀態：📝 擬定中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 相對連結檢查腳本，並先清掉既有死連結 | 📝 待做 |
| 2 | 搬成 `docs/<lang>/`，改寫全 repo 的引用 | 📝 待做 |
| 3 | 譯本同步機制：譯本檔頭、檢查腳本、CI | 📝 待做 |
| 4 | 部落格草稿與鐵人賽寫作工作檔的路徑修正 | 📝 待做 |

## 背景

`docs/` 根目錄的公開文件目前以後綴區分語言：`xxx.md` 是英文、`xxx.zh-TW.md` 是繁中，全部放在同一層。
框架預計推廣到日本與中國大陸，會加入 `ja` 與 `zh-CN`。四種語言都放同一層，根目錄會很難瀏覽。

更根本的問題是**同步**。現行規則「修改必須同步兩份」沒有任何機制在執行，兩種語言時還能靠紀律，
四種語言時一定會漂掉，而且沒有人會發現。

文件網站（MkDocs 等）另案處理，不在本 plan 範圍。

## 已定案的決策

1. **語言資料夾，同檔名**：`docs/en/caching.md`、`docs/zh-TW/caching.md`；日後加 `docs/zh-CN/`、`docs/ja/`。
2. **權威來源是繁體中文（`zh-TW`）**，其他語言都是譯本。
3. **`zh-CN` 獨立翻譯，不從 `zh-TW` 轉換字形**：轉換工具換不了用語（資料庫／数据库、物件／对象）。
   語言代碼沿用「語言-地區」寫法，不混用 `zh-Hans`。
4. **範圍**：只做結構與同步機制，網站另案。
5. **不搬的部分**：
   - `docs/changelogs/` 維持後綴。讀者依版號找，同一版的各語言排在一起才是對的排序。
     （已發佈的 GitHub Release 說明連結是 `blob/<tag>/…`，釘在 tag 上，
     見 [nuget-publish.yml](../../.github/workflows/nuget-publish.yml)。所以搬不搬都不會斷，
     不搬的理由只有排序。）
   - `docs/adr/` 目前只有繁中，不動；日後要翻譯時再決定結構。
   - `src/`、`samples/`、`apps/`、`tools/` 各資料夾的 `README.md`，以及根目錄的 `README` 與 `CHANGELOG`，
     維持後綴。每個資料夾只有一份文件，沒有「同層太亂」的問題。

## 目標結構

```
docs/
├── README.md          ← 語言入口頁（GitHub 瀏覽 docs/ 時預設顯示）
├── en/
│   ├── README.md      ← 原 docs/README.md（英文索引）
│   └── caching.md …
├── zh-TW/
│   ├── README.md      ← 原 docs/README.zh-TW.md（中文索引）
│   └── caching.md …   ← 去掉 .zh-TW 後綴
├── adr/  changelogs/  ← 不動
└── plans/  repo-ops/  internal/  blogs/  ← 不動
```

## 階段 1：相對連結檢查

先做這一步的原因：階段 2 要改寫全 repo 大量連結，沒有檢查工具就無從確認改對了；而 repo 目前沒有任何 markdown 連結檢查。

- 在根目錄新增 `check-md-links.sh`，與 [check-public-docs.sh](../../check-public-docs.sh) 並列。
  - 掃描 markdown 的相對連結 `](path)` 與 `](path#anchor)`，排除 `http(s):`、`mailto:` 與純錨點。
  - 以該檔所在位置解析路徑，確認目標檔案或資料夾存在。
- **掃描範圍**：全 repo 的 `.md`。
  - 排除 `bin/`、`obj/`、`node_modules/`。
  - 排除 `docs/plans/archive/`：凍結的歷史紀錄。
  - 排除 `docs/internal/`、`docs/blogs/`：gitignored，部落格草稿在階段 4 處理。
  - **active 的 `docs/plans/*.md` 要掃**：那些還會有人照著做。
- **錨點先不驗**：中文與標點的 heading slug 規則各 renderer 不同，驗了誤報會很多。列為已知限制。
- 搬移前先跑一次，既有的死連結以獨立 commit 修掉，讓階段 2 從零開始比對。
- 反引號裸路徑（`` `docs/x.md` ``）與 XML doc 的 `<c>docs/…</c>` 不是 markdown 連結，這支腳本抓不到，由階段 2 的 grep 補上。

## 階段 2：搬移

### 步驟

1. **搬檔**，用 `git mv` 保留歷史：
   - `docs/<name>.md` → `docs/en/<name>.md`
   - `docs/<name>.zh-TW.md` → `docs/zh-TW/<name>.md`（去掉後綴）
2. **索引**：兩份索引各自搬入語言資料夾；新寫一份短的 `docs/README.md` 當語言入口頁。
3. **改寫連結**（一次性腳本放在 scratchpad，不入版控）：
   - **語言內互連**：`zh-TW/` 內的 `caching.zh-TW.md` 改成 `caching.md`；`en/` 內不變。
   - **語言切換列**：`[繁體中文](caching.zh-TW.md)` 改成 `[繁體中文](../zh-TW/caching.md)`，反向同理。
   - **指向 docs 子目錄與 repo 其他位置的連結**：深度加一層，例如 `adr/…` 改成 `../adr/…`、`../wire-contracts/` 改成 `../../wire-contracts/`。
   - **從 docs 外部指進來的連結**：見下方受影響檔案清單。
4. **測試路徑**：[BoApiSurfaceTests.cs](../../tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs) 的
   `Baseline_MatchesPublicMethodReference` 目前以 `InlineData` 傳檔名，再用 `Path.Combine(root, "docs", fileName)` 組路徑。
   改成傳語言（`"en"`、`"zh-TW"`），組成 `Path.Combine(root, "docs", lang, "api-method-reference.md")`。
   **不改這支，搬完測試就會紅。**
5. **索引頁的說明句**：兩份索引開頭寫著「英文版為主檔 `xxx.md`、繁中為 `xxx.zh-TW.md`」，
   改寫成語言資料夾的說明，並註明繁中為源、其他為譯本。兩份一起改。
6. **規則文字**：
   - [public-docs.md](../../.claude/rules/public-docs.md)：
     - 「哪些是公開文件」表中的 `docs/` 根目錄那一列，改成 `docs/<lang>/`。
     - 「落地檢查」的預期輸出路徑一併更新。
     - §4 雙語同步先改路徑，最終版在階段 3 完成時改。
   - `~/.claude/rules/code-style.md` 的「文件語言規則」（使用者層、跨 repo，**不入本 repo 版控**）：
     README 的後綴規則保留，補一句「文件集採 `docs/<lang>/` 結構者，依該 repo 的規則」。
7. **commit 分兩個、一起 push**：
   - 第一個 commit 只做 rename，確保 git 正確偵測為搬移，`git log --follow` 與 blame 才追得到。
   - 第二個 commit 改寫連結與測試。
   - 中間那個 commit 的連結是壞的、測試是紅的，所以兩個一定要一起 push。

### 受影響檔案（實作時對帳用）

**markdown 連結**（`check-md-links.sh` 會驗）：

- 搬移的文件本身，含兩份索引
- 根目錄的 `README.md` / `README.zh-TW.md` / `CHANGELOG.md` / `CHANGELOG.zh-TW.md`
- `src/*/README.md` 與 `README.zh-TW.md`
- `samples/Web.Js.Demo/README.md` / `README.zh-TW.md`
- `wire-fixtures/README.md`
- `docs/adr/*.md`、`docs/changelogs/*.md`、`docs/repo-ops/**/*.md`、active 的 `docs/plans/*.md`

**反引號裸路徑與 XML doc**（腳本抓不到，逐一改）：

- `src/Directory.Build.targets`（註解）
- `tests/Bee.Api.AspNetCore.UnitTests/ArchitectureBoundaryGateTests.cs`（XML doc 與斷言訊息）
- `tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs`（XML doc、`DisplayName`、路徑）
- `.claude/CLAUDE.md`（「先讀 `docs/README.md`」改指 `docs/zh-TW/README.md`）
- `.claude/rules/database.md`、`.claude/rules/definition.md`
- `.claude/skills/bee-add-bo-method/SKILL.md`、`.claude/skills/bee-framework-review/SKILL.md`
- `check-public-docs.sh`（檔頭預期輸出的註解）

### 驗證

- `./check-md-links.sh` 無輸出。
- `./check-public-docs.sh` 的輸出與搬移前相同，只有路徑不同。
- 以下 grep 除了本 plan 之外無輸出（`blogs`、`internal` 不入本 repo 版控，排除；`blogs` 由階段 4 處理）：
  ```bash
  grep -rnE "docs/[a-z0-9-]+(\.zh-TW)?\.md" . --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude-dir=archive --exclude-dir=node_modules --exclude-dir=blogs --exclude-dir=internal
  ```
- Release build 通過，`Bee.Business.UnitTests` 與 `Bee.Api.AspNetCore.UnitTests` 全綠。

## 階段 3：譯本同步機制

### 譯本檔頭

每份譯本的第一行：

```markdown
<!-- source: zh-TW/caching.md blob: <40 位 hex> -->
```

- 用 HTML 註解，GitHub 與 MkDocs 都不會顯示。
- **記的是源文件的 blob hash（`git hash-object docs/zh-TW/caching.md`），不是 commit hash**。
  - 同一個 commit 同時改源文件和譯本是現行常態，但 commit hash 在 commit 之前算不出來，只能事後再補一個 commit。
    blob hash 只看檔案內容，commit 前就算得出來。
  - 附帶好處：不依賴 git 歷史深度，也不受 rebase 影響。
- 要追查譯本落後了什麼：`git log --all --find-object=<blob>` 找出當時的 commit，再 diff 到現在。
- 源文件的任何改動（包括排版）都會讓譯本被判為過期。**這是刻意的**：要不要重翻由人判斷後重新蓋章，不讓腳本去猜。

### 檢查腳本 `check-docs-i18n.sh`

**腳本檔頭的設定是語言清單與政策的唯一權威來源**：

- 源語言：`zh-TW`
- 譯本語言與政策：目前只有 `en=strict`。日後加 `ja=partial`、`zh-CN=partial`，partial 語言另列必翻清單。

檢查項目：

1. **未宣告的語言**：`docs/` 下出現形如語言代碼的資料夾，但設定裡沒有，就報錯。
2. **源文件不得有檔頭**：防止把翻譯方向寫反。
3. **譯本必須有檔頭**，而且檔頭指向的源文件存在（抓孤兒譯本）。
4. **過期**：檔頭記的 blob 與源文件目前的 blob 不同。
5. **缺譯**：strict 語言要求每份源文件都有譯本；partial 語言只要求必翻清單。
6. **語言切換列**：每份文件（源文件與譯本）標題下的切換列，必須恰好列出「這份文件實際存在的其他語言版本」，
   順序依設定、以各語言的自稱標示（English／繁體中文／简体中文／日本語）。缺列、多列、指錯都算違規。
   - 現況（2026-09-11 查）：兩種語言的每份文件都在第 3 行有切換列，而且互相指對。
     但這是手工維護的結果，沒有任何機制在檢查。
   - 必須由腳本管的理由：到四種語言時，每份文件要列三個連結，**每加一種語言就要改所有語言的所有文件**。
     partial 語言沒翻的文件又不能列（會變成死連結），所以每份文件的切換列都不一樣，手改一定漂。

輔助指令：

- `--stamp <譯本路徑>`：把檔頭的 blob 更新成源文件目前的值。
  **蓋章等於宣告「已經對照源文件更新過」**，腳本不會、也無法驗證翻譯內容。
- `--fix-switch`：依設定與實際存在的譯本，重寫全部文件的語言切換列。切換列只由這個指令產生，不手改。

失敗政策：

- 第 6 項不分語言政策一律 exit 1：它的對錯是確定的，修法也只是跑一次 `--fix-switch`。
- strict 語言違反第 2~5 項 → exit 1。
- partial 語言的「過期」與「非必翻文件缺譯」→ 只列報告，exit 0。

執行位置：

- [build-ci.yml](../../.github/workflows/build-ci.yml) 早期加一個 step。只用 bash 和 git，幾秒內跑完，精簡與完整兩種模式都跑。
- `.claude/CLAUDE.md` 的常用命令列入。

### 初次蓋章

階段 3 落地時，對 `docs/en/` 全部蓋章。**這等於假設現在的中英文內容一致，本 plan 不驗證這一點**，內容校對另案處理。

### 規則更新

- `public-docs.md` §4 改寫成：
  - 源文件是 `zh-TW`。
  - 改了源文件，push 前要更新 `en` 譯本並 `--stamp`，否則 CI 會紅。
    源文件與譯本可以分成不同 commit，但要一起 push（CI 看的是 push 後的 HEAD）。
  - 語言切換列一律由 `--fix-switch` 產生，不手改。
  - 執行機制指向 `check-docs-i18n.sh`。這符合「帶絕對語氣的宣稱必須指得出執行它的機制」。
- 兩份索引頁：說明翻譯狀態的判定方式，不列清單。

### 新增語言的程序（本 plan 不執行，只先寫下）

0. **前置條件**：[plan-docs-positioning.md](plan-docs-positioning.md)（把公開文件的定位從 ERP 改成以表單為基礎的內部資訊系統）必須先完成。
   繁中是翻譯源頭，先翻再改定位，同一批修改就得在每種語言各做一次。
1. 在腳本設定加入該語言，以及 partial 的必翻清單。
2. 建立 `docs/<lang>/README.md` 索引；尚未翻譯的文件連到 `en` 版並標示。
3. **術語表**：`terminology` 目前是一張中英對照表（英文名稱／中文名稱／說明）。
   加語言時擴充術語欄，作為譯者的用語錨點。
   多語術語欄要放同一張表（單一來源），還是各語言各一份（讀者要看自己語言的說明欄），屆時再決定。
4. 跑 `--fix-switch`，重寫**所有語言、所有文件**的語言切換列（不只索引頁）。

## 階段 4：部落格草稿與鐵人賽寫作工作檔

`docs/blogs/` 在 bee-library 中是 gitignored，本身是獨立的 private 子 repo。
本階段的修改都要**在該子 repo 內另外 commit**，不會出現在 bee-library 的 diff 裡。

GitHub 對搬走的檔案不會轉址，`blob/main/docs/x.md` 這種外部連結在搬移後會 404。

- **鐵人賽文章沒有連到 docs**（2026-09-11 查 `docs/blogs/ithome-2026-ironman/`，含英文譯稿）：
  文章中指向 repo 的網址都是 `src/` 下的原始碼或 repo 首頁，不受本次搬移影響。
- **鐵人賽的寫作工作檔**有幾處以裸路徑提到 docs 文件，依性質分兩類：
  - **現行指示**（`writing-rules.md`、`plan.md`）：寫作 session 會照著讀，改成新路徑。
  - **紀錄**（`decision-log.md`、`day-notes.md`、`northwind-case-assessment.md`）：記的是當時讀了什麼，不改。
- **根目錄的部落格草稿**（`docs/blogs/blog-*.md`，不屬鐵人賽）：有三處連到 `blob/main/docs/*.zh-TW.md`，
  分別是 dependency-map、architecture-overview、api-bo-contract-design，改成新路徑。
  若這幾篇已發佈到 HackMD，發佈版要手動改。

## 待確認

| 項目 | 建議 | 理由 |
|------|------|------|
| `check-md-links.sh` 是否掛 CI | 掛 | 只用 bash，幾秒完成；連結是否正確本來就沒有其他機制在把關 |
| `check-docs-i18n.sh` 是否也掛 commit 前 hook | hook 只提示，由 CI 擋 | 直接改 main 的工作流下，CI 紅是事後才知道，hook 提示可以提早；但分兩個 commit 翻譯是合理情境，不該擋 commit |
| 舊路徑是否留轉址用的 stub 檔 | 不留 | stub 會讓「根目錄同層很亂」原樣回來，而且 stub 本身又是一份會漂的指標 |

## 風險

- 階段 1 第一次跑，可能掃出大量既有的死連結。數量多就另外分批 commit 修，不要混進搬移的 commit。
- agent 讀文件的入口會改變，`.claude/CLAUDE.md` 的「架構參考」必須同一批改，否則下一個 session 會照舊路徑去讀。
- 目前會用路徑讀 docs 檔案的程式碼，只查到 `BoApiSurfaceTests` 這一支（2026-09-11 grep）。
  階段 2 的 grep 驗證會再全 repo 掃一次，不依賴這次的結論。
