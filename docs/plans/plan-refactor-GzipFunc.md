# 計畫：重構 `GzipFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫：[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/Serialization/GzipFunc.cs`(62 行,2 個 public 方法 + 1 個私有常數)

```csharp
namespace Bee.Base.Serialization;

public static class GzipFunc
{
    private const long MaxDecompressedBytes = 50 * 1024 * 1024; // 50 MB(zip bomb 防護)

    public static byte[] Compress(byte[] bytes);
    public static byte[] Decompress(byte[] bytes);
}
```

> 主計畫進度表寫 3 個方法,實際 audit 後僅 2 個 public method,
> 第 3 項應為私有常數 `MaxDecompressedBytes`,清點誤計。

## Method Audit 表

| # | 方法簽章 | 處理路徑 | 新位置/名稱 | 替代方案備註 |
|---|---------|--------|------------|------------|
| 1 | `Compress(byte[] bytes)` | C | `Bee.Base.Serialization.Gzip.Compress` | 同 namespace,類別改名 |
| 2 | `Decompress(byte[] bytes)` | C | `Bee.Base.Serialization.Gzip.Decompress` | 同 namespace,類別改名 |

### 命名選擇理由

考慮過三種方案:

| 方案 | 樣式 | 評估 |
|------|------|------|
| A. `Gzip`(靜態 utility) | `Gzip.Compress(bytes)` | ✅ **採用**。對齊 BCL `Path`、`Convert`、`Encoding` 等 static utility 命名 |
| B. `byte[]` 擴充方法 | `bytes.GzipCompress()` | ❌ 否決。`byte[]` 太通用,擴充會污染所有 byte 陣列的 IntelliSense |
| C. `GzipExtensions` | — | ❌ 否決。沒有 `this` 擴充就不該叫 `*Extensions` |

額外考量:
- BCL 已有 `System.IO.Compression.GZipStream`,但沒有 `byte[] → byte[]` 的高階 helper,因此保留 wrapper 有意義
- `Decompress` 內含 zip bomb 防護(50 MB 上限),非單純的 BCL stream 替代,屬於本框架專屬安全邏輯

### 不採 path A(BCL 替代)的理由

直接用 `GZipStream` 雖然可行,但呼叫端需自行處理:
1. `MemoryStream` 生命週期
2. zip bomb 防護的 50 MB 上限檢查

這 2 個都是橫跨呼叫點的關注,wrapper 必須保留。

## 影響範圍

**全 repo grep `GzipFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品程式碼 | `src/Bee.Base/Serialization/GzipFunc.cs` | 1(類別定義本身) |
| 產品程式碼 | `src/Bee.Api.Core/Transformers/GzipPayloadCompressor.cs` | 2 |
| 測試 | `tests/Bee.Base.UnitTests/GzipFuncTests.cs` | 4(類別內 4 處 `GzipFunc.X` 呼叫) |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1(主計畫表中提及) |

合計 8 處 caller,集中在 3 個檔案,改動範圍小。

## 執行步驟

1. **改檔名與類別名**
   - `src/Bee.Base/Serialization/GzipFunc.cs` → `src/Bee.Base/Serialization/Gzip.cs`
   - 內部 `public static class GzipFunc` → `public static class Gzip`
   - XML doc summary 文字 `Utility library for GZip compression and decompression.` 保留(僅類名變)

2. **改測試檔名與類別名**
   - `tests/Bee.Base.UnitTests/GzipFuncTests.cs` → `tests/Bee.Base.UnitTests/GzipTests.cs`
   - 內部 `public class GzipFuncTests` → `public class GzipTests`
   - 4 處 `GzipFunc.Compress` / `GzipFunc.Decompress` → `Gzip.Compress` / `Gzip.Decompress`

3. **改產品端 caller**
   - `src/Bee.Api.Core/Transformers/GzipPayloadCompressor.cs`:2 處 `GzipFunc.X` → `Gzip.X`

4. **更新主計畫**
   - 進度表第 1 列:`📝` → `✅`,完成日填入
   - 將「方法數 3」修正為 2,並加備註說明清點差異

## 驗證

```bash
# 確認沒有遺漏的 GzipFunc 引用
grep -rn "GzipFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore
dotnet build src/Bee.Api.Core/Bee.Api.Core.csproj --configuration Release --no-restore

# Test(影響範圍只有 Bee.Base.UnitTests)
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄文字
- Build 0 warning, 0 error
- Bee.Base.UnitTests 全綠(GzipTests 3 個 test method 應正常通過)

## Commit 訊息草稿

```
refactor(base): rename GzipFunc to Gzip

Align with .NET BCL static utility naming (Path, Convert, Encoding).
Public methods unchanged; namespace Bee.Base.Serialization preserved.

First class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

本次決定的 idiom,後續同 path C 類別沿用:

- **path C 類別命名**:去掉 `Func` 後綴,以名詞做 namespace 內的靜態 utility(對齊 BCL `Path`/`Convert` 等)
- 不另立 `*Extensions` 命名(除非真的有 `this` 擴充)
- 不擴充 `byte[]`、`object` 等過度通用型別

## 風險與回滾

- 範圍小(8 處 caller,3 個檔案),單一 commit 即可完成
- 無外部 NuGet 消費者,可接受 public API breaking
- 若 build 或 test 失敗,單一 `git revert` 即可回滾
