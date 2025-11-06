# 在 BeeNET 中以 DRM 取代 ORM 的 ERP 系統開發實務

ERP 系統往往面對高度變動的流程與資料結構，傳統以強型別類別綁定資料表的 ORM（Object Relational Mapping）模式，對於「版本快速迭代」、「跨客戶客製化」等需求就顯得僵化。BeeNET 採用的 DRM（Definition Relational Mapping）則以表單定義（FormDefine）描述資料模型，執行階段再轉換為 SQL 與資料關聯，形成 Definition-Driven Architecture（定義驅動架構），能在不重新編譯程式的情況下調整欄位與關聯。

## DRM 與 ORM 的關鍵差異

| 面向 | ORM（Object Relational Mapping） | DRM（Definition Relational Mapping） |
| --- | --- | --- |
| 關聯來源 | 強型別類別 | FormDefine 結構化定義 |
| 綁定方式 | 編譯期 | 執行期動態綁定 |
| 模型更新 | 需重新編譯程式 | 更新定義即可生效 |
| 擴充性 | 限於程式碼層 | 支援外部配置與快取重載 |
| 適用場景 | 固定資料結構 | 多變動態表單與企業系統 |

DRM 讓資料定義與程式碼解耦：FormDefine 只需以 XML 維護欄位與關聯，BeeNET 在執行時透過快取與資料存取層載入定義並生成查詢，避免每次調整資料結構都要動到程式碼。

## Definition-Driven Architecture 在 BeeNET 的組成

BeeNET 的後端設定以 `BackendInfo` 為核心，執行時會注入定義存取（`IDefineAccess`）、快取來源等服務。`LocalDefineAccess` 透過 Bee.Cache 快取層從檔案載入定義並提供查詢，保留更新後可立即重載的能力。這些組件共同構成 Definition-Driven Architecture，使得 DRM 能在執行期整合資料模型與 SQL 邏輯。【F:src/Bee.Define/Info/BackendInfo.cs†L9-L110】【F:src/Bee.Cache/DefineAccess/LocalDefineAccess.cs†L7-L123】

### FormDefine 讓模型在執行期組裝

FormDefine 描述資料表、欄位與跨表關聯，例如專案表單定義中，就透過 `RelationProgId="Department"` 與 `RelationProgId="Employee"` 描述部門與員工的關聯欄位；同時宣告對應回寫欄位（`ref_*`），讓 DRM 能在查詢時自動補齊關聯欄位。更新 FormDefine 後，即可在下次查詢時套用最新結構，無須重新部署程式。【F:samples/Define/FormDefine/Project.FormDefine.xml†L1-L32】【F:samples/Define/FormDefine/Department.FormDefine.xml†L1-L20】【F:samples/Define/FormDefine/Employee.FormDefine.xml†L1-L23】

## 使用 IFormCommandBuilder 建立 SQL

`IFormCommandBuilder` 定義了以 FormDefine 為基礎的 SQL 建置介面，主要負責 Select/Insert/Update/Delete 語法的產生。BeeNET 針對 SQL Server 提供 `SqlFormCommandBuilder` 實作，透過建構子載入指定 ProgId 的 FormDefine，並於 `BuildSelectCommand` 中呼叫 `SqlSelectCommandBuilder`。此時系統會根據定義計算欄位、Join、Where 與 Sort，最後組成 `DbCommandSpec` 供 Repository 層執行。【F:src/Bee.Db/Interface/IFormCommandBuilder.cs†L8-L32】【F:src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs†L8-L62】

`SqlSelectCommandBuilder` 是核心的 Select 語法生成器：

```csharp
var builder = new SqlFormCommandBuilder("Project");
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");
```

其內部會先取得 FormTable，透過 `SelectContextBuilder` 建構查詢上下文，再動態加入 Join、Where 與 Order By 子句。如果條件或排序牽涉關聯欄位，會自動補上對應的 Join 並轉換欄位名稱，確保生成的 SQL 與定義保持一致。【F:src/Bee.Db/Providers/SqlServer/SqlSelectCommandBuilder.cs†L11-L155】【F:src/Bee.Db/Providers/SqlServer/SqlSelectCommandBuilder.cs†L157-L232】

## Query 目錄的輔助元件

`SelectContextBuilder` 解析 FormTable 裡帶有 `RelationProgId` 的欄位，遞迴建立 `TableJoin` 與欄位映射集合。這讓 DRM 能支援多層次的外鍵關聯，並為每個關聯欄位預先準備查詢來源與別名，在後續建構 SQL 時直接套用。【F:src/Bee.Db/Query/Context/SelectContextBuilder.cs†L8-L120】

對應的資料結構包含：

- `SelectContext`：保存欄位映射與 Join 集合。
- `TableJoinCollection`：依唯一鍵管理 Join，避免重複加入。
- `QueryFieldMappingCollection`：記錄虛擬欄位（`RelationField`）對應的來源欄位與別名。

這些類別位於 `Bee.Db/Query/Context`，是 DRM 將定義轉換為 SQL 所需的中繼資料。

## 搭配 Bee.Repository 與快取服務

在應用層，Repository 介面可依 ProgId 取得對應的 Form 儲存庫實例，進一步整合業務邏輯。`FormRepositoryProvider` 與 `DataFormRepository` 封裝了 ProgId，讓上層服務能直接依定義查詢資料。配合 `LocalDefineAccess` 的快取更新機制，ERP 模組可以在不重啟服務的前提下同步新欄位或關聯設定。【F:src/Bee.Repository/Provider/FormRepositoryProvider.cs†L7-L27】【F:src/Bee.Repository/Form/DataFormRepository.cs†L8-L21】【F:src/Bee.Cache/DefineAccess/LocalDefineAccess.cs†L14-L123】

## 實作流程：在 BeeNET 中使用 DRM

1. **撰寫 FormDefine**：在 `samples\Define\FormDefine` 類似的目錄中建立 XML，描述資料表、欄位型別、關聯 ProgId 與欄位映射。
2. **設定 BackendInfo**：啟動時指定 `BackendInfo.DefinePath` 與 `DefineAccess` 實作，讓系統知道定義檔位置與快取策略。【F:src/Bee.Define/Info/BackendInfo.cs†L9-L110】
3. **透過 Repository 查詢**：於服務層透過 `SqlFormCommandBuilder`（或其介面）建立查詢命令，並交由 Repository 執行。若需要自訂條件，可提供 `FilterNode` 與 `SortFIeldCollection` 讓 DRM 自動套用欄位映射。【F:src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs†L37-L57】【F:src/Bee.Db/Providers/SqlServer/SqlSelectCommandBuilder.cs†L68-L147】
4. **更新定義即時生效**：修改 FormDefine 後，清除快取或等待快取失效，新的欄位/關聯設定立即生效，無需重新編譯。

## 範例：專案表單的 Select 語法生成

在單元測試 `BuildSelectCommand` 中，以 `Project` 為例示範 DRM 的使用方式：建構 `SqlFormCommandBuilder("Project")` 並呼叫 `BuildSelectCommand`，即可取得含必要 Join 的 SQL 命令。測試案例也覆蓋了帶條件與排序的情境，證明 DRM 能自動補齊跨表欄位並產生參數化查詢。【F:tests/Bee.Db.UnitTests/DbTest.cs†L215-L273】

若需檢視實際的 FormDefine，可開啟 `Project.FormDefine.xml`、`Employee.FormDefine.xml` 與 `Department.FormDefine.xml`，理解欄位如何映射，這對撰寫 Filter 與 Sort 欄位尤其重要。【F:samples/Define/FormDefine/Project.FormDefine.xml†L1-L32】【F:samples/Define/FormDefine/Employee.FormDefine.xml†L1-L23】【F:samples/Define/FormDefine/Department.FormDefine.xml†L1-L20】

## 在 ERP 專案中的應用建議

- **建置共用欄位模板**：將常見欄位（如建立者、狀態碼）抽成共用 FormDefine 片段，透過 RelationFieldMappings 引用，確保多模組一致性。
- **善用快取設定**：依據部署環境調整 `LocalDefineAccess` 的快取策略，確保定義更新能即時反映且不影響效能。【F:src/Bee.Cache/DefineAccess/LocalDefineAccess.cs†L14-L123】
- **自動化測試守門**：以 `BuildSelectCommand` 類似的測試驗證新定義，避免欄位名稱或關聯設定錯誤在佈署後才被發現。【F:tests/Bee.Db.UnitTests/DbTest.cs†L233-L273】

透過 DRM，BeeNET 讓 ERP 開發能以定義驅動的方式快速演進，兼具動態彈性與系統穩定性，是面對多客製、多變動需求的有效解法。
