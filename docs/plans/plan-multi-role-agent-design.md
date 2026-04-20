# 多角色排程 Agent 整合開發流程：適用情境建議書

**狀態：📝 參考文件（非待執行 Plan）**
**建立：2026-04-20**

---

## 背景

Claude Code Routines 允許建立多個獨立的排程 Agent，每個 Agent 可以有不同的角色、觸發條件與行為邊界。本文件整理適合以排程 Agent 自動化的開發情境，供未來實作參考。

目前已建立的 Agent：
- `bee-sonar-fix-daily`：每日 SonarCloud 品質巡檢（唯讀，僅更新 snapshot）
- `bee-sonar-autofix`：每週一自動修正白名單 Sonar issues，走 PR 流程

---

## 核心設計原則

### 1. 安全等級分層

每個 Agent 依風險程度分層，對應不同的保護機制：

| 等級 | 行為 | 保護機制 | 範例 |
|------|------|---------|------|
| **L1 唯讀** | 只讀取資料、產出報告 | 無 | 品質巡檢、覆蓋率報告 |
| **L2 建議** | 建立草稿、回覆 issue | 人工確認後才送出 | PR 草稿、issue 回覆建議 |
| **L3 自動 PR** | 修改程式碼，走 PR + CI | CI 通過 + 人工 Review | Sonar autofix、測試補全 |
| **L4 直接 merge** | 自動 merge（謹慎使用） | CI 通過 + 自動化測試覆蓋率 | 純格式修正 |

**本專案建議止步 L3**，L4 需累積足夠信任再開放。

### 2. Plan 驅動模式

複雜任務不由 Agent 自行判斷「做什麼」，而是由人工寫好 plan，Agent 負責「怎麼做」：

```
人工判斷 + 撰寫 plan（docs/plans/plan-xxx.md）
  → 狀態標記：🔄 待執行
  → git push / 手動觸發 Agent
  → Agent 讀 plan → 執行 → 開 PR
  → 人工 Review + merge
  → Agent 更新 plan 狀態：✅ 已完成
```

### 3. 硬性邊界

無論哪個 Agent，以下項目一律不得碰：
- `.github/workflows/`（CI 流程）
- `src/Directory.Build.props` 版本號（發佈用）
- `.claude/`（Agent 設定）
- 直接 push 到 `main`（除非是 L1 的 snapshot 更新）

---

## 適用情境分類

### A. 品質維護類

#### A1. SonarCloud 每日巡檢（已實作）
- **角色**：品質監控員
- **觸發**：每日定時
- **任務**：抓取 SonarCloud 指標，比對前次快照，有異動時 commit snapshot 並 log
- **安全等級**：L1
- **適合自動化**：⭐⭐⭐⭐⭐

#### A2. SonarCloud 白名單自動修正（已實作）
- **角色**：低風險程式碼清潔員
- **觸發**：每週一
- **任務**：修正 12 條白名單規則 issue，開 PR
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐⭐⭐

#### A3. 覆蓋率缺口報告
- **角色**：測試品質分析師
- **觸發**：每週（或 PR merge 後）
- **任務**：抓 SonarCloud coverage 報告，找出覆蓋率 < 80% 的模組，建立 GitHub issue 列出缺口
- **安全等級**：L1（建 issue 屬 L2）
- **適合自動化**：⭐⭐⭐⭐
- **實作重點**：
  - 用 `measures/component_tree` API 取各檔案覆蓋率
  - 用 `gh issue create` 建 issue，加 `needs-tests` label

---

### B. 測試補全類

#### B1. Plan 驅動測試補全
- **角色**：測試撰寫員
- **觸發**：手動（人工寫好 plan 後觸發）
- **任務**：讀取 plan 中指定的模組與測試要求，補撰對應的 xUnit 測試，開 PR
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐⭐
- **前提**：plan 需包含：
  - 目標測試專案
  - 待測方法列表
  - 測試案例描述（含邊界條件）
  - 是否需要 `[DbFact]`（會影響 CI 是否 skip）
- **實作重點**：
  - 讀 src 對應程式碼理解介面
  - 參考 `tests/Bee.Tests.Shared/` 既有模式
  - `[DisplayName]` 用繁體中文

#### B2. Issue 驅動測試補全
- **角色**：測試撰寫員
- **觸發**：GitHub issue 標籤 `needs-tests`（Webhook 或輪詢）
- **任務**：讀 issue 描述 → 找對應 src 程式碼 → 補測試 → 開 PR，回覆 issue
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐

---

### C. 重構與維護類

#### C1. Plan 驅動機械式重構
- **角色**：重構執行員
- **觸發**：手動（人工寫好 plan 後觸發）
- **任務**：依 plan 執行批次命名改動、資料夾搬移、命名空間對齊等機械式操作，開 PR
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐⭐
- **適合情境**：
  - IDE0130 命名空間對齊（大量檔案）
  - 統一命名慣例（如 Interface 前綴補 `I`）
  - 資料夾結構重組

#### C2. 依賴版本升級
- **角色**：套件維護員
- **觸發**：每月定時 或 手動
- **任務**：掃描 `*.csproj` 的 NuGet 版本，比對 NuGet API 的最新版，建 issue 或 PR 升級 patch 版本
- **安全等級**：L2（建 issue）或 L3（直接開 PR）
- **適合自動化**：⭐⭐⭐
- **注意**：major/minor 版本升級可能有 breaking change，只自動升 patch

---

### D. 文件同步類

#### D1. API 變更文件同步
- **角色**：文件同步員
- **觸發**：PR merge 後（偵測到 public API 異動）
- **任務**：比對 git diff，若有 public 介面或方法簽章變更，更新對應的 `README.md` / `docs/` 文件
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐
- **實作重點**：
  - 比對 `git diff HEAD~1` 找 public API 異動
  - 只更新文件，不改原始碼

#### D2. CHANGELOG 自動維護
- **角色**：發佈文件員
- **觸發**：手動（發佈前）
- **任務**：掃描上次 tag 以來的 commits，分類整理成 CHANGELOG 條目，開 PR
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐⭐

---

### E. Issue / PR 管理類

#### E1. Issue 分類與標籤
- **角色**：Issue 管理員
- **觸發**：新 Issue 建立（Webhook）或每日輪詢
- **任務**：讀 issue 標題 + 描述，判斷類型（Bug / Feature / Enhancement / Question），加對應 label，回覆確認訊息
- **安全等級**：L2
- **適合自動化**：⭐⭐⭐⭐
- **限制**：只加 label + 回覆，不做程式碼異動

#### E2. PR Review 摘要
- **角色**：Review 助理
- **觸發**：PR 建立（Webhook）
- **任務**：讀 PR diff，產出結構化摘要（異動模組、潛在風險點、測試建議），作為 PR comment
- **安全等級**：L2
- **適合自動化**：⭐⭐⭐
- **注意**：只做摘要，不做 approve / merge

---

### F. Plan 執行類

#### F1. 通用 Plan Executor（建議重點實作）
- **角色**：計畫執行員
- **觸發**：手動（人工推送含「🔄 待執行」的 plan 後觸發）
- **任務**：
  1. 掃描 `docs/plans/` 找狀態為「🔄 待執行」的 plan
  2. 讀取 plan 內容（背景、步驟、限制）
  3. 依步驟實作（修程式碼 / 補測試 / 更新文件）
  4. 開 PR，PR body 引用對應 plan
  5. 更新 plan 狀態為「🚧 進行中」
- **安全等級**：L3
- **適合自動化**：⭐⭐⭐⭐（plan 品質決定成功率）
- **Plan 撰寫要求**：
  - 步驟必須可操作（「讀 X 檔，在 Y 方法後加 Z」，而非「改善效能」）
  - 明確的邊界（哪些檔案可動、哪些不可動）
  - 預期輸出（PR 應包含什麼、測試應覆蓋哪些案例）

---

## 實作優先順序建議

| 優先 | Routine | 複雜度 | 效益 |
|------|---------|-------|------|
| ✅ 已完成 | SonarCloud 每日巡檢 | 低 | 高 |
| ✅ 已完成 | Sonar 白名單 autofix | 中 | 高 |
| 1 | Plan Executor（F1） | 中 | 極高（通用） |
| 2 | Issue 分類（E1） | 低 | 中 |
| 3 | 覆蓋率缺口報告（A3） | 低 | 高 |
| 4 | Plan 驅動測試補全（B1） | 中 | 高 |
| 5 | 依賴版本升級（C2） | 中 | 中 |
| 6 | CHANGELOG 自動維護（D2） | 低 | 中 |

---

## Webhook 整合說明

Routines 支援 Webhook 觸發（除定時與手動外）。整合 GitHub Webhook 的流程：

1. 建立 Routine → 取得 Webhook URL（Routine 設定頁）
2. GitHub repo → Settings → Webhooks → 新增 Webhook
3. 設定觸發事件（Issues、Pull Request、Push 等）
4. Payload 傳入 Routine，Agent 可解析 `$GITHUB_EVENT` 或類似環境變數

> 注意：Webhook 觸發的 Agent 需在 prompt 中說明如何取得事件 payload，目前尚未在本專案驗證具體格式，實作前需先測試。

---

## 遠端環境限制備忘

基於目前測試結果：

| 工具 | 可用 | 備註 |
|------|------|------|
| `curl` / `wget` | ✅ | 需在 allowlist 設定目標 host |
| `python3` | ✅ | stdlib 完整可用 |
| `git` / `gh` | ✅ | GitHub OAuth 已預注入 |
| `dotnet build/test` | ❌ | 遠端無法安裝 .NET SDK（網路限制） |
| `docker` | ❌ | 沙盒環境不支援 |

**因此所有需要 build/test 驗證的 Agent，統一改由 CI（`build-ci.yml`）把關，PR 不得在 CI 未通過前 merge。**
