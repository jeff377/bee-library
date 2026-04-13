# Bee.Business

> 商業邏輯層，提供身分驗證、Session 管理、定義資料存取，以及自訂函式執行框架。

[English](README.md)

## 架構定位

- **層級**：商業邏輯層（Business Logic Layer）
- **下游**（依賴此專案者）：`Bee.Api.Core`（透過 Provider 調用）
- **上游**（此專案依賴）：`Bee.Api.Contracts`、`Bee.Definition`、`Bee.Repository.Abstractions`

## 目標框架

- `netstandard2.0` -- 廣泛相容 .NET Framework 4.6.1+ 與 .NET Core 2.0+
- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 自訂函式執行

- `IBusinessObject` -- 基底介面，公開 `ExecFunc`（需驗證）與 `ExecFuncAnonymous`（匿名存取）進入點
- `ExecFuncArgs` / `ExecFuncResult` -- 自訂函式分派的輸入/輸出契約
- `ExecFuncAccessControlAttribute` -- 方法層級 Attribute，宣告每個函式的身分驗證需求
- `BusinessFunc` -- 商業邏輯操作的輔助工具

### 系統操作

- `ISystemBusinessObject` -- 系統層級操作：`CreateSession`、`GetDefine`、`SaveDefine`
- 每個操作對應 Args/Result 組合：`LoginArgs`/`LoginResult`、`PingArgs`/`PingResult`、`CreateSessionArgs`/`CreateSessionResult`、`GetDefineArgs`/`GetDefineResult`、`SaveDefineArgs`/`SaveDefineResult`、`CheckPackageUpdateArgs`/`CheckPackageUpdateResult`、`GetPackageArgs`/`GetPackageResult`、`GetCommonConfigurationArgs`/`GetCommonConfigurationResult`

### 表單操作

- `IFormBusinessObject` -- 表單層級商業邏輯介面，繼承 `IBusinessObject`，用於 FormSchema 驅動的操作

### 身分驗證與安全

- `LoginAttemptTracker` -- 記憶體內帳戶鎖定機制（預設：連續 5 次失敗觸發 15 分鐘鎖定）
- `AccessTokenValidationProvider` -- 驗證已認證 API 呼叫的存取權杖
- `StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider` -- 可插拔的加密金鑰策略，用於 API Payload 保護

### 資料與快取

- `CacheDataSourceProvider` -- 為商業邏輯提供快取資料來源
- `BusinessArgs` / `BusinessResult` -- 跨商業操作共用的基底輸入/輸出型別

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `IBusinessObject` | BO 基底介面（`ExecFunc`、`ExecFuncAnonymous`） |
| `ISystemBusinessObject` | 系統操作（`CreateSession`、`GetDefine`、`SaveDefine`） |
| `IFormBusinessObject` | 表單層級商業邏輯介面 |
| `BusinessObjectProvider` | 建立 BO 實例的工廠 |
| `LoginAttemptTracker` | 連續失敗後的帳戶鎖定 |
| `AccessTokenValidationProvider` | 存取權杖驗證 |
| `StaticApiEncryptionKeyProvider` | 固定加密金鑰策略 |
| `DynamicApiEncryptionKeyProvider` | 每次 Session 動態加密金鑰策略 |
| `ExecFuncArgs` / `ExecFuncResult` | 自訂函式分派契約 |
| `ExecFuncAccessControlAttribute` | 方法層級身分驗證需求宣告 |
| `BusinessArgs` / `BusinessResult` | 操作的基底輸入/輸出型別 |

## 設計慣例

- **命令模式（Command Pattern）** -- `ExecFunc` 透過反射以名稱調用方法，動態分派自訂商業邏輯。
- **工廠模式（Factory Pattern）** -- `BusinessObjectProvider` 根據存取權杖與上下文建立 `SystemBusinessObject` 和 `FormBusinessObject` 實例。
- **樣板方法模式（Template Method）** -- `BusinessObject` 基底類別定義執行骨架，子類別覆寫 `DoExecFunc()` 實作特定邏輯。
- **策略模式（Strategy Pattern）** -- 加密金鑰提供者（`StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider`）為可替換的實作。
- **Attribute 驅動存取控制** -- `ExecFuncAccessControlAttribute` 宣告每個方法的身分驗證需求，於分派時檢查。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Business/
  Attributes/       # ExecFuncAccessControlAttribute
  BusinessObjects/  # BusinessObject、SystemBusinessObject、FormBusinessObject、
                    # BusinessObjectProvider、IExecFuncHandler、
                    # SystemExecFuncHandler、FormExecFuncHandler
  Provider/         # LoginAttemptTracker、StaticApiEncryptionKeyProvider、
                    # DynamicApiEncryptionKeyProvider、CacheDataSourceProvider
  System/           # 系統操作的 Args/Result 組合
                    # （Login、Ping、CreateSession、GetDefine、SaveDefine、
                    #   CheckPackageUpdate、GetPackage、GetCommonConfiguration）
  Validator/        # AccessTokenValidationProvider
  *.cs（根目錄）     # IBusinessObject、ISystemBusinessObject、IFormBusinessObject、
                    # BusinessFunc、ExecFuncArgs、ExecFuncResult、
                    # BusinessArgs、BusinessResult
```
