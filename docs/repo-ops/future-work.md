# 未來工作構想

尚未啟動、也還沒寫成 plan 的方向。**這裡只記「為什麼要做、要等什麼、啟動時第一步是什麼」**；
真正啟動時依 `plan-workflow:plan-write` 寫 `docs/plans/plan-<主題>.md` 交使用者 review 後才執行。

## 對外開發者 skill 包（Claude Code plugin）

**目標**：做一組給**使用 Bee.NET 框架的外部開發者**的 skill 包，讓他們快速上手。

**關鍵區別——這不是「把現有 skills 分享出去」**：

- `.claude/skills/bee-*`（`bee-app-scaffold` / `bee-add-form` / `bee-add-bo-method` 等）是
  **在 bee-library repo 內開發用**的內部視角：引用 `src/Bee.*`、`apps/Bee.Northwind` 等內部路徑。
  **直接 ship 給外部消費者不了**（原因是內部路徑與內部視角，不是版控與否——
  `.claude/skills/` 與 `.claude/commands/` 自 2026-07-23 起已入版控）。
- 開發者包是**消費端視角**（裝了 `Bee.*` NuGet 的人）：只引用公開 API surface + 公開文件 +
  公開範例；必須**版控、發佈、隨版本維護**；打包成 Claude Code **plugin**。

**散佈與錨點**：做成一個 plugin（如 `bee-dotnet`），**錨定在畢業後的公開
`bee-northwind-avalonia` repo** 當活範例（非內部 `apps/`）。

**要等什麼**：**自然接在 Northwind 畢業之後**——畢業才有公開參考實作可指。現在不必動。

**預計內容**（現有知識的消費端改寫 + 補上手路徑）：`bee-quickstart`（裝 NuGet → 最小 app → 跑起來）、
`bee-app-scaffold`（PackageReference 版）、`bee-add-form`、`bee-add-bo-method`、
`bee-formschema-reference`（lookup / 明細 / 下拉 / 唯讀 / scope 完整慣例）、
`bee-concepts`（FormSchema 中樞 / DataSet DTO / BO / Repository 雙軌 / common-company scope）、
序列化、快取。

**啟動時第一步**：寫 `docs/plans/plan-bee-developer-skills.md`（plugin 結構、各 skill 的消費端改寫、
散佈/維護機制、與發版綁定）。消費端最易錯的觀念是 DB scope，見 `.claude/rules/database.md`。
