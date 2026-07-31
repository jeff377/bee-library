# Commit 前驗證 hook

本 repo 以 Claude Code `PreToolUse` hook 在 agent 執行 `git commit` **之前**強制驗證。
設定於 `.claude/settings.json`（入版控），實作於 `.claude/hooks/pre-commit-verify.sh`。

## 為什麼是 hook 而不是規則條文

規則條文（如本目錄其他檔案）要求 agent **自願遵守**；hook 由 Claude Code 外殼**強制執行**，
agent 無法略過。以下兩種失誤都屬於「agent 沒有自覺去檢查」，寫成條文無效：

- 以 incremental build 的結果宣稱「build is clean」，而完整建置其實有警告
- public API 變更以「補進 `PublicAPI.Unshipped.txt`」讓 build 轉綠，未判定二進位相容性

## 兩項檢查

| 檢查 | 行為 | 說明 |
|------|------|------|
| Clean Release build | **阻擋**（exit 2） | `dotnet build Bee.Library.slnx -c Release --no-incremental`。`--no-incremental` 是重點——強制全數重新編譯，結果不受既有 `obj/` 快取影響。搭配 `TreatWarningsAsErrors=true`，任何警告即失敗。實測約 5 秒。 |
| `PublicAPI.Unshipped.txt` 異動 | **提示**（不阻擋） | 列出 diff，要求於 commit message 或回覆中說明相容性判定。 |

第二項為何不阻擋：`Microsoft.CodeAnalysis.PublicApiAnalyzers` 已於 `src/Directory.Build.props`
全域啟用，「變更未申報」（RS0016 等）**本來就是編譯錯誤**，第一項即可攔下。分析器看不到的是
「已申報但不相容」——例如對既有 public constructor 增加 optional 參數，語法相容但二進位不相容，
申報後 build 轉綠即靜默通過。此提示的作用是把該判定從靜默轉為必須正視，故不阻擋、只攤開。

## 失敗開放（fail-open）

腳本在下列情況一律 exit 0 放行，不阻斷工作：

- 無法解析 hook 輸入
- 不在 git repo 內，或找不到 `Bee.Library.slnx`（例如 agent 正在其他 repo 內 commit）
- 找不到 `dotnet`

驗證 hook 卡死整個 repo，比偶爾漏檢更糟。

## 涵蓋範圍與限制

**只攔截 agent 透過 Bash 工具發出的 `git commit`。** 使用者在自己的終端機直接 commit
不受影響——hook 掛在 Claude Code 的工具呼叫上，不是 git 的 `.git/hooks/`。

同理，git 的 `--no-verify` 對本 hook **無效**（那是 git 自身 hook 的旗標）。

## 暫時停用

需要繞過時（例如 WIP commit、或建置因環境問題失敗）：

1. 註解或移除 `.claude/settings.json` 的 `hooks` 區塊，重開 session
2. 或改由使用者在自己的終端機執行該次 commit

不要為了讓 hook 通過而修改測試或原始碼——那是本 hook 要防的事情本身。
