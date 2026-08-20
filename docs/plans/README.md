# 計畫文件索引

`docs/plans/` 下進行中 / 待動工的 plan 清單，供人工快速掃讀目前有哪些計畫、進行到哪。
已完成並封存者見 [archive/README.md](archive/README.md)。

> **plan 是階段性工作文件，不是參考資料**：它記錄的是當時的打算，完成後未必等於現行行為。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../.claude/rules/public-docs.md)）。

**維護約定**：新增 plan、變更 plan 狀態、或將 plan 移入 `archive/` 時，一併更新本檔與封存索引。

| 計畫 | 狀態 | 說明 |
|------|------|------|
| [FormLayout 收回設計階段，移除執行階段自動推導](plan-formlayout-design-time-only.md) | 📝 擬定中（2026-08-20） | `FormLayout` 一律由設計階段產出並存成定義檔，執行階段不再由 `FormSchema` 自動推導；分 4 階段（框架路徑／公開文件／DefineEditor 產生入口／端到端實測） |
| [PropertyGridControl：用宣告式 metadata 驅動屬性編輯](plan-property-grid-control.md) | 📝 擬定中（2026-08-17） | 交付吃 `System.ComponentModel` 標註（`[Description]` / `[Category]` / `[Browsable]` / `[TypeConverter]`）的 Avalonia PropertyGrid 控件；分 2 階段。承接 2026-08-07 體檢移交的 D-3 / D-5，與 TreeView 那份可並行 |
| [TreeViewBuilder：用 `[TreeNode]` 標註驅動結構樹](plan-tree-view-builder.md) | 📝 擬定中（2026-08-17） | 把無人消費的 `[TreeNode]` / `[TreeNodeIgnore]` 標註接回實際的 TreeView；分 5 階段（建樹核心／Avalonia builder／命令 provider／拖曳／在地化）。承接 2026-08-07 體檢移交的 D-3 / D-5，與 PropertyGrid 那份可並行 |
| [列級租戶隔離（`sys_company_id`）](plan-row-level-tenancy.md) | 📝 擬定中（2026-07-30） | 試用公司共用 company 資料庫，以公司編號在列的層級區隔；與既有的資料庫級隔離正交並存。**D1–D5 全數定案，可動工**；分 4 階段，階段 1（`st_session` 公司欄位化）獨立可先交付 |
