# 計畫文件索引

`docs/plans/` 下進行中 / 待動工的 plan 清單，供人工快速掃讀目前有哪些計畫、進行到哪。
已完成並封存者見 [archive/README.md](archive/README.md)。

> **plan 是階段性工作文件，不是參考資料**：它記錄的是當時的打算，完成後未必等於現行行為。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../.claude/rules/public-docs.md)）。

**維護約定**：新增 plan、變更 plan 狀態、或將 plan 移入 `archive/` 時，一併更新本檔與封存索引。

| 計畫 | 狀態 | 說明 |
|------|------|------|
| [部署層管理員（不綁公司的營運權限）](plan-deployment-admin.md) | 📝 擬定中（2026-07-30） | 為 API Key、公司管理、跨公司稽核這類**不屬於任何公司**的資產提供授權路徑；框架現有判定寫死在公司範圍內，用它守部署層資產會讓某家公司的管理員取得全部署的能力。**直接解鎖 API Key plan 的階段 3**；D1–D4 待裁決 |
| [列級租戶隔離（`sys_company_id`）](plan-row-level-tenancy.md) | 📝 擬定中（2026-07-30） | 試用公司共用 company 資料庫，以公司編號在列的層級區隔；與既有的資料庫級隔離正交並存。**D1–D5 全數定案，可動工**；分 4 階段，階段 1（`st_session` 公司欄位化）獨立可先交付 |
| [API Key 存放機制與預設驗證強化](plan-api-key-store.md) | 🚧 進行中（2026-07-30） | `X-Api-Key` 的存放位置與預設驗證行為：`st_api_key` + 兩段式金鑰 + 雜湊存放 + 相容閘門。**階段 1（存放模型與驗證 + 產生金鑰 BO 方法）、2（呼叫端識別落進稽核）、4（用戶端存放）已完成**；僅剩階段 3（管理表單與輪替流程），且該階段**受阻於部署層管理員 plan**。另「相容模式的長期處置」待決 |
| [客製化共同前置](plan-customization-foundation.md) | 🚧 進行中（**F1、F2 已完成**，2026-08-01） | 三類客製的共同基礎。**下列三份客製 plan 的前置**。缺口 A（消費端接線，Layout 除外）與 B（`CustomizePath` 的 host 設定與文件）已補完；伺服端管道已通，但**至今沒有 head 走過 `EnterCompany`**，實務上 `CustomizeId` 仍恆為空。剩 F3（進公司流程 + `ResetDefineCache` 收回框架 + 端到端測試）、F4（客製快取失效訊號、DB 版 reader 條件註冊）；「客製檔誰維護」仍未決 |
| [Layout 客製化](plan-customization-layout.md) | 📝 擬定中（**L1–L4 已定案，L5 待裁決**，2026-08-01） | `FormLayout` 的租戶客製：版面重排、欄位隱藏、區塊調整。前置：共同前置。定案「定義檔優先、缺檔才生成」+「整檔取代」+「`GetDefine` 取原始檔／`GetFormLayout` 取運行階段版本」。查出**兩個 UI head 根本不向 server 要 layout**（自行本地生成），故工程量比原稿大——UI head 必須一起改。**唯一擋動工項：layout 檔的 caption 怎麼在地化（L5）** |
| [業務邏輯客製化](plan-customization-business.md) | 📝 擬定中（討論稿，2026-07-25） | BO 的租戶客製：單據行為、驗證規則、流程差異。前置：共同前置 |
| [語系客製化](plan-customization-language.md) | 🚧 進行中（**G1–G3 已完成**，2026-08-01） | 語系資源的租戶客製：欄位標題、表單名稱、訊息、選項文字。前置：共同前置。四個伺服端消費端已接上 `SessionInfo.CustomizeId`；決策 G1 定案 **G1-b**（enum 維持整組取代，per-key 只適用 `LanguageItem` 的 Key），故 G5 取消。剩 G4（`GetLanguage` API 疊加，決策 G3 未定）、G6（端到端測試，卡 foundation F3） |
