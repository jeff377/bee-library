# Define/ 設定樹

五個 XML（+ TableSchema 資料夾）。放在 host 專案的 `Define/` 下，`Program.cs` 靠 walk-up 定位。
以下為可貼用最小內容（SQLite 單檔 dev 設定）。

## SystemSettings.xml — 根設定

```xml
<?xml version="1.0" encoding="utf-8"?>
<SystemSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <CommonConfiguration>
    <Version>1.0.0</Version>
    <IsDebugMode>true</IsDebugMode>
    <!-- 你的 args/result 命名空間；Encoded/Encrypted 的 typeless 序列化靠這個白名單 -->
    <AllowedTypeNamespaces>Xxx.Server.Contracts</AllowedTypeNamespaces>
    <!-- body codec 不在這裡設：由每個請求在信封宣告，未宣告即 MessagePack（adr-044）。 -->
    <ApiPayloadOptions>
      <Compressor>gzip</Compressor>
      <Encryptor>aes-cbc-hmac</Encryptor>
    </ApiPayloadOptions>
  </CommonConfiguration>
  <BackendConfiguration>
    <LogOptions>
      <DbAccess>
        <Level>Warning</Level>
        <AffectedRowThreshold>10000</AffectedRowThreshold>
        <ResultRowThreshold>10000</ResultRowThreshold>
        <ExecutionTimeThreshold>300</ExecutionTimeThreshold>
      </DbAccess>
    </LogOptions>
    <SecurityKeySettings>
      <MasterKeySource>
        <Type>Environment</Type>
        <Value>BEE_MASTER_KEY</Value>
      </MasterKeySource>
    </SecurityKeySettings>
    <Components>
      <!-- 用程式 DI 覆寫 factory 時這裡留空即可 -->
      <BusinessObjectProvider></BusinessObjectProvider>
    </Components>
  </BackendConfiguration>
  <FrontendConfiguration />
  <WebsiteConfiguration />
  <BackgroundServiceConfiguration />
</SystemSettings>
```

## DatabaseSettings.xml — 邏輯 DB id → 連線字串

`common` 是**框架強制**（st_session、st_cache_notify）。`company` 放業務資料。dev 單檔可共用同一個 .db。

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Items>
    <DatabaseItem Id="common"  CategoryId="common"  DisplayName="Shared (SQLite)"
        DatabaseType="SQLite" ConnectionString="Data Source=xxx.db;Cache=Shared" />
    <DatabaseItem Id="company" CategoryId="company" DisplayName="Company (SQLite)"
        DatabaseType="SQLite" ConnectionString="Data Source=xxx.db;Cache=Shared" />
  </Items>
</DatabaseSettings>
```

## DbCategorySettings.xml — 哪些表屬於哪個 category/DB

seeder 迭代這裡建表。加一張表 = 新增一個 `TableSchema` 檔 + 這裡一個 `TableItem`。

```xml
<?xml version="1.0" encoding="utf-8"?>
<DbCategorySettings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Categories>
    <DbCategory Id="company" DisplayName="Company Database">
      <Tables />   <!-- <TableItem Id="ft_xxx" /> ... -->
    </DbCategory>
  </Categories>
</DbCategorySettings>
```

## ProgramSettings.xml — 程式清單 + （可選）宣告式 BO 綁定

用**程式 resolver** 綁 progId→BO 時，這裡可留空（或只放選單項）。用**宣告式**綁定時：

```xml
<?xml version="1.0" encoding="utf-8"?>
<ProgramSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Categories>
    <ProgramCategory Id="main" DisplayName="Main">
      <Items>
        <ProgramItem ProgId="Game" DisplayName="Game"
            BusinessObject="Xxx.Server.BusinessObjects.GameBO, Xxx.Server" />
        <!-- BusinessObject 留空 → 走框架預設 FormBusinessObject（定義驅動 CRUD） -->
      </Items>
    </ProgramCategory>
  </Categories>
</ProgramSettings>
```
`BusinessObject` 是 **assembly-qualified 型別名**：`"Namespace.Type, AssemblyName"`。

## TableSchema/

實體 DB schema，依 category 分資料夾：
- `TableSchema/common/st_cache_notify.TableSchema.xml`（由 `Defaults.MaterializeTo` materialize，不用手寫）
- `TableSchema/company/ft_xxx.TableSchema.xml`（你的業務表）

每個檔定義 `<Fields>`（`<DbField FieldName DbType Length>`）與 `<Indexes>`。Bee 慣例欄位：`sys_no`
（AutoIncrement PK）、`sys_rowid`（Guid 關聯鍵，unique `rx_`）、`sys_id`（字串業務碼，unique `uk_`）、
`sys_name`。外鍵是 `*_rowid` Guid 欄 + `fk_` 索引。`{0}` 佔位符 = 表名。

> **dev 免業務表也能跑**：只要 `common` DB + st_cache_notify 存在，`System.Ping`/`Login` 與回記憶體/seed
> 資料的 BO 就能運作。業務表可之後逐步加。

## appsettings.json / launchSettings.json

- `appsettings.json`：只放 logging；**Bee 設定不在這**（在 Define + 環境變數）。連線字串在 DatabaseSettings.xml。
- `launchSettings.json`：`applicationUrl` 決定 dev port。
- **API key 是 client 端常數**，不是 server 設定（預設驗證只檢查非空）。
