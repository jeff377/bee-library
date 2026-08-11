# Bee.Expressions

[English](README.md)

框架運算式引擎以 DynamicExpresso 為底的實作。由商業邏輯層（存檔前的欄位計算與規則驗證）與
UI 用戶端（輸入時的即時預覽）共用，因此同一個計算欄位在兩邊得到相同結果。

## 主要公開 API

| 型別 | 用途 |
|------|------|
| `DynamicExpressoEvaluator` | 預設的 `IExpressionEvaluator`。解析與編譯一次，以「運算式文字 + 參數簽章」快取，之後逐列呼叫 |

## 抽象位於 `Bee.Base`

`IExpressionEvaluator`、`ExpressionPolicy`、`ExpressionEvaluationException` 在
`Bee.Base.Expressions`，不在本套件。這個分界讓 `Bee.Definition` 與 `Bee.Business` 完全不相依
DynamicExpresso——它們透過抽象消費引擎，只有組裝層（`Bee.Hosting`，或自建 evaluator 的
UI head）才引用本套件。見 [ADR-038](../../docs/adr/adr-038-definition-dependency-boundary.md)。

**需要「挑一個實作」時引用本套件；只需要「接受一個實作」時引用 `Bee.Base` 即可。**

## 時區

`Evaluate` 接收 `timeZoneId`。讀取時鐘的 helper（`Today()`、`Now()`）依該時區解析，
因此從其他地區建立的資料列仍以使用者自己的今天為預設。`UtcNow()` 則明示 UTC。
時區 id 為空即代表 UTC。見 [ADR-032](../../docs/adr/adr-032-datetime-timezone.md)。

## 安全性

**這不是沙箱。** 未註冊的**型別名稱**（`File`、`Assembly`、`Process`）會在解析期失敗，
但值上的成員存取是以反射解析的，而 `GetType()` 是 `object` 的公開成員——任何在範圍內的變數
都是通往反射 API 的起點。上游 DynamicExpresso 本身也聲明同樣的限制。

真正的控制點是運算式的**來源**而非解析器：運算式存在定義檔中，而寫入定義是部署期作業
（`SystemBO.SaveDefine` 為 `LocalOnly`）。任何讓遠端或低權限呼叫端得以提供運算式文字的改動，
都會使此處變成伺服端的遠端程式碼執行——請維持該邊界。

## AOT / trimming

`IsDynamicCodeSupported` 為 false 時，`Expression.Compile` 會退回直譯器，
因此本引擎在 iOS、Android 與 WASM 上無需停用任何功能即可運作。

## 相依

`Bee.Base`（本套件實作其中的 `IExpressionEvaluator` 抽象）· DynamicExpresso
