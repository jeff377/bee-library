# 計畫：MasterKeySource 預設改為環境變數

**狀態：✅ 已完成（2026-05-26）**

## 背景

`SystemSettings.SecurityKeySettings.MasterKeySource` 目前預設為
`Type=File` + `Value=Master.key`，部署時會在 `DefinePath` 下產生
`Master.key` 檔案。這在 production 場景有幾個痛點：

- 違反 [12-factor app](https://12factor.net/config) 的「config in env」原則
- container 化部署需要 mount 額外 volume 或加 build step copy 檔案
- 多實例部署需要同步檔案內容到所有節點
- 檔案散落在檔案系統，secret 管理工具（K8s Secret、Vault、AWS Secrets Manager）較難整合

改成 env var 預設後：

- container / Kubernetes / cloud function 直接吃 env var
- 多實例共享一份 secret 設定
- 無檔案 artifacts，符合「無狀態部署」習慣
- 既有 user explicit `<Type>File</Type>` 設定不變、向下相容

## 設計選擇

### 選 A：bootstrap auto-set demo key（已採用）

sample / test bootstrap 偵測 `BEE_MASTER_KEY` env var 未設時，**自動 set
一個 hardcoded demo key**，模仿現有 `DemoCredentials.UserId="demo"` 的
hardcoded demo 設計。

**取捨：**

- ✅ Sample 與 test 維持零設定，`dotnet run` / `./test.sh` 直接跑
- ✅ 跨 process run 使用同一把 key，`quickstart.db` 等持久化資料不會解不開
- ⚠️ Demo key 進 source code — 但 sample / test 本來就是 demo，與「demo / demo」
  hardcoded 帳密同等風險，可接受
- ⚠️ Production host 必須**明確 override** demo key — README 要寫清楚

### 未選擇的選項

- **選 B**（手動 export）：production-like 但每個新 user 都會踩「忘記 set」
  的坑，sample first-run experience 變差
- **選 C**（autoCreate 每次新 generate）：跨 process key 不一致，
  `quickstart.db` 內加密資料跨 run 解不開

## 變更清單

### 1. Code default

**檔案**：`src/Bee.Definition/Settings/SystemSettings/MasterKeySource.cs`

`Type` 預設由 `MasterKeySourceType.File` 改為 `MasterKeySourceType.Environment`。
影響：透過 `new MasterKeySource()` 程式化建構時拿到新預設；對既有
explicit `<Type>` 的 XML 無影響。

### 2. sample SystemSettings.xml

**檔案**：`samples/Define/SystemSettings.xml`

```xml
<SecurityKeySettings>
  <MasterKeySource>
    <Type>Environment</Type>
    <Value>BEE_MASTER_KEY</Value>
  </MasterKeySource>
</SecurityKeySettings>
```

`Value` explicit 寫 `BEE_MASTER_KEY` 而非空字串，方便讀檔的人立刻看到
env var 名稱、不必去 source 追 fallback 預設。

### 3. test SystemSettings.xml

**檔案**：`tests/Define/SystemSettings.xml`

同上 — Type 改 Environment、Value 寫 `BEE_MASTER_KEY`。

### 4. sample bootstrap auto-set demo key

**檔案**：`samples/Bee.Samples.Shared/DemoBackend.cs`

`AddBeeBackend` 開頭加：

```csharp
// Demo-only: ensure a master key is available so the bundled demos can
// run with zero setup. Production hosts MUST set BEE_MASTER_KEY via the
// real deployment mechanism (K8s Secret, env file, etc.) — see README.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
{
    Environment.SetEnvironmentVariable("BEE_MASTER_KEY", DemoMasterKey);
}
```

`DemoMasterKey` 放在 `DemoCredentials.cs` 或新建 `DemoMasterKey.cs`（評估後決定，
傾向放 `DemoCredentials.cs` 同檔，保持 demo hardcoded 集中）。
值為固定 Base64 encoded AES-CBC-HMAC 合併金鑰。

### 5. test bootstrap auto-set demo key

**檔案**：`tests/Bee.Tests.Shared/TestProcessBootstrap.cs`

process bootstrap 一開頭就 set env var（在任何測試 class 構造前完成，
避免測試間 race condition）：

```csharp
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
{
    Environment.SetEnvironmentVariable("BEE_MASTER_KEY", TestMasterKey);
}
```

`TestMasterKey` 用獨立常數（不共用 sample 的 `DemoMasterKey`，避免測試與
sample 互相 leak），值為固定 Base64 編碼。

### 6. 遺留檔案清理

`samples/Define/Master.key` 與 `tests/Define/Master.key` 已在 `.gitignore`
內，**git 不需動作**。本機可手動刪除以避免混淆；CI 環境每次 fresh checkout
不會有殘留。

### 7. CHANGELOG migration note

`4.6.0` release notes 加一段：

> **MasterKeySource default changed to Environment** — new installations now
> default to reading the master key from `$BEE_MASTER_KEY` instead of a
> `Master.key` file. Existing deployments with explicit `<Type>File</Type>`
> in `SystemSettings.xml` are unaffected. To migrate: set `BEE_MASTER_KEY`
> to the Base64 content of your current `Master.key` and update the XML to
> `<Type>Environment</Type>`.

### 8. README 標明 production 必須 override demo key

`samples/QuickStart.Server/README.md`（中英）與 `samples/README.md` 適當位置
加註：「demo backend 為 zero-setup 體驗會自動注入 hardcoded master key；
production host 必須在 process 啟動前以真實的 secret 注入 `BEE_MASTER_KEY`」。

## 驗證方式

1. `dotnet build` 全綠
2. `./test.sh` 全綠（特別注意 master key 相關的測試 — `MasterKeySourceTests`、
   `EncryptionKeyProtectorTests`、`MasterKeyProviderTests` 等）
3. 起 `QuickStart.Server` + `Blazor.Server.Demo` 各一輪，確認 Login + 後續
   Encrypted 流程正常（key exchange 用得到 master key）
4. 確認 process 跑完後**沒有**新生 `Master.key` 檔案（autoCreate 不應觸發）
5. unset env var → 起 sample → 確認 bootstrap auto-set 機制有作用、demo 依然
   能跑

## 不在本 plan 範圍

- 把 demo key 改成「per-developer 自動 generate 並寫進 user-local 設定」
  （過度設計；hardcoded 就夠）
- Cloud secret integration 樣板（K8s Secret / Vault helper）— 視未來需求
  另開 plan
- 移除 `MasterKeySourceType.File` 選項（向下相容必須保留）

## 相關連結

- `src/Bee.Definition/Security/MasterKeyProvider.cs` — 實際讀取邏輯
- `src/Bee.Definition/Settings/SystemSettings/MasterKeySource.cs` — config model
- `samples/Bee.Samples.Shared/DemoCredentials.cs` — hardcoded demo 設計參考
- 預定發佈版本：`4.6.0`（與 [JS 前端整合 + 修正] 等變更一同 ship）
