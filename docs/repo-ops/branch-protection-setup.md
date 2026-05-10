# GitHub 分支保護設定指南

本文件記錄 `main` 分支保護規則的設定方式，適用於個人開發者在多裝置（Mac / Windows / App）環境下的工作流程。

## 適用情境

| 裝置 | 編譯環境 | 工作方式 |
|------|----------|----------|
| Mac / Windows | 有 | 可直接 push to `main` |
| App（如 Claude Code） | 無 | 建分支 → PR → CI 通過後合併 |

## 設定內容

使用 GitHub Classic Branch Protection API，對 `main` 分支啟用以下規則：

| 設定項目 | 值 | 說明 |
|----------|----|------|
| `required_status_checks.contexts` | `["build"]` | PR 合併前必須通過 `build` job |
| `required_status_checks.strict` | `true` | PR 分支必須與 main 同步後才能合併 |
| `enforce_admins` | `false` | Repo admin 可直接 push，不受 PR 限制 |
| `required_pull_request_reviews` | `null` | 不要求 Code Review（個人專案） |
| `restrictions` | `null` | 不限制誰可以 push |
| `allow_force_pushes` | `false` | 禁止 force push |
| `allow_deletions` | `false` | 禁止刪除 main 分支 |
| `required_linear_history` | `false` | 允許 merge commit |
| `required_signatures` | `false` | 不要求 commit 簽章 |

## 設定指令

使用 `gh` CLI 一鍵設定：

```bash
gh api repos/{owner}/{repo}/branches/main/protection \
  --method PUT \
  --input - <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["build"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null
}
EOF
```

> `contexts` 中的 `"build"` 需對應 CI workflow 中的 job 名稱。

## 驗證設定

```bash
# 查看目前保護規則
gh api repos/{owner}/{repo}/branches/main/protection

# 移除保護規則（如需重設）
gh api repos/{owner}/{repo}/branches/main/protection --method DELETE
```

## 前提條件

1. **CI Workflow 必須已存在且觸發過**：GitHub 需要至少執行過一次 `build` job，才能識別該 status check context
2. **Workflow 需包含 `pull_request` 觸發條件**：

```yaml
on:
  pull_request:
    branches:
      - main
```

## 注意事項

- `enforce_admins: false` 是讓 admin 可以直接 push 的關鍵，設為 `true` 則所有人都必須走 PR
- 如果 repo 有多人協作，建議將 `required_pull_request_reviews` 設為 `{"required_approving_review_count": 1}`
- `strict: true` 要求 PR 分支在合併前必須與 main 同步（rebase），可設為 `false` 放寬此限制
