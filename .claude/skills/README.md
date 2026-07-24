# Project Skills

本目錄的 skill 是 bee-library 的**工程慣例知識**（隨 repo 入版控，同 `.claude/rules/`）。
每個 skill 一個資料夾，內含 `SKILL.md`；Claude 依 `SKILL.md` frontmatter 的 `description`
自動判斷何時載入，使用者亦可用 `/<skill-name>` 主動呼叫。

> **權威來源在各 `SKILL.md` 的 frontmatter `description`** —— 那是給模型觸發用的完整描述。
> 本 README 只是給**人**看的一行索引，不複製完整 description，以免兩處 drift。
> 要知道某 skill 的精確作用與觸發條件，開該資料夾的 `SKILL.md`。

## Bee 框架開發

| Skill | 一句話用途 |
|-------|-----------|
| **bee-app-scaffold** | 搭一個獨立 Bee.NET 後端應用 / demo 的接線慣例（DB scope、auth、seeder） |
| **bee-add-form** | 在已接好的 app 上加一張 CRUD 表單（4 處純定義修改，不寫 UI） |
| **bee-add-bo-method** | 新增對外公開的 BO 方法（跨 contract / wire / BO / Repository / Client） |
| **bee-add-cache-object** | 新增框架快取物件（Define 定義快取 / DB 相依快取） |
| **bee-scaffold-from-formschema** | 從 FormSchema 反推 FormLayout / TableSchema / 雙語 LanguageResource |
| **bee-serialization** | 物件三棲序列化（XML 持久化 + JSON/MessagePack wire）設計指引 |
| **bee-framework-review** | 框架全面體檢方法論（八面向唯讀審查 + 分級重構計畫） |
| **bee-sample-add** | 為 samples/ 加一個新示範專案 |

## 通用工作流（不綁 Bee 框架）

| Skill | 一句話用途 |
|-------|-----------|
| **plan-write** | 撰寫 / 更新 `docs/plans/` 計畫文件的狀態列與階段表格格式 |
| **changelog-draft** | 整理自上一版 tag 至 HEAD 的雙語 CHANGELOG 草稿 |
| **demo-smoke** | 對 samples/ 的 demo 跑端到端冒煙測試 |

## 新增 skill

1. 建 `.claude/skills/<name>/SKILL.md`，frontmatter 至少含 `name` 與 `description`
2. `description` 要寫清楚「做什麼 + 使用者說什麼時觸發」（決定模型能否正確自動載入）
3. 在本 README 對應分類補一行 hook
4. 預設入版控；純個人用、不宜共享的 skill 才於 `.gitignore` 排除

> 註：另有個人用 skill（如 `hackmd-blog`）已於 `.gitignore` 排除、不入版控，故不列於此。
