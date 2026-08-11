# 版本變更記錄

[English](CHANGELOG.md)

本檔記錄專案的所有重要變更。

## [Unreleased]

### 新增

- `Bee.Api.Client`：`ApiSessionContext` 承載 per-session 的 client 狀態 —— 登入時建立的傳輸金鑰與登入者的時區。connector 新增接它的建構子多載；不傳則共用 `ApiSessionContext.Ambient`，那是既有行為，對單使用者宿主仍然正確。

### 修正

- `Bee.Api.Client` / `Bee.Web.Blazor.Server`：單一 process 服務多個使用者時，不再互相覆蓋傳輸金鑰。`ApiClientInfo.ApiEncryptionKey` 與 `UserTimeZoneId` 原本是 process-wide static，因此在 `BeeBlazorProviderMode.Remote` 下最後登入者勝出，先前使用者的加密請求會解不開、直到重新登入。`BeeApiConnectorFactory` 改註冊為 scoped，每個 circuit 拿到自己的 context。`Local` 模式從未受影響。`ApiClientInfo.ApiKey` 刻意維持 static —— 它識別的是應用程式，不是使用者。
- `Bee.Api.Core` / `Bee.Base`：未變更的資料列不再在 wire 上攜帶兩份相同的值。MessagePack 與 JSON 兩個寫入端都對 `Unchanged` 列同時送出 Current 與 Original，而兩個讀取端都只由 Current 還原 —— 且 `DataFormRepository.GetData` 回傳前呼叫 `AcceptChanges()`，因此每一筆從資料庫讀出的列都是 Unchanged。讀取的 payload 與序列化成本因此減半。
- `Bee.Definition`：運算式求值約快 4.7 倍。先前每次求值都把整列的所有欄位交給引擎，而非運算式實際引用的那幾個，成本因此與**欄數**成正比、與運算式無關。實測 30 欄 / 5 計算欄 / 1000 列：57.2 ms → 12.2 ms。
- `Bee.Definition` / `Bee.Api.Client`：兩處在並行下會交出或重建 process-wide 快取定義狀態的問題已修正。`FormTable.RelationFieldReferences` 的反向索引建立在無保護的 null 檢查後，兩個觸碰同一份快取 schema 的請求可能各建一份、拿到不同實例，且建立過程的驗證例外會從一個看起來只是讀取的 property getter 冒出來。`FormDefinitionLoader.GetLocalizedSchemaAsync` 在未指定語言時直接回傳共用的快取 schema，儘管它的文件寫著「絕不交出快取實例」——而 `CultureInfo.InvariantCulture.Name` 是空字串，那正是最常走到的路徑。
- `Bee.Db` / `Bee.ObjectCaching`：`DatabaseSettingsChanged` 事件改為在**重新載入**時觸發，而非每次載入。它唯一的發布者是設定快取的載入路徑，而 `DbConnectionManagerService` 收到後會清空連線快取——且是在它自己的 `GetOrAdd` value factory 內被觸達，因此每次設定快取 miss 都會丟棄先前建立的所有連線項目。真正的重新載入（設定檔有監看）仍會觸發，那正是外部編輯得以傳播到連線快取的機制。`DbConnectionManagerService` 另實作 `IDisposable` 並退訂——static 事件會持有訂閱者直到行程結束。
- `Bee.Business`：`EnterCompany` 改為先解析完公司的角色、能力與記錄範圍身分，才寫入 session。session 物件由同一個 access token 的所有並行請求共用，而 `CompanyId` 與 `Roles` 正是授權判斷的兩個輸入——先前把寫入與查詢交錯，留下一段橫跨三次可能觸及資料庫之呼叫的窗口，期間並行請求可能以「新公司 + 前一家公司的角色」做出授權決定。
- `Bee.Db`：`WhereBuilder` 在有 `selectContext` 而需改寫條件欄名時，不再遺失 `SecondValue` 與 `IgnoreIfNull`。先前 `BETWEEN` 會失去上界，`IgnoreIfNull` 條件則變成 `= NULL` —— 在 SQL 裡永不成立，於是查詢靜默回傳零筆，而不是忽略該條件。

## [4.20.0]

> 本版修掉一個反序列化漏洞，並完成兩項解耦。安全項：wire 的型別白名單只檢查 assembly-qualified name 第一個逗號之前的字串，而泛型型別的那個逗號落在**參數清單裡面** —— 夾帶在泛型參數中的不允許型別因此從未被檢查，且未認證的呼叫端就到得了。同時 wire 上的 `object` 值由逐值攜帶型別名改為判別式封套，運算式抽象下沉至 `Bee.Base`，定義層不再讓每個消費者背上 DynamicExpresso 相依。**兩項 wire 變更都要求 client 與 server 同版部署。** 框架登入另有預設實作，對從未覆寫過的部署是行為變更。

📄 詳細變更與設計脈絡：[docs/changelogs/4.20.0.zh-TW.md](docs/changelogs/4.20.0.zh-TW.md)

### 安全性

- `Bee.Business`：`LoginAttemptTracker` 的失敗記錄表改為有界。表內每一把 key 都是攻擊者指定的 user id（`System.Login` 是匿名的），而未達鎖定門檻的條目先前**永不過期、也無人清掃**，一串不重複的 user id 就能讓它無限成長。現在條目會自行過期、失敗計數以視窗計算、追蹤帳號數設有上限。
- `Bee.Business` / `Bee.Api.AspNetCore`：API key gate 失效現在會被回報。停用最後一把啟用中的金鑰會讓 gate 靜默退回「接受任何非空 `X-Api-Key`」——而那是金鑰輪替的正常步驟——先前唯一的訊號是啟動時的一次性快照。現在停用最後一把金鑰會記 error，啟動檢查在非 Development 環境也改以 error 層級回報。
- `Bee.Api.Core`：型別白名單改為驗證 assembly-qualified name 指名的**每一個**型別 —— 外層型別、每個泛型參數、陣列元素 —— 且無法解析的名稱改為拒絕而非放行。先前 ``Bee.Base.Collections.Dictionary`1[[Disallowed.Type, Other]], Bee.Base`` 這樣的名稱會通過檢查，因為切在第一個逗號後留下的片段仍帶著允許的命名空間前綴。`System.Login` 是匿名的，且 payload 型別解析發生在商業物件被呼叫之前，故該路徑未認證即可觸達。自 4.0.2 起存在。

### 破壞性變更

- **Wire**：`object` 型別的成員（`Parameter.Value`、`FilterCondition.Value` / `SecondValue`、`SerializableDataColumn.DefaultValue`，以及 `SerializableDataRow` 內的儲存格值）由逐值攜帶 assembly-qualified 型別名改為整數判別碼封套。這些成員掛在每個 request 與 response 上，因此 4.19 的 client 無法與 4.20 的 server 溝通，反之亦然。見 [ADR-037](docs/adr/adr-037-wire-explicit-registration.md)
- `Bee.Expressions` → `Bee.Base`：`IExpressionEvaluator`、`ExpressionPolicy` 與 `ExpressionEvaluationException` 移至 `Bee.Base.Expressions`。**沒有 type forward，舊名稱直接編譯不過。** 見 [ADR-038](docs/adr/adr-038-definition-dependency-boundary.md)
- `Bee.Definition` / `Bee.Business` / `Bee.UI.Avalonia`：`FormExpressionCalculator`、`FormRuleProcessor`、`FormLiveComputation` 的建構子改收新命名空間的 evaluator。**`FormLiveComputation` 的那個參數是選擇性的，因此省略它的呼叫端原始碼照樣編得過 —— 但既有已編譯的組件會擲 `MissingMethodException`，必須重新編譯。**
- `Bee.Repository.Abstractions`：`IUserRepository` 新增 `VerifyPassword(userId, password)` —— 介面新增成員，外部自行實作者必須補上。
- `Bee.Business`：`SystemBusinessObject.AuthenticateUser` 的預設實作不再無條件回 `false`，改為比對 `st_user`。**從未覆寫它的部署原本沒有可用的登入，改版後 `st_user` 內的帳號可以登入。** 已覆寫的部署不受影響。

### 新增

- `Bee.Base`：`Bee.Base.Expressions` —— evaluator 抽象、政策輔助方法與例外型別。`Bee.Expressions` 只剩 `DynamicExpressoEvaluator`。
- `Bee.Definition`：`LanguageEnum.Entries` 補上 setter（見下方行動端修正）。
- `Bee.Business`：`LoginAttemptTracker.MaxTrackedAccounts` 與 `DefaultMaxTrackedAccounts`，供宿主自訂上限。
- 建置期診斷 **BEE9001**（`Bee.Base` / `Bee.Definition` 的相依邊界）與 **BEE9002**（三個版號屬性必須同步）。[Analyzer 規則](docs/analyzer-rules.zh-TW.md)

### 修正

- `Bee.Definition`：`LanguageEnum.Entries` 原本是對映為重複 `[XmlElement]` 的 get-only 集合。iOS 使用的 reflection-only `XmlSerializer` 路徑對這種成員是**指派**而非 `Add`，因而擲 `ArgumentException: Property set method not found`，外顯為誤導的「There is an error in XML document」。setter 採「清空後逐一填回既有實例」，owner 反向連結不會斷開。
- `Bee.Definition`：`st_user.password` 由 40 字元放寬為 200。`PasswordHasher` 產出的雜湊為 79 字元，五家 provider 中有四家會截斷，截斷後驗證永遠不會成功。此缺陷先前未浮現，是因為在本版之前框架沒有任何地方真的把雜湊寫進 `st_user`。
- **版號**：三個版號屬性移至 repo 根的 `Version.props`，由 `src/` 與 `tools/` 各自 import。`AssemblyVersion` 與 `FileVersion` 重新與 `Version` 同步——已發布的 4.19.0 套件內組件標的是 `4.18.0.0`，以組件 identity 無從與 4.18.0 區分，該版不重新發布。**`Bee.Cli` 先前自帶一份版號、已落後十二個 minor**，因此自 4.9.0 起每一版發布的 `Bee.Cli` 內組件標的都是 `4.8.0.0`；現在它與其他專案一樣取用框架版號。BEE9002 會在版號不一致時讓建置失敗，而單一來源則消除了「兩個專案各說各話」的可能——那是任何 per-project 檢查都攔不到的。

### 變更

- `Bee.Definition`：套件的相依鏈不再帶 `Bee.Expressions`，因此也不再帶 `DynamicExpresso.Core`。`Bee.Cli`、`DefineEditor` 這類只讀定義的消費者不再繼承一個運算式引擎。
- `samples`：移除 `Avalonia.Demo`；Avalonia 的端到端示範改為 `apps/Bee.Northwind`。

### 升級指引

```diff
- using Bee.Expressions;
+ using Bee.Base.Expressions;
```

server 與 client 一起部署 —— 見上方 wire 說明。任何會建構 `FormLiveComputation` 的組件都要重新編譯，即使它沒有傳入 evaluator。若你自行實作 `IUserRepository`，補上 `VerifyPassword`。若你依賴 `AuthenticateUser` 拒絕所有登入，請覆寫它。

## [4.19.0]

> 本版把定義層與傳輸格式解耦。`Bee.Definition` 不再引用 MessagePack：wire 綁定的一切知識移入 `Bee.Api.Core`，由手寫 formatter 承擔。分界線是「這個格式會不會讓定義層長出外部套件相依」—— XML 與 JSON 是 BCL 詞彙，留下；MessagePack 是明確的技術選擇，外置。六個從不需要 MessagePack 的下游套件不再被迫繼承它，四對刻意重複的集合型別也合併回單一實作。**`FilterNode` 與 `ParameterCollection` 的 wire 格式有變，client 與 server 必須同版升級。** 依嚴格 SemVer 這屬 major；v4.x 的 pre-stable 政策下仍以 minor 發佈，破壞性變更逐條列於下方。

📄 詳細變更與設計脈絡：[docs/changelogs/4.19.0.zh-TW.md](docs/changelogs/4.19.0.zh-TW.md)

### 破壞性變更

- **Wire**：`FilterNode` / `FilterCondition` / `FilterGroup` 由 `[Union]` 陣列形式改為以 `Kind` 為判別碼的 map；`ParameterCollection` 由單鍵 map 改為純陣列。`ParameterCollection` 掛在每一個 request 與 response 上，因此 4.18 的 client 無法與 4.19 的 server 溝通，反之亦然。
- `Bee.Definition`：移除 `Collections.MessagePackCollectionBase<T>`、`MessagePackCollectionItem`、`MessagePackKeyCollectionBase<T>`、`MessagePackKeyCollectionItem` —— 改用 `Bee.Base.Collections` 的對應型別，兩者除標註外完全相同。
- `Bee.Definition`：移除 `Serialization.SafeTypelessFormatter`；typeless 白名單遷入 `Bee.Api.Core` 並改為 internal。
- `Bee.Base`：移除 `IObjectSerializeProcess`、`SerializeFormat` 與 `SerializationLifecycle.NotifyAfterDeserialize` —— 該介面 production 無實作者，兩個歷史用途皆已被刻意遷走。
- `Bee.Analyzers`：**BEE4001**–**BEE4004** 退役，它們把關的標註機制已不存在。[Analyzer 規則](docs/analyzer-rules.zh-TW.md)

### 變更

- `Bee.Definition` / `Bee.Api.Contracts`：移除 `MessagePack` 套件參考，全 repo 僅存於 `Bee.Api.Core`。見 [ADR-036](docs/adr/adr-036-wire-serialization-externalized.md)
- `Bee.Api.Core`：wire 綁定改為 contractless 加九支手寫 formatter。沒有框架管理成員需排除的型別完全不需要 formatter。
- `Bee.Analyzers`：**BEE4006** 的判定改以框架集合與集合項目的基底型別為準，不再依賴 `[MessagePackObject]`，藉此保住對 item 型別的覆蓋。

### 升級指引

```diff
- using Bee.Definition.Collections;
+ using Bee.Base.Collections;

- public class MyItems : MessagePackCollectionBase<MyItem> { }
+ public class MyItems : CollectionBase<MyItem> { }

- public class MyItem : MessagePackCollectionItem { }
+ public class MyItem : CollectionItem { }
```

Server 與 client 需一併部署 —— 見上方 wire 說明。

## [4.18.0]

> 本版是一次框架全面體檢的產出，不是功能週期。主線是 **`System.ExecFunc` 的派發面**：`UpgradeTableSchema`（在呼叫端指定的任一資料庫裡把表刪掉重建）與 `TestConnection`（對呼叫端指定的任一 host 發出站連線）原本任何已認證呼叫端都能觸達。兩者現已限本機呼叫，派發器對未標註 handler 的預設由*放行已認證*改為**拒絕**，並新增建置期 analyzer 讓這種漏標不可能再發生。帳號鎖定——有實作卻從未註冊——改為預設啟用，並修正 4.17.0 的一項版號缺陷。

📄 詳細變更與設計脈絡：[docs/changelogs/4.18.0.zh-TW.md](docs/changelogs/4.18.0.zh-TW.md)

### 破壞性變更

- `Bee.Business`：`IExecFuncHandler` 的方法未標 `[ExecFuncAccessControl]` 時，派發改為**拒絕**，不再視同 `Authenticated`。
- `Bee.Definition` / `Bee.Api.Client`：移除 `SystemActions.ExecFuncLocal`、`SystemApiConnector.ExecFuncLocalAsync` 與 `FormApiConnector.ExecFuncLocalAsync`——自 2025-10-03 起每次呼叫都擲 `MissingMethodException`。
- `Bee.Base` / `Bee.Definition`：移除 `ISerializableClone` 與 `DatabaseSettings.CreateSerializableCopy()`；它們防的那條「就地加密」管線從未存在。
- `Bee.Db`：`DbParameterSpecCollection` 的兩個便利 `Add` 多載移為 `DbParameterSpecCollectionExtensions`——原始碼相容、二進位破壞性。

### 安全性

- `Bee.Business`：`UpgradeTableSchema` 與 `TestConnection` 改為 `LocalOnly`。原本只要一個有效 access token 就能對呼叫端指定的資料庫觸發破壞性 DDL，或把伺服器當成對外埠掃描器並注入連線字串。
- `Bee.Hosting`：帳號鎖定改為預設啟用——`AddBeeFramework` 以 `TryAdd` 註冊 `LoginAttemptTracker`（5 次 / 15 分鐘）。
- `Bee.Business`：`GetDefine(DefineType.DatabaseSettings)` 改為直接讀定義檔，密碼維持 `enc:` 密文，不再從快取實例送出已解密的值。

### 新增

- `Bee.Business`：`ExecFuncAccessControlAttribute.LocalOnly`，以及帶 `isLocalCall` 的 `InvokeExecFunc` 多載（舊多載保留，視同遠端呼叫）。
- `Bee.Analyzers`：**BEE3003**——`IExecFuncHandler` 實作上的 public 方法必須宣告 `[ExecFuncAccessControl]`。[analyzer 規則](docs/analyzer-rules.zh-TW.md)
- `Bee.Db`：`DbParameterSpecCollectionExtensions`。
- `Bee.Api.Contracts` / `Bee.Db`：序列化 analyzer（BEE4002–4006）現在會跑這兩個專案，先前對它們完全靜默。

### 變更

- `Bee.Base`：`JsonCodec` 改為共用 `static readonly JsonSerializerOptions`，不再每次呼叫新建；wire 路徑輸出改 compact（`SerializeToFile` 維持縮排）。
- 建置：`AssemblyVersion` / `FileVersion` 恢復與 `Version` 同步——見下方升級指引。

### 修正

- `Bee.Db` / `Bee.Api.Client`：`DbProviderRegistry`、`DbDialectRegistry` 與 `ClientDefineAccess` 快取改用並行安全集合；三者都會被並行讀取，而原本的路徑非原子。
- `Bee.Business`：`SystemBusinessObject` 供應 `PluginSettings` 時不再直接序列化共用快取實例。

### 升級指引

```diff
  public class MyExecFuncHandler : IExecFuncHandler
  {
+     [ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
      public void DoSomething(ExecFuncArgs args, ExecFuncResult result) { ... }
  }
```

- **ExecFunc handler**：每個 public 方法都要標註，否則 BEE3003 會讓建置失敗。只在行程內執行的操作用 `LocalOnly = true`。
- **`DbParameterSpecCollection.Add`**：原始碼不需改；對 4.17.0 或更早版本編譯的組件需重新編譯。
- **帳號鎖定**：要維持原行為，在 `AddBeeFramework` 之前先註冊 no-op 的 `ILoginAttemptTracker`。
- **組件 identity**：已發布的 4.17.0 套件內組件標的是 `4.16.0.0`。4.18.0 直接跳到 `4.18.0.0`；`4.17.0.0` 從未存在，且 4.17.0 不重新發布。
- **回溯補記**：`IExcelHelper` 移除於 4.16.0 卻從未記錄，該條目已補進 [4.16.0 說明](docs/changelogs/4.16.0.zh-TW.md)。

## [4.17.0]

> 本版主線是**業務邏輯 plugin**——在套裝 BO 的既有流程上掛載客製程式碼，不換掉整個 BO 類別，只在特定時點追加一段。這是客製化的第五種機制，補上「輕量擴充」這一格；同時帶來客製層的**第一條寫入路徑**（`PluginSettings` 是唯一有維護 API 的客製定義）。另有兩項客製化行為修正：`ProgramItem` 覆寫由「整筆取代」改為**屬性級繼承**、BO 型別解析失敗改為**降級並記錄**——兩者都是 `ProgramItem` 於 4.16.0 新增 `Repository` 綁定後才浮現的問題。

📄 完整說明與設計脈絡：[docs/changelogs/4.17.0.zh-TW.md](docs/changelogs/4.17.0.zh-TW.md)

### 新增

- `Bee.Definition`：新增 `PluginSettings` 定義型別（`DefineType` 第 13 個成員，加在列舉尾端）與完整讀取管線——路徑、三個 storage、快取、reader、overlay。
- `Bee.Business`：新增 `FormBusinessPlugin` 基底與四個掛載點（`BeforeSave` / `AfterSave` / `BeforeDelete` / `AfterDelete`），在各 `Do*` 子方法的最終實作之後執行，**與繼承可疊著用**。[ADR-035](docs/adr/adr-035-business-logic-plugin.md)
- `Bee.Business`：新增 `FormPluginChain` / `FormPluginRunner` / `IFormPluginResolver` / `PluginSettingsResolver`——**兩層相加**（套裝在前、客製在後）、**per-operation 實例**、解析失敗一律拋。
- `Bee.Business`：新增 `SystemBO.GetCustomizePluginSettings` / `SaveCustomizePluginSettings`（`LocalOnly`），**寫入前逐一驗證**型別可載入、繼承 `FormBusinessPlugin`、且至少 override 一個時點。
- `Bee.Definition`：新增 `ICustomizeDefineWriter` 與 `CustomizeDefineWriter`——客製層的第一條寫入路徑，寫完即 evict 該租戶 cache slot。
- `Bee.Business`：`BusinessObject` 新增 `protected IBeeContext Context`。
- 新增[租戶客製化指引](docs/customization.zh-TW.md)（雙語）：五種客製的決策表、語系與 Layout 的 how-to、以及不能客製什麼與為什麼。

### 變更

- `Bee.Definition`：`ProgramItem` 的客製覆寫由**整筆取代**改為**屬性級繼承**——客製只寫要改的屬性，未寫的沿用套裝。修正「只換 BO 卻無聲打掉套裝專屬 Repository」。[ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)
- `Bee.Business`：一般 progId 的 BO 型別解析失敗維持降級到 `FormBusinessObject`，但改為**記錄 error**（訊息帶 progId、型別名與宣告層）。
- `Bee.Business`：`DeleteContext.Snapshot` 的載入條件加入「該 progId 有 delete 時點的 plugin」，避免其有無取決於變更稽核開關。
- `Bee.Definition` / `Bee.ObjectCaching`：`IDefineAccess` / `IDefineStorage` / `ICustomizeDefineReader` / `ICacheContainer` **新增 `PluginSettings` 相關成員**——自行實作這些介面者需補上。
- `Bee.Db`：`DbDefineStorage.Write` 新增 `customizeId` 參數；租戶列與 base 列僅差 `customize_id`，無需 schema 變更。
- `FormBusinessObject`：六個 `Do*` 子方法補 `<remarks>` 標明**是否在交易中**，並寫明 `DoBefore*` 的 TOCTOU 空窗與 `DoAfter*` 的「拋例外時資料已提交」。純文件、零行為變更。
- `api-bo-contract-design`：命名表補 `XxxContext` 一列——跨層傳輸用 `Args`/`Result`，流程內共享狀態用 `Context`。

## [4.16.0]

> Bee.NET 仍處 pre-stable 演進階段。本版是 CHANGELOG 開始記錄以來最大的一版：227 個 commit、四條主線。**ProgId 成為框架唯一的定址模型**——`ProgramSettings` 收斂為純型別註冊表，把每個 progId 綁定到它的商業物件**與** Repository，導覽選單分離為獨立定義 [ADR-034](docs/adr/adr-034-progid-type-registry.md)。**多租戶客製化延伸到語系與版面**，前後端共用同一套疊加演算法 [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)。**應用程式身分有了生命週期**——API 金鑰可儲存、驗證，並由部署層權限軸把關其管理。以及**框架慣例移到建置期**，成為 22 條 analyzer 規則。數項變更屬破壞性，依 pre-stable 政策以 minor 發佈，因目前尚無外部消費者。本條目並含 `v4.15.0` 之後未在 subject 標 `!` 的破壞性變更。

📄 完整說明與設計脈絡：[docs/changelogs/4.16.0.zh-TW.md](docs/changelogs/4.16.0.zh-TW.md)

### 新增

- `Bee.Definition`：隨套件提供 Roslyn analyzer，把 22 條框架慣例變成建置期診斷——定義檔合法性、wire 合約形狀、BO 存取控制。見 [Analyzer 規則](docs/analyzer-rules.zh-TW.md)。
- `Bee.Definition`：新增 `MenuSettings` 定義型別，承接導覽選單（巢狀 `MenuFolder` / `MenuEntry`、全樹唯一 `Id`、設計期 `Visible`）。
- `Bee.Definition`：`ProgramItem.Repository`，與 `BusinessObject` 並列，把 progId 綁定到它的 Repository。
- `Bee.Repository.Abstractions`：新增 `IRepositoryFactory`，以兩個泛型軸成為所有 Repository 的唯一入口。
- Session 撐得過快取逐出、行程重啟與多節點路由：登入寫入重建種子至 `st_session`，角色 / 客製代碼 / record scope 於每次重建重算。
- 應用程式身分：API 金鑰存於 `st_api_key`、由 `IApiKeyValidator` 驗證、經 `IDeploymentAuthorizationService` 管理。見 [API 金鑰管理](docs/api-key-management.zh-TW.md)。
- `Bee.Api.Client`：新增 `FormDefinitionLoader`，把原始定義組裝成執行階段的 schema 與 layout。
- `Bee.Business`：新增 `DerivedApiEncryptionKeyProvider`（現為預設）、`SessionCompanyBinder`、`BusinessObject.CreateFormRepository<T>()`。
- `Bee.Definition`：新增 `st_user.culture`、`BackendConfiguration.DefaultLanguage` / `SessionCleanupOptions`；`Bee.Hosting`：新增 `ExpiredSessionCleanupService`。
- `Bee.Expressions`：運算式沙箱新增 `UtcNow()`，與 `Today()`、`Now()` 並列。見[運算式規則](docs/expression-rules.zh-TW.md)。

### 變更 —— 破壞性（編譯期可發現）

- `Bee.Definition`：`ProgramSettings` 改為攤平、server 端專用的型別註冊表；移除 `ProgramCategory`，選單移至 `MenuSettings`。定義檔需拆分。[ADR-034](docs/adr/adr-034-progid-type-registry.md)
- `Bee.Business`：所有商業物件改由註冊表解析——`ProgId` 上移至 `BusinessObject` 基底、`IFormBoTypeResolver` 更名 `IBoTypeResolver`、三個 `Create` 方法收斂為 `CreateBusinessObject(token, progId, isLocalCall)`，並移除 `BackendComponents.BusinessObjectFactory`。
- `Bee.Repository.Abstractions`：移除 `ISystemRepositoryFactory` / `IFormRepositoryFactory` / `IAuditLogRepositoryFactory` 與 `IReportFormRepository`；Repository 經 `RepositoryBase` 統一為 `(ctx, accessToken, progId)` 建構子。
- `Bee.Api.Core` / `Bee.Api.Client`（**wire**）：定義類 API 一律供應原始定義 + XML 信封；移除 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`。[ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)
- 移除 `Bee.UI.Maui` 與 `Bee.Web.Blazor.Wasm`；UI 收斂為 Avalonia + Blazor.Server 雙軌。
- `Bee.Definition`：移除 `Bee.Definition.Documents.IExcelHelper`（89 行，全 repo 零實作零呼叫）。*2026-08-07 回溯補記。*
- `Bee.ObjectCaching`：移除 `IEvictableCache` 與 `ICacheContainer.TryEvict(string)`。
- `Bee.Repository.Abstractions`：`IDataFormRepository.GetNewData()` 增加 `timeZoneId`；`ISessionRepository.CreateSession(...)` 拆為 `Insert` / `Update` / `Delete` / `DeleteExpiredSessions`；`IUserRepository.GetTimeZone` 換為 `GetLocale` 並新增 `GetName`。
- `Bee.Business` / `Bee.Expressions`：`IFormRuleProcessor` 與 `IExpressionEvaluator.Evaluate` 增加 `timeZoneId`。
- `Bee.Definition`：`IDefineStorage` 新增 `GetChangeSource(...)`；`IApiEncryptionKeyProvider.GenerateKeyForLogin` 改收 token 並新增 `SupportsSessionRebuild`；`ICacheDataSourceProvider.GetSessionUser` 換為 `GetSessionInfo`。
- `Bee.Base`：時間 `Cxxx` 家族單參數多載改回傳 nullable；`CDate` 更名 `CDateOnly`；列舉末端新增 `FieldDbType.Time`。
- `Bee.Api.Core`（**wire**）：`SerializableData*` 改採 property-name key。
- `Bee.Business`：`SystemBO.SaveDefine` 與 `SystemBO.CreateSession` 改為 `LocalOnly`。
- 移除零使用的公開表面：八個型別（含 `IEnterpriseObjectService`）與三個成員。

### 變更 —— 破壞性（靜默，無編譯錯誤）

- **系統時間戳改為 UTC** —— 任何讀取或比較這些值之處會整體位移一個時區，且不會有編譯失敗。
- **日期一律 `DateOnly`** —— `FormRowDefaults.Apply` 與 `FieldDbTypeExtensions.DefaultForDbType` 增加預設參數；`Today()` 改回傳 `DateOnly`。
- **預設值改變**：`SessionInfo.TimeZone` / `Culture` 預設改為空字串（登入時由 `st_user` 填入，未設值退回 `BackendConfiguration`）；`BackendDefaultTypes.ApiEncryptionKeyProvider` 改指導出式 provider，升級後現存 session 失效一次。
- **`SysInfo` 的反序列化允許清單**由 `Bee.Contracts` 更正為 `Bee.Api.Contracts`。
- **`SystemBO.CreateSession` 簽發的 session 現在真的可用**，傳入 `OneTime` 改擲 `NotSupportedException`。
- **Session 讀取不再有副作用** —— 過期列改以查詢過濾而非順手刪除，回收由 `ExpiredSessionCleanupService` 負責。

### 修正

- `Bee.Hosting`：啟用稽核記錄時 `IAuditLogWriteRepository` 無法解析。
- `Bee.Api.Core`：`IsDebugMode` 啟用時基礎設施例外保留原訊息；日光節約前推缺口不再擲例外。
- `Bee.Db`：全新建立的 SQL Server 表，`FieldDbType.DateTime` 改宣告為 `datetime2(7)`，與 ALTER 及 rebuild 路徑一致。
- `Bee.Definition`：`FieldDbType.Date` 欄位改對映到 `DateEdit`。
- `Bee.Base`：`StringUtilities.Replace` 改用 ordinal 比對；`DataTable` 的 JSON round-trip 保留字串與 decimal 的保真度。

### 安全性

- 應用程式身分由部署層權限軸把關，該軸與公司層權限永不互相 fallback；每個部署層操作皆寫入稽核。
- 識別碼型字串比對全面改為 ordinal；三處手寫的常數時間比對迴圈改用 `CryptographicOperations.FixedTimeEquals`。
- `XmlCodec.Deserialize` 禁用 DTD 處理；定義檔路徑拒絕逃出根目錄；master key 檔案於 Unix 僅擁有者可讀寫；session 查詢失敗不再回傳 UserID。

### 升級指引

定義檔拆分、Repository 工廠遷移，以及需要人工稽核的靜默變更清單，見 [docs/changelogs/4.16.0.zh-TW.md](docs/changelogs/4.16.0.zh-TW.md#升級指引)。

## [4.15.0]

> Bee.NET 仍處 pre-stable 演進階段。本版是發版前的 **wire 與 API 收斂**。MessagePack 合約序列化由位置式整數鍵改為 **property-name key**,使 JSON 與 MessagePack 共用同一份以屬性名為準的 wire 合約,並消除建構子順序 / 跨繼承 key 編號的 footgun [ADR-030](docs/adr/adr-030-messagepack-name-based-keys.md)。另外,API **合約介面依軸分入命名空間**(`Bee.Api.Contracts.System` / `.Form` / `.AuditLog`),對齊既有的實作層。兩項變更技術上皆屬破壞性 —— 分別是 wire 格式與 `using` —— 但依 pre-stable 政策以 minor 發佈,因目前尚無外部消費者。

📄 完整說明與設計脈絡:[docs/changelogs/4.15.0.zh-TW.md](docs/changelogs/4.15.0.zh-TW.md)

### 變更

- `Bee.Api.Core` / `Bee.Definition`(**破壞性 —— wire**):72 個合約型別(57 個 `Bee.Api.Core.Messages` request/response + 15 個 `Bee.Definition` / `Bee.Api.Contracts` DTO 與非 `[Union]` 集合 item)由整數 `[Key(n)]` 改為 `[MessagePackObject(keyAsPropertyName: true)]`;MessagePack payload 由位置式陣列改為屬性名 map,與 JSON 一致。刻意排除(維持整數鍵):`[Union]` 多型型別(`FilterNode` / `FilterCondition` / `FilterGroup`)、集合容器、以及 `SerializableData*` DataSet/DataTable plumbing。[ADR-030](docs/adr/adr-030-messagepack-name-based-keys.md)
- `Bee.Api.Contracts`(**破壞性 —— source**):System / Form / AuditLog 三軸合約介面(及其 DTO)由根命名空間移入 `Bee.Api.Contracts.System` / `.Form` / `.AuditLog`,對齊已軸分的 `Bee.Business.*` 與 `Bee.Api.Core.Messages.*` 層;根命名空間僅保留跨 BO 的 `ExecFunc` request/response。純 source-level —— 序列化實作類別命名空間不變,對 wire 無影響。

### 升級指引

參照被搬移合約介面的外部消費者,將 `using` 更新至對應軸命名空間:

```diff
- using Bee.Api.Contracts;
+ using Bee.Api.Contracts.System;   // ILoginRequest, IPingRequest, …
+ using Bee.Api.Contracts.Form;     // IGetListRequest, ISaveRequest, …
+ using Bee.Api.Contracts.AuditLog; // 異動軸合約, RecordFieldChange
```

MessagePack wire 格式變更不需改程式碼,但 client 與 server 必須跑相同(或相容)版本 —— 舊的位置式鍵 payload 無法被新的 name-based formatter 讀取。

## [4.14.0]

> Bee.NET 仍處 pre-stable 演進階段。本版新增兩大子系統：**宣告式運算式與規則引擎**（新套件 `Bee.Expressions` —— 計算欄、存檔/刪除前驗證規則、Avalonia 前端即時預覽，全 schema 驅動、一般表單零 BO 程式碼）[ADR-028](docs/adr/adr-028-expression-rule-engine.md)，以及**稽核軌跡 / 日誌查詢子系統**（六軸 `st_log_*`：登入 / 異動 / 檢視 / 異常，以 `DataSet` DiffGram 擷取異動、背景寫入）[ADR-027](docs/adr/adr-027-audit-trail.md)。並將**記憶體 `DataSet` 欄名正規化為小寫** [ADR-029](docs/adr/adr-029-lowercase-field-names.md) —— 此為 wire 可見變更（JSON / MessagePack key，如 `SYS_ROWID` → `sys_rowid`）：外部 JS/TS client 須改用小寫 key，`UppercaseColumnNames` 擴充方法更名。.NET 消費端不受影響（欄名查找大小寫無關）。依 pre-stable 政策以 minor 發佈，雖然此 wire/API 變更嚴格而言屬破壞性。

📄 完整說明與設計脈絡：[docs/changelogs/4.14.0.zh-TW.md](docs/changelogs/4.14.0.zh-TW.md)

### 新增

- `Bee.Expressions`（新套件）：可攜求值引擎（`IExpressionEvaluator` / `DynamicExpressoEvaluator`，DynamicExpresso 封裝、沙箱化），含編譯快取、`ExpressionPolicy` 型別/null 對映與相依分析 —— 前後端共用，前端算值與後端一致。[ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Definition`：`FormField.ValueExpression`（計算欄）與 `DefaultValueExpression`，以及 `FormSchema` 上的 `FormRule` / `FormRuleCollection`（`When` / `Condition` / `Message` / `Trigger` = `BeforeSave` | `BeforeDelete`）；共用 `FormExpressionCalculator`。[ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Business`：`FormBusinessObject.Save` / `Delete` 重構為模板方法（`DoBeforeSave` / `DoSave` / `DoAfterSave` + 刪除對應），`IFormRuleProcessor` 依 schema 套用預設值、計算欄（經 `NumberFormatResolver` 捨入）與驗證規則 —— 一般 CRUD 表單零 BO 程式碼。[ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.UI.Avalonia`：編輯時前端即時重算計算欄（`FormLiveComputation`），含 Tier 2 幣別/單位捨入 context 與 graceful degrade；新列套用 `DefaultValueExpression`。[ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Business` / `Bee.Repository`：稽核軌跡子系統 —— 六軸 `st_log_*`（`login` / `change` / `access` / `anomaly_api` / `anomaly_db`）、`IAuditLogWriter` 背景寫入、存檔/刪除以 `DataSet` DiffGram 擷取前後影像。[ADR-027](docs/adr/adr-027-audit-trail.md)
- `Bee.Business` / `Bee.Api.*`：稽核日誌查詢讀取側 —— `GetChangeLog` / `GetChangeDetail`（異動軸清單 + 明細二段式）、登入/檢視/異常清單、異常彙總（`Summary` + Top-N）。[ADR-027](docs/adr/adr-027-audit-trail.md)
- `Bee.UI.Avalonia` / `Bee.UI.Core`：前端權限 **capability** —— 元件級降級（無 Read 隱藏、無 Update 唯讀），來源為 `EnterCompany` capability 快照；`ClientInfo.Company` 與 `ClientDefineAccess.GetCurrencySettingsAsync` / `GetUnitSettingsAsync`。
- `Bee.Definition`：record scope 權限支援多個 Owner / Dept 欄（OR 聯集）。

### 變更

- `Bee.Base` / data（**破壞性 —— wire 與公開 API**）：記憶體 `DataSet` 欄名正規化為**小寫**（`DataTableExtensions.AddColumn`，以及 `DbAccess` 讀取邊界的 `LowercaseColumnNames`，統一各 provider 大小寫）。JSON / MessagePack payload 欄名 key 由大寫改小寫（如 `SYS_ROWID` → `sys_rowid`）；`UppercaseColumnNames` 擴充方法更名為 `LowercaseColumnNames`。[ADR-029](docs/adr/adr-029-lowercase-field-names.md)
- `Bee.Db`：SQL Server `DateTime` 欄由 `datetime` 遷移至 `datetime2(7)`（亞毫秒精度 + pre-1753 範圍）；`datetime2` 參數改寫僅限 SQL Server。
- `Bee.Base`：字串 key 的大小寫無關比較統一收斂為 `OrdinalIgnoreCase`（文化無關；避開 Turkish-I 隱患）。

### 修正

- `Bee.Expressions`：變數表以宣告的 `FormField.FieldName` 為 key，使運算式能對應大寫儲存的 `DataColumn` 欄名，不再於存檔時因未知識別字失敗；`ExpressionPolicy.CoerceValue` 處理 string 型 `Guid` / `byte[]` 欄，並將空字串 GUID 對映為 `Guid.Empty`（SQLite 以 TEXT 儲存 GUID）。

### 升級指引

以字面欄名 key 讀取 `DataSet` JSON 的外部 JS/TS client 須改用小寫：

```diff
- const rowId = row.current.SYS_ROWID;
+ const rowId = row.current.sys_rowid;
```

呼叫已更名欄名擴充方法的 .NET 端：

```diff
- dataTable.UppercaseColumnNames();
+ dataTable.LowercaseColumnNames();
```

## [4.13.0]

> Bee.NET 仍處 pre-stable 演進階段。本版新增 ERP 級數值層：欄位上的語意 `NumberKind` 驅動顯示格式、捨入策略與小數位數來源 —— **round-then-sum** 合計、逐欄 **多幣別**（SAP CUKY 式，JPY=0 / USD=2 / BHD=3）與 **計量單位**（SAP UNIT 式，KG=3 / PCS=0）位數皆於 runtime 解析，並附 Avalonia `NumericEdit` 編輯器。所有新增皆向後相容（新成員預設空；`CompanyInfo` 尾端加 MessagePack key）。無破壞性變更。[ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)

📄 詳細變更與設計脈絡：[docs/changelogs/4.13.0.zh-TW.md](docs/changelogs/4.13.0.zh-TW.md)

### 新增

- `Bee.Definition`：`FormField` 與 `LayoutFieldBase` 上的 `NumberKind` 語意（`Quantity` / `Weight` / `Amount` / `Percent` / `UnitPrice` / `Cost` / `ExchangeRate`），驅動顯示格式、捨入策略與位數來源。[ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)
- `Bee.Definition`：`NumberFormatResolver`（`ResolveDecimals` / `ResolveFormat` / `RoundByKind` / `RoundCash`）與 `NumberFormatApplier.Bake` —— round-then-sum 合計、兩層捨入（幣別/單位自然小數 + 選配現金捨入）、顯示格式 bake 於 per-call schema clone（絕不 mutate 快取）。
- `Bee.Definition`：`CurrencySettings` 幣別主檔（`DefineType.CurrencySettings`，curated ISO 4217，SAP TCURX 式），透過 `FormField.CurrencyField` / `FormSchema.CurrencyField` 逐欄綁定；金額位數跟幣別走。
- `Bee.Definition`：`UnitSettings` 計量單位主檔（`DefineType.UnitSettings`，SAP T006 式），透過 `FormField.UnitField` 逐欄綁定；數量／重量位數跟單位走。
- `Bee.Definition`：`CompanyInfo` 新增 `NumberFormats`、`DefaultCurrency`、`CashRounding`、`AllowedCurrencies`（`[Key(4)]`–`[Key(7)]`），由四個新 `st_company` 欄位承載；空值退框架預設。
- `Bee.UI.Avalonia`：`NumericEdit` 編輯器（`ControlType.NumericEdit`）—— focus 顯完整精度、blur 依 `NumberFormat` 格式化、右對齊、顯示捨入絕不回寫。
- `Bee.UI.Avalonia`：`GridControl` per-cell 幣別／單位感知格式化（逐列解析 `CurrencyField` / `UnitField`）與 `AmountColumnSummary` 混幣／混單位合計 helper。

## [4.12.1]

> Bee.NET 仍處 pre-stable 演進階段。本 patch 在 `Bee.Definition` 內嵌 trimmer descriptor，讓定義型別圖在 full trim / AOT 下保留，補完 4.12.0 起步的 Avalonia **iOS** / **Android** Release 打包路徑（4.12.0 讓同一批型別可於 reflection-only XmlSerializer 反序列化）。無破壞性變更。

📄 詳細變更與設計脈絡：[docs/changelogs/4.12.1.zh-TW.md](docs/changelogs/4.12.1.zh-TW.md)

### 修正

- `Bee.Definition`：隨套件內嵌 `ILLink.Descriptors.xml`，在 full trim / AOT 下保留定義型別圖（`Bee.Definition.*` + `Bee.Base.Collections.*`），使裝置端 `XmlCodec.Deserialize<FormSchema>` 路徑在 trimmed iOS / Android Release 建置下不被裁掉。自動套用至所有下游 trimmed / AOT 消費端，呼叫端無需任何改動。

## [4.12.0]

> Bee.NET 仍處 pre-stable 演進階段。本版讓 `Bee.UI.Avalonia` 控件家族在手機／窄視窗下響應式，並讓 `Bee.Definition` 型別可於 AOT reflection-only XmlSerializer 反序列化 —— 兩者合起來讓 Avalonia 的 **iOS** 與 **Android** head 得以成立。無破壞性變更。

📄 詳細變更與設計脈絡：[docs/changelogs/4.12.0.zh-TW.md](docs/changelogs/4.12.0.zh-TW.md)

### 新增

- `Bee.UI.Avalonia`：`FormView` 響應式佈局 —— 主檔欄位於 `CompactWidthThreshold`（預設 600 DIP）以下由多欄重排為單欄、明細 grid 由 `InCell` 切為 `EditForm`。
- `Bee.UI.Avalonia`：`ListView` 窄視窗卡片佈局 —— 以每筆一張卡取代寬欄 grid。
- `Bee.UI.Avalonia`：`RowEditPanel`（EditForm）依宿主寬度 1 ↔ 2 欄重排；`RowEditDialog` 桌面視窗可調整大小。

### 修正

- `Bee.Definition`：定義集合型別可於 AOT reflection-only XmlSerializer 反序列化（單一 public `Add(T)`、無參數建構子）—— 讓 iOS / Android head 得以成立。呼叫端語法與 XML 格式皆不變。[ADR-025](docs/adr/adr-025-define-types-aot-xmlserializer-compat.md)
- `Bee.UI.Avalonia`：`RowEditDialog` 在單視圖宿主（iOS / Android / 瀏覽器）改走 `OverlayLayer`，取代會崩潰的 native `Window`。
- `Bee.UI.Avalonia`：`FormView` 表單本體垂直捲動，窄單欄佈局下方控件仍可觸及。
- `Bee.UI.Avalonia`：`GridControl` lookup 可編輯 cell 顯示開窗放大鏡圖示。

## [4.11.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「前端↔後端存取全面 async 化」：client 連線生命週期與型別化定義快取卸除 sync-over-async 橋接（`SyncExecutor` 移除），連帶讓單視窗的 Avalonia Browser (WASM) head 可行。本版含**破壞性變更**，範圍限於 `Bee.UI.Core`、`Bee.Api.Client` 與 Avalonia / MAUI head 的 client 建構／連線面，另含 SQLitePCLRaw 的**安全性升級**。

📄 詳細變更與設計脈絡：[docs/changelogs/4.11.0.zh-TW.md](docs/changelogs/4.11.0.zh-TW.md)

### 破壞性變更

- 移除公開的同步 client API，改用 async —— `ClientInfo.Initialize(string)` / `SetEndpoint`、`ApiConnectValidator.Validate`、`IUIViewService.ShowApiConnect`（改用對應 `...Async`）；`SyncExecutor` 移除。
- 型別更名 `RemoteDefineAccess` → `ClientDefineAccess`（移至 `Bee.Api.Client` root）、`LocalDefineAccess` → `CacheDefineAccess`。

### 安全性

- SQLitePCLRaw 升級至 3.x（GHSA-2m69-gcr7-jv3q），取代先前的 NU1903 抑制。

### 新增

- `Bee.UI.Avalonia`：單視窗 host 的對話框疊層路徑（`OverlayLayer`），讓 lookup / 列編輯對話框得以在 Avalonia Browser (WASM) head 運作。
- `Bee.UI.Avalonia`：`FormDataObject` 新增 `RowAdded` / `RowDeleted` / `IsDirtyChanged` 事件。

### 變更

- `Bee.UI.Avalonia`：欄位編輯器改為離開／Enter 才提交，非逐字提交。
- `Bee.UI.Avalonia`：欄位標題統一標示唯讀（括號、僅留底線）與必填（藍色）。
- `Bee.Definition`：`FormLayoutGenerator` 生成的主 section 不再重複表單名。

### 修正

- `Bee.UI.Avalonia`：`GridControl.Bind` 明確綁定時自我初始化編輯狀態。

### 升級指引

```diff
- ClientInfo.Initialize(endpoint);
- ClientInfo.SetEndpoint(endpoint);
+ await ClientInfo.InitializeAsync(endpoint);
+ await ClientInfo.SetEndpointAsync(endpoint);
```
```diff
- RemoteDefineAccess access = ...;   // LocalDefineAccess cache = ...;
+ ClientDefineAccess access = ...;   // CacheDefineAccess  cache = ...;
```

## [4.10.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「lookup 關連機制全面落地」：relation 欄自動成為開窗式 lookup 編輯器、複合顯示「編號 - 名稱」、主表 `ButtonEdit` 與明細 InCell 兩種選取入口，搭配後端 `GetLookup` 取數。同時把 Avalonia 單筆/清單拆為 `FormView` / `ListView` 兩個關注點（ERP 慣例的清單／單筆分離），並將 DataForm 持久化收斂到 DataTable 級 `DataAdapter` 路徑（自製 `SqliteDataAdapter` 讓 SQLite 同樣走 adapter）。本版含**數個 breaking change**，範圍限於 `Bee.UI.Avalonia` 與 `Bee.Db` 的建構面。

📄 詳細變更與設計脈絡：[docs/changelogs/4.10.0.zh-TW.md](docs/changelogs/4.10.0.zh-TW.md)

### 破壞性變更

- `Bee.UI.Avalonia`：移除 `DynamicForm` / `SingleFormBase`；清單職責拆至新 `ListView`、單筆收斂於 `FormView`，兩者移至新命名空間 `Bee.UI.Avalonia.Views`。
- `Bee.UI.Avalonia`：`GridControl`（含 `GridControlBinder` / `GridEditMode`）由 `Bee.UI.Avalonia.Controls.Editors` 移至 `Bee.UI.Avalonia.Controls`。
- `Bee.Db`：移除逐列 `InsertCommandBuilder` / `UpdateCommandBuilder`（`DeleteCommandBuilder` / `SelectCommandBuilder` 仍保留）。

### 新增

- `Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`：定義驅動的開窗 lookup 關連機制 — `DisplayField` / `LookupFields`、自動解析的 `ButtonEdit`、後端 `FormBusinessObject.GetLookup`、前端 `LookupPanel` / `LookupDialog` 與 `GridControl` InCell 開窗（[ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)）。
- `Bee.Definition`：`FormField.ReadOnly`，由 `FormLayoutGenerator` 傳遞到 `LayoutField` / `LayoutColumn`。
- `Bee.Db`：自製 `SqliteDataAdapter`（經 `SqliteProviderFactory`），使 SQLite 走 adapter 路徑。

### 變更

- `Bee.Db`：`DataFormRepository.Save` 改用 DataTable 級 IUD（`DataAdapter.Update`）；無變更的 DataSet 為 no-op 回 0（[ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md)）。
- `Bee.UI.Avalonia`：`FormView` 清單列雙擊開啟唯讀檢視。

### 修正

- `Bee.Db`：SQLite GUID 欄加 `COLLATE NOCASE`（CREATE 與 ALTER ADD）。
- `Bee.Db`：新增資料列依 `FormSchema` 經 `FormRowDefaults` 補非空預設；master 連結以原值 `sys_rowid` 寫入明細 `sys_master_rowid`。
- `GetNewData` 骨架補 `RelationField` 欄位。
- `SelectContextBuilder`：修多關連 JOIN 解析。
- `Bee.UI.Avalonia`：`ListView` 清單捲軸於列數超出可視範圍時可正常捲動。

## [4.9.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「Avalonia 可編輯表單全面落地」：與 `ControlType` 一一對應的 field editor 控件組、新的 `GridControl`（含 in-cell 與彈窗兩種列編輯）、表單模式生命週期（`SingleFormBase` 向整棵控件樹廣播 `FormMode`），以及定義層 `FormEditModes` 依表單模式的可編輯設定。本版含 **一個 breaking change**，範圍僅限 Avalonia 家族：`Bee.UI.Avalonia` 移除 `DynamicGrid`（Blazor / MAUI 版不受影響）。另含 MessagePack 相依套件的**安全性升級**。

📄 詳細變更與設計脈絡：[docs/changelogs/4.9.0.zh-TW.md](docs/changelogs/4.9.0.zh-TW.md)

### 破壞性變更

- `Bee.UI.Avalonia`：移除 `DynamicGrid`，`FormView` 列表改用 `GridControl` 渲染（`ContentControl` 組合式控件，`DataGrid` 成員改經 `GridControl.InnerGrid`）。Blazor / MAUI `DynamicGrid` 不受影響。

### 安全性

- MessagePack：`3.1.4` → `3.1.7`（GHSA-hv8m-jj95-wg3x）— 修正 LZ4 解壓對惡意輸入拋 `AccessViolationException`（NU1903 高嚴重性）。

### 新增

- `Bee.UI.Avalonia`：field editor 控件組 — 七個編輯器與 `ControlType` 一一對應（`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`），含 `FieldEditorBinder`、`FormScope` attached property、`FieldEditorFactory`；`DynamicForm` 改經此組渲染。
- `Bee.UI.Avalonia`：新增 `GridControl` — 對應 `LayoutGrid` 的組合式 grid（`InnerGrid`），兩種綁定模式 + `FormScope` ambient 綁定、依 `LayoutColumn.ControlType` in-cell 編輯、`AllowActions` 增刪列、`AllowEdit`（[ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)）。
- `Bee.UI.Avalonia`：新增 `GridEditMode`（`InCell` / `EditForm`）+ `RowEditPanel` / `RowEditDialog`，底層為 `FormDataObject` 列編輯協定（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`）。
- `Bee.UI.Avalonia`：新增 `SingleFormBase`，持有並廣播 `FormMode`；`FormView` 繼承並引入 View / Edit / Add 模式生命週期。
- `Bee.Definition`：新增 `FormEditModes` `[Flags]` 列舉 + `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes`（預設 `All`）；與 `ReadOnly` / `AllowActions` AND 合成，預設值不落 XML。
- `Bee.UI.Avalonia`：`FormDataObject` 新增 `FieldValueChanged` / `DataSetReplaced` 事件（ADO.NET 橋接），另加列多載 `GetField` / `SetField`。
- `samples/Avalonia.Editors.Gallery`：原生 vs 繼承編輯器比對、in-cell 編輯、`EditForm` 模式比對區。
- DefineEditor：Semi.Avalonia 主題、Welcome tab、tab dirty 標記 + 右鍵選單 + 全部儲存、未儲存提示、macOS 選單補齊。

### 變更

- `Bee.UI.Avalonia`：`FormView` 載入資料改進唯讀 `View` 模式，須按 Edit 鈕才進編輯。

### 修正

- `Bee.Api`：MessagePack 3.1.5+ 黑名單擋掉 `DataTable` 反序列化；`SafeMessagePackSerializerOptions` 改為框架白名單優先放行。
- `Bee.UI.Avalonia`：`FormDataObject` async CRUD continuation 改回 UI 執行緒（移除 `ConfigureAwait(false)`）。
- `Bee.UI.Avalonia`：`DynamicForm` `DateEdit` 在非 UTC 時區不再拋例外。
- `Bee.UI.Avalonia`：`ComboBox` 選取框正常顯示選值；`DropDownEdit` / in-cell `ComboBox` 改用 `DisplayMemberBinding`。
- `Bee.UI.Avalonia`：`GridControl` 在 `AddRow` / `DeleteSelectedRow` 後重新 realize 列。
- `Bee.UI.Avalonia`：`ButtonEdit` 唯讀時停用內嵌 lookup 按鈕；圖示改為 chromeless `PathIcon`。
- Demo 後端啟動時建立 `st_cache_notify`，消除 `CacheNotifyPoller` warning。

## [4.8.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「框架預設定義升為一等公民」：所有 `st_*` 系統表 schema、框架預設 `Department` / `Employee` 表單、以及 bootstrap 設定 template 全部以 embedded resource 形式 ship 在 `Bee.Definition.dll` 內，透過新公開 API `Bee.Definition.Defaults` 對外存取。新增 `Bee.Cli` dotnet tool（`dotnet bee defines materialize ...`）+ DefineEditor 自動 materialize hook，把首次 setup 縮成一條指令。本版含 **一個 breaking change**：框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee` 對齊既有 `st_*` 命名空間。

📄 詳細變更與設計脈絡：[docs/changelogs/4.8.0.zh-TW.md](docs/changelogs/4.8.0.zh-TW.md)

### 破壞性變更

- 框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee`；已落地部署需自行 `RENAME TABLE`——範例見 [資料表結構升級指南 §框架表改名](docs/database-schema-upgrade.zh-TW.md)。FormSchema progId、C# 型別名、欄位名皆未變動。

### 新增

- `docs/framework-reserved-names.md`（雙語）：框架保留命名 registry（`st_*` 系統表、保留 `progId`）。
- `Bee.Definition`：框架預設定義檔（11 個 `st_*` `TableSchema` XML、`Department` / `Employee` 的 `FormSchema` / `FormLayout` / `Language`、精簡 `DbCategorySettings.xml`、`SystemSettings.xml` template、空殼 `DatabaseSettings.xml`）改以 embedded resource 形式 ship，naming 為 `Bee.Definition.Defaults/{相對路徑}`。
- `Bee.Definition.Defaults` API：`Defaults.MaterializeTo(path, options)`（skip-existing）、`Defaults.ListEmbedded()`、`Defaults.OpenEmbedded(relativePath)`；runtime `IDefineStorage` 不變。
- `TestProcessBootstrap.SharedDefinePath`：process-wide 合併後 define 目錄；`BeeTestFixture` 預設 `DefinePath` 改指向此處。
- `Bee.Cli` dotnet tool（`dotnet bee`）：`defines materialize --path ./Define [--overwrite] [--filter <prefix>]`、`defines list`、`--version`；版本與框架 lock-step，經 `nuget-publish.yml` 發佈。保留 subcommand group（`schema` / `tenant` / `samples`）本版尚未實作。
- DefineEditor 開啟資料夾時自動 materialize 框架預設（`Defaults.MaterializeTo`，skip-existing）；有寫入時 status bar 顯示物化檔數。

## [4.7.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「ERP 權限機制、i18n 與多租戶客製化全面落地」：新增權限線 A/B/record-scope 三段式機制、多國語系基礎建設、多租戶客製化覆蓋層、跨節點 DB 快取失效機制與「定義存 DB」儲存後端，並加開第三個桌面平台支援 — 新增 `Bee.UI.Avalonia` 套件。本版無 breaking change（既有公開 API 簽章未動）；但首次啟動會自動建立多張新系統表（`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company` 等），如以 framework 自動 schema 升級之外另自管 DDL 的部署需手動補建。

📄 詳細變更與設計脈絡：[docs/changelogs/4.7.0.zh-TW.md](docs/changelogs/4.7.0.zh-TW.md)

### 新增

- `Bee.UI.Avalonia`：新 Avalonia 12 桌面控制項套件 — `DynamicForm` / `DynamicGrid` / `FormView`、`FormDataObject`、`FileEndpointStorage`；附範例 `samples/Avalonia.Demo`。對應 [ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md)。
- ERP 權限機制（line-A + line-B + record-scope）：`PermissionModels` registry、`FormSchema.PermissionModelId`、`FormField.ScopeRole`、`AuthorizationService.Can`、`st_role` / `st_role_grant` / `st_user_role` 資料模型、`EnterCompany` 填充 `SessionInfo.Roles`、FormBO 權限 gate，以及 `ScopeResolver` 列級過濾 + `Update` / `Delete` 對 `sys_rowid` 權威 re-query。對應 [ADR-019](docs/adr/adr-019-permission-authorization-model.md)。
- i18n：`LanguageResource`（XML / JSON / MessagePack）、`ILanguageService` + `GetLangText`、`FormSchema` 自動本地化、`LangEnumName` 列舉下拉本地化、`SystemBO.GetLanguage` JSON-RPC 入口。
- 多租戶客製化覆蓋層：`CustomizeId` 全程隨身傳遞，定義讀取端疊加覆蓋（base define + customize override），整合至 `IDefineAccess`，`RemoteDefineAccess` 切換租戶時清快取。對應 [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)。
- DB 快取失效（跨節點）：`st_cache_notify` 表 + `ICacheNotifyService.Touch`、`CacheNotifyPoller` 背景輪詢 + 靜態路由 registry、以 `sys_update_time` 增量抓取。對應 [ADR-017](docs/adr/adr-017-db-cache-invalidation.md)。
- `DbDefineStorage`：`st_define` 表 + `DbDefineStorage` + `ICustomizeDefineReader`；定義可改存 DB（原 XML 路徑仍可用），DI 延遲解析打破與 `IDbAccessFactory` 的建構循環。對應 [ADR-018](docs/adr/adr-018-db-define-storage.md)。
- 組織部門樹：三棲 `DepartmentTree`（以 `DepartmentNode.Children` 巢狀）+ per-company 快取 + `GetDepartmentTree` JSON-RPC API。
- `ProgramItem.BusinessObject`：progId 可綁定 BO 型別，取代慣例命名解析。
- `tools/define-editor`：Avalonia 桌面工具，9 種定義型別視覺化編輯，支援 i18n live switch、自動驗證、單檔發佈、macOS `.app` bundle。non-shipping tool。

### 變更

- `DepartmentTree`：序列化由扁平 list 改為以 `DepartmentNode.Children` 巢狀。
- `st_cache_notify`：去除非系統欄的 `sys_` 前綴，系統欄保留。
- `CacheNotifyPoller`：改回以 `sys_update_time` 的 `O(1)` 增量抓取。

### 修正

- MySQL：statement-binlog 下 `ALTER ADD Guid NOT NULL DEFAULT (UUID())` replication-unsafe；dialect 拆為 `ADD COLUMN`（常數預設）+ `ALTER COLUMN SET DEFAULT (UUID())`。
- Oracle：`ALTER MODIFY ... NOT NULL` 對既已 NOT NULL 欄重發拋 ORA-01442；僅在 nullability 改變時才下 hint。
- Oracle：String / Text 欄一律建 nullable（`''` 視為 `NULL`，fresh `CREATE TABLE` 掛 ORA-01400）。
- MAUI `DynamicForm`：`SetField` 改為 idempotent、`ConvertToColumnValue` 補非 null fallback、`ReloadList` 保留 `sys_rowid`。
- `ObjectCaching`：以 lazy `FileModificationToken` 取代 `PhysicalFileProvider` 修正 CI 競爭（移除 `Microsoft.Extensions.FileProviders.Physical` 參考）。
- `DemoBusinessObjectFactory`：補上漏注入的 `ILanguageService`。
- `RolePermissionRepository`：SQL 串接補空格（SonarCloud S2857）。

## [4.6.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「開放 JSON-RPC 給 JS 前端」：FormBO / SystemBO 共 7 個 CRUD / Session 方法 `ProtectionLevel` 降為 `Public`、新增兩個 JSON-native 取得方法（`GetFormSchema` / `GetFormLayout`），並修正 Plain 路徑 DataSet 反序列化與 Blazor WebAssembly RSA 相關阻塞問題。`MasterKeySource` 預設值改為 `Environment`，依嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.6.0.zh-TW.md](docs/changelogs/4.6.0.zh-TW.md)

### 新增

- `Bee.Business`：`SystemBO.GetFormSchema` / `GetFormLayout` — JSON-native 取得方法，回傳 `FormSchema` / `FormLayout`；`.NET` 對應 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`；皆為 `Public + Authenticated`。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- `docs`：新增中英雙語 [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md) — wire format、headers、認證流程、可呼叫方法清單、`JsonRpcErrorCode` 對應表、TypeScript wrapper。

### 變更

- `Bee.Definition`：`MasterKeySource` 預設改為 `Environment`（從 `$BEE_MASTER_KEY` 讀取，不再產生 `Master.key`）（**breaking**）；已明確設定 `<Type>File</Type>` 的 host 不受影響。對應決策：[ADR-015](docs/adr/adr-015-master-key-environment-default.md)。
- `Bee.Business`：7 個 BO 方法 `ProtectionLevel` 降為 `Public` — `FormBO.GetNewData` / `GetData` / `Save` / `Delete` 與 `SystemBO.EnterCompany` / `LeaveCompany` / `Logout`（`Encrypted` → `Public`，仍 `Authenticated`）；向下相容。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- `Bee.Definition`：`FormSchema.MasterTable` 加 `[JsonIgnore]`（XML / MessagePack 不受影響）；JS / TS 客戶端改從 `tables[0]` 讀取，取代 `masterTable`。

### 修正

- `Bee.Base`：`RsaCryptor` 改用 PEM（SPKI / PKCS#1）取代 XML key 格式，並加 `OperatingSystem.IsBrowser()` fallback — 解封 Blazor WebAssembly 登入。
- `Bee.Api.Core`：`ApiInputConverter` 補齊 Plain 路徑的 `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`，修正 `DataSet` rows 為空與 `Save` 一律回 "DataSet has no pending changes"。
- `Bee.UI.Maui`：`DynamicForm` 新增 public `Refresh()` 驅動 `Rebuild()`，使 in-place `DataSet` mutation 後（New / Save / Delete）正確重繪。

### 升級指引

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

## [4.5.0]

> Bee.NET 仍處於 pre-stable 演進階段。本次新增三層前端套件（`Bee.UI.Core` 跨平台共通層、`Bee.UI.Maui` MAUI 行動／桌面控制項、`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` 兩個 Blazor RCL），並把 API connector 介面整批轉為 async-only。介面簽名變動由嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.5.0.zh-TW.md](docs/changelogs/4.5.0.zh-TW.md)

### 新增

- `Bee.UI.Core`：新增跨平台 UI 共通層（共用 ViewModel、`FormDataObject`、`SystemApiConnector`、`ClientInfo`），由 `bee-ui-core` 併入。對應 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。
- `Bee.UI.Maui`：新增 MAUI 控制項層，提供 `DynamicForm` / `DynamicGrid` / `FormPage` 與 `MauiPreferenceEndpointStorage`；預設 `net10.0`，平台 TFM 透過 `-p:BeeUiMauiFullPlatforms=true` 開啟。
- `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`：新增 Blazor RCL，提供 `DynamicForm` / `DynamicGrid` / `FormPage`、`BeeAccessTokenProvider`、`BeeLoginPanel`、`AddBeeBlazor`。
- `UserMessageException` + `JsonRpcErrorCode.UserMessage`：後端 throw 由 `ApiConnector` 重建為 client 端 `UserMessageException`，可直接以 `.Message` 呈現。
- `FormBusinessObject`：新增 `GetNewData` / `GetData` / `Save` / `Delete`，使 `IFormBusinessObject` 涵蓋完整單筆 CRUD。
- `samples/`：新增 demo 家族 — `QuickStart.Server` + `QuickStart.Console`、`Blazor.Server.Demo` + `Blazor.Wasm.Demo`、`Maui.Demo`；共用 `Bee.Samples.Shared` 並備有 `.smoke.yaml`。

### 變更

- `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector`：轉為 async-only，移除同步方法，改用 `*Async` 版本。
- `ExceptionExtensions`：由 `Bee.Base` 搬至 `Bee.Base.Exceptions`。
- `ClientInfo`：改為 static class，`ClientInfo.SystemApiConnector.Initialize()` 改為 async。見 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。

### 升級指引

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

## [4.4.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含 API 搬遷與少量 breaking change。本次包含介面簽名變動（`IFormRepositoryFactory.CreateDataFormRepository`、`IDataFormRepository.GetList`）與屬性移除（`CompanyInfo.LogDatabaseId`），嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.4.0.zh-TW.md](docs/changelogs/4.4.0.zh-TW.md)

### 新增

- `Bee.Business`：`FormBO.GetList` 統一查詢入口，透過 `IDataFormRepository` 並支援 `PagingOptions`/`PagingInfo`；`FormApiConnector.GetList`/`GetListAsync` 用戶端入口。
- `Bee.Business`：`SystemBO` 新增 `EnterCompany`/`LeaveCompany`/`Logout`；`SessionInfo` 加 nullable `CompanyId`；`Login` 補入 `ISystemBusinessObject`；新增 `CompanyInfo` 與 `ICompanyInfoService`。對應 [ADR-012](docs/adr/adr-012-session-company-context.md)。
- `Bee.Business`：新增 `DbScope` enum（`Common`/`Company`/`Log`）與 `IRepositoryDatabaseRouter`；`BusinessObject` 新增 `ResolveDatabaseId(DbScope)`、`CreateDataFormRepository(progId)` protected helper。對應 [ADR-010](docs/adr/adr-010-logical-database-category.md)。
- `Bee.Db`：`SelectCommandBuilder` 跨 5 dialect 分頁（`OFFSET/FETCH` 或 `LIMIT/OFFSET`）+ 新增 `BuildCount`。
- `Bee.ObjectCaching`：`KeyObjectCache<T>` 負向快取（預設 5 分鐘絕對過期，virtual `GetNegativePolicy` 可覆寫/停用）。對應 [ADR-009](docs/adr/adr-009-cache-implementation.md)。
- `Bee.Business`：`IBusinessObjectFactory` typed wrapper `CreateFormBO(token, progId)`／`CreateSystemBO(token)`。
- `Bee.Repository`：新增 `st_company`/`st_user_company` 系統表 + `ICompanyRepository`/`IUserCompanyRepository`；預設 common `DbCategorySettings` 已含此兩表。
- `JsonRpcErrorCode`：新增 `CompanyNotEntered` (-32002, HTTP 409)、`CompanyAccessDenied` (-32003, HTTP 403)。

### 變更

- `IFormRepositoryFactory.CreateDataFormRepository`：加入 `Guid accessToken` 參數，配合 `IRepositoryDatabaseRouter` 路由。
- `IDataFormRepository.GetList`：回傳 `DataFormListResult`（`Table` + `Paging`），加入 `PagingOptions? paging` default 參數。
- `CompanyInfo.LogDatabaseId` 移除：`DbScope.Log` 改為固定 `databaseId = "log"`。
- `SelectCommandBuilder`：未知表名改拋 `InvalidOperationException`（原 `KeyNotFoundException`）。

### 升級指引

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // 框架固定路由；跨公司隔離改用 sys_company_rowid 列級分區
```

## [4.3.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含命名空間搬遷。本次調整以嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.3.0.zh-TW.md](docs/changelogs/4.3.0.zh-TW.md)

### 新增

- `Bee.Hosting`：新套件 — 框架 composition root，將所有後端服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊到任意 `IServiceCollection`，不依賴 ASP.NET Core。

### 變更

- `Bee.Hosting`：`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 由 `Bee.Api.AspNetCore` 搬入（命名空間 `Bee.Api.AspNetCore` → `Bee.Hosting`）。
- `Bee.Api.AspNetCore`：現在僅包含 ASP.NET Core 整合（`UseBeeFramework` + `ApiServiceController`）；原有 4 個 ProjectReference 全部合併至 `Bee.Hosting`。

### 升級指引

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

```diff
  <!-- *.csproj -->
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.2.*" />
+ <PackageReference Include="Bee.Hosting" Version="4.3.*" />
```

## [4.2.0] 與更早版本

見 git 歷史（`git log --oneline`）。
