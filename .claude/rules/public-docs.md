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
| `src/**/*.cs` 的 **XML doc（`///`）** | 隨 NuGet 套件的 `.xml` 一起發佈，直接出現在消費端 IntelliSense —— 讀者與 README 相同 |

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

改動公開文件後，或懷疑有遺漏時跑：

```bash
./check-public-docs.sh
```


預期輸出：(1) 只剩 `docs/README.md` / `docs/README.zh-TW.md` 對 `plans/` 資料夾的**性質說明**
（不是連結，且已標明「階段性工作文件、非參考資料」）；(2)(4)(5)(6) 完全無輸出。

> `docs/repo-ops/` 已排除——依上表它是**維運文件、不是公開文件**，引用 plan 完全合法。

**(3) 會有已知誤報，須逐筆判讀**，不可無腦清空：

| 誤報樣態 | 例子 | 為何不算違規 |
|---------|------|------------|
| `plan` 是 API / 型別名 | `docs/database-schema-upgrade*.md` 的 `Orchestrator.Plan(diff)`、`UpgradePlan`、`plan.Warnings` | 指程式碼識別符，與 `docs/plans/` 無關 |
| 指向**尚不存在**的未來規劃 | adr-012 / adr-015 / adr-023 的「另立 plan」「另開 plan」「由獨立 plan 評估」 | 語意等同「另案處理」，沒指向任何可讀文件 |

判別法：**這句話指得到一份現在讀得到的 plan 檔嗎？** 指得到才是違規。

### (7) 死指標：問的是另一件事

(1)~(6) 問「**該不該**引用」，母體限定公開文件。(7) 問「引用的**對象還在不在**」，
**不看引用者是誰，掃全 repo** —— 因此涵蓋 `tests/`、`.claude/`、根目錄建置檔這些前六道
刻意排除的地方。維運文件、測試註解、agent 設定引用 plan 都合法，但**指著已經不存在的
plan 一律不合法**。

這道之所以必要：封存 plan **會依保留期限被清除**，前一輪還合法的引用就集體變成死指標，
而編譯器不看散文、測試不跑它 —— 沒有任何機制會發現。2026-09-06 那次清除，全 repo 掃出
**8 處**指向早已不存在的 plan，最舊的目標消失於好幾輪之前。

**(7) 同樣有已知誤報，須逐筆判讀**：

| 誤報樣態 | 例子 | 為何不算違規 |
|---------|------|------------|
| 示範「什麼叫違規」的 ❌ 樣本 | 本檔第 3 節 code fence 內那兩個 `plan-…` 檔名 | 樣本的作用是示範型式，指不到檔案正是它要教的事 |
| 指向**還沒寫**的未來 plan | `docs/repo-ops/future-work.md` 的「啟動時第一步：寫 `plan-bee-developer-skills`」 | 那是待辦事項的產出物，不是指路 |

判別法：**這句話是要讀者去開那個檔，還是只是提到那個名字？** 要讀者去開才是死指標。

`docs/plans/archive/` 排除在 (7) 之外：封存 plan 是凍結的歷史紀錄，提到當時存在、後來被
清除的兄弟 plan 完全合理。**active 的 `docs/plans/*.md` 則會掃** —— 那是還會被人照著做的文件。

> **別自行縮減腳本的檢查範圍或副檔名** —— 每一道都對應過一批長期漏網，理由寫在腳本檔頭。
>
> 發現違規時修法一律相同：**實質說明留著，plan 指標直接拿掉**；需要延伸閱讀的改指公開文件。
