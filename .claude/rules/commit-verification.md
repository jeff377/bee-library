# Commit 前驗證 hook

本 repo 以 Claude Code `PreToolUse` hook 在 agent 執行 `git commit` **之前**強制驗證。
宣告於 `.claude/settings.json`，實作於 `.claude/hooks/pre-commit-verify.sh`。

> **兩項檢查為何不對稱、為何 `--no-incremental`、為何必須 fail open、
> 「其他 repo」的 cwd 如何判定 —— 全部寫在腳本檔頭。** 要細節就讀那支，本檔不複寫。

## agent 要知道的四件事

1. **clean Release build 失敗會擋下 commit**（exit 2）。搭配 `TreatWarningsAsErrors=true`，
   任何警告即失敗。實測約 5 秒。
2. **`PublicAPI.Unshipped.txt` 有異動時只提示、不擋**，但**必須在 commit message 或回覆中
   說明相容性判定** —— analyzer 擋得住「未申報」，擋不住「已申報但二進位不相容」
   （例如對既有 public 建構子加 optional 參數）。這個提示的作用就是把該判定從靜默轉為必須正視。
3. **不要為了讓 hook 通過而修改測試或原始碼** —— 那正是本 hook 要防的事情本身。
4. **`git --no-verify` 對它無效**（那是 git 自身 hook 的旗標）。它掛在 Claude Code 的工具呼叫上，
   不是 `.git/hooks/`，所以**使用者在自己終端機直接 commit 不受影響**。

## 為什麼是 hook 而不是規則條文

條文要求 agent **自願遵守**，hook 由外殼**強制執行**。以下兩種失誤都源於「agent 沒有自覺去
檢查」，寫成條文無效：以 incremental build 宣稱「build is clean」；public API 變更靠補
`PublicAPI.Unshipped.txt` 讓 build 轉綠而未判二進位相容性。

## 暫時停用

需要繞過時（WIP commit、或建置因環境問題失敗）：註解 `.claude/settings.json` 的 `hooks`
區塊並重開 session，或改由使用者在自己的終端機執行該次 commit。
