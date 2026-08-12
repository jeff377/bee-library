# Bee.Definition：定義型別的設計規範

本檔在 agent 觸及 `src/Bee.Definition/` 下任何檔案時自動載入（巢狀 `CLAUDE.md` 為 lazy loading）。
**跨層的那條（cache 內定義資料 init 後不可異動）留在 `.claude/rules/definition.md`（常駐）**
—— 它的違反者是所有 `IDefineAccess` 消費端，不只本專案。

相依邊界（本專案不得再加 `PackageReference`）見 `.claude/rules/dependency-boundary.md`；
行動端的型別形狀要件見 `.claude/rules/apple-mobile-trim.md`。

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

參考實作：`FormFieldCollection`、`LayoutColumnCollection`、`DbFieldCollection`、`LanguageItemCollection`。

## 欄位參照屬性不帶 `Name` 後綴

「以字串名稱參照其他欄位」的屬性：單數用 `XxxField`、逗號分隔清單用 `XxxFields`，**不帶 `Name`**。

既有族系：`FormSchema.ListFields` / `LookupFields`、`FieldMapping.SourceField` / `DestinationField`、
`FormField.DisplayFields`。`FormField.FieldName` 帶 `Name` 是因為它是欄位自己的 identity、不是參照。

## 集合型別的行動端相容要件（reflection-only `XmlSerializer`）

iOS AOT 路徑走 reflection-only `XmlSerializer`，對型別形狀比桌面嚴格。
**這三點在桌面完全不會顯現，只在行動端爆炸**，而違反者一律是本專案的定義型別：

- 集合型別**只能公開一個** public instance `Add` —— 多個多載會擲 `AmbiguousMatchException`。
  便利多載必須位移為擴充方法（見 `code-style.md` 的一型別一檔例外條款）。
- 集合型別**必須有無參數建構子**，否則擲 `MissingMethodException`。
- **對映為重複 `[XmlElement]` 的集合屬性必須有 public setter**（2026-08-10 新增）。
  reflection-only 路徑對這種成員是**指派**而非 `Add`，get-only 會擲
  `ArgumentException: Property set method not found`，外顯為誤導的
  「There is an error in XML document (行, 列)」。**`[XmlArray]` 的 get-only 集合不受影響**
  —— 差別只在對映方式，不在集合本身。
  setter 寫成「清空後逐一 `Add` 進既有實例」而非直接換掉欄位，才不會斷開 owner 連結
  （實例：`Language/LanguageEnum.cs` 的 `Entries`）。

盤點全定義層有無同型問題的反射掃描手法見
`../../docs/repo-ops/gotchas/mobile-trim-aot.md`。

## `Defaults/` 只是 scaffold 來源，runtime 不參與

`Defaults/` 由 embedded resource 載入，是**「開新專案」的 scaffold 來源**
（把 `st_employee` / `st_department` 等框架系統表的 FormSchema/TableSchema 鋪成初始定義檔）。

**runtime 的定義載入只讀 `PathOptions.DefinePath`**（後端 `CacheDefineAccess` +
`FileDefineStorage`；前端走 `ClientDefineAccess`，經 API 取得而不碰檔案系統）。
**不存在「DefinePath 缺漏就 fallback 到 Defaults」的載入優先序機制。**

要在專案用某框架系統表，把定義從 `Defaults/` **複製進專案的 `DefinePath`** 為起點，
再視需要擴充（保留框架標準欄位，權限/組織等功能依賴之）。
