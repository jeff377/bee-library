# 計畫：個人文件移出 docs/，改放獨立的個人目錄

**狀態：📝 擬定中（2026-09-11）**

## 背景

`docs/` 底下混著兩種 gitignored 的內容：

- `docs/blogs/`：部落格草稿與 2026 iThome 鐵人賽寫作工作檔，本身是獨立的 private repo（`jeff377/bee-blogs`）。
- `docs/internal/`：含未修安全項的 review 類 plan，只是 gitignored，沒有任何 repo 備份。

本 plan 原是 [plan-docs-multilingual-layout.md](archive/plan-docs-multilingual-layout.md) 的階段 4（2026-09-11 拆出）。
那份 plan 把公開文件搬進 `docs/<lang>/` 之後，部落格草稿裡指向舊路徑的部分也要跟著修正，一併在這裡處理。

## 時程

**延後**：等 2026 iThome 鐵人賽 Day 30 發佈完成後再做（2026-09-11 決定）。
連載期間每天都照固定路徑發佈，中途搬移會打斷流程。

## 分界原則（2026-09-11 定案）

**個人文件移到個人目錄，非個人文件都留在 `docs/`。** `docs/repo-ops/` 等入版控的維運文件因此不動。

## 做法（2026-09-11 定案方向）

1. 在 bee-library 內另建一個個人文件目錄，把 `docs/blogs/` 與 `docs/internal/` 移過去。
2. `.gitignore` 只排除這一個個人目錄，拿掉 `docs/blogs/`、`docs/internal/` 兩條。
3. 個人目錄本身用 private repo 存放。`docs/internal/` 目前沒有任何 repo 備份，搬過去之後才有。

這樣 `docs/` 底下就不再有 gitignored 的內容，公開文件規則裡「`docs/` 下哪些不算」的例外也能跟著縮小。

## 待確認（執行時決定）

| 項目 | 說明 |
|------|------|
| 個人目錄的名稱與位置 | 未定。內容不只部落格，名稱不要綁定 blog |
| private repo 是否沿用 `bee-blogs` | 同上，repo 名稱可能要改 |
| `docs/plans/` 是否也移入（未來考慮） | 要先解決兩件事：plan 目前入版控，`.claude/`、`docs/repo-ops/` 與檢查腳本都有指向它的引用，移進 gitignored 目錄後這些會變成 clone 的人打不開的路徑（`check-md-links.sh` 也會報）；另外交接流程是「commit 交接文件，新 session 在 worktree 讀」，worktree 讀不到 gitignored 的個人目錄，這個流程要跟著改 |

## 受影響位置（2026-09-11 盤點，執行時一律重掃）

bee-library 內：

- `.gitignore` 的 `docs/blogs/`、`docs/internal/` 兩條，以及封存 plan 那段註解（它提到 `docs/internal/`；「ADR 與 README 會連結到封存 plan」一句也已與公開文件規則矛盾，順手改掉）。
- [check-md-links.sh](../../check-md-links.sh) 檔頭的範圍說明，與 [check-public-docs.sh](../../check-public-docs.sh) 的 `exclude_md` 對兩者的排除。
- [public-docs.md](../../.claude/rules/public-docs.md)「哪些不是」表。
- `.claude/CLAUDE.md`「含未修安全弱點清單的 review 類 plan 改放 `docs/internal/`」那條例外。
- `.claude/skills/bee-framework-review/SKILL.md` 的公開文件範圍說明。

使用者層 `~/.claude/skills/`（不入本 repo 版控）：

- `ithome-publish`、`medium-publish`：工作檔路徑，以及 `git -C docs/blogs` 那段檢查。
- `hackmd-blog`：文章存放位置。

## 個人文件內指向 docs 的路徑

以下修改都在個人 repo 內另外 commit，不會出現在 bee-library 的 diff 裡。
「新路徑」指公開文件搬進 `docs/<lang>/` 之後的位置（`docs/zh-TW/xxx.md`、`docs/en/xxx.md`）。

GitHub 對搬走的檔案不會轉址，`blob/main/docs/x.md` 這種外部連結在搬移後會 404。

- **鐵人賽文章沒有連到 docs**（2026-09-11 查 `docs/blogs/ithome-2026-ironman/`，含英文譯稿）：
  文章中指向 repo 的網址都是 `src/` 下的原始碼或 repo 首頁，不受影響。
- **鐵人賽的寫作工作檔**有幾處以裸路徑提到 docs 文件，依性質分兩類：
  - **現行指示**（`writing-rules.md`、`plan.md`）：寫作 session 會照著讀，改成新路徑。
  - **紀錄**（`decision-log.md`、`day-notes.md`、`northwind-case-assessment.md`）：記的是當時讀了什麼，不改。
- **根目錄的部落格草稿**（`docs/blogs/blog-*.md`，不屬鐵人賽）：有三處連到 `blob/main/docs/*.zh-TW.md`，
  分別是 dependency-map、architecture-overview、api-bo-contract-design，改成新路徑。
  若這幾篇已發佈到 HackMD，發佈版要手動改。
