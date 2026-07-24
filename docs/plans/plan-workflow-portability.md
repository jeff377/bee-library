# 計畫：plan 工作流可攜化（抽 skill → plugin 散佈）

**狀態：🚧 進行中（2026-07-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 本 repo 抽出 `plan-write` skill、CLAUDE.md 瘦成 gate | ✅ 已完成（2026-07-24） |
| 2 | 包成 `plan-workflow` plugin，供其他 repo 安裝 | ✅ 已完成（2026-07-24） |
| 3 | 多人共同開發 repo 的落地調整（CONTRIBUTING、版控、負責人） | 📝 待做 |

## 背景

目前 plan 工作流的規則全部寫在 `.claude/CLAUDE.md` 的「工作流程」章節（第 53–106 行，共 54 行）。
要把這套作法複製到其他公司多人共同開發的 repo 時，「整段複製 CLAUDE.md」與「複製 skill 資料夾」
兩種作法都會產生 N 份副本，規則演進後必然 drift（改了 A repo，B/C/D repo 停在舊版且無人知情）。

### 為什麼不是「全部包成 skill」

plan 規則其實是兩種不同性質的內容混在一起：

| 內容 | 性質 | 為何不能互換位置 |
|------|------|-----------------|
| 「需要規劃的任務必須先寫 plan、等確認才執行」<br>「回覆中必須附 plan 連結」 | **always-on gate** | skill 靠 description fuzzy 觸發，會漏；而漏掉的正是「該擬計畫卻直接動手」這種最該擋下的情況。gate 必須常駐 CLAUDE.md |
| 狀態列格式、階段表格欄位、emoji 對應規則、封存流程 | **按需樣板** | 只有真的在寫／更新 plan 當下才需要。常駐 CLAUDE.md 是每次對話都付出的 token 成本 |

54 行裡約 42 行屬於後者。抽出後 CLAUDE.md 該章節剩約 12 行。

### 一個 skill 還是兩個

初步構想是拆 `plan-write`（撰寫格式）＋ `plan-status`（狀態更新／封存）兩個，
理由是觸發時機不同。**改為單一 skill**：狀態列格式在「建立 plan 的第一分鐘」就要用到
（開頭就得寫上狀態列），拆兩個會讓寫 plan 時被迫兩個都載，反而更差。
內容總量約 60 行，單一 skill 的合理範圍內。

### 觸發可靠性的設計

skill 不依賴 fuzzy 觸發：CLAUDE.md 的 gate 末尾直接寫明
「狀態列格式、多階段表格、封存細節見 `plan-write` skill」——
讀到 gate 就有明確指標去載入，等於把觸發從機率變成確定。

## 階段 1：本 repo 抽出 skill

### 1.1 新增 `.claude/skills/plan-write/SKILL.md`

frontmatter：

```yaml
---
name: plan-write
description: 撰寫 / 更新 docs/plans/ 下的計畫文件 —— 狀態列格式（✅ 已完成 / 🚧 進行中 / 📝 擬定中 + ISO 日期）、多階段 plan 的階段表格（階段 / 範圍 / 狀態）、整體狀態與階段狀態的對應規則、封存流程。當使用者要「寫一份 plan」、「擬計畫」、「更新 plan 狀態」、「標記 plan 完成」、「封存 plan」，或你即將建立 / 修改 docs/plans/ 下任何文件時使用。
---
```

body 內容＝現行 CLAUDE.md 第 65–106 行（「Plan 狀態標記格式」＋「多階段 plan：狀態列下加階段表格」
兩個小節）**逐字搬移**，不改寫規則本身；另補一段「常見錯誤」（見 1.3）。

### 1.2 CLAUDE.md 工作流程章節瘦身

第 53–106 行替換為：

```markdown
## 工作流程

### 執行前先擬計畫

任何需要事先規劃的任務（重構、新功能、架構調整等），必須：

1. 將計畫寫成 md 文件，存至 `docs/plans/` 目錄，檔名格式：`plan-<主題>.md`
2. **每次建立或修改 plan 文件後，回覆中必須附上該 plan 的連結**（markdown 相對連結），
   讓使用者可在對話中直接點開、不需自行翻找
3. 等待使用者確認後，才開始執行
4. **Plan 執行完畢時，立刻在文件頂部標記完成狀態**
5. 由使用者要求時才將計畫文件移至 `docs/archive/` 封存（此目錄已 gitignored）

> 狀態列格式、多階段 plan 的階段表格、封存細節 → 見 `plan-write` skill。
```

淨減約 42 行常駐 context。

### 1.3 skill 內補「常見錯誤」段

現行規則沒明寫、但實際反覆發生的：

- 狀態列寫成完整段落而非單行（規則有寫「只放單行」，但沒給反例）
- 多階段 plan 只更新階段表格、忘了同步更新整體狀態列
- 完成日期填成「開始日」而非「實作落地當天」
- 表格外又用 prose 重述「目前在 Phase X」（規則已禁，補反例）

### 1.4 驗收

- `.claude/CLAUDE.md` 該章節 ≤ 15 行且 gate 五條完整保留
- 新開一段對話請 Claude 寫一份測試 plan，確認會主動載入 `plan-write` 並產出正確狀態列
- `.claude/skills/plan-write/` 入版控（`.gitignore` 僅排除 `hackmd-blog`，新 skill 預設入版控）

## 階段 2：包成 plugin 散佈

階段 1 驗證可用後才做。

### 2.1 命名（已落地）

| 層 | 名稱 | 說明 |
|----|------|------|
| GitHub repo | `claude-plugins`（public） | 已建於 https://github.com/jeff377/claude-plugins |
| marketplace name | `jeff377-plugins` | `marketplace.json` 的 `name` 欄位；`install ...@jeff377-plugins` 用此名 |
| plugin | `plan-workflow` | 只裝 plan 工作流，**不含** PR / 發版 / code style |
| skill | `plan-write` | 呼叫為 `/plan-workflow:plan-write` |

> **命名釐清（2026-07-24 實測修正）**：`claude plugin validate` 擋的是 **`marketplace.json` 裡的
> `name` 欄位**含 `claude-*` 保留字樣（判為冒充官方 marketplace），**不是 GitHub repo 名**。
> 故 GitHub repo 可維持 `claude-plugins`，只需把 marketplace `name` 設為 `jeff377-plugins`。
> 兩者關係：`/plugin marketplace add` 吃 GitHub repo 路徑（`jeff377/claude-plugins`）、
> `/plugin install ...@` 與 `extraKnownMarketplaces` 的 key 吃 marketplace name（`jeff377-plugins`）。
> **散佈變更**：原評估可私有，實際選 public —— 內容僅 plan 格式規範、無敏感資訊，public 讓任何 repo
> （含 CI、跨組織）以 `source: github` 無痛取用；私有需成員 / CI 有 repo 存取權 + token。

**為何不叫 `dev-conventions` 之類的大帽子**：PR 流程、發版流程、程式碼風格在不同公司幾乎必然不同，
裝在一起會逼對方接收不適用的規則。真正可跨公司通用的只有 plan 工作流本身。
未來要加 PR / 發版慣例時，在**同一個 marketplace 內另開 plugin**（`pr-workflow` 等），
安裝粒度更細，不需為此預留命名空間（同 `code-style.md`「不為假設的未來建類」）。

### 2.2 repo 結構

```
claude-plugins/                    ← 這個 repo 即 marketplace
├── .claude-plugin/
│   └── marketplace.json           ← 索引；同 repo 內的 plugin 用相對路徑 "./plugins/plan-workflow"
└── plugins/
    ├── plan-workflow/
    │   ├── .claude-plugin/
    │   │   └── plugin.json        ← 只有 manifest 放這；skills/ 一律放 plugin 根層
    │   └── skills/
    │       └── plan-write/
    │           └── SKILL.md
    └── bee-developer-skills/      ← 未來（見 memory `bee-developer-skill-pack-goal`），獨立 plugin
```

### 2.3 各 repo 的接線

於專案 `.claude/settings.json` 宣告 marketplace 並啟用 plugin，成員信任該資料夾後即隨 repo 生效
（`extraKnownMarketplaces` 的 key 與 `enabledPlugins` 的 `@` 後段都是 **marketplace name**，非 repo 名）：

```json
{
  "extraKnownMarketplaces": {
    "jeff377-plugins": {
      "source": { "source": "github", "repo": "jeff377/claude-plugins" }
    }
  },
  "enabledPlugins": {
    "plan-workflow@jeff377-plugins": true
  }
}
```

兩者都入版控（project scope），等於規則跟著 repo 走。

### 2.4 已確認的機制（原「待確認」）

- **skill 不會互相覆蓋**：plugin skill 一律帶命名空間，`/plan-write` 與 `/plan-workflow:plan-write`
  會並存而非取代（agents 才是本地覆蓋 plugin）。故 bee-library **必須擇一**：
  移入 plugin 後就刪掉 `.claude/skills/plan-write/`，否則兩份並存必然 drift。
- **CLAUDE.md gate 的指標要改名**：階段 1 寫「見 `plan-write` skill」，移入 plugin 後
  改為「見 `/plan-workflow:plan-write`」。
- **開發期不需要 marketplace**：`claude --plugin-dir ./plugins/plan-workflow` 直接載入測試，
  `/reload-plugins` 熱更新，`claude plugin validate` 驗證結構。marketplace 只在要給別人裝時才需要。
- **`validate` 擋 `claude-*` 命名**：marketplace 名不可含官方保留字樣，否則驗證失敗（見 2.1）。
- **安全**：plugin 能以使用者權限執行任意程式碼。自建 marketplace 無虞，
  但團隊日後若要引入外部 plugin，須先建立審核共識。

### 2.5 尚待人工驗收（互動式終端）

`/plugin` 系列為互動式終端指令，無法由 agent session 代跑。發佈後請在終端自行驗收一次：

```
/plugin marketplace add jeff377/claude-plugins
/plugin install plan-workflow@jeff377-plugins
/reload-plugins
```

確認 `/plan-workflow:plan-write` 可呼叫、且請 Claude 擬計畫時會產出正確狀態列。

### 2.6 bee-library 改用 plugin（✅ 已執行，2026-07-24）

**決定翻轉：bee-library 改用 plugin，刪除本地 `.claude/skills/plan-write/`。**
原先（2026-07-24 稍早）決定「暫不遷移、先保留本地副本」，因確認 repo 已建好、schema 正確後，
判定「兩份 drift」的代價大於「等終端驗收」的謹慎，故直接遷移，消除雙份來源。

已落地的四處改動：

1. **刪本地 skill**：`git rm .claude/skills/plan-write/`（改由 plugin 提供）。
2. **`.claude/settings.json` 宣告啟用**：加 `extraKnownMarketplaces.jeff377-plugins`
   （repo `jeff377/claude-plugins`）+ `enabledPlugins["plan-workflow@jeff377-plugins"] = true`。
3. **CLAUDE.md gate 指標改名**：`見 plan-write skill` → `見 /plan-workflow:plan-write skill`。
4. **`.claude/skills/README.md`**：移除 plan-write 列，補註記指向 plugin。

> 內容比對：遷移前本地版與 plugin 版差異僅 `docs/archive/` 的 gitignore 假設措辭（plugin 版較通用），
> 該 bee 特定細節在 CLAUDE.md gate 第 5 條本就保留，遷移零損失。
>
> ⚠️ **尚待終端驗收（2.5）**：`extraKnownMarketplaces` + `enabledPlugins` 已入版控，但 plugin
> 實際載入需在互動式終端信任資料夾 / 執行 `/reload-plugins` 後才生效。請依 2.5 指令驗收一次，
> 確認 `/plan-workflow:plan-write` 可呼叫。

## 階段 3：多人 repo 的落地調整

單人 repo 不痛、多人 repo 會痛的三件事：

1. **plan 進版控** — 目前 `docs/archive/` 是 gitignored（單人可行）。多人時封存的 plan 是團隊記憶，
   應改為 `docs/plans/archive/` 並入版控。**本 repo 維持現狀不動**，此調整只套用到多人 repo。
2. **階段表格加「負責人」欄** — 階段表格本來就是天然的分工單位，多人時欄位改為
   `階段 / 範圍 / 負責人 / 狀態`。
3. **慣例同時寫進 `CONTRIBUTING.md`** — 團隊裡一定有人用 Cursor / Copilot / 純手寫，
   CLAUDE.md 對他們是隱形的。工具無關的部分（目錄、命名、狀態列語意）放 CONTRIBUTING.md 作為
   權威來源；CLAUDE.md 只負責「叫 Claude 遵守它」。

## 不做的事

- **不改動 plan 規則本身的內容**（階段 1 是純位置搬移，規則語意零變更）
- **不動 `docs/plans/` 現有 15 份 plan**（格式已符合現行規則）
- **不把其他 rules（testing / security / sonarcloud / maui / avalonia）一併重整** —— 那些是
  bee-library 專屬技術規則，不具跨公司可攜性，與本計畫無關
