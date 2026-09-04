# Bee.Business

> 商業邏輯層，提供身分驗證、Session 管理、定義資料存取，以及自訂函式執行框架。

[English](README.md)

## 架構定位

- **層級**：商業邏輯層（Business Logic Layer）
- **在相依圖中的位置**：見[專案相依性全景圖](../../docs/dependency-map.zh-TW.md)。**此處不逐一列出** —— 權威來源是 csproj，而散落在每份套件 README 的散文拷貝會漂且無人察覺。它們確實漂了：`Bee.Hosting` 抽出後，有四份 README 的下游數個月都沒把它補上。

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 自訂函式執行

- `IBusinessObject` -- 基底介面，公開 `ExecFunc`（需驗證）與 `ExecFuncAnonymous`（匿名存取）進入點
- `ExecFuncArgs` / `ExecFuncResult` -- 自訂函式分派的輸入/輸出契約
- `ExecFuncAccessControlAttribute` -- 方法層級 Attribute，宣告每個函式的身分驗證需求

### 系統操作

- `ISystemBusinessObject` -- 跨 BO 契約：`Login`、`CreateSession`、`EnterCompany`、`LeaveCompany`、`Logout`。純 API 方法（`Ping`、`GetFormSchema`、`GetFormLayout`、`GetLanguage` 等）在具象類別上 public + `[ApiAccessControl]`，刻意不放進此介面
- 每個操作對應 Args/Result 組合：`LoginArgs`/`LoginResult`、`PingArgs`/`PingResult`、`CreateSessionArgs`/`CreateSessionResult`、`GetDefineArgs`/`GetDefineResult`、`SaveDefineArgs`/`SaveDefineResult`、`GetPackageArgs`/`GetPackageResult`、`GetCommonConfigurationArgs`/`GetCommonConfigurationResult`

### 表單操作

- `IFormBusinessObject` -- 表單層級商業邏輯介面，繼承 `IBusinessObject`，用於 FormSchema 驅動的操作

### 身分驗證與安全

- `LoginAttemptTracker` -- 記憶體內帳戶鎖定機制（預設：連續 5 次失敗觸發 15 分鐘鎖定）
- `AccessTokenValidator` -- 驗證已認證 API 呼叫的存取權杖
- `StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider` -- 可插拔的加密金鑰策略，用於 API Payload 保護

### 資料與快取

- `CacheDataSourceProvider` -- 為商業邏輯提供快取資料來源
- `BusinessArgs` / `BusinessResult` -- 跨商業操作共用的基底輸入/輸出型別

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `IBusinessObject` | BO 基底介面（`ExecFunc`、`ExecFuncAnonymous`） |
| `ISystemBusinessObject` | 跨 BO 系統操作（純 API 方法留在具象類別） |
| `IFormBusinessObject` | 表單層級商業邏輯介面 |
| `BusinessObjectFactory` | 建立 BO 實例的工廠 |
| `LoginAttemptTracker` | 連續失敗後的帳戶鎖定 |
| `AccessTokenValidator` | 存取權杖驗證 |
| `StaticApiEncryptionKeyProvider` | 固定加密金鑰策略 |
| `DynamicApiEncryptionKeyProvider` | 每次 Session 動態加密金鑰策略 |
| `ExecFuncArgs` / `ExecFuncResult` | 自訂函式分派契約 |
| `ExecFuncAccessControlAttribute` | 方法層級身分驗證需求宣告 |
| `BusinessArgs` / `BusinessResult` | 操作的基底輸入/輸出型別 |

## 設計慣例

- **命令模式（Command Pattern）** -- `ExecFunc` 透過反射以名稱調用方法，動態分派自訂商業邏輯。
- **工廠模式（Factory Pattern）** -- `BusinessObjectFactory` 根據存取權杖與上下文建立 `SystemBusinessObject` 和 `FormBusinessObject` 實例。
- **樣板方法模式（Template Method）** -- `BusinessObject` 基底類別定義執行骨架，子類別覆寫 `DoExecFunc()` 實作特定邏輯。
- **策略模式（Strategy Pattern）** -- 加密金鑰提供者（`StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider`）為可替換的實作。
- **Attribute 驅動存取控制** -- `ExecFuncAccessControlAttribute` 宣告每個方法的身分驗證需求，於分派時檢查。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Business/
  Attributes/       # ExecFuncAccessControlAttribute
  Form/             # 表單層 BO 原型（命名空間 Bee.Business.Form）
                    # IFormBusinessObject、FormBusinessObject、FormExecFuncHandler
  System/           # 系統層 BO 原型（命名空間 Bee.Business.System）
                    # ISystemBusinessObject、SystemBusinessObject、SystemExecFuncHandler，
                    # 以及系統操作的 Args/Result 組合
                    # （Login、Ping、CreateSession、GetDefine、SaveDefine、
                    #   GetPackage、GetCommonConfiguration）
  Providers/        # StaticApiEncryptionKeyProvider、DynamicApiEncryptionKeyProvider、
                    # CacheDataSourceProvider
  Security/         # LoginAttemptTracker（記憶體內帳戶鎖定追蹤器）
  Validator/        # AccessTokenValidator
  *.cs（根目錄）     # BusinessObject、BusinessObjectFactory、IBusinessObject、
                    # IExecFuncHandler、ExecFuncArgs、ExecFuncResult、
                    # BusinessArgs、BusinessResult
```
