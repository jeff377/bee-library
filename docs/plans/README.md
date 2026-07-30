# 計畫文件索引

`docs/plans/` 下進行中 / 待動工的 plan 清單，供人工快速掃讀目前有哪些計畫、進行到哪。
已完成並封存者見 [archive/README.md](archive/README.md)。

> **plan 是階段性工作文件，不是參考資料**：它記錄的是當時的打算，完成後未必等於現行行為。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../.claude/rules/public-docs.md)）。

**維護約定**：新增 plan、變更 plan 狀態、或將 plan 移入 `archive/` 時，一併更新本檔與封存索引。

| 計畫 | 狀態 | 說明 |
|------|------|------|
| [Bee.Analyzers — 框架慣例編譯期化](plan-bee-analyzers.md) | 🚧 進行中（2026-07-30） | 以 Roslyn analyzer 把框架慣例變成編譯期診斷，讓「只引用套件、無 source」的外部開發者（與其 AI 工具）零安裝取得慣例把關。兩大戰場：`AdditionalFiles` 讀 XML 定義檔的跨檔一致性、以及三棲序列化的沉默失敗（未註冊 formatter → 空集合）；分 5 階段，**階段 1（三種分析管線 + 端到端驗證）已完成** |
| [列級租戶隔離（`sys_company_id`）](plan-row-level-tenancy.md) | 📝 擬定中（2026-07-30） | 試用公司共用 company 資料庫，以公司編號在列的層級區隔；與既有的資料庫級隔離正交並存。**D1–D5 全數定案，可動工**；分 4 階段，階段 1（`st_session` 公司欄位化）獨立可先交付 |
| [API Key 存放機制與預設驗證強化](plan-api-key-store.md) | 🚧 進行中（2026-07-30） | `X-Api-Key` 的存放位置與預設驗證行為：`st_api_key` + 兩段式金鑰 + 雜湊存放 + 相容閘門。**階段 1（存放模型與驗證 + 產生金鑰 BO 方法）、階段 2（呼叫端識別落進稽核）已完成**；階段 3（管理與輪替）／4（用戶端存放）待做。僅「相容模式的長期處置」待決 |
| [客製化共同前置](plan-customization-foundation.md) | 📝 擬定中（討論稿，2026-07-25） | 三類客製的共同基礎（缺口 A、B）。**下列三份客製 plan 的前置**，未補則其餘三份無法生效 |
| [Layout 客製化](plan-customization-layout.md) | 📝 擬定中（討論稿，2026-07-25） | `FormLayout` 的租戶客製：版面重排、欄位隱藏、區塊調整。前置：共同前置 |
| [業務邏輯客製化](plan-customization-business.md) | 📝 擬定中（討論稿，2026-07-25） | BO 的租戶客製：單據行為、驗證規則、流程差異。前置：共同前置 |
| [語系客製化](plan-customization-language.md) | 📝 擬定中（討論稿，2026-07-25） | 語系資源的租戶客製：欄位標題、表單名稱、訊息、選項文字。前置：共同前置 |
