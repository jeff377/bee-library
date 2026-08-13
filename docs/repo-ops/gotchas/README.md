# 踩雷誌（Gotchas）

維護 bee-library 時實際踩過、且**下次很可能再踩**的雷，附症狀、根因與正解。

**這不是規範文件。** 硬性規則寫在 `.claude/rules/`（每個 session 常駐）；本目錄是**按需查閱**的
脈絡與推導過程——記錄「為什麼那條規則長那樣」「當時症狀看起來像什麼」，避免同一個坑用同樣的
誤判方式再走一次。

**這也不是公開文件**（見 `.claude/rules/public-docs.md`）：讀者是 bee-library 的維護者，
不是框架使用者。對外的設計決策寫 `docs/adr/`，對外的行為說明寫 `docs/` 根目錄。

| 檔案 | 涵蓋 |
|------|------|
| [database.md](database.md) | Oracle `''`=NULL、MySQL TEXT/UUID、SQLite GUID 大小寫、decimal scale、datetime2 參數層 |
| [serialization-and-expressions.md](serialization-and-expressions.md) | MessagePack ctor 順序與 wire 事實、運算式引擎兩雷、AOT 實測結論 |
| [avalonia-controls.md](avalonia-controls.md) | Avalonia 控件實證雷（DataGrid、唯讀外觀、事件、並行） |
| [mobile-trim-aot.md](mobile-trim-aot.md) | 行動端 trim / AOT：決策樹推導、reflection-only 重現法保真度、build 與驗證命令配方 |
| [test-ci-release.md](test-ci-release.md) | 測試 fixture 缺口、CI path filter 的驗證死角、發佈與體檢流程雷 |
| [northwind-heads.md](northwind-heads.md) | Northwind 四 head 工具鏈（含 iOS 的 Xcode 版本綁定）與獨立 repo 同步流程 |
| [definition-and-customization.md](definition-and-customization.md) | 客製範圍的兩種數法（同一個漏連踩三次）、覆蓋層粒度、`FormSchema` 中樞圖的兩種衍生 |

## 寫入原則

- **只記「再踩機率高」的**。一次性的環境問題、已被框架根治且不會復發的，不留。
- 每則要能回答三件事：**症狀長什麼樣**、**根因**、**正解**。少了症狀就查不到它。
- 已根治的雷仍值得留，但要明寫「已修（commit）」與**殘留的注意事項**——沒有殘留就刪掉。
- 對應的硬規則若已寫進 `.claude/rules/`，這裡只放脈絡，不重複條文。
