# Bee.NET 文件

[English](README.md)

本目錄存放 Bee.NET 框架對外公開的開發者文件。以下所列文件皆為雙語版（繁體中文 + 英文），英文版為主檔（`xxx.md`），繁體中文版為 `xxx.zh-TW.md`。

---

## 入門

| 文件 | 說明 |
|------|------|
| [架構總覽](architecture-overview.zh-TW.md) | 定義導向架構（Definition-Driven Architecture）的設計理念與實踐模式 |
| [術語對照表](terminology.zh-TW.md) | 框架中英文術語對照參考 |
| [專案相依性全景圖](dependency-map.zh-TW.md) | 16 個 `src/` 專案的相依關係視覺化 |

## 開發指引

| 文件 | 說明 |
|------|------|
| [端到端開發指引](development-cookbook.zh-TW.md) | 從定義到 API 的核心開發流程，含初始化順序、請求管線、ExecFunc 模式 |
| [開發限制與反模式](development-constraints.zh-TW.md) | 框架的設計限制與禁止事項，供 AI Coding 工具參考 |
| [JSON-RPC 前端整合指引](jsonrpc-frontend-integration.zh-TW.md) | 從 JavaScript / TypeScript 前端呼叫 Bee.NET JSON-RPC API（前端無 .NET）— wire format、認證流程、TS wrapper |

## 資料庫

| 文件 | 說明 |
|------|------|
| [資料庫命名規範](database-naming-conventions.zh-TW.md) | 表名、欄位、索引、系統欄位的命名規則；跨資料庫大小寫敏感性對照 |
| [DatabaseSettings 與 DbCategorySettings 指引](database-settings-guide.zh-TW.md) | 兩個資料庫相關設定檔的結構、存取方式與運作流程 |
| [資料庫 Schema 升級](database-schema-upgrade.zh-TW.md) | Schema 升級流程與策略 |

## 設計概念

| 文件 | 說明 |
|------|------|
| [API ↔ BO 契約設計](api-bo-contract-design.zh-TW.md) | API 契約三層分離（Contracts / API Type / BO Type） |
| [FormMap](formmap.zh-TW.md) | Bee.Db 採用的資料庫存取模式，以 FormSchema 為單位動態產生 SQL |

---

## 其他子目錄

以下子目錄不列入本 README 的主清單，視需要直接參閱：

- **`adr/`** — 架構決策紀錄（ADR-001 至 ADR-010+），記錄重大設計決策的背景與理由
- **`plans/`** — 設計階段或已完成初始計畫的設計文件
- **`repo-ops/`** — 本 repo 的維運文件（CI / 分支保護），與框架使用者無關
