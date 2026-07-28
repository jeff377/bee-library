# Bee.NET 文件

[English](README.md)

本目錄存放 Bee.NET 框架對外公開的開發者文件。以下所列文件皆為雙語版（繁體中文 + 英文），英文版為主檔（`xxx.md`），繁體中文版為 `xxx.zh-TW.md`。

---

## 入門

| 文件 | 說明 |
|------|------|
| [架構總覽](architecture-overview.zh-TW.md) | 定義導向架構（Definition-Driven Architecture）的設計理念與實踐模式 |
| [術語對照表](terminology.zh-TW.md) | 框架中英文術語對照參考 |
| [專案相依性全景圖](dependency-map.zh-TW.md) | 17 個 `src/` 專案的相依關係視覺化 |

## 開發指引

| 文件 | 說明 |
|------|------|
| [端到端開發指引](development-cookbook.zh-TW.md) | 從定義到 API 的核心開發流程，含初始化順序、請求管線、ExecFunc 模式 |
| [開發限制與反模式](development-constraints.zh-TW.md) | 框架的設計限制與禁止事項，供 AI Coding 工具參考 |
| [JSON-RPC 前端整合指引](jsonrpc-frontend-integration.zh-TW.md) | 從 JavaScript / TypeScript 前端呼叫 Bee.NET JSON-RPC API（前端無 .NET）— wire format、認證流程、TS wrapper |
| [權限與授權指南](permission-authorization.zh-TW.md) | 兩層授權（動作 gate + record scope）的設定與運作：PermissionModels、FormField.ScopeRole、roles/grants 三表、讀取過濾與寫入端權威 re-query |

## 資料庫

| 文件 | 說明 |
|------|------|
| [資料庫命名規範](database-naming-conventions.zh-TW.md) | 表名、欄位、索引、系統欄位的命名規則；跨資料庫大小寫敏感性對照 |
| [框架保留命名](framework-reserved-names.zh-TW.md) | 框架擁有的 `st_*` 系統表與保留 `progId` registry |
| [DatabaseSettings 與 DbCategorySettings 指引](database-settings-guide.zh-TW.md) | 兩個資料庫相關設定檔的結構、存取方式與運作流程 |
| [資料庫 Schema 升級](database-schema-upgrade.zh-TW.md) | Schema 升級流程與策略 |
| [資料庫方言差異（DDL）](database-dialect-differences.zh-TW.md) | 跨方言的 DDL 規則與例外（預設值、nullability、引號、AutoIncrement）；文字/數值欄為何 NOT NULL |

## 設計概念

| 文件 | 說明 |
|------|------|
| [API ↔ BO 契約設計](api-bo-contract-design.zh-TW.md) | API 契約三層分離（Contracts / API Type / BO Type） |
| [API 方法參考](api-method-reference.zh-TW.md) | 透過 JSON-RPC 對外公開的所有 BO 方法單頁總覽，含 `[ApiAccessControl]` 設定與用途 |
| [FormMap](formmap.zh-TW.md) | Bee.Db 採用的資料庫存取模式，以 FormSchema 為單位動態產生 SQL |
| [時間型別總覽：Date、DateTime、Time](temporal-types.zh-TW.md) | 跨層對照參考：三者如何選擇，以及各自在資料庫、`DataSet`、程式碼與三種序列化中的承載方式 |
| [日曆日與時間點的欄位語意](date-semantics.zh-TW.md) | `FieldDbType.Date` 欄位如何在 wire 上自我描述、.NET 與 JS/TS 端如何讀取，以及自寫 SQL 時如何宣告 |
| [時刻欄位](time-semantics.zh-TW.md) | 何時使用 `FieldDbType.Time`、定寬 `"HH:mm"` 的儲存形式、如何讀成 `TimeOnly`，以及它為何不是時距 |
| [時區處理](datetime-timezone.zh-TW.md) | UTC 儲存、轉換發生在哪裡、使用者時區的設定，以及自寫 SQL 與非 .NET 用戶端該做什麼 |

---

## 其他子目錄

以下子目錄不列入本 README 的主清單，視需要直接參閱：

- **`adr/`** — 架構決策紀錄，記錄重大設計決策的背景與理由
- **`plans/`** — 設計階段或已完成初始計畫的設計文件。屬**階段性工作文件、非參考資料**：舊 plan 未必符合現行行為。本目錄外的文件一律不連結進來，請以上方各文件為準
- **`repo-ops/`** — 本 repo 的維運文件（CI / 分支保護），與框架使用者無關
