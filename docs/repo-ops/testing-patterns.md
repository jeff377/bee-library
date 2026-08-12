# 測試撰寫樣板

本 repo 自家測試套件（`tests/Bee.*.UnitTests`）的可貼用樣板。**維運文件、非公開文件**
——讀者是 bee-library 的維護者，不是框架使用者。

**規則與判準常駐於 `.claude/rules/testing.md`**（哪種情境用哪個 attribute、哪個 fixture、
env var 命名規則、全域狀態的平行安全要求）。本檔只放程式碼形狀，**不重複條文**。
兩邊有衝突時以 `rules/testing.md` 為準。

---

## 基本形狀

### 單一驗證：`[Fact]`

```csharp
[Fact]
[DisplayName("建立 Session 應回傳有效 Token")]
public void CreateSession_ReturnsValidToken()
{
    var token = _repo.CreateSession(user);
    Assert.NotNull(token);
}
```

### 參數化：`[Theory]` + `[InlineData]`

```csharp
[Theory]
[InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
[InlineData(DefineType.UserSettings, typeof(UserSettings))]
[DisplayName("ToClrType 回傳正確型別")]
public void ToClrType_ValidType(DefineType defineType, Type expectedType)
{
    var result = defineType.ToClrType();
    Assert.Equal(expectedType, result);
}
```

### 需要資料庫：`[DbFact(DatabaseType)]`

每個 `DatabaseType` 各寫一支，連線 ID 用 `common_{dbtype_lower}`
（由 `TestDbConventions.GetDatabaseId` 產生）：

```csharp
[DbFact(DatabaseType.SQLServer)]
[DisplayName("SQL Server 上 ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_SqlServer_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common_sqlserver");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}

[DbFact(DatabaseType.PostgreSQL)]
[DisplayName("PostgreSQL 上 ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_PostgreSQL_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common_postgresql");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}
```

### 需要本機服務：`[LocalOnlyFact]` / `[LocalOnlyTheory]`

> **下面是示意、不是現存程式碼——別去 grep 它。** 兩個 attribute 目前無使用者
> （2026-08-11 實測），留著是因為「需要本機服務的整合測試」這個情境仍成立。

```csharp
[LocalOnlyTheory]
[InlineData("http://localhost/jsonrpc/api")]
[DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
public void Validate_ValidUrl_ReturnsRemoteConnectType(string apiUrl) { ... }
```

---

## Per-class fixture

需要 DI-resolved 後端服務（`IDefineAccess` / `ISessionInfoService` /
`IBusinessObjectFactory` 等）時：

```csharp
public class MyTests : IClassFixture<BeeTestFixture>
{
    private readonly BeeTestFixture _fx;
    public MyTests(BeeTestFixture fx) { _fx = fx; }

    [Fact]
    public void Foo()
    {
        var access = _fx.GetRequiredService<IDefineAccess>();
        // ...
    }
}
```

Fixture 的選擇（`BeeTestFixture` / `UseTempDefinePath` / `SharedDbFixture`）見
`.claude/rules/testing.md` —— **選錯 fixture 是「本機綠、CI 紅」的頭號成因**，
那條判準留在常駐區。

---

## 寫檔隔離：`SaveDefine` 系列必須切到 temp

規則見 `.claude/rules/testing.md`（`tests/Define/` 是多專案共用的固定資料，不得寫入）。
以下是兩種做法的形狀。

### Fixture-level（推薦）

```csharp
public sealed class WritableDefineFixture : BeeTestFixture
{
    public WritableDefineFixture() : base(b => b.UseTempDefinePath()) {}
}

public class MySaveTests : IClassFixture<WritableDefineFixture>
{
    private readonly WritableDefineFixture _fx;
    public MySaveTests(WritableDefineFixture fx) { _fx = fx; }

    [Fact]
    public void SaveDbCategorySettings_WritesFile()
    {
        var access = _fx.GetRequiredService<IDefineAccess>();
        access.SaveDbCategorySettings(new DbCategorySettings());
        Assert.True(File.Exists(_fx.PathOptions.GetDbCategorySettingsFilePath()));
    }
}
```

### Method-level inline temp dir

對純資料寫入測試（不需 DI），inline temp dir 比建立 fixture subclass 更輕：

```csharp
[Fact]
public void SaveSystemSettings_WritesFile()
{
    var tempDir = Path.Combine(Path.GetTempPath(), $"bee-save-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    try
    {
        var paths = new PathOptions { DefinePath = tempDir };
        var access = new CacheDefineAccess(new FileDefineStorage(paths), paths);
        access.SaveSystemSettings(new SystemSettings());
        Assert.True(File.Exists(paths.GetSystemSettingsFilePath()));
    }
    finally
    {
        try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
    }
}
```

---

## 全域狀態的序列化 marker

需要保護尚未 DI 化的 process-wide static 時（判準與現存清單見 `rules/testing.md`）：

```csharp
// 1. 在 test 專案根目錄宣告 collection
[CollectionDefinition("DbConnectionState")]
public class DbConnectionStateCollection
{
    // 純 marker，無 fixture
}

// 2. 所有會修改該 static 的 test class 加同一 [Collection]
[Collection("DbConnectionState")]
public class DbConnectionManagerTests { ... }

[Collection("DbConnectionState")]
public class DbAccessFactoryTests { ... }
```

**用 `const` 而非字串字面值**（如 `ProcessWideStateCollection.Name`）：打錯字的字面值會讓
xUnit 建一個沒人共用的隱式分組，看起來有序列化、實際沒有，且不會有編譯錯。
