# ADR-015：`MasterKeySource` 預設改為 `Environment` — 對齊 12-factor「config in env」

## 狀態

已採納（2026-05-26）

## 背景

`SystemSettings.SecurityKeySettings.MasterKeySource` 決定 server 從何處讀取 **master key**（衍生 `ApiEncryptionKey` 與 `KeyEncryptionKey` 的根密鑰）。v4.5 之前 `Type` 預設為 `File` + `Value=Master.key`，部署時若該檔不存在會 auto-generate 並寫入 `DefinePath` 下。

這個預設在早期本機開發 / 單機部署情境工作得不錯：開箱即跑、無需額外設定。但隨著 production 部署形態演進，痛點逐漸放大：

| 痛點 | 描述 |
|------|------|
| **違反 [12-factor app](https://12factor.net/config) 的「config in env」原則** | secret 屬 config 範疇，應與 build artifact 分離；存進檔案會使 secret 綁定特定 host 檔案系統 |
| **container 化部署摩擦** | Docker / Kubernetes 部署需 `mount` 額外 volume 或在 build step `COPY` 檔案，破壞 image 的「**無狀態 + 不可變**」屬性 |
| **多實例同步成本** | 水平擴展時 N 個節點需同步同一份 `Master.key` 檔案內容，需要額外的 secret 分發機制 |
| **與 secret 管理工具整合困難** | K8s Secret、HashiCorp Vault、AWS Secrets Manager、Azure Key Vault 等業界主流工具一律以 env var 注入 secret，無「自動讀取磁碟檔案」的接點 |
| **檔案 artifacts 留在檔案系統** | 部署後 `Master.key` 與其他 config 檔混在同目錄，scan / backup 流程容易誤含 secret |

需要把 master key 來源從「**檔案**」對齊到「**env var**」這個業界共識；同時保留既有 `File` 選項供既有部署 / 本機開發。

## 決策

**`MasterKeySource.Type` 預設從 `File` 改為 `Environment`；既有 `Type=File` 部署完全不受影響。**

### 四項核心要點

1. **Code default 改為 `Environment`**

   `MasterKeySource` ctor 預設 `Type = MasterKeySourceType.Environment`、`Value = "BEE_MASTER_KEY"`：

   - 新部署若未明確設定 `<MasterKeySource>` 區塊 → 從 `$BEE_MASTER_KEY` 讀
   - 既有 `SystemSettings.xml` 已明確寫 `<Type>File</Type>` → **不受影響**，繼續從檔案讀
   - 既有 `SystemSettings.xml` 已明確寫 `<Type>Environment</Type>` → **不受影響**（已對齊）

2. **`MasterKeySourceType.File` 保留為合法選項**

   - 不移除 `File` enum 值，向下相容必須保留
   - 本機開發若不想設環境變數，仍可手動標 `<Type>File</Type>`
   - 某些 air-gapped 部署（secret 透過 USB 拷貝、無 env injection 機制）仍合理使用 `File`

3. **Sample / test bootstrap auto-set demo key（zero-setup 體驗）**

   開放 env var 預設後，sample / test 跑起來需要使用者**先設環境變數**才能 boot — 這對 first-run experience 是重大退化。對策：

   - `samples/Bee.Samples.Shared/DemoBackend.cs` 與 `tests/Bee.Tests.Shared/TestProcessBootstrap.cs` 在 process bootstrap 階段檢查 `BEE_MASTER_KEY`，若未設則 auto-set 一個 **hardcoded demo key**
   - `DemoMasterKey` 常數放在 `DemoCredentials.cs`（與 `DemoCredentials.UserId="demo"` 同檔），值為固定 Base64 編碼的 AES-CBC-HMAC 合併金鑰
   - `TestMasterKey` 與 `DemoMasterKey` **獨立常數**（不共用），避免 sample 與 test 互相 leak

   這沿用既有 hardcoded demo（`DemoCredentials.UserId="demo"` / `Password="demo"`）的安全等級：sample / test 本來就是 demo，與「`demo` / `demo`」帳密同等風險，可接受。

4. **Production 必須明確 override demo key**

   - Sample README（中英）與 `samples/QuickStart.Server/README*.md` 已標明：「demo backend 為 zero-setup 體驗會自動注入 hardcoded master key；**production host 必須在 process 啟動前以真實的 secret 注入 `BEE_MASTER_KEY`**」
   - 加密金鑰本身的特性是「**hardcoded master key = 加密形同無加密**」（任何人拿到 source 就能解密 payload），這條規則無法在 framework 層自動強制，依賴 deployment 文化與 README 提醒

## 理由

### 為何不選「全自動 generate 新 key」（autoCreate 每 run）

評估過讓 process 啟動時若 env var 未設**自動生成新 key 並僅存記憶體**：

- ✅ 不留檔案 artifact
- ❌ 跨 process run **金鑰不一致**：`quickstart.db` 等 SQLite 持久化資料以舊 key 加密，下次 run 用新 key 解不開
- ❌ 跨實例水平擴展**金鑰不一致**：N 個節點各自 generate 一份 key，互相無法解對方加密的資料

實務上「持久化資料」必須以**穩定金鑰**加解密，跨 process / 跨節點都必須對齊。autoCreate per-run 破壞這個前提。

### 為何不選「手動 export」（無 auto-set）

評估過 sample / test 啟動時若 env var 未設**直接報錯，要求使用者手動 export**：

- ✅ Production-like：使用者習慣「啟動前要設環境變數」的儀式
- ❌ Sample / test first-run experience 退化：每個新接觸的開發者第一次 clone repo 跑 `./test.sh` / `dotnet run --project samples/QuickStart.Server` 都會看到 error，必須先去翻 README 找到要 export 什麼
- ❌ 「忘了 export」會持續困擾 returning developer（switch machine、新 shell session、CI runner 不同 job 等）

採 bootstrap auto-set demo key 平衡兩端：本機 / CI 自動 zero-setup，production 由 deployment 機制（K8s Secret、env file、Vault 注入等）覆寫 env var，**完全不需要改 code**。

### 為何 `Value` 寫 `BEE_MASTER_KEY`（明示 env var 名）而非留空

`samples/Define/SystemSettings.xml` 與 `tests/Define/SystemSettings.xml` 內 `<Value>` 明確寫 `BEE_MASTER_KEY` 而非空字串：

- ✅ 讀檔的人**立刻**看到要設哪個 env var，不必去 source 追 fallback 預設
- ✅ 未來若有部署想換 env var 名（如多租戶 `TENANT_A_MASTER_KEY` / `TENANT_B_MASTER_KEY`），改 XML 即可
- ⚠️ 與「code default 也是 `BEE_MASTER_KEY`」重複，但顯式 > 隱式，多寫一行不算成本

### 為何不在 framework 層強制「production 必須 override demo key」

- framework 無法可靠判別「目前是 production 還是 sample」（環境變數 / `ASPNETCORE_ENVIRONMENT` 都可被偽造或忘記設定）
- 即使能判別，硬性 throw 會誤殺合法情境（如 staging 想跑 demo data）
- 真正的防護線是 **deployment review process**：每個 production deploy 應有 secret 注入檢查清單，與 framework 層的 enforcement 無關

依賴 README 警語 + CHANGELOG migration note 提醒使用者，配合 `samples/README.md` 的 master key 段落明確區隔 demo / production 流程。

## 替代方案（已評估後不採納）

1. **保留 `File` 為預設，新增 `Environment` 選項供使用者切換**
   - 拒絕原因：使用者必須主動知道「**有這個更好的選項**」並動手切換 — 業界主流早已是 env var，預設值不對齊等於把 onboarding 摩擦留給每個新使用者

2. **完全移除 `MasterKeySourceType.File`**
   - 拒絕原因：air-gapped 部署、本機開發等場景仍有合理使用情境；移除違反「不破壞既有 deployment」原則

3. **autoCreate per-run（記憶體生成）**
   - 拒絕原因：見〈為何不選「全自動 generate 新 key」〉

4. **手動 export，無 auto-set demo key**
   - 拒絕原因：見〈為何不選「手動 export」〉

5. **demo key 改成「per-developer 自動 generate 並寫進 user-local 設定」**
   - 拒絕原因：過度設計；hardcoded demo key 與 hardcoded demo 帳密（`demo` / `demo`）同等風險，已可接受。Per-developer generation 引入額外的 keyring / user profile / cross-machine sync 複雜度，不值得

6. **整合 `dotnet user-secrets`**
   - 拒絕原因：`user-secrets` 是 ASP.NET Core dev-time 機制，將 secret 移到 user profile 內 JSON 檔，**仍是「config in file」**，與 env var 路徑分歧；且非所有 Bee 部署都跑在 ASP.NET Core 內（QuickStart.Console 純 console、未來其他 host 可能也不是）

## 後果

### 部署矩陣

| 部署情境 | 動作 |
|---------|------|
| **新部署，無既有 `SystemSettings.xml`** | host 啟動前 `export BEE_MASTER_KEY=<base64>`，預設 code default 拿到 |
| **新部署，自寫 `SystemSettings.xml`** | 預設範本內已是 `<Type>Environment</Type><Value>BEE_MASTER_KEY</Value>` |
| **既有部署，`<Type>File</Type>` 已 explicit 設定** | **不需任何動作**，繼續從檔案讀 |
| **既有部署，想遷移到 env var** | 兩步：(1) `export BEE_MASTER_KEY="$(cat $DEFINE_PATH/Master.key)"`；(2) 改 XML 為 `<Type>Environment</Type>` |
| **Sample / test 本機跑** | **不需任何動作**，bootstrap auto-set demo key |
| **CI fresh checkout** | **不需任何動作**，bootstrap auto-set demo key（每次 fresh，無 file artifact 殘留）|

### 安全模型變化

| 維度 | 改變前（File 預設） | 改變後（Environment 預設） |
|------|------------------|--------------------------|
| Secret 存放位置 | host 檔案系統（`$DefinePath/Master.key`） | host 進程環境變數 |
| Secret 在 image / build artifact 內 | 可能（若 COPY 進去）| 否（env var 由 deployment 注入） |
| Secret 在 backup / log 內 | 風險：檔案 backup 流程可能誤含 | 風險：env var 可能被 `ps auxe` / `/proc/<pid>/environ` 讀到（需 host-level 防護） |
| Container 部署 | 需 mount volume 或 build step | 直接 `--env-file` / `-e BEE_MASTER_KEY=...` |
| Multi-instance 同步 | 需檔案分發 | 同一 env var 由 secret store 注入所有節點 |
| Secret rotation | 改檔 → 所有節點同步 → 重啟 | 改 secret store → restart pods（cloud-native standard flow）|

兩種方式各有 attack surface，**env var 路徑與業界 secret 管理工具對齊**是主要動機，不是「絕對更安全」。

### 對既有部署的零成本承諾

- 既有 `<Type>File</Type>` deployment：**完全無改動需要**
- 既有 `Master.key` 檔案：可繼續用，不需刪
- `MasterKeyProvider` 讀取邏輯：File / Environment 分支皆既有實作，無 breaking
- CHANGELOG 內已寫明 migration 步驟與「不需動」的情境

### CHANGELOG 標記為 breaking

依嚴格 SemVer，「**預設值改變影響新部署**」屬 breaking change（即使既有部署不受影響）。`CHANGELOG.md` v4.6.0 已標 **breaking** 並附 migration 指引；pre-stable 政策下版本以 minor (4.6.0) 發佈，與 v4.4 / v4.5 一致。

## 相關連結

- [計畫：MasterKeySource 預設改為環境變數](../archive/plan-masterkey-default-env-var.md) — 實作 plan，含變更清單與驗證方式（已封存）
- `src/Bee.Definition/Settings/SystemSettings/MasterKeySource.cs` — config model 與 ctor 預設
- `src/Bee.Definition/Security/MasterKeyProvider.cs` — File / Environment 分支讀取邏輯
- `samples/Bee.Samples.Shared/DemoCredentials.cs` — `DemoMasterKey` hardcoded 常數
- `tests/Bee.Tests.Shared/TestProcessBootstrap.cs` — `TestMasterKey` 常數與 bootstrap auto-set 邏輯
- [samples/README.zh-TW.md](../../samples/README.zh-TW.md) — master key 段落，標明 demo vs production 區隔
- [12-factor app: Config](https://12factor.net/config) — 對齊的業界原則

## 不在範圍

- **Cloud secret manager 整合樣板（K8s Secret / Vault / AWS Secrets Manager helper）** — 視未來實際 production deployment 需求另開 plan
- **Secret rotation flow** — env var 改變後的 process restart / hot-reload 機制，目前依賴 deployment 工具，framework 層不介入
- **多租戶 / per-tenant master key** — 目前 framework 單一 master key 模型尚未到分租戶階段，未來若需要再開 ADR
- **`MasterKeySourceType.File` 退場時程** — 短期內持續維護，移除日期視 production 部署遷移進度評估
- **`Value` 內容用第二層間接（如 `$BEE_MASTER_KEY_FILE` 指向檔案路徑、再從檔案讀）** — 為 secret 管理工具產生短暫檔案的場景；目前由 deployment 工具直接 inject env var 已能涵蓋，不引入第二層
