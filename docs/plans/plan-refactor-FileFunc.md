# 計畫：重構 `FileFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/FileFunc.cs`(257 行,**21 個 public 方法** + 1 個 private)

分為 3 區塊:File / Directory / Path methods。

> 主計畫進度表寫 22,實際 audit 後 21 個。

## 設計分類

依「prod caller 數」與「是否純 BCL 包裝」判斷:

### Group A:整個刪除(10 個,0 prod caller)

| 方法 | 原因 |
|------|------|
| `FileDelele` | typo + 0 caller |
| `FileToBytes` | 純 `File.ReadAllBytes` 包裝,0 caller |
| `BytesToFile` | DirectoryCheck + WriteAllBytes,**0 caller** |
| `FileToStream` | 0 caller |
| `StreamToFile` | DirectoryCheck + Seek + Copy,**0 caller** |
| `DirectoryCreate` | 純 `Directory.CreateDirectory`,0 caller |
| `IsPathRooted` | 純 `Path.IsPathRooted`,0 caller |
| `GetParentDirectory` | 純 `Directory.GetParent(...).FullName`,0 caller |
| `GetFileName` | `Path.GetFileName` 加 bool 參數的 overload,0 caller |
| `GetAppPath` | `AppDomain.CurrentDomain.BaseDirectory`,0 caller |

### Group B:Path A 刪除 + inline 至 callers(5 個,純 BCL 包裝)

| 方法 | Prod | 改後 |
|------|------|------|
| `FileExists(path)` | **9** | `File.Exists(path)` |
| `DirectoryExists(path)` | 1 | `Directory.Exists(path)` |
| `GetDirectory(path)` | 1 | `Path.GetDirectoryName(path)` |
| `GetExtension(path)` | 1 | `Path.GetExtension(path)` |
| `PathCombine(paths)` | 6 | `Path.Combine(paths)` |

合計 18 處 caller 改為 BCL 直呼。所有方法都是 1-line 純包裝,改成 BCL 同樣易讀且不需透過 `FileFunc` 中間層。

### Group C:Path C 保留 + 改名(5 個 + 1 private,有框架加值)

| 方法 | Prod | 加值內容 |
|------|------|---------|
| `FileWriteText(path, contents)` | 2 | UTF-8 no BOM 預設 + DirectoryCheck |
| `FileWriteText(path, contents, encoding)` | (overload)| DirectoryCheck + 指定編碼 |
| `FileReadText(path)` | 2 | 檔案不存在時回傳空字串(非拋例外)|
| `IsLocalPath(input)` | 1 | Regex 判斷 Windows 本機路徑 / UNC |
| `GetAssemblyPath()` | 1 | `AppDomain.RelativeSearchPath` 邏輯 |
| `DirectoryCheck(path, isFilePath)` | (internal)| 上面 writer 方法的 helper,改為 private |

`FileFunc` → **`FileUtilities`**(對齊 `HttpUtilities` idiom)。

### Path 選擇理由

- **不選 path B(string 擴充方法)**:`string` 過度通用,擴充 path 操作會污染所有 string IntelliSense
- **不選保留整個 `FileFunc`(path C 整體 rename)**:21 個方法中 15 個是 0-caller 或純 BCL 包裝,留著只是技術債
- **不拆 `PathUtilities` / `TextFile` 等多類**:剩下 5 個方法太少,拆多類增加 cognitive load,KISS

### CA1724 檢查

`FileUtilities` 不對應任何 BCL namespace 末段,安全。

## 影響範圍

**全 repo grep `FileFunc.` 結果**:53 處(prod 27 + tests 26)

| 檔案 | 改寫方式 |
|------|---------|
| `src/Bee.Api.Client/ApiConnectValidator.cs` | 6 處改 BCL inline / FileUtilities |
| `src/Bee.Base/AssemblyLoader.cs` | 2 處 |
| `src/Bee.Base/Serialization/{Json,Xml}Codec.cs` | 4 處(FileWriteText/FileReadText)→ FileUtilities |
| `src/Bee.Base/Serialization/SerializationExtensions.cs` | 1 處(GetExtension)→ Path.GetExtension |
| `src/Bee.Definition/DefinePathInfo.cs` | 1 處(PathCombine)→ Path.Combine |
| `src/Bee.Definition/Security/MasterKeyProvider.cs` | 1 處 → Path.Combine |
| `src/Bee.Definition/Storage/FileDefineStorage.cs` | 1 處 → File.Exists |
| `src/Bee.ObjectCaching/Define/{Database,Program,System}SettingsCache.cs` | 3 處 → File.Exists |
| `tests/Bee.Base.UnitTests/FileFuncTests.cs` | 大幅重寫(刪除 deleted method 的測試,保留有意義者)|

## 執行步驟

### 1. 新增 `src/Bee.Base/FileUtilities.cs`

只包含 5 個保留方法 + 1 個 private `DirectoryCheck`。

```csharp
namespace Bee.Base;

public static class FileUtilities
{
    public static void FileWriteText(string filePath, string contents) { ... }  // UTF-8 no BOM
    public static void FileWriteText(string filePath, string contents, Encoding encoding) { ... }
    public static string FileReadText(string filePath) { ... }  // empty on missing
    public static bool IsLocalPath(string input) { ... }  // Windows path / UNC regex
    public static string GetAssemblyPath() { ... }  // AppDomain logic
    private static void DirectoryCheck(string path, bool isFilePath = false) { ... }
}
```

### 2. 更新 18 處生產端 caller(perl 批次)

對應替換表:
```
FileFunc.FileExists(    →  File.Exists(
FileFunc.DirectoryExists(  →  Directory.Exists(
FileFunc.GetDirectory(  →  Path.GetDirectoryName(
FileFunc.GetExtension(  →  Path.GetExtension(
FileFunc.PathCombine(   →  Path.Combine(
FileFunc.FileWriteText( →  FileUtilities.FileWriteText(
FileFunc.FileReadText(  →  FileUtilities.FileReadText(
FileFunc.IsLocalPath(   →  FileUtilities.IsLocalPath(
FileFunc.GetAssemblyPath(  →  FileUtilities.GetAssemblyPath(
```

各 caller 檔案需檢查是否需新增 `using System.IO;`(BCL `File`/`Directory`/`Path` 都在此 namespace)。

### 3. 刪除 `FileFunc.cs`

```bash
git rm src/Bee.Base/FileFunc.cs
```

### 4. 拆解測試

`tests/Bee.Base.UnitTests/FileFuncTests.cs` → 改名為 `FileUtilitiesTests.cs`,大幅刪減:

**保留測試**:`FileWriteText` / `FileReadText`(UTF-8 / 編碼 / 不存在)/ `IsLocalPath` / `GetAssemblyPath` / `DirectoryCheck`(透過 FileWriteText 間接驗證)

**刪除測試**:
- 對應 Group A 刪除方法的:`FileDelele`、`FileToBytes`/`BytesToFile`、`FileToStream`/`StreamToFile`、`DirectoryCreate`、`IsPathRooted`、`GetParentDirectory`、`GetFileName`、`GetAppPath`(7-8 個 test)
- 對應 Group B inline 的(純 BCL passthrough,測試只是驗證 BCL 行為,trivial):`FileExists`、`DirectoryExists`、`GetDirectory`、`GetExtension`、`PathCombine`(5-6 個 test)

預期測試數約從 19 縮為 7-8 個。

### 5. 更新主計畫

進度表第 10 列:`📝` → `✅`,完成日填入,方法數 `22` → `21`,處理路徑 `A+C`。

## 驗證

```bash
grep -rn "FileFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore
# 受影響的多個 src 專案都需 build
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內歷史紀錄
- 全部 0 warning, 0 error
- Bee.Base.UnitTests 測試數**減少約 11-12 個**(刪除 deleted method 的測試 + 純 BCL 測試),其餘全綠

## Commit 訊息草稿

```
refactor(base): split FileFunc — inline pure BCL wrappers, keep framework conventions in FileUtilities

FileFunc had 21 methods. Audit showed three buckets:

A. Ten methods with zero production callers (FileDelele [sic],
   FileToBytes / BytesToFile, FileToStream / StreamToFile,
   DirectoryCreate, IsPathRooted, GetParentDirectory, GetFileName,
   GetAppPath) — deleted entirely.

B. Five pure BCL wrappers with production callers (FileExists,
   DirectoryExists, GetDirectory, GetExtension, PathCombine) —
   inlined at 18 call sites to use File.Exists / Directory.Exists
   / Path.GetDirectoryName / Path.GetExtension / Path.Combine
   directly. The wrappers added zero value over BCL.

C. Five methods with framework value-add (FileWriteText with UTF-8
   no BOM default, FileReadText with empty-on-missing semantic,
   IsLocalPath regex check, GetAssemblyPath AppDomain logic, plus
   DirectoryCheck as private helper) move to FileUtilities,
   aligning with HttpUtilities idiom from the earlier HttpFunc
   rename.

FileFunc.cs is removed entirely. FileFuncTests renamed to
FileUtilitiesTests with the deleted-method tests dropped.

Tenth class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

無新原則,沿用既有 idiom:
- 同 `DateTimeFunc.IsDate/Format` 的 path A 刪除原則:0 prod caller + 純 BCL 包裝 = 刪除
- 同 `HttpFunc → HttpUtilities` 的 path C rename idiom(避開 BCL `File`/`Path`/`Directory` 命名衝突)
- 同 `BusinessFunc` 拆解原則:整個類別內方法走不同 path,逐一判斷

## 風險與回滾

- 變動範圍大:18 處 prod caller + 大量測試重組
- 但每個替換確定性高(BCL inline 純機械式)
- Public API breaking:`FileFunc` 整個移除,16 個方法消失或搬位置
- 無外部 NuGet 消費者,可接受
- 若失敗單一 `git revert` 即可回滾
