# Bee.NET 文件

[English](README.md)

本目錄存放 Bee.NET 框架對外公開的開發者文件。以下所列文件皆為雙語版（繁體中文 + 英文），英文版為主檔（`xxx.md`），繁體中文版為 `xxx.zh-TW.md`。

清單**依讀者所處階段排列**，不依主題。每份文件標示**類型**（教學 / 概念 / 指引 / 參考）與**篇幅**（短 < 150 行、中 150–350、長 > 350），開啟前即可判斷投入成本。若習慣依主題找，見文末的[依主題查閱](#依主題查閱)。

**閱讀路徑**

- **第一次接觸** → 讀[開始使用](#1-開始使用)三份即可動手。
- **想理解設計取捨** → 加讀[核心概念](#2-核心概念)。
- **動手中卡住** → 查[開發指引](#3-開發指引)對應那份。
- **寫欄位、命名、查 API** → 直接翻[查詢參考](#4-查詢參考)。

---

## 1. 開始使用

讀完這三份就能建出第一個應用。

| 文件 | 類型 | 篇幅 | 說明 |
|------|------|------|------|
| [快速上手](getting-started.zh-TW.md) | 教學 | 中 | 從零建出第一個 Bee.NET 後端：套件、`DefinePath`、DI 接線、第一張表單與商業物件，再由用戶端呼叫 |
| [架構總覽](architecture-overview.zh-TW.md) | 概念 | 長 | 定義導向架構（Definition-Driven Architecture）的設計理念與實踐模式 |
| [定義檔全景](definition-files-overview.zh-TW.md) | 概念 | 中 | 全部 11 種定義檔的全景圖：各自管什麼、彼此怎麼串、改了哪個會影響哪一層 |

> 遇到不熟的術語？另開一頁擺著[術語對照表](terminology.zh-TW.md)。

## 2. 核心概念

框架為何長成這樣。

| 文件 | 類型 | 篇幅 | 說明 |
|------|------|------|------|
| [FormMap](formmap.zh-TW.md) | 概念 | 中 | Bee.Db 採用的資料庫存取模式：以 FormSchema 為單位動態產生 SQL，以及它為何不是 ORM |
| [API ↔ BO 契約設計](api-bo-contract-design.zh-TW.md) | 概念 | 中 | API 契約三層分離（Contracts / API Type / BO Type）與驅動它的命名慣例 |
| [專案相依性全景圖](dependency-map.zh-TW.md) | 概念 | 短 | 16 個 `src/` 專案的相依關係，以及維持相依圖無環的規則 |

## 3. 開發指引

要做某件事時翻這裡。

| 文件 | 類型 | 篇幅 | 說明 |
|------|------|------|------|
| [端到端開發指引](development-cookbook.zh-TW.md) | 指引 | 長 | 從定義到 API 的核心開發流程：初始化順序、請求管線、ExecFunc 模式、快取失效 |
| [運算式與規則](expression-rules.zh-TW.md) | 指引 | 短 | 在 FormSchema 以宣告式運算式做欄位運算與存檔/刪除前驗證，取代手寫 BO 程式碼 |
| [租戶客製化](customization.zh-TW.md) | 指引 | 中 | 讓某一家公司得到不同的標題、版面或行為，而不分岔套裝定義：該用哪一種機制、各自怎麼寫、以及什麼不能客製 |
| [權限與授權指南](permission-authorization.zh-TW.md) | 指引 | 中 | 兩層授權（動作 gate + record scope）的設定與運作：PermissionModels、`FormField.ScopeRole`、roles/grants 三表、讀取過濾與寫入端權威 re-query；另含部署層授權（管的是整個部署的資產，與公司權限互不授予） |
| [API 金鑰管理](api-key-management.zh-TW.md) | 指引 | 短 | API 金鑰識別的是「呼叫的應用程式」而非使用者：閘門如何自行啟用、誰能管理金鑰、以及輪替流程 |
| [JSON-RPC 前端整合指引](jsonrpc-frontend-integration.zh-TW.md) | 指引 | 長 | 從 JavaScript / TypeScript 前端呼叫 JSON-RPC API（前端無 .NET）：wire format、認證流程、TypeScript wrapper |
| [DatabaseSettings 與 DbCategorySettings 指引](database-settings-guide.zh-TW.md) | 指引 | 長 | 兩個資料庫相關設定檔的結構、存取方式與運作流程 |
| [資料庫 Schema 升級](database-schema-upgrade.zh-TW.md) | 指引 | 中 | 將定義變更同步到線上資料庫：diff → plan → execute 管線、ALTER vs 重建、乾跑 |

## 4. 查詢參考

工作中隨手查。

| 文件 | 類型 | 篇幅 | 說明 |
|------|------|------|------|
| [術語對照表](terminology.zh-TW.md) | 參考 | 長 | 框架中英文術語對照，依層別編排 |
| [API 方法參考](api-method-reference.zh-TW.md) | 參考 | 短 | 透過 JSON-RPC 對外公開的所有 BO 方法單頁總覽，含 `[ApiAccessControl]` 設定與用途 |
| [框架保留命名](framework-reserved-names.zh-TW.md) | 參考 | 短 | 框架擁有的 `st_*` 系統表與保留 `progId` registry |
| [資料庫命名規範](database-naming-conventions.zh-TW.md) | 參考 | 中 | 表名、欄位、索引、系統欄位的命名規則；跨資料庫大小寫敏感性對照 |
| [資料庫方言差異（DDL）](database-dialect-differences.zh-TW.md) | 參考 | 中 | 跨方言的 DDL 規則與例外（預設值、nullability、引號、AutoIncrement）；文字/數值欄為何 NOT NULL |
| [時間型別總覽：Date、DateTime、Time](temporal-types.zh-TW.md) | 參考 | 中 | 三者如何選擇，以及各自在資料庫、`DataSet`、程式碼與三種序列化中的承載方式 |
| [時區處理](datetime-timezone.zh-TW.md) | 參考 | 短 | UTC 儲存、轉換發生在哪裡、使用者時區的設定，以及自寫 SQL 與非 .NET 用戶端該做什麼 |
| [Analyzer 規則](analyzer-rules.zh-TW.md) | 參考 | 短 | 隨套件提供的建置期診斷：規則清單、如何調整嚴重度、版本政策 |
| [開發限制與反模式](development-constraints.zh-TW.md) | 參考 | 中 | 框架的設計限制與禁止事項，亦適合供 AI Coding 工具參考 |

## 5. 深入閱讀

| 目錄 | 說明 |
|------|------|
| [`adr/`](adr/README.md) | 架構決策紀錄 —— 理解「為何這樣設計」的主要來源。索引列出全部 ADR 與其狀態（已採納 / 已取代） |
| [`changelogs/`](changelogs/) | 根 `CHANGELOG.md` 背後的逐版變更明細 |

---

## 依主題查閱

同一批文件依主題分組。一份文件出現在多個主題是刻意設計。

| 主題 | 文件 |
|------|------|
| **資料庫** | [命名規範](database-naming-conventions.zh-TW.md) · [保留命名](framework-reserved-names.zh-TW.md) · [設定指引](database-settings-guide.zh-TW.md) · [Schema 升級](database-schema-upgrade.zh-TW.md) · [方言差異](database-dialect-differences.zh-TW.md) · [FormMap](formmap.zh-TW.md) |
| **定義層** | [定義檔全景](definition-files-overview.zh-TW.md) · [架構總覽](architecture-overview.zh-TW.md) · [運算式與規則](expression-rules.zh-TW.md) · [保留命名](framework-reserved-names.zh-TW.md) |
| **多租戶** | [租戶客製化](customization.zh-TW.md) · [定義檔全景](definition-files-overview.zh-TW.md) · [開發指引](development-cookbook.zh-TW.md) |
| **API 與前端** | [契約設計](api-bo-contract-design.zh-TW.md) · [API 方法參考](api-method-reference.zh-TW.md) · [JSON-RPC 前端整合](jsonrpc-frontend-integration.zh-TW.md) · [權限與授權](permission-authorization.zh-TW.md) · [API 金鑰管理](api-key-management.zh-TW.md) |
| **型別與時間** | [時間型別總覽](temporal-types.zh-TW.md) · [時區處理](datetime-timezone.zh-TW.md) |
| **品質與規範** | [Analyzer 規則](analyzer-rules.zh-TW.md) · [開發限制與反模式](development-constraints.zh-TW.md) · [命名規範](database-naming-conventions.zh-TW.md) |

---

## 其他子目錄

不列入上方清單，視需要直接參閱。

- **`plans/`** — 設計階段或已完成初始計畫的設計文件。屬**階段性工作文件、非參考資料**：舊 plan 未必符合現行行為。本目錄外的文件一律不連結進來，請以上方各文件為準。
- **`repo-ops/`** — 本 repo 的維運文件（CI / 分支保護），與框架使用者無關。
