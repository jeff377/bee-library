# 計畫 D：Bee.Business/Provider 命名與職能對齊

**狀態：📝 擬定中**

## 背景

`src/Bee.Business/Provider/` 目前收納 4 個類別：

| 類別 | 實作的 Bee.Definition 契約 | 領域 |
|------|------------------------------|------|
| `StaticApiEncryptionKeyProvider` | `IApiEncryptionKeyProvider` | 安全 / API 加密 |
| `DynamicApiEncryptionKeyProvider` | `IApiEncryptionKeyProvider` | 安全 / API 加密 |
| `LoginAttemptTracker` | `ILoginAttemptTracker` | 安全 / 登入鎖定 |
| `CacheDataSourceProvider` | `ICacheDataSourceProvider` | 快取 / 資料來源 |

## 問題分析

從不同視角看，這個資料夾的設計有兩種解讀：

### 解讀 A：依「角色」歸類（目前狀態，可接受）

`Bee.Business.Provider` 是「**`Bee.Definition.*` 中各種 `I*Provider` / `I*Tracker` 抽象的具體實作集中地**」。命名空間反映的是「角色」（這些都是 abstraction 的具體 implementation），而非「領域」。

類比 .NET 中許多 `*Providers/` 資料夾的慣例（如 ASP.NET Core 的 `Microsoft.Extensions.FileProviders`、`Microsoft.Extensions.Logging.Console`）。

**優點**：
- 設計意圖清楚（這是「實作層」）
- 新增實作有明確去處
- 4 份檔案分散到 2-3 個子目錄反而會稀釋

**缺點**：
- 名稱 `Provider`（單數）容易被誤讀為「單一 provider 的命名空間」
- `LoginAttemptTracker` 無 `Provider` 後綴，讀者進入該資料夾會疑惑

### 解讀 B：依「領域」歸類（理論上更精準）

按業務領域分：
- `Bee.Business.Security/`：`StaticApiEncryptionKeyProvider`、`DynamicApiEncryptionKeyProvider`、`LoginAttemptTracker`
- `Bee.Business.Caching/` 或併入 `System/`：`CacheDataSourceProvider`

**優點**：
- 命名空間直接表達領域
- 與 `Bee.Definition.Security/`、`Bee.Business.System/` 對稱

**缺點**：
- `Bee.Business/` 已有 `System/`、`Form/`、`Validator/` 等子命名空間，再增加會稀釋
- 4 份檔案切成 2-3 個子目錄，每個都很瘦
- 失去「實作集中地」的設計意圖

## 三種選項

### 選項 1：完全不動（保持現狀）

理由：4 份檔案數量不大，現有「角色歸類」設計可接受。

### 選項 2：改名 `Provider` → `Providers`（小改）

只改資料夾與命名空間複數化：
- `src/Bee.Business/Provider/` → `src/Bee.Business/Providers/`
- `Bee.Business.Provider` → `Bee.Business.Providers`

**動作**：純命名空間改名，不動檔案內容（除 namespace 行）。

**好處**：複數化更清楚表達「容器」語意（多個 provider 集中）。

**對外 API**：是 breaking change，但很表面。

### 選項 3：依領域拆分（解讀 B 的方案）

| 來源 | 目標命名空間 |
|------|-------------|
| `StaticApiEncryptionKeyProvider` | `Bee.Business.Security` |
| `DynamicApiEncryptionKeyProvider` | `Bee.Business.Security` |
| `LoginAttemptTracker` | `Bee.Business.Security` |
| `CacheDataSourceProvider` | `Bee.Business.System`（或新建 `Bee.Business.Caching`） |

**好處**：與 `Bee.Definition.Security` 對稱。

**壞處**：4 份檔案散成 2 處，「實作集中」的設計意圖消失。

## 推薦方案

**選項 2（改名為 `Providers` 複數化）**。

理由：
1. 保留「實作集中地」的設計意圖（解讀 A 仍成立）
2. 單純複數化能消除「單一 provider 命名空間」的誤讀
3. 影響面最小（4 份檔案搬移 + namespace 更名）
4. 與其他現存命名空間（`Bee.Business.Form`、`Bee.Business.System`、`Bee.Business.Validator`）保持對稱風格 ── 這些也都是單數，反映「該領域的東西放這裡」。但 `Provider` 不同，它指的是「多個 provider」的集合，複數更精準

亦可接受**選項 1**（完全不動），畢竟 4 份檔案集中放並無大礙。

不推薦**選項 3**，因為拆散了設計意圖，且 4 → 2-3 個子目錄收益不大。

## 搬移清單（若採選項 2）

| 來源 | 目標 |
|------|------|
| `src/Bee.Business/Provider/CacheDataSourceProvider.cs` | `src/Bee.Business/Providers/CacheDataSourceProvider.cs` |
| `src/Bee.Business/Provider/StaticApiEncryptionKeyProvider.cs` | `src/Bee.Business/Providers/StaticApiEncryptionKeyProvider.cs` |
| `src/Bee.Business/Provider/DynamicApiEncryptionKeyProvider.cs` | `src/Bee.Business/Providers/DynamicApiEncryptionKeyProvider.cs` |
| `src/Bee.Business/Provider/LoginAttemptTracker.cs` | `src/Bee.Business/Providers/LoginAttemptTracker.cs` |

每個檔案內 `namespace Bee.Business.Provider` → `namespace Bee.Business.Providers`。

## 影響面

- 移動檔案：4 份
- 跨 package 預估更新 `using` 檔案數：< 20 份（待 grep 確認）

### 對外 API breaking change 對應表

| 舊 namespace | 新 namespace |
|-------------|-------------|
| `Bee.Business.Provider` | `Bee.Business.Providers` |

不升版號，release notes 標註 `using Bee.Business.Provider;` → `using Bee.Business.Providers;`。

## 執行步驟

1. **重命名目錄與命名空間**
   1. `git mv src/Bee.Business/Provider src/Bee.Business/Providers`
   2. `sed -i '' 's/namespace Bee\.Business\.Provider/namespace Bee.Business.Providers/'` 套用至搬移後的 4 份檔案
   3. 全 repo 取代 `using Bee.Business.Provider;` → `using Bee.Business.Providers;`
2. **驗證**
   1. `dotnet build --configuration Release` 全綠
   2. `./test.sh` 全綠
3. **文件同步**
   1. 更新 `src/Bee.Business/README.md` 與 `README.zh-TW.md` Directory Structure

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| 純表面改名，價值有限，可能不值得做 breaking change | 接受此說法；可選擇執行 plan A / B 時順帶（影響面已含 breaking）；或選擇選項 1（完全不動） |
| 改名後 `Bee.Business.Form` / `Bee.Business.System` / `Bee.Business.Validator` 仍是單數，與 `Providers` 不對稱 | 接受此非對稱：Form / System / Validator 是「該領域內容放這裡」，單數合理；Providers 是「多個 provider 集中」，複數合理 — 兩者語意層次不同 |

### 不採用的替代方案

1. **選項 3（依領域拆分）**：4 份檔案散成 2-3 處，每個子目錄太瘦；失去「實作集中」的設計意圖
2. **改名為其他名稱**（如 `Implementations`、`Concretes`）：太抽象，不如 `Providers` 直接

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠
- [ ] `src/Bee.Business/Provider/` 已不存在；`Providers/` 含 4 份檔案
- [ ] 全 repo 不再出現 `Bee.Business.Provider`
- [ ] 套件版本號未變動（仍為 4.0.x）
- [ ] CI 通過

## 待決事項

執行前請確認：
1. 採用哪個選項？（推薦選項 2，亦可選擇選項 1 完全不動）
2. 是否與 plan A / B 合併執行？（可一併打包到單一 PR / commit；獨立執行也無妨，影響面小）
