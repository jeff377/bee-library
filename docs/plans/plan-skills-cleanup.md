# 計畫：repo 層級 skill 精簡

**狀態：🚧 進行中（2026-08-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `bee-jsonrpc-backend` × `bee-app-scaffold` 的 bootstrap 複寫收斂為單一來源 | ✅ 已完成（2026-08-12） |
| 2 | `bee-serialization` 的 wire 三段改指路，不複寫 | ✅ 已完成（2026-08-12） |
| 3 | `hackmd-blog` 移出 repo，改置使用者層 | 📝 待做 |
| 4 | `bee-scaffold-from-formschema` 修檔名錯誤並收斂定位 | 📝 待做 |
| 5 | `changelog-draft` 中性化後移入 `dev-workflow` plugin | 📝 待做 |
| 6 | `releasing.md` 拆分：程序移入 plugin skill，原則與防護欄留常駐 | 📝 待做 |

## 背景

`.claude/skills/` 現有 12 支（11 支入版控 + `hackmd-blog` 已 gitignore）。逐支比對
「內容是否已有權威來源」「指向的檔案是否還在」「觸發面是否重疊」後，八支明確該留，
四支需要處理。

處理原則與 `~/.claude/rules/releasing.md` 的那條一致：**凡有單一權威來源的資訊，
一律指路、不複寫** —— 複寫的部分每次都會漂，而且沒有任何機制會發現（編譯器不看 skill、
測試不跑它、CI 不驗它）。這也是 repo 剛以十一個階段清完的模式，skill 目錄是尚未掃到的一塊。

**不動的七支**（本 plan 不涉及）：`bee-add-bo-method`、`bee-add-cache-object`、
`bee-add-form`、`bee-sample-add`、`bee-framework-review`、`demo-smoke`，
以及階段 4 之後的 `bee-scaffold-from-formschema`。

---

## 階段 1 — bootstrap 複寫收斂為單一來源

### 問題

`bee-jsonrpc-backend`（2026-08-12 由 `1d918b72` 從使用者層搬入）與 `bee-app-scaffold`
各自完整寫了同一段 host bootstrap 序列：

- `ResolveDefinePath` walk-up 找 `Define/SystemSettings.xml`
- `DbProviderRegistry.Register` + `DbDialectRegistry.Register`
- `SystemSettingsLoader.Load` → `SysInfo.Initialize` → `ApiServiceOptions.Initialize`
- `builder.Services.AddBeeFramework(...)`
- 覆寫服務必須在 `AddBeeFramework` **之後**註冊（last-wins）

此外「空 controller 繼承 `ApiServiceController`」「demo 免 `st_user` 登入三件套」
「`ProgramSettings` 綁 BO」也是兩邊各一份。

### 決定：保留兩支，不合併

兩者的讀者、BO 基底與藍本都不同，合併會生出一支雙軌的肥 skill：

| | `bee-jsonrpc-backend` | `bee-app-scaffold` |
|---|---|---|
| BO 基底 | `BusinessObject`（一般 RPC） | `FormBusinessObject`（ERP 定義驅動） |
| 藍本 | `samples/QuickStart.Server`、`samples/Bee.Samples.Shared` | `apps/Bee.Northwind/` |
| 獨有內容 | client 呼叫、`[ApiAccessControl]` 必標、`AllowedTypeNamespaces`、`X-Api-Key`、wire DTO enum 雷 | DB scoping、company context、seeder |

觸發面確實可分（「建一個 JSON-RPC server + client 往返」vs「接 DB scope / company / seeder」），
問題只在複寫。**單靠「一份寫、一份指路」即可解決，不需動觸發設計。**

### 做法

1. **權威來源定在** `.claude/skills/bee-jsonrpc-backend/references/backend-bootstrap.md`
   —— 它已是最完整的一份（含 walk-up、materialize、master key、factory 註冊的可貼用樣板）。
2. `bee-app-scaffold` 的 **Part 2（Host bootstrap）** 改為指路該檔，只保留它**獨有**的差異：
   - `Defaults.MaterializeTo(DefinePath, Filter: 只挑框架表)`
   - **不要**設 `ApiClientInfo.LocalServiceProvider`（會逼 Server 依賴 `Bee.Api.Client`，違反其硬性規則 1）
   - 覆寫 `ICompanyInfoService`（Part 3 的前置）
3. `bee-app-scaffold` 的 **Part 4（自訂 auth）** 改為指路
   `references/business-object.md` 的登入三件套，只留「`Login` 順帶蓋 company」這段差異。
4. 兩支的「不適用 / 分工表」互相補上對方，讓 `bee-jsonrpc-backend` 也標明
   「要 DB scope / company / seeder → 走 `bee-app-scaffold`」。

### 順帶處理

`bee-jsonrpc-backend` 的「次要藍本」列了兩個 repo 外專案（`bee-northwind-avalonia`、
`bee-jsonrpc-sample`）—— 在 bee-library 內驗不到、漂掉也不會有人發現。改為只列
repo 內可驗證的藍本，repo 外的降為一句不帶路徑的提及或直接移除。

### 驗收

- 兩支中 bootstrap 序列只出現一次，另一支是指路
- `.claude/skills/README.md` 的「觸發面重疊」註記依實際分工更新
- skill 內引用的所有 repo 內路徑實際存在

---

## 階段 2 — `bee-serialization` 改指路

### 問題

以下三段在 skill 內是第四份複寫：

| skill 章節 | 權威來源 |
|-----------|---------|
| wire 型別必須顯式註冊（ADR-037） | [../../src/Bee.Api.Core/CLAUDE.md](../../src/Bee.Api.Core/CLAUDE.md) |
| `object` 成員走判別式封套 | 同上 |
| 行動端 AOT 的型別形狀要件 | [../../.claude/rules/apple-mobile-trim.md](../../.claude/rules/apple-mobile-trim.md) § 序列化型別的行動端相容要件 |

`.claude/rules/serialization.md`（常駐骨幹）已對前兩者指路 `Bee.Api.Core/CLAUDE.md`，
skill 卻又抄了一份實質內容。

### 做法

保留 skill **獨有**的部分，其餘改指路：

- 保留：兩軸用途表（XML=持久化 / JSON+MessagePack=傳輸）、物件 recipe、集合基底選型、
  三棲 round-trip 測試樣板、完整 checklist
- 改指路：上表三段，各留一句結論 + 指向權威來源，刪掉細節與樣板
- 「參考檔案」「相關規範」兩節同步收斂，避免重複列同一批路徑

### 驗收

- 上表三段在 skill 內只剩結論句 + 指路
- 篇幅顯著下降（現 234 行）
- 與 `rules/serialization.md`、`Bee.Api.Core/CLAUDE.md` 無內容衝突

---

## 階段 3 — `hackmd-blog` 移出 repo

### 問題

`hackmd-blog` 已於 `.gitignore` 排除、內容與 bee-library 零關係（作者資訊 + 部落格寫作流程）。
放在 repo 目錄的唯一效果是**只有在這個 repo 才叫得到**。

### 做法

1. 將 `.claude/skills/hackmd-blog/` 搬至 `~/.claude/skills/hackmd-blog/`（使用者層，全 repo 可用）
2. 移除 `.gitignore` 第 28 行的 `.claude/skills/hackmd-blog/` 排除規則
3. 移除 `.claude/skills/README.md` 末尾「另有個人用 skill（如 `hackmd-blog`）已於 `.gitignore`
   排除」那則註記，以及「新增 skill」第 4 點的 gitignore 例外說法（若已無實例）

搬移屬使用者層動作（repo 外），repo 內只有 `.gitignore` 與 `README.md` 兩處改動。

### 驗收

- repo 內不再有 `hackmd-blog` 的任何痕跡（含 `.gitignore` 與 README）
- 在**其他 repo** 的 session 內可正常觸發該 skill

---

## 階段 4 — `bee-scaffold-from-formschema` 修錯與收斂定位

### 問題 A：檔名寫錯（實質錯誤，照抄會開不到檔）

skill 的「樣板對照」寫：

```
tests/Define/TableSchema/company/ft_employee.TableSchema.xml
```

實際檔名是 **`st_employee.TableSchema.xml`**。這剛好也是
[`rules/database.md`](../../.claude/rules/database.md) 講的例證：`st_` 前綴**不代表**在 common，
`st_employee` 是框架所有但落在 company scope。

### 問題 B：FormLayout 那類產出的定位過寬

`FormSchema.GetFormLayout(layoutId)` 是 runtime 產生的
（[`src/Bee.Definition/Forms/FormSchema.cs:242`](../../src/Bee.Definition/Forms/FormSchema.cs) →
`FormLayoutGenerator.Generate`），因此 **FormLayout 落檔是客製化，不是必要步驟**。
`bee-add-form` 明說「FormLayout 由框架從 FormSchema 自動產生，不必寫」；本 skill 卻把它
列為三大產出之一，兩支對同一件事給讀者相反印象。

### 做法

1. 修正 `ft_employee` → `st_employee`；同時掃一次該 skill 其餘路徑是否還在
2. 三類產出的定位改寫：
   - **FormLayout** —— 標明「預設不必落檔（框架 runtime 產生）；只在**要客製化版面**或
     **補 `tests/Define/` fixture** 時才產」
   - **TableSchema**、**LanguageResource** —— 維持現狀（前者 seeder 要用、後者無 generator）
3. 在 `bee-add-form` 的對應段落補一句指向本 skill 的條件（「要客製版面才落檔」），
   讓兩支的說法一致

### 驗收

- 引用的所有路徑實際存在
- 與 `bee-add-form` 對 FormLayout 的說法一致、不再互相矛盾

---

---

## 階段 5 — `changelog-draft` 中性化後移入 plugin

### 為什麼搬

1. **跨專案規則已經在指名它。** `~/.claude/rules/releasing.md`（使用者層，對所有 repo 生效）
   寫著「CHANGELOG 產出可用 `changelog-draft` skill（**若該 repo 有**）」——
   跨專案規則指向一個只存在於單一 repo 的 skill。搬進 plugin 後該括號可拿掉。
2. **plugin 定位早已預留。** `dev-workflow` 由 `plan-workflow` 改名的理由即
   「定位擴為『開發流程』以容納後續 CI、源碼掃描、**套件發佈**等 skill」。
3. **內容約八成已通用** —— git 範圍定位、Conventional Commits 分類、版號建議、
   交付報告、「不做什麼」全與專案無關。

plugin 原始碼在 `~/Desktop/repos/claude-plugins/plugins/dev-workflow/skills/`
（**不是**改 `~/.claude/plugins/cache/` 下的快取）。

### 中性化項目

| 硬編處 | 現況 | 改法 |
|--------|------|------|
| Step 4 掃 ADR | 絕對路徑 `/Users/jeff/Desktop/repos/bee-library/docs/adr/` | repo 相對路徑；允許 repo 無 `docs/adr/` |
| 版號檔 | 寫死 `Version.props` | 改「該 repo 的版號單一來源」，判定交回 `releasing.md` |
| pre-stable v4.x、從 4.3.0 起記錄、不回補 | 硬編專案事實 | 開場從既有 CHANGELOG 反推 |
| 兩層結構 + 雙語 | 當成鐵則 | 沿用該 repo 既有結構；偵測不到才問單層/兩層、單語/雙語 |
| 範本 `docs/changelogs/4.11.0.md` | 專案檔 | 「以該 repo 最近一版明細檔為範本」 |
| ADR-011 舉例 | 專案 ADR | 換成不指名的說明 |

**設計轉向：從「規定慣例」改為「反推慣例」。** 開場先偵測該 repo 的既有結構
（CHANGELOG 是否雙語兩檔、有無 per-version 明細目錄、首個記錄版本），沿用之；
偵測不到才問使用者。如此在 bee-library 的行為完全不變，別的 repo 也能用。

> 那個絕對路徑是現在就該修的實質 bug —— 它把使用者的機器路徑寫死在 skill 裡。

### bee-library 側：零殘留

不在 repo 內補「本專案 CHANGELOG 慣例」文件。那些事實的權威來源本來就是
`CHANGELOG.md` 與 `docs/changelogs/` 自己；唯一讀不出來的「pre-stable 允許 minor
含 breaking」已寫在 `~/.claude/rules/releasing.md`。

### 做法

1. 在 plugin repo 新增 `plugins/dev-workflow/skills/changelog-draft/SKILL.md`（中性化版）
2. 依 plugin 慣例 bump 版本並更新其 marketplace / plugin manifest
3. 刪除 `.claude/skills/changelog-draft/`，並從 `.claude/skills/README.md`
   的「通用工作流」表移除、於「Plugin 提供」表補上一列
4. 更新 `~/.claude/rules/releasing.md`：拿掉「（若該 repo 有）」

### 驗收

- 中性化版內無任何絕對路徑、無 bee-library 專屬版號 / 檔名
- 在 bee-library 跑一次乾跑（不寫檔），確認偵測到雙語 + 兩層結構、行為與現況一致
- `.claude/settings.json` 的 `enabledPlugins` 已含 `dev-workflow`，故新 skill 自動可用

---

## 階段 6 — `releasing.md` 拆分

### 問題：模態錯置

`~/.claude/rules/releasing.md`（154 行）是**常駐** rule，佔使用者層常駐總量（576 行）的
**27%**，每個 session、每個 repo 都載入 —— 換來的是一個約一個月用一次的程序。
這正是 `dev-workflow:config-audit` 在抓的「常駐 vs 按需錯置」。

階段 5 完成後另有一個接縫問題：`releasing.md` 寫「CHANGELOG 產出可用 `changelog-draft`
skill（**若該 repo 有**）」，該括號存在的原因就是兩者分居不同層。兩支同在 plugin
後變成內部 skill 互引，括號自然消失。

### 拆法

| 去處 | 內容 | 約略 |
|------|------|------|
| 留 `~/.claude/rules/` | 指路不複寫原則（升格為獨立條目）＋ 不自動推 tag 的安全約束 | ~20 行 |
| 移 `dev-workflow:release` | 前置條件表、版號判定、破壞性變更兩類檢查、四步驟、`PublicAPI` 合併、分兩次推送 | ~130 行 |

**為何這兩塊不能跟著搬：**

1. **「凡有單一權威來源的資訊，文件一律指路、不複寫」** —— 它現在寄生在
   「### 2. 版號」底下，但這不是發版程序，是**通用文件原則**（本 repo 剛以十一個階段
   套用它）。跟著搬走就只有發版時才載入，而它該管的是每天寫文件的時候。
2. **「不自動推送 tag、發布是不可逆的對外動作」** —— 防護欄若只在 skill 被喚起時載入，
   「agent 根本沒想到要喚起 skill」的情況下它就不存在，而那恰是最危險的情況。
   NuGet 版本發布後刪不掉。

### 中性化與落地細節

`releasing.md` 本身已寫得相當通用（bee-library 特例都已標註，如
`Version.props` vs `src/Directory.Build.props`），需處理的是兩點：

- **`~/.claude/scripts/merge-public-api-shipped.sh` 必須一起進 plugin。**
  否則 plugin 依賴一個不隨它散佈的使用者層檔案，換機器即壞。
- **description 標明適用範圍為 .NET / NuGet repo** —— `PublicAPI` analyzer、
  slnx build、`nuget-publish.yml` 皆為 .NET 專屬，不假裝語言中立。

`tools/Bee.Cli` 版號漂十二個 minor 那段建議保留：它教的是通用陷阱
（repo 內有不繼承共用版號檔的可發布專案，per-project 一致性閘門擋不到）。

### 相依

**排在階段 5 之後** —— `release` skill 的第一步就是呼叫 `changelog-draft`，
後者需先在 plugin 內就位。

### 驗收

- `~/.claude/rules/releasing.md` 縮為 ~20 行，或併入既有 rule 後刪除
- 常駐總量下降幅度可量測
- `merge-public-api-shipped.sh` 隨 plugin 散佈，skill 內不指向 `~/.claude/scripts/`
- 在 bee-library 走一次乾跑（不推 tag），確認流程與現況一致

---

## 全域驗收

- `.claude/skills/` 由 12 支降為 9 支
- `.claude/skills/README.md` 索引與實際目錄一致（含分工註記）
- 各 skill 內引用的 repo 內路徑全數存在（可用同一支掃描確認）
- 不動任何 `src/` / `tests/` 程式碼；本 plan 為純文件與設定改動
