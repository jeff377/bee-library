# 定義層（Bee.Definition）規範

## 集合屬性一律繼承 base class

定義檔的子集合（FormSchema / FormLayout / TableSchema / LanguageResource 等）一律繼承
`Bee.Base.Collections.KeyCollectionBase<T>`（item 有 key）或 `CollectionBase<T>`（item 無 key）。
**禁用** `List<T>`、`Collection<T>`、`IList<T>` 等裸 BCL 集合作為公開屬性型別。

**Why:** 集中序列化、`IObjectSerialize` 生命週期、`ITagProperty`、Owner 反向導航、key 唯一性檢查
到單一基底。未來要支援新序列化格式、加 change notification、或做 cache invalidation，只改基底即可。
**用裸 `List<T>` 等於繞過基底機制，未來基底加新行為時這個 collection 不會跟到，會變成例外。**

- 新增集合屬性時先寫 `<Item>Collection` 繼承對應基底，再把屬性宣告為該 collection 型別。
- Item 繼承 `KeyCollectionItem` / `CollectionItem`；domain-meaningful key 走代理模式
  （`FormField.FieldName { get => Key; set => Key = value; }` + `[XmlAttribute]`）。
- MessagePack 場景用 `MessagePackKeyCollectionBase` / `MessagePackCollectionBase` 變體。
- 唯一可繞過情境：Item 是純 value-type DTO 且**從不出現在 Bee.Definition 範圍內**。
- 集合型別的行動端 AOT 形狀要件（單一 public `Add`、須有無參數建構子）見 `rules/apple-mobile-trim.md`。

參考實作：`FormFieldCollection`、`LayoutColumnCollection`、`DbFieldCollection`、`LanguageItemCollection`。

## 欄位參照屬性不帶 `Name` 後綴

「以字串名稱參照其他欄位」的屬性：單數用 `XxxField`、逗號分隔清單用 `XxxFields`，**不帶 `Name`**。

既有族系：`FormSchema.ListFields` / `LookupFields`、`FieldMapping.SourceField` / `DestinationField`、
`FormField.DisplayFields`。`FormField.FieldName` 帶 `Name` 是因為它是欄位自己的 identity、不是參照。

## Cache 內的定義資料 init 後不可異動

所有透過 `IDefineAccess.GetX(...)` 取得的物件（FormSchema、FormLayout、TableSchema、SystemSettings、
DatabaseSettings、ProgramSettings、DbCategorySettings、LanguageResource）都是 **process-wide cache
共用實例**，每個 session 拿到同一個 reference。**init 完成後不可在 runtime mutate**，否則跨 session
洩漏 / race。`SessionInfo` 是例外（本來就 per-session）。

- 需要 per-session 變動 → 先 `cached.Clone()` 再 mutate。
- 持久化變更走 `IDefineAccess.SaveX(...)`（寫 storage + invalidate cache slot）。
- 新加 Define 系列類別時記得補 `Clone()`。
- **`XmlCodec.Serialize(cachedInstance)` 不能當免費 deep-clone** —— 它透過
  `IObjectSerialize.SetSerializeState` 在**來源**上 mutate state，並行下 `IsSerializeEmpty` 等
  以 state 為條件的邏輯會錯亂（已踩過，fix commit `aa843f71`）。
- code review 看到「對 cache 取出的 instance 直接 mutate」或拿 `XmlCodec.Serialize(cached)` 當克隆，
  必須擋下。

完整規範見 `docs/development-constraints.md` § Definition Data Immutability After Init。

## `Defaults/` 只是 scaffold 來源，runtime 不參與

`src/Bee.Definition/Defaults/` 由 embedded resource 載入，是**「開新專案」的 scaffold 來源**
（把 `st_employee` / `st_department` 等框架系統表的 FormSchema/TableSchema 鋪成初始定義檔）。

**runtime 的定義載入只讀 `PathOptions.DefinePath`**（後端 `CacheDefineAccess` +
`FileDefineStorage`；前端走 `ClientDefineAccess`，經 API 取得而不碰檔案系統）。**不存在「DefinePath 缺漏就 fallback 到 Defaults」的載入優先序機制。**
要在專案用某框架系統表，把定義從 `Defaults/` **複製進專案的 `DefinePath`** 為起點，再視需要擴充
（保留框架標準欄位，權限/組織等功能依賴之）。

## BO 介面是 BO-to-BO 解耦層，與 API 開放面各自獨立

`IBusinessObject` / `ISystemBusinessObject` / `IFormBusinessObject` 等 axis 介面的定位是
**BO-to-BO 解耦層**：caller 透過 `IBusinessObjectFactory` 以 progId 解析、cast 到介面後呼叫，
不繫結具體 BO 類別。這樣 host 端 BO 客製化（多租戶換 SystemBO 子類、業務替換 FormBO 子類）
才不破壞 caller。

**兩個表面各自獨立，彼此不蘊含**：`[ApiAccessControl]` 是給**外部**（client 經
`JsonRpcExecutor` 呼叫）的表面，axis 介面是給**內部**呼叫的表面。**沒有硬性規定**
——開放給 API 的方法不必然要上介面，介面上的方法也不必然要開放給 API。

判準只有一條。新增 BO method 時問：「會不會有另一個 BO、背景作業或排程透過
`_ctx.BoFactory.CreateXxxBO(...)` 拿到後呼叫它？」是 → 放介面；否 → 不放。
介面爆成「所有 public 方法集合」就失去意義，也增加 host 端客製化負擔。

> **判斷時務必把 server 端的背景呼叫端算進去，不能只看「client 會不會呼叫」。**
> `Login` 曾被本規則誤列為「只給 client、不放介面」的例子（2026-08-12 更正）——它有真實的
> 內部呼叫端：**背景作業會以某身份登入建立連線**，再模擬該使用者登打表單或執行作業。
> 判準本身沒錯，錯的是漏算了背景作業這類呼叫端。
