# 計畫：開發流程強化（commit 驗證 hook + dev-workflow plugin）

**狀態：✅ 已完成（2026-07-31）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | bee-library commit 前驗證 hook（clean build + PublicAPI diff 攤開） | ✅ 已完成（2026-07-31） |
| 2 | plugin `plan-workflow` → `dev-workflow` 改名（僅 plugin 層） | ✅ 已完成（2026-07-31） |
| 3 | 新增 `plan-execute` skill 至 dev-workflow | ✅ 已完成（2026-07-31） |

## 背景

`/insights` 報告（2026-07-31，涵蓋 2026-05-27 ～ 07-30 共 106 個 session）盤點出的三類摩擦中，
建議新增的 Custom Skill 經比對後**幾乎全與現有資產重複**：

| 報告建議 | 現有資產 | 判定 |
|---------|---------|------|
| `archive-plans` | `plan-workflow:plan-write`（含封存流程） | 完全重複 |
| Sonar 清零 skill | `.claude/commands/sonar-fix.md` | 完全重複 |
| release/CHANGELOG skill | `.claude/skills/changelog-draft/` | 大幅重複 |
| parallel audit swarm | `.claude/skills/bee-framework-review/` | 大幅重複 |

重複的成因是分析素材只有 session transcript——看得到行為頻率，看不到既有 skill 清單。

扣除重複後，**真正的缺口有二**，本計畫各對應一個階段：

1. **驗證證據閘門**——「宣稱 build clean 但 incremental 掩蓋警告」「PublicAPI baseline 被破壞」。
   本質是 agent 缺乏自覺檢查，寫成 skill 無效（skill 需自願叫用），必須由 harness 強制 → **hook**。
2. **plan 執行時的閘門**——`plan-write` 管撰寫、`plan-handoff` 管交接，無人管「執行時」。
   曾因依據未 push 的過期 plan 實作，導致整個 session 改動全數 revert。

階段 2 的改名是為階段 3 讓路：plugin 定位從「plan 專用」擴為「開發流程」後，
CI、源碼掃描、套件發佈等 skill 才有正當歸屬，不必再各自散在專案 command。

## 階段 1：commit 前驗證 hook

### 設計

`PreToolUse` hook，matcher `Bash`，由腳本自行判斷是否為 `git commit` 指令；
非 commit 直接 exit 0 放行，是 commit 才跑檢查。檢查未過 **exit 2 阻擋**，
stderr 內容會回饋給 agent，使其看見失敗原因並修正。

設定寫在 `.claude/settings.json`（入版控），指向獨立腳本
`.claude/hooks/pre-commit-verify.sh`——邏輯不內嵌 JSON，維持可讀與可測。

### 檢查項目與理由

**(1) Clean Release build**

```
dotnet build Bee.Library.slnx -c Release --no-incremental
```

`--no-incremental` 是重點：報告記錄的失誤正是 incremental build 掩蓋了警告卻宣稱 clean。
搭配既有 `TreatWarningsAsErrors=true`，任何警告即為失敗。

成本已實測：`dotnet clean` 後完整 Release build **9.7 秒**（0 警告 0 錯誤，34 個專案）。
（實測用 `clean` + `build`，hook 改用 `--no-incremental`，兩者工作量相當。）

**(2) `PublicAPI.Unshipped.txt` 異動攤開**

分析器 `Microsoft.CodeAnalysis.PublicApiAnalyzers` 4.14.0 已於 `src/Directory.Build.props`
全域啟用，故「public API 變更未申報」（RS0016 等）**已經是編譯錯誤**，(1) 即可攔下。

真正的漏洞在下一步：把變更**寫進 `Unshipped.txt` 即可讓 build 轉綠**，而語意上的
breaking change（如對既有 public constructor 增加 optional 參數 → 二進位不相容）
就此靜默通過。此檢查不阻擋，而是偵測到 `PublicAPI.Unshipped.txt` 有異動時，
把 diff 明列於 stderr 並要求 agent 在 commit message 或回覆中說明相容性判定，
把「靜默」轉為「必須正視」。

成本為一次 `git diff`，可忽略。

### 明確不做

- **不跑測試**：DB 測試受容器狀態影響，易產生誤擋；且已有 `./test.sh` 與 CI 兩道把關。
- **不做 PostToolUse per-edit 檢查**：每次編輯都建置會拖慢日常節奏，收益不足。
- **不攔 `git push`**：push 後 CI 會跑，且已有 `/ci-watch`。

### 驗證方式

1. 故意引入一個編譯警告 → 嘗試 commit → 應被阻擋且 stderr 指出警告位置。
2. 故意改動任一 `PublicAPI.Unshipped.txt` → 嘗試 commit → 應放行但 stderr 列出 diff。
3. 一般 commit（無警告、未動 PublicAPI）→ 應正常通過，額外耗時約 10 秒。
4. 非 commit 的 Bash 指令（如 `git status`、`ls`）→ 不得觸發建置。

通過後在 `.claude/rules/` 補一節說明此 hook 的存在與繞過方式（`--no-verify` 不適用於
Claude Code hook，需說明正確的暫時停用途徑）。

## 階段 2：plugin 改名為 dev-workflow

### 三層命名釐清

現況為三層獨立命名，**本階段只動第三層**：

| 層級 | 現況 | 變更後 |
|------|------|--------|
| GitHub repo | `jeff377/claude-plugins` | 不變 |
| marketplace | `jeff377-plugins` | 不變 |
| plugin | `plan-workflow` | **`dev-workflow`** |

（repo 名已於 commit `7ad2806` 改過一次並刻意保留 marketplace 名，本次維持該分界。）

### 影響面

於 `/Users/jeff/Desktop/repos/claude-plugins`：

- `plugins/plan-workflow/` 目錄 → `plugins/dev-workflow/`（`git mv`）
- `plugins/dev-workflow/.claude-plugin/plugin.json` 的 `name` 欄位
- `plugins/dev-workflow/README.md` 定位敘述（從「plan 流程」擴為「開發流程」）
- 根 `.claude-plugin/marketplace.json` 的 plugin 條目
- 根 `README.md`

於各消費端 repo：

- `bee-library/.claude/settings.json` 的 `enabledPlugins`：
  `plan-workflow@jeff377-plugins` → `dev-workflow@jeff377-plugins`
- 需掃描其他 repo 是否也宣告了此 plugin（bee-ui-core 等）

於本機：

- plugin 快取 `~/.claude/plugins/cache/jeff377-plugins/plan-workflow/` 需重裝生效
- 改名後 skill 叫用前綴變為 `dev-workflow:plan-write` / `dev-workflow:plan-handoff`

### 步驟

1. plugin repo 內完成改名與文件更新，commit + push
2. 更新 bee-library `settings.json`，並掃描其他 repo 的宣告
3. 以 `claude plugin` CLI 重裝，**完全重開 session** 後確認 skill 以新前綴出現
4. 確認 `dev-workflow:plan-write` 與 `plan-handoff` 可正常叫用

> 安裝與查驗一律走 `claude plugin` CLI，不使用斜線指令——此環境無互動式斜線面板。

### 為何排在階段 3 之前

`plan-execute` 若先加進 `plan-workflow` 再隨改名搬移，等於同一份檔案改兩次、
消費端也要重裝兩次。先改名再新增，一次到位。

## 階段 3：新增 plan-execute skill

補上「plan 執行時」的空隙。與既有兩個 skill 的邊界：

| skill | 職責 |
|-------|------|
| `plan-write` | 撰寫、更新狀態、封存 |
| `plan-handoff` | 定案後交接給新 session |
| **`plan-execute`（新）** | **執行期間的驗證閘門** |

### 核心三條

1. **來源確認**——以 `git show HEAD:<plan-path>` 讀取，與工作區內容比對；
   plan 未 commit、或與 remote 不一致時停下確認，不得依據 session 早期快取的版本實作。
2. **範圍宣告與對帳**——動筆前列出預期受影響的檔案清單；階段結束以
   `git diff --name-only` 對照，超出宣告範圍者須逐項說明。
3. **平行路徑檢查**——改動任一實作路徑後，主動搜尋是否存在對應的平行路徑
   （JSON ↔ MessagePack、DataFeed ↔ live），存在而未同步修改時必須明確標示。
   此條源自兩次半套修正的實際紀錄。

刻意**不**納入報告建議的完整 8 步流程（含 CI 輪詢、多 DB 測試矩陣、flaky 重跑規則）：
CI 輪詢已有 `/ci-watch`，測試紀律已在 `.claude/rules/testing.md`，重複規範只會稀釋。

## dev-workflow 後續候選

改名後可陸續納入（本計畫**不執行**，僅記錄方向）：

- `/ci-watch`、`/sonar-fix` 由 bee-library 專案 command 升格為 plugin skill（需先去除 repo 專屬假設）
- 套件發佈流程（`changelog-draft` 之後的 bump → tag → 發佈 → 輪詢）
- 新增 src 套件時的發佈 checklist（現為記憶項，未成文）

升格的前提是**去專案化**：目前這些資產含 bee-library 專屬路徑與慣例，
直接搬進 plugin 會在其他 repo 失效。

## 執行結果與計畫外事項

三階段皆按計畫完成，以下為與計畫書寫時不同、或執行中才確定的事項：

- **hook 需重開 session 才生效**。實地以含警告的 commit 測試，未被阻擋——
  Claude Code 於 session 啟動時固定 hook 設定，中途新增不會即時套用。
  探針 commit 已回退，此限制已寫入 `.claude/rules/commit-verification.md`。
- **建置成本比預估更低**。計畫記的 9.7 秒含 `dotnet clean`；hook 實際採
  `--no-incremental`，實測約 5 秒。
- **階段 2 遇到 remote 有未同步的 commit**（`5ec08f1`，改進 `plan-write` 的連結慣例與
  封存目錄約定），且改的正是改名前的路徑。以 rebase 合併，remote 的改進全數保留。
  這正是 `plan-execute` 第 1 條要防的情境——本機看到的不等於最新。
- **plugin README 一併修正兩處既有錯誤**（計畫外，但屬同一批編輯範圍）：
  安裝指引原用 `/plugin`、`/reload-plugins` 斜線指令，在桌面版 / 網頁版不存在，
  已改為 `claude plugin` CLI；plugin 表格漏列 `plan-handoff`，已補。
- **版號**：改名為破壞性變更 → `2.0.0`；新增 skill → `2.1.0`。

## 風險與回退

| 風險 | 因應 |
|------|------|
| hook 誤擋正常 commit | 腳本預設保守：無法解析輸入時 exit 0 放行，不阻斷工作 |
| 建置 10 秒對頻繁小 commit 造成負擔 | 實測 9.7 秒可接受；若體感過重，改為僅在 `src/` 有異動時觸發 |
| 改名後其他 repo 的 plugin 宣告失效 | 步驟 2 逐一掃描；未掃到者症狀為 skill 消失，重加宣告即可復原 |
| plugin 快取殘留舊名 | 重裝 + 完全重開 session；必要時手動清 `~/.claude/plugins/cache/` |

三階段皆為獨立可交付，任一階段可單獨回退而不影響其他。
