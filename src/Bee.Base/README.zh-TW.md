# Bee.Base

> 跨層共用工具程式庫，提供型別轉換、加密、序列化、集合、追蹤與背景服務等功能。

[English](README.md)

## 架構定位

- **層級**：基礎設施層（Bee.NET 框架最底層）
- **下游**（依賴此專案者）：幾乎所有其他 `Bee.*` 專案（`Bee.Definition`、`Bee.Business`、`Bee.Api.Core` 等）
- **上游**（此專案依賴）：無（使用執行階段內建的 `System.Text.Json`）

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 型別轉換與字串工具

- `BaseFunc` -- 安全型別轉換，支援預設值回退（`CInt`、`CStr`、`CBool` 等）
- `StrFunc` -- 字串操作輔助方法（編碼、格式化、比較）
- `DateTimeFunc` -- 日期工具，包含民國曆支援

### 加密與安全

- `AesCbcHmacCryptor` -- AES-256-CBC 加密搭配 HMAC-SHA256 驗證（每次加密使用隨機 IV）
- `RsaCryptor` -- RSA 非對稱加密
- `PasswordHasher` -- PBKDF2-SHA256 密碼雜湊
- `FileHashValidator` -- 透過 SHA-256 驗證檔案完整性
- `AesCbcHmacKeyGenerator` -- 加密金鑰產生器

### 序列化與壓縮

- `SerializeFunc` -- 統一的 XML / JSON 序列化，採用 `System.Text.Json`
- `XmlSerializerCache` -- 快取 XML 序列化器實例，避免重複反射
- `GzipFunc` -- Gzip 壓縮 / 解壓縮，用於 Payload 處理

### 集合

- `KeyCollectionBase<T>` -- 泛型鍵值集合基底類別
- `StringHashSet` -- 可控制大小寫的字串 HashSet
- `CollectionExtensions` -- LINQ 風格的集合擴充方法

### 資料存取輔助

- `DataTable` / `DataSet` / `DataRow` 擴充方法，簡化 ADO.NET 操作
- `FieldDbType` 與 `DbTypeConverter` -- 資料庫型別對應工具

### 追蹤與診斷

- `Tracer` / `TraceContext` -- 結構化診斷追蹤
- `TraceListener` / `TraceWriter` -- 可插拔的追蹤輸出目標

### 背景服務

- `BackgroundService` -- 長時間執行非同步工作的基底類別
- `BackgroundAction` -- 輕量級的射後不理（fire-and-forget）任務封裝

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `BaseFunc` | 安全型別轉換（含預設值） |
| `StrFunc` | 字串編碼、格式化、比較 |
| `DateTimeFunc` | 日期工具與民國曆 |
| `AesCbcHmacCryptor` | 認證式對稱加密 |
| `PasswordHasher` | 密碼雜湊（PBKDF2-SHA256） |
| `SerializeFunc` | XML / JSON 序列化 |
| `IObjectSerialize` | 序列化提供者介面 |
| `IKeyObject` | 跨層鍵值實體介面 |
| `Tracer` | 診斷追蹤進入點 |
| `BackgroundService` | 非同步背景工作基底類別 |

## 設計慣例

- **靜態工具類別** -- `BaseFunc`、`StrFunc`、`DateTimeFunc` 以靜態方法公開功能，不持有實例狀態。
- **常數時間比較** -- `CompareBytes` 用於 HMAC / 雜湊驗證，防止時序攻擊（Timing Attack）。
- **雙框架條件編譯** -- 使用 `#if NETSTANDARD2_0` 處理不同執行階段 API 的差異。
- **介面導向擴充** -- 序列化透過 `IObjectSerialize` 與 `IObjectSerializeProcess` 抽象化。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Base/
  Attributes/          # TreeNodeAttribute、TreeNodeIgnoreAttribute
  BackgroundServices/  # BackgroundService、BackgroundAction
  Collections/         # KeyCollectionBase<T>、StringHashSet、CollectionExtensions
  Data/                # DataTable/DataSet 擴充、FieldDbType、DbTypeConverter
  Security/            # AES、RSA、PBKDF2、檔案雜湊工具
  Serialization/       # JSON/XML 序列化、GZip 壓縮
  Tracing/             # Tracer、TraceContext、TraceListener、TraceWriter
  *.cs（根目錄）        # BaseFunc、StrFunc、DateTimeFunc、FileFunc、HttpFunc、
                       # IPValidator、SysInfo、ApiException、IKeyObject 等
```
