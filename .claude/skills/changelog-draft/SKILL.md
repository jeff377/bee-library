---
name: changelog-draft
description: 整理 CHANGELOG（自上一版 tag 至 HEAD），產出雙語草稿供使用者 review。當使用者提到「整理 CHANGELOG」、「準備發版」、「draft changelog」、「發 v4.x.x」、「下一版要發了」、「整理 release notes」之類情境時使用。
---

# CHANGELOG 整理

整理自上一版 tag 至 HEAD 的所有 commits，產出符合 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/) 格式的雙語 CHANGELOG 條目（中／英），由使用者 review 後再走 `~/.claude/rules/releasing.md` 流程發版。

## 設計前提

- 本框架仍處 pre-stable 演進階段（v4.x），對外公開 API 表面尚無外部消費者；minor 版本允許包含 API 搬遷與命名空間調整
- CHANGELOG 從 **v4.3.0 開始記錄**，**不回補歷史**（使用者已明確表態）
- 雙語維護：英文版主檔 `CHANGELOG.md`、繁中版 `CHANGELOG.zh-TW.md`，**必須同步更新**
- 不自動 commit／push／改 `Directory.Build.props`／打 tag —— 這些屬 `releasing.md` 流程，等使用者 review 完才動手
- 文件分工：`docs/` 是現行框架對齊版（forward-looking）、`docs/adr/` 是設計決策史、`CHANGELOG.md` 是版本差紀錄；三者互補，CHANGELOG 條目對應重大行為改動時應**連結對應 ADR**

## 執行流程

### Step 1：定位範圍與 sanity check

並行執行：

```bash
git tag --sort=-creatordate | head -3              # 取最近三個 tag，確認上版
git log <prev_tag>..HEAD --pretty=format:'%h %s'   # 取 subject 概覽
git log <prev_tag>..HEAD --pretty=format:'%h%n%s%n%b%n---END---'  # 取完整訊息（含 body）
git status                                         # 確認沒有 uncommitted changes
```

若有 uncommitted changes，先告知使用者並停下來確認（避免漏掉未進版的變更）。
若 `<prev_tag>..HEAD` 為空（沒有新 commit），明確告訴使用者「無變更，不需發版」並結束。

### Step 2：依 Conventional Commits 分類

依 commit subject 前綴判斷：

| 前綴 | 分類 | 是否進 user-facing CHANGELOG |
|------|------|------------------------------|
| `feat:` / `feat(scope):` | 新增 | ✅ 必入 |
| `feat!:` 或 commit body 含 `BREAKING CHANGE:` | 變更（breaking） | ✅ 必入，且需寫升級指引 |
| `fix:` | 修正 | ✅ 必入 |
| `perf:` | 變更（效能） | ✅ 必入 |
| `refactor:` | 視情況 | ⚠️ 改到公開 API / 命名空間 / 預設行為才入；純內部重構 omit |
| `docs:` / `test:` / `chore:` / `build:` / `ci:` / `style:` | 多半 omit | ⚠️ 除非影響使用者（如：發版前升級套件依賴版本、改變預設設定值） |

**核心判斷原則**：這個變更會讓使用者的 csproj / using / 程式碼 / XML 設定檔需要改嗎？
- 會 → 列入並寫清楚要改什麼
- 不會 → omit（但在最終報告中列出，讓使用者確認沒誤判）

**模糊情境的處理**：
- 同一個 PR 的多個 commits（feat + 後續 fix）→ 合併成一條 user-facing 描述
- `refactor` 改了 internal 但有公開 API surface 變化 → 列入
- 多 commit 串成同一個邏輯改動 → 看 commit message 群組脈絡判斷

### Step 3：版號建議

| Commits 內容 | 建議升版 |
|--------------|---------|
| 任一 breaking change | major（e.g. 5.0.0） |
| 至少一個 feat（無 breaking） | minor（e.g. 4.4.0） |
| 僅 fix / perf / 內部變動 | patch（e.g. 4.3.1） |

> **Pre-stable 例外**：本框架明文允許在 minor 中包含 API 搬遷（見 v4.3.0 changelog 開頭說明）。即使依嚴格 SemVer 為 major，pre-stable 政策下可建議 minor，**附理由讓使用者拍板**。

### Step 4：連結 ADR

對重大行為改動（breaking / 新模組 / 架構搬遷），掃 `docs/adr/` 目錄找對應條目：

```bash
ls /Users/jeff/Desktop/repos/bee-library/docs/adr/ | grep -i <關鍵字>
```

例如 v4.3.0 的 `AddBeeFramework` 搬至 `Bee.Hosting` → 對應 [ADR-011 DI replaces service locator](docs/adr/adr-011-di-replaces-service-locator.md)。

若找不到對應 ADR 但屬重大改動，標註「⚠️ 建議補 ADR」給使用者，不自動建立。

### Step 5：產雙語 draft 並寫入檔案

採「**兩層：主檔精簡條目 + per-version 明細檔**」結構（2026-06-24 確立）。每次發版**同時產兩種輸出**：

**(A) 主 CHANGELOG（精簡，雙語兩檔）** —— `CHANGELOG.md` / `CHANGELOG.zh-TW.md`：

```markdown
## [4.x.0]

> 主題引言（1–3 句）＝這版的重點摘要。承載「為什麼這版重要」與整體脈絡，
> 是讀者掃讀時唯一需要看的敘事。pre-stable 期間若版本性質特殊（如嚴格 SemVer
> 屬 major、政策下以 minor 發佈）也在此一句帶過。

📄 詳細變更與設計脈絡：[docs/changelogs/4.x.0.md](docs/changelogs/4.x.0.md)

### 新增

- `<套件>`：<一行 WHAT，含關鍵 API／namespace>。

### 變更

- `<套件>`：<一行差異>。重大設計決策連結 ADR：[ADR-011](docs/adr/adr-011-di-replaces-service-locator.md)

### 修正

- `<套件>`：<一行 bug fix>。

### 升級指引（僅 breaking 才需要）

\`\`\`diff
+ using Bee.NewNs;
- using Bee.OldNs;
\`\`\`
```

**(B) 明細檔（完整敘事，單語兩檔）** —— 每版兩個檔，與主 CHANGELOG 的雙檔結構對稱：
- `docs/changelogs/<版號>.md`（英文，如 `docs/changelogs/4.11.0.md`）
- `docs/changelogs/<版號>.zh-TW.md`（繁中，如 `docs/changelogs/4.11.0.zh-TW.md`）

- 每檔頂部加標題 + 語言切換導覽列：`[English](<版號>.md) ・ [繁體中文](<版號>.zh-TW.md) ・ [← CHANGELOG](../../CHANGELOG.md)`（繁中檔的 `← CHANGELOG` 指向 `../../CHANGELOG.zh-TW.md`），再一句說明「本檔是 CHANGELOG `[x.x.0]` 條目的詳細版」
- 內容＝**主檔精簡前的完整版**：每條 bullet 保留多句 WHY／設計權衡／受影響範圍、升級指引含標題式情境說明
- 結構與主檔逐節對齊（破壞性變更／安全性／新增／變更／修正／升級指引），讓讀者能在主檔一行條目與明細檔展開版之間對照
- **參考既有範本**：`docs/changelogs/4.11.0.md` + `docs/changelogs/4.11.0.zh-TW.md`

**精簡原則（主檔，最重要，避免條目膨脹）**：
- **每條 bullet 一行**：只寫 WHAT（改了什麼公開 API／行為）+ 受影響套件前綴。**不寫多句 WHY／設計權衡**（WHY 進明細檔 B）
- **主檔 WHY 的家**：整體脈絡 → `>` 主題引言；完整敘事 → 明細檔 `docs/changelogs/<版號>.md`（英）/ `<版號>.zh-TW.md`（繁中）；深層設計理由 → `docs/adr/`
- 主檔砍掉「This is what lets…」「matching … expectations」這類解釋性尾句（它們進明細檔）
- 主檔升級指引留可操作的 `diff`，散文說明進明細檔
- **既有版本已全數套兩層結構**（4.3.0–4.11.0 主檔皆精簡 + `docs/changelogs/<版號>.md` 明細，2026-06-24 一次補齊）；發新版時沿用此結構，不需再回頭改舊版

**雙語規則**：
- 主檔：英文版（`CHANGELOG.md`）條目用動詞原形或 `套件：` 句首；繁中版（`CHANGELOG.zh-TW.md`）逐條對齊翻譯
- 明細檔：英文檔（`<版號>.md`）與繁中檔（`<版號>.zh-TW.md`）逐節對應；`CHANGELOG.md` 連到 `<版號>.md`、`CHANGELOG.zh-TW.md` 連到 `<版號>.zh-TW.md`
- 升級指引 diff block：中英版 code 完全相同，只翻譯說明文字

**寫入位置**：
- 主檔：在兩份 CHANGELOG 中**插在最新版條目之前**（最新版在最上面），不刪除既有內容
- 明細檔：新增 `docs/changelogs/<版號>.md` + `docs/changelogs/<版號>.zh-TW.md`

### Step 6：交付 review

完成後給使用者一份簡短報告：

1. **建議版號** + 理由（依 Step 3 規則 + pre-stable 考量）
2. **user-facing 條目數量摘要**（X 新增、Y 變更、Z 修正）
3. **被 omit 的 commits 清單**（讓使用者判斷有無誤判）
4. **建議補 ADR 的項目**（若有）
5. **下一步提示**：「review 完請依 `~/.claude/rules/releasing.md` 走後續流程（更新 `Directory.Build.props`、commit、push tag）」

## 不做什麼

- ❌ **不自動 commit／push** CHANGELOG 改動
- ❌ **不改** `src/Directory.Build.props` 的 `<Version>` —— 屬 `releasing.md` 步驟
- ❌ **不打 git tag** —— 屬 `releasing.md` 步驟
- ❌ **不回補 v4.3.0 以前的版本** —— 使用者已表態現階段不需要
- ❌ **不為 omit 的 commit 編造 user-facing 描述** —— 誠實標 omit，由使用者決定
- ❌ **不自動建立新 ADR** —— 只標註建議，由使用者決定要不要補
