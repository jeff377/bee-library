# 公開文件規範

**公開文件 = 寫給框架外部開發者（NuGet 套件使用者）閱讀的文件。**
判別法：這份文件的預期讀者是「用 Bee.NET 開發應用的人」還是「維護 Bee.NET 的人」？前者即公開文件。

## 哪些是公開文件

| 範圍 | 內容 |
|------|------|
| repo 根目錄 | `README.md` / `README.zh-TW.md`、`CHANGELOG.md` / `CHANGELOG.zh-TW.md` |
| `docs/` 根目錄 | 所有 `.md`（架構總覽、API 參考、資料庫指引、術語表、開發指引與限制…，含 `docs/README.md` 索引） |
| `docs/adr/` | 全部 ADR —— 長效決策紀錄，外部讀者理解「為何這樣設計」的主要來源 |
| `docs/changelogs/` | 全部逐版變更說明（根 `CHANGELOG.md` 的分版明細） |
| **所有** `README.md` / `README.zh-TW.md` | 不分位置：`src/*/`、`samples/*/`、`apps/*/`、`tools/*/` 皆是 |

## 哪些不是

| 範圍 | 性質 |
|------|------|
| `docs/plans/`（含 `archive/`） | **階段性工作文件**，隨版本演進，舊 plan 未必符合現行行為 |
| `docs/repo-ops/` | 本 repo 的維運文件（CI / 分支保護），與框架使用者無關 |
| `docs/internal/`、`docs/blogs/` | gitignored，內部設計稿 / 部落格草稿 |
| `.claude/`（`CLAUDE.md`、`rules/`、`skills/`、`commands/`） | 給 agent 的工程規範，非產品文件 |

## 硬性規則

### 1. 公開文件**不得**連結或引用 `docs/plans/` 下任何文件

plan 是**階段性**文件：實作過程中會改、完成後會封存，且**舊 plan 不保證正確**——它記錄的是當時的打算，不是現行行為。公開文件一旦指過去，外部讀者就會把過期的設計當成現況。

這條**不分連結型式**：markdown 連結、反引號裸路徑（`` `docs/plans/plan-xxx.md` ``）、純文字提及一律不行。

```markdown
<!-- ❌ 禁止：ADR 指向 plan -->
實施步驟與驗證見 [plan-messagepack-name-based-keys.md](../plans/plan-messagepack-name-based-keys.md)。

<!-- ❌ 禁止：裸路徑也算引用 -->
> 執行細節見 `docs/plans/plan-datetime-timezone.md`。

<!-- ✅ 正確：把結論寫進 ADR 本身，或指向另一份公開文件 -->
實施範圍與例外見下方「執行結果與最終範圍」。
```

### 2. 需要交代背景時的替代做法

寫公開文件時若覺得「這裡需要引用 plan」，代表**該內容還沒落到正確的位置**：

- 設計決策與理由 → 寫進對應 **ADR**（ADR 才是長效決策紀錄）
- 使用方式 / 行為說明 → 寫進 `docs/` 下對應的公開文件
- 實作位置 → 直接引用**原始碼路徑**（`src/Bee.Db/DbAccess.cs`），不繞道 plan
- 版本變更 → 寫進 `docs/changelogs/<version>.md`

歷史脈絡真的只在 plan 裡而不值得升格 → **不引用**。需要時由維護者自行查 `docs/plans/archive/` 或 git history。

### 3. 非公開文件之間可自由互連

`docs/plans/` 內部（plan ↔ plan、plan → ADR / 原始碼 / 公開文件）、`.claude/` 內部不受此限。
限制只在「公開 → plan」這個方向。

### 4. 雙語同步

公開文件有雙語版時（`xxx.md` / `xxx.zh-TW.md`），任何修改**兩份都要改**，包含依本規範移除 plan 引用。

## 落地檢查

改動公開文件後，或懷疑有遺漏時：

```bash
grep -rn --include="*.md" -e "plans/" -e "](plan-" docs/ README*.md CHANGELOG*.md src/ samples/ apps/ tools/ | grep -v "^docs/plans/" | grep -v "^docs/internal/" | grep -v "^docs/blogs/"
```

預期輸出：只剩 `docs/README.md` / `docs/README.zh-TW.md` 對 `plans/` 資料夾的**性質說明**（不是連結，且已標明「階段性工作文件、非參考資料」）。
