# Bee.DefineEditor

Bee.NET 定義檔（DefinePath 下的 9 種 XML）的桌面維護工具。Avalonia 12 + .NET 10 + CommunityToolkit.Mvvm，跨平台（Windows / macOS / Linux）。

## 定位

- **開發期工具**：非框架發布套件、非 sample；放在 `tools/`，獨立 `Bee.Tools.slnx`，不上 NuGet、不跑 CI。
- **純 offline**：直接讀寫 DefinePath 下的 XML，不連遠端 server、不連資料庫。
- **與框架同步**：以 ProjectReference 連 `Bee.Definition` 與 `Bee.Base`，所有讀寫走 `XmlCodec.SerializeToFile` / `DeserializeFromFile`，零序列化轉換。

## 支援的定義型別

開啟方案後，左側方案樹按型別分組列出 DefinePath 下所有檔案；點選右側載對應編輯器：

| 型別 | 編輯器 | 主要功能 |
|------|--------|---------|
| **SystemSettings**（單例） | [SystemSettingsDocumentView](Views/SystemSettingsDocumentView.axaml) | 5 個 Configuration 子節點 + BackendConfiguration 4 個內嵌 options + ExtendedProperties 自由 KV |
| **DbCategorySettings**（單例） | [DbCategorySettingsDocumentView](Views/DbCategorySettingsDocumentView.axaml) | Categories → Tables 兩層；驗證重複 Id / TableName |
| **ProgramSettings**（單例） | [ProgramSettingsDocumentView](Views/ProgramSettingsDocumentView.axaml) | Categories → Programs 兩層；ProgramItem 含 ProgId / DisplayName / BusinessObject |
| **PermissionModels**（單例） | [PermissionModelsDocumentView](Views/PermissionModelsDocumentView.axaml) | Models → Rules 兩層；Action / Scope 為下拉；含 `PermissionModels.Validate()` 整合 |
| **DatabaseSettings**（單例） | [DatabaseSettingsDocumentView](Views/DatabaseSettingsDocumentView.axaml) | Servers + Items 兩個 group；含 **連線字串貼上拆解**（SQL Server / PostgreSQL / MySQL / Oracle） + 4 類靜態驗證 |
| **FormSchema**（多份） | [FormSchemaDocumentView](Views/FormSchemaDocumentView.axaml) | Tables → Fields → Relation / Lookup 對應；RelationProgId 來自方案內其他 FormSchema 候選 |
| **TableSchema**（多份） | [TableSchemaDocumentView](Views/TableSchemaDocumentView.axaml) | Fields + Indexes 兩個 group；IndexField 含 SortDirection；驗證 PrimaryKey 唯一性 |
| **FormLayout**（多份） | [FormLayoutDocumentView](Views/FormLayoutDocumentView.axaml) | Sections（→ LayoutField）+ Details（LayoutGrid → LayoutColumn） |
| **Language**（多份） | [LanguageDocumentView](Views/LanguageDocumentView.axaml) | Items（Key/Value）+ Enums（→ Entry code/text） |

每個編輯器都有共用的工具列（儲存 / 新增 / 驗證 / 刪除）、底部狀態列、驗證結果面板與 `IsDirty` 指示。

## 開發期跑法

直接從原始碼啟動：

```bash
dotnet run --project tools/DefineEditor/Bee.DefineEditor.csproj --configuration Debug
```

啟動後左上「開啟方案…」選一個 DefinePath 資料夾即可。`tests/Define/` 內含可直接開啟的測試 fixture。

### Headless smoke

`--smoke <FormSchema-fixture-path>` 模式跑全部 round-trip（FormSchema + 8 個 multi-instance editor + ConnectionStringParser），不啟動視窗：

```bash
dotnet run --project tools/DefineEditor/Bee.DefineEditor.csproj --configuration Debug \
    -- --smoke tests/Define/FormSchema/Employee.FormSchema.xml
```

預期輸出：

```
[smoke:formschema] OK
[smoke:permission]  OK (0 non-error issues)
[smoke:db]          OK
[smoke:program]     OK
[smoke:system]      OK
[smoke:db-settings] OK
[smoke:parser]      OK (SQL Server + PostgreSQL + dialect-mismatch warning)
[smoke:table-schema] OK
[smoke:form-layout] OK
[smoke:language]    OK
[smoke] OK — FormSchema + 8 multi-instance editors + ConnectionStringParser all green.
```

## Publish（framework-dependent）

預設打包 framework-dependent — 不含 .NET runtime，目標機要先裝 .NET 10。每個 RID 約 31 MB（含 Avalonia 該平台原生依賴）。

```bash
# macOS Apple Silicon
dotnet publish tools/DefineEditor/Bee.DefineEditor.csproj -c Release \
    -r osx-arm64 --self-contained false -p:PublishTrimmed=false

# macOS Intel
dotnet publish tools/DefineEditor/Bee.DefineEditor.csproj -c Release \
    -r osx-x64 --self-contained false -p:PublishTrimmed=false

# Windows x64
dotnet publish tools/DefineEditor/Bee.DefineEditor.csproj -c Release \
    -r win-x64 --self-contained false -p:PublishTrimmed=false

# Linux x64
dotnet publish tools/DefineEditor/Bee.DefineEditor.csproj -c Release \
    -r linux-x64 --self-contained false -p:PublishTrimmed=false
```

輸出在 `tools/DefineEditor/bin/Release/net10.0/<rid>/publish/`。或執行 [publish.sh](publish.sh) 一次打包 4 個平台。

### 也可以 self-contained

若目標機不便裝 .NET runtime，可內含 runtime 一起打包（約 100–210 MB／平台）：

```bash
./publish.sh --self-contained
# 或單一 RID
dotnet publish tools/DefineEditor/Bee.DefineEditor.csproj -c Release \
    -r osx-arm64 --self-contained true -p:PublishTrimmed=false
```

### RID 為何不可省略

省略 `-r` 改打 portable 雖然也是 framework-dependent，但 Avalonia 的原生依賴（Skia / Avalonia.Native 等）會把所有平台的 `runtimes/<rid>/native/*` 全帶上，結果反而比 self-contained 還大（實測 564 MB）。所以即使是 framework-dependent，仍指定 RID。

### 已知不打開的選項

| 選項 | 不開的原因 |
|------|-----------|
| `PublishSingleFile=true` | `Bee.Base.AssemblyLoader` 用 `Module.Name`（含 `RequiresAssemblyFilesAttribute`），strict 模式下 IL3002 build 失敗 |
| `PublishTrimmed=true` | 框架重度依賴 XmlSerializer 的反射展開；trim 容易把 nested define type 的 metadata 砍掉導致 runtime 解序列化失敗 |

## 不在工具範圍

- 連遠端 Bee server / JSON-RPC API（純本機檔）
- DatabaseSettings 實連測試（交 server 端健康檢查）
- 多人協作 / 鎖定機制
- Customize 層覆蓋編輯（Phase 6 後若需要另議；目前在方案樹上不標示覆蓋）

詳見 [docs/plans/plan-define-editor.md](../../docs/plans/plan-define-editor.md)。
