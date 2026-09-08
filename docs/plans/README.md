# 計畫文件索引

`docs/plans/` 下進行中 / 待動工的 plan 清單，供人工快速掃讀目前有哪些計畫、進行到哪。
已完成並封存者見 [archive/README.md](archive/README.md)。

> **plan 是階段性工作文件，不是參考資料**：它記錄的是當時的打算，完成後未必等於現行行為。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../.claude/rules/public-docs.md)）。

**維護約定**：新增 plan、變更 plan 狀態、或將 plan 移入 `archive/` 時，一併更新本檔與封存索引。

| 計畫 | 狀態 | 說明 |
|------|------|------|
| [捨入政策可設定化](plan-rounding-mode.md) | 📝 擬定中（2026-09-01） | 明細計算欄的捨入模式（`MidpointRounding` / 方向）由硬編改為可設定；**階段 0 是「要不要做」的決策點**——現況全 `src/` 只有兩處 `Math.Round`、production 呼叫點僅計算欄一處，若判定 `AwayFromZero` 足夠即可只補文件結案。與多幣別加總無關（round-then-sum 之下對合計再捨是 no-op） |
| [PropertyGridControl：用宣告式 metadata 驅動屬性編輯](plan-property-grid-control.md) | 📝 擬定中（2026-08-17） | 交付吃 `System.ComponentModel` 標註（`[Description]` / `[Category]` / `[Browsable]` / `[TypeConverter]`）的 Avalonia PropertyGrid 控件；分 2 階段。承接 2026-08-07 體檢移交的 D-3 / D-5，與 TreeView 那份可並行 |
| [TreeViewBuilder：用 `[TreeNode]` 標註驅動結構樹](plan-tree-view-builder.md) | 📝 擬定中（2026-08-17） | 把無人消費的 `[TreeNode]` / `[TreeNodeIgnore]` 標註接回實際的 TreeView；分 5 階段（建樹核心／Avalonia builder／命令 provider／拖曳／在地化）。承接 2026-08-07 體檢移交的 D-3 / D-5，與 PropertyGrid 那份可並行 |
| [列級租戶隔離（`sys_company_id`）](plan-row-level-tenancy.md) | 📝 擬定中（2026-07-30） | 試用公司共用 company 資料庫，以公司編號在列的層級區隔；與既有的資料庫級隔離正交並存。**D1–D5 全數定案，可動工**；分 4 階段，階段 1（`st_session` 公司欄位化）獨立可先交付 |
