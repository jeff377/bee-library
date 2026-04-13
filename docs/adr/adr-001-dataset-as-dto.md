# ADR-001：使用 DataSet 作為跨層 DTO

## 狀態

已採納

## 背景

框架需要一個跨層資料傳輸物件（DTO），在 Business Object、Repository、API 之間傳遞 Master-Detail 表單資料。常見選項包括：

1. 強型別 POCO / Entity 類別
2. ADO.NET DataSet / DataTable
3. Dictionary / dynamic 動態物件

## 決策

採用 ADO.NET `DataSet` 作為跨層 DTO。

## 理由

- **動態結構**：FormSchema 在執行時期定義欄位，表單結構不是編譯時期已知的。DataSet 天然支援動態欄位，不需要為每個表單產生 POCO 類別。
- **Master-Detail 原生支援**：DataSet 包含多個 DataTable，可自然對應表單的主表與明細表（Master-Detail）關係，無需額外包裝。
- **變更追蹤**：DataRow 內建 `RowState`（Added / Modified / Deleted / Unchanged），Repository 層可直接根據狀態產生對應的 INSERT / UPDATE / DELETE，不需要額外的 Change Tracker。
- **序列化成熟**：DataSet 的 XML / Binary 序列化在 .NET 生態系中已高度成熟，搭配自訂 MessagePack Formatter 也能高效傳輸。
- **跨框架相容**：netstandard2.0 原生支援 DataSet，不依賴新版 API。

## 取捨

- **型別安全性降低**：欄位存取以字串索引，編譯時期無法檢查欄位名稱拼寫錯誤。
- **IntelliSense 支援較差**：不像強型別 POCO 有屬性提示。
- **不符合現代 .NET 慣例**：多數新框架偏好強型別 POCO + EF Core，DataSet 被視為「較舊」的做法。

## 影響

- 所有跨層資料傳遞統一使用 DataSet，不混用 POCO DTO
- FormSchema 驅動的 CRUD 操作依賴 DataRow.RowState 判斷操作類型
- 自訂 MessagePack Formatter（位於 `Bee.Api.Core/MessagePack/`）負責 DataSet 的高效序列化
- `Bee.Base/Data/` 提供 DataTable / DataSet / DataRow 擴展方法簡化操作
