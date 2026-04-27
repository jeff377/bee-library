# 計畫：撰寫 FormMap 設計文件

**狀態：✅ 已完成（2026-04-27）**

## 待確認事項決議（2026-04-27）

1. 文件語言：**雙語（en + zh-TW）**
2. 跨文件更新範圍：**四項一次做完**
3. HackMD 文章引用：**不放**（節省維護負擔；未來框架成熟時另寫新文）
4. 是否新增 ADR-008：**不新增**（FormMap 是 ADR-005 的下游實作）

## 背景

Bee.Db 採用一種**有別於 ORM** 的資料庫存取模式：以 FormSchema 為單位描述業務實體、透過 `RelationProgId` 串接成「表單關聯」（Form-Level Relation），執行階段由 `SelectContextBuilder` 等元件動態組出 SQL，不依賴強型別 entity class，產出為 `DataSet`。

這個模式目前的狀態：

- 設計理念已成形且實作完成（`Bee.Db.Query` 路徑）
- 上位概念「Definition-Driven Architecture」已在 `README` / `architecture-overview` / `ADR-005` 中說明
- 但**資料存取層這個具體模式尚未在 repo 中明確命名與說明**
- 使用者在 HackMD 已撰文（舊名 DR-Map），近期決議改稱 **FormMap**

對套件使用者而言，第一次接觸 Bee.Db 時很容易誤以為它是某種 ORM 的變體。缺乏明確說明會讓使用者：

- 用 ORM 的心智模型套用 → 誤解設計意圖（例如期待 LINQ、navigation property、change tracking）
- 不清楚與 EF Core / Dapper 的差異定位 → 難以判斷是否適合自己的場景
- 看到「不支援複合鍵 JOIN / 子查詢」等限制時，無法理解這是**設計取捨而非缺漏**

## 目標

在 `docs/` 新增一份正式文件，明確說明 **FormMap** 這個資料庫存取模式的概念、原理、與 ORM 的差異、應用場景，並在相關文件加入交叉參照。

## 命名與位置

- 主文件（英文）：`docs/formmap.md`
- 繁中對照：`docs/formmap.zh-TW.md`
- 雙語版頂部互連結（遵循 repo 既有雙語 docs 慣例，如 `architecture-overview.md` / `.zh-TW.md`）

## 文件大綱

以下為繁中版大綱，英文版同結構：

1. **一句話定義** —— FormMap 是什麼
2. **為什麼不是 ORM**
   - 對照表（映射對象、載體、模型來源、變更代價、執行期彈性）
   - 強調 FormMap 與 ORM 是**平行方案**，不是子集
3. **核心概念**
   - Form-Level vs Table-Level 關聯
   - 單階宣告、多階執行（每張 FormSchema 只描述自己直接參照的下一層；多層 JOIN 由執行期遞迴展開）
   - 在 Definition-Driven Architecture 中的位置（FormMap 是 DDA 在資料存取層的具體模式）
4. **程式碼與 SQL 範例**（4-5 例，遞進式）
   - 單表查詢（無 JOIN）
   - 條件觸發 JOIN（Where 用到參考欄位）
   - 排序觸發 JOIN（Order By 用到參考欄位）
   - 多參考欄位（多層 JOIN 自動展開）
   - 複合條件（FilterGroup + Sort）
5. **實作對應**
   - `SelectContextBuilder`：FormSchema 鏈遞迴 → `TableJoin` 集合
   - `FromBuilder` / `SelectBuilder` / `WhereBuilder` / `SortBuilder`：四件式 SQL 子句生成
   - `SelectCommandBuilder`：總組裝
6. **適用 / 不適用 場景**
   - 適用：FormSchema 驅動的 CRUD、ERP 動態欄位／多租戶／客製化
   - 不適用：報表 / 批次 / 任意 SQL 形狀 → 走 BO + AnyCode 那一軌（雙軌策略）
7. **與其他 Bee.Db 元件的關係**（簡短交叉參照）
8. **延伸閱讀**
   - HackMD 原始文章（DR-Map 舊名，註明已更名）
   - ADR-005、architecture-overview、terminology

## 跨文件更新

| 文件 | 更新內容 |
|---|---|
| `docs/terminology.md` | 「1. 架構模式與核心概念」表格新增一條 `FormMap \| 表單映射 \| ...` |
| `src/Bee.Db/README.md` + `.zh-TW.md` | 開頭一段簡述 FormMap 並 link 到 `docs/formmap.md` |
| `docs/architecture-overview.md` + `.zh-TW.md` | 資料層段落補一句指向 FormMap 文件 |
| `docs/adr/adr-005-formschema-driven.md` | 「影響」段加一行 cross-reference 到 FormMap 文件 |

## 不在範圍

- 重命名 DR-Map：repo 內未出現該字串，無需替換
- HackMD 文章改名：**現有文章維持不動**（保留 DR-Map 命名與內容）。未來框架發展到可實際應用的程度時，再撰寫**另一篇新文章**，並於新文中說明 DR-Map → FormMap 的更名歷程。本次 plan 不觸及 HackMD
- 新增獨立 ADR：FormMap 是 ADR-005「FormSchema 定義驅動」的下游實作模式，未產生新的設計取捨決策，不另立 ADR
- 程式碼重命名：不動類別名／命名空間（FormMap 是模式名，不是型別名）

## 執行步驟

1. 撰寫 `docs/formmap.zh-TW.md`（主稿）
2. 撰寫 `docs/formmap.md`（英文版，內容同步）
3. 更新 `docs/terminology.md` 加術語條目
4. 更新 `src/Bee.Db/README.md` + `.zh-TW.md`
5. 更新 `docs/architecture-overview.md` + `.zh-TW.md`
6. 更新 `docs/adr/adr-005-formschema-driven.md`「影響」段
7. 在本 plan 頂部標記 `**狀態：✅ 已完成（YYYY-MM-DD）**`

## 待確認事項

開工前請確認：

1. **文件語言**：採雙語（en + zh-TW）還是只寫繁中？
   - 建議雙語，因為 NuGet 上的套件使用者以英文為主
2. **跨文件更新範圍**：上表四項全做？還是先只寫主文件，跨文件更新延後？
3. **HackMD 文章引用**：在「延伸閱讀」放網址，並註明「該文使用舊名 DR-Map，已更名為 FormMap，未來會有新文章說明更名」。OK 嗎？
4. **是否新增 ADR**：我傾向不新增（理由見「不在範圍」），如果你認為值得記錄「為什麼選 FormMap 而非 EF Core」這類取捨，可以加一支 ADR-008
