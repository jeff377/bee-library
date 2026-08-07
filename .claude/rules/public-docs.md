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

改動公開文件後，或懷疑有遺漏時：

```bash
# (1) markdown — 路徑 / 連結型引用
grep -rn --include="*.md" -e "plans/" -e "](plan-" docs/ README*.md CHANGELOG*.md src/ samples/ apps/ tools/ | grep -v "^docs/plans/" | grep -v "^docs/internal/" | grep -v "^docs/blogs/" | grep -v "^docs/repo-ops/"

# (2) markdown — 點名 plan 檔名（含反引號裸名，如 `plan-audit-*` 系列）
grep -rnE --include="*.md" "plan-[a-z0-9]+(-[a-z0-9]+)+" docs/ README*.md CHANGELOG*.md src/ samples/ apps/ tools/ | grep -v "^docs/plans/" | grep -v "^docs/internal/" | grep -v "^docs/blogs/" | grep -v "^docs/repo-ops/"

# (3) markdown — 純文字指向現存 plan（「見 plan …」「plan 的 …」「本 plan」等）
grep -rnE --include="*.md" "見 plan|本 plan|plan (的|內|各)|(the|migration|integration) plan" docs/ README*.md CHANGELOG*.md src/ samples/ apps/ tools/ | grep -v "^docs/plans/" | grep -v "^docs/internal/" | grep -v "^docs/blogs/" | grep -v "^docs/repo-ops/"

# (4) 原始碼與建置檔註解 — 路徑型引用（XML doc 會進消費端 IntelliSense；
#     .xml/.csproj/.props/.targets 因 ILLink descriptor 這類 EmbeddedResource 也會進套件）
grep -rn "docs/plans" src/ samples/ apps/ tools/ --include="*.cs" --include="*.axaml" --include="*.razor" \
  --include="*.xml" --include="*.csproj" --include="*.props" --include="*.targets" | grep -v "/obj/" | grep -v "/bin/"

# (5) 原始碼與建置檔註解 — 點名 plan 檔名（如 `(see plan-numeric-core.md §1.4)`），比照 (2)
grep -rnE "plan-[a-z0-9]+(-[a-z0-9]+)+" src/ samples/ apps/ tools/ --include="*.cs" --include="*.axaml" --include="*.razor" \
  --include="*.xml" --include="*.csproj" --include="*.props" --include="*.targets" | grep -v "/obj/" | grep -v "/bin/"
```

預期輸出：(1) 只剩 `docs/README.md` / `docs/README.zh-TW.md` 對 `plans/` 資料夾的**性質說明**
（不是連結，且已標明「階段性工作文件、非參考資料」）；(2)(4)(5) 完全無輸出。

> `docs/repo-ops/` 已排除在三個 markdown 檢查之外——依上表它是**維運文件、不是公開文件**，
> 引用 plan 完全合法（`future-work.md` 指向「日後要寫的 plan」、`gotchas/` 記錄體檢方法）。
> 2026-08-07 前未排除，每次跑檢查都會有兩筆固定誤報。

**(3) 會有已知誤報，須逐筆判讀**，不可無腦清空：

| 誤報樣態 | 例子 | 為何不算違規 |
|---------|------|------------|
| `plan` 是 API / 型別名 | `docs/database-schema-upgrade*.md` 的 `Orchestrator.Plan(diff)`、`UpgradePlan`、`plan.Warnings` | 指程式碼識別符，與 `docs/plans/` 無關 |
| 指向**尚不存在**的未來規劃 | adr-012 / adr-015 / adr-023 的「另立 plan」「另開 plan」「由獨立 plan 評估」 | 語意等同「另案處理」，沒指向任何可讀文件 |

判別法：**這句話指得到一份現在讀得到的 plan 檔嗎？** 指得到才是違規。

> **(2)(3) 是 2026-07-31 才補上的**：先前只有 (1)，抓不到「點名 plan 檔名」與「純文字提及」，
> 因此 adr-014 / adr-021 / adr-026 / adr-027 / adr-028 / adr-030、`docs/changelogs/4.14.0*`、
> `samples/Web.Js.Demo/README*` 共 14 處長期漏網。
>
> **(4) 是 2026-07-28 補上的**：先前檢查只 grep `.md`，因此 `src/Bee.Db/Providers/{Oracle,MySql}`
> 底下 14 處指向 `docs/plans/` 的 XML doc 與註解長期漏網。替代寫法見下節——
> 這些位置的實質說明本來就已寫在註解裡，plan 指標拿掉即可；需要延伸閱讀的改指
> `docs/database-dialect-differences.md`。
>
> **(4)(5) 的副檔名於 2026-08-07 擴充**：先前只 grep `.cs` / `.axaml` / `.razor`，
> 抓不到建置檔與資料檔，因此三處長期漏網——`src/Bee.Definition/ILLink.Descriptors.xml`、
> `src/Bee.Definition/Bee.Definition.csproj`、`src/Directory.Build.props`。
> 其中 **descriptor 是以 `<EmbeddedResource>` 打進 NuGet 套件的實際發佈物**，
> 不是純內部檔案。處理方式同下：實質說明本來就寫在註解裡，plan 指標拿掉即可；
> `Directory.Build.props` 那處改指維運文件 `docs/repo-ops/public-api-baseline.md`。
>
> **(5) 是 2026-08-01 補上的**：(4) 只抓路徑型 `docs/plans`，抓不到只寫檔名的
> `(see plan-numeric-core.md §1.4)` 這種形式——正因如此，`Bee.Definition` 的
> `FormField` / `FormSchema` / `LayoutFieldBase` / `CompanyInfo` / `FileDefineStorage` /
> `NumberFormatApplier` 與 `Bee.Hosting`、`Bee.UI.*` 共 14 處長期漏網。處理方式同 (4)：
> 實質說明留著，plan 指標直接拿掉。
