# ADR-005：FormSchema 定義驅動架構

## 狀態

已採納

## 背景

企業應用系統（ERP、進銷存等）通常包含大量表單，每個表單涉及 UI 佈局、資料庫結構、驗證規則等多個面向。常見的開發方式：

1. **Code-First**：先寫程式碼（Entity / ViewModel），再衍生資料庫與 UI
2. **Database-First**：先設計資料庫，再產生程式碼
3. **Definition-Driven**：以定義（Schema）為中樞，同時驅動所有面向

## 決策

採用 `FormSchema` 作為 Single Source of Truth（唯一真相來源），同時驅動 UI（FormLayout）、資料庫（TableSchema）與商業邏輯。

## 理由

- **減少重複定義**：傳統開發中，同一個「員工姓名」欄位需要在 Entity、ViewModel、DB Migration、UI Form 中各定義一次。FormSchema 只需定義一次，其餘自動衍生。
- **NoCode / LowCode 支援**：FormSchema 以 XML 格式儲存，非程式設計師也能透過工具修改表單定義，不需要重新編譯。
- **一致性保證**：UI 顯示的欄位、資料庫的欄位、驗證規則皆源自同一份定義，不會發生不同層級定義不一致的問題。
- **快速開發**：新增表單時不需要手寫 CRUD 程式碼，FormSchema 驅動的 Repository 自動產生 SQL。
- **漸進式複雜度**：簡單表單用 NoCode（純定義），中等複雜度用 LowCode（定義 + 少量程式碼），高度客製用 AnyCode（完全自訂 BO + Repository）。

## 取捨

- **學習曲線**：開發者需要理解 FormSchema、FormLayout、TableSchema 的關係與衍生規則。
- **靈活性限制**：高度動態的 UI 或非表單型的功能（如儀表板、報表）不適合用 FormSchema 驅動。
- **除錯複雜**：問題可能源自定義檔而非程式碼，需要同時檢查 XML 定義與程式邏輯。
- **執行時期唯讀**：FormSchema 在啟動時載入後不可修改，動態新增欄位需要重新載入定義。

## 影響

- `Bee.Definition/Forms/FormSchema.cs`：定義中樞，包含所有欄位、表格、關聯
- `Bee.Definition/Database/TableSchema.cs`：資料庫維度的投影，由 FormSchema 衍生
- `Bee.Definition/Layouts/FormLayout.cs`：UI 維度的投影
- `Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs`：依據 FormSchema 自動產生 SQL
- `Bee.Db/Query/SelectCommandBuilder.cs`：組合 SELECT / FROM / WHERE / ORDER BY
- 架構詳細說明於 `docs/architecture-overview.md`
