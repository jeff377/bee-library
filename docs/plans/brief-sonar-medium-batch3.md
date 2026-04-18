# 第 3 批啟動 Brief：SonarCloud MEDIUM 泛型與 API 批

**用途**：給新 session 一鍵接手第 3 批處理，無須翻歷史對話。

---

## 1. 給接手者的話

你是新 session，沒有先前對話脈絡。本 brief 一次給足你接手所需資訊。

工作目標：完成 SonarCloud MEDIUM 第 3 批 **25 處**修正（CA1822 + CA2263），push 到 `main`，由 CI 與 SonarCloud 重掃驗證。

整體進度：原始 140 筆 → 目前 82 筆（已完成第 1、2 批，commits `ad84576`、`e89bf4b`）。

---

## 2. 必讀文件（順序）

1. **[.claude/CLAUDE.md](../../.claude/CLAUDE.md)** — 專案總覽與工作流程
2. **[.claude/rules/code-style.md](../../.claude/rules/code-style.md)** — 命名與格式
3. **[.claude/rules/sonarcloud.md](../../.claude/rules/sonarcloud.md)** — SonarCloud 規則對照
4. **[.claude/rules/pull-request.md](../../.claude/rules/pull-request.md)** — 提交流程（本機可驗證 → 直接 push main）
5. **[docs/plans/plan-sonar-medium-cleanup.md](plan-sonar-medium-cleanup.md)** — 主計畫與批次規劃
6. **[.claude/commands/ci-watch.md](../../.claude/commands/ci-watch.md)** — CI 自動監測 slash command

---

## 3. 環境前提

- 桌面 macOS，可本機 `dotnet build` + `dotnet test`
- 預設**直接 push `main`**（不走 PR；branch protection 已被 jeff377 帳號 bypass）
- `gh` CLI 已登入、SonarCloud API 公開可讀（無需 token）
- 兩個本機 baseline 失敗測試與本批**無關**，可忽略：
  - `Bee.Business.UnitTests.SystemBusinessObjectTests.Login_WithRsaKeyPair_ReturnsDecryptableSessionKey`（測試用 default `AuthenticateUser` 永遠回 `false`，本機環境無 CI=true 故未跳過）
  - `Bee.Base.UnitTests.FileFuncTests.IsPathRooted_RecognizesAbsolutePaths(input: "C:\\temp\\file.txt")`（macOS 不識 Windows 風格絕對路徑）

---

## 4. 第 3 批範圍

| 規則 | 預計數 | 說明 | 風險 |
|------|------|------|------|
| **CA1822** | 14 | 方法可改 `static`（未存取 instance 成員） | **中**（src 內公開方法改 static 為 ABI 變更） |
| **CA2263** | 11 | 改用泛型 overload | 低 |

> 預計數來自 2026-04-18 第 1 次抓取（API 預設 ps=100）。執行前**務必重新抓取**最新清單，數量可能因新 commit 而變動。

### 抓取指令

```bash
curl -s "https://sonarcloud.io/api/issues/search?projects=jeff377_bee-library&impactSeverities=MEDIUM&issueStatuses=OPEN&ps=100" | python3 -c "
import json, sys
from collections import defaultdict
data = json.load(sys.stdin)
print(f'Total: {data.get(\"total\", \"?\")}')
by_rule = defaultdict(list)
for issue in data.get('issues', []):
    rule = issue.get('rule', '?')
    file = issue.get('component', '?').replace('jeff377_bee-library:', '')
    line = issue.get('line', '?')
    msg = issue.get('message', '')
    by_rule[rule].append((file, line, msg))
for rule in ['external_roslyn:CA1822', 'external_roslyn:CA2263']:
    items = by_rule.get(rule, [])
    print(f'\\n== {rule} ({len(items)}) ==')
    for f, l, m in items:
        print(f'  {f}:{l}  {m}')
"
```

---

## 5. 風險評估與修法建議

### 5-1. CA1822（方法可改 static）

依「方法所在位置」分兩類：

**A. tests/ 內的方法** — 直接改 `static`，無風險。

**B. src/ 內的方法** — 需區分公開性：
- `public` / `protected` instance method → 改 `static` 是 ABI 變更（呼叫端 `obj.Method()` 仍可編譯，但 IL 指令從 `callvirt` 變 `call`，下游 NuGet 消費端需重編譯）
- `private` / `internal` → 安全，直接改

**對 public/protected 的判斷準則**：
- 若是輔助型方法（如 `GetCacheKey`、`ConvertToColumnControlType`），且不打算允許子類覆寫 → 改 static 合理
- 若可能被子類覆寫或屬於語意上的「實例操作」→ **保留 instance**，於 SonarCloud UI 標 `Won't Fix`（不修程式碼，標記為已知例外）並說明理由

### 5-2. CA2263（改用泛型 overload）

模式：
```csharp
// Before
var obj = JsonSerializer.Deserialize(json, typeof(MyType), options);

// After
var obj = JsonSerializer.Deserialize<MyType>(json, options);
```

**驗證點**：
- 確認泛型 overload 存在（編譯不會騙你，但要先看簽章）
- 呼叫端是否仍依賴 `object` 回傳（如要 cast 為 `MyType`，改泛型後可直接得到 `MyType`，移除多餘 cast）

---

## 6. 執行步驟

1. **抓取最新清單**（指令見第 4 節），對齊涉及檔案與位置
2. **更新計畫文件**（`docs/plans/plan-sonar-medium-cleanup.md`）：將第 3 批標為 `🚧 進行中`
3. **讀涉及檔案**（並行 Read）
4. **修改**（CA2263 機械改寫優先；CA1822 src/ 內逐一評估）
5. **本機驗證**：
   ```bash
   dotnet build --configuration Release
   dotnet test --configuration Release --no-build --settings .runsettings
   ```
   接受兩個 baseline 失敗（見第 3 節）；其餘必須全綠
6. **commit + push**（commit message 格式參考前兩批，繁中、`refactor(sonar):` 前綴）
7. **啟動 CI 監測**：
   ```bash
   gh run list -b main -L 1 --json databaseId,status,headSha
   gh run watch <id> --exit-status
   ```
   或直接走 `/ci-watch` slash command
8. **驗證 SonarCloud**：CI 完成後 30-60 秒，重抓 MEDIUM 數量，確認下降量約 25
9. **標記計畫完成**：在 `plan-sonar-medium-cleanup.md` 將第 3 批改為 `✅ 已完成（YYYY-MM-DD，commit <hash>）`，並補一行進度紀錄

---

## 7. 預期結果

- SonarCloud MEDIUM：**82 → 約 57**（-25）
- 第 3 批 CA1822 / CA2263 規則的剩餘數應降到 0 或僅剩補抓批（API 100 筆限制外）的少數
- CI 通過，無新警告

---

## 8. 結束後

- 更新 `plan-sonar-medium-cleanup.md` 進度紀錄
- 詢問使用者是否繼續第 4 批（行為敏感批）或補抓批
- 若全數完成，將計畫文件標為 `✅ 已完成`

---

## 9. 備忘

- **絕對不要**為了消 warning 而把不應改 static 的 public method 改掉（破壞 OO 設計）
- **絕對不要**用 `--no-verify` / `--force` push
- 若連續 3 次同類修正失敗，停下並回報，不要無限迴圈
- commit 訊息使用繁體中文
- 任何架構性疑慮先停下問使用者
