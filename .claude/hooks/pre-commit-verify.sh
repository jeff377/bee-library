#!/usr/bin/env bash
#
# PreToolUse hook: verify the tree before any `git commit` reaches the shell.
#
# Two checks, deliberately asymmetric:
#   1. Clean Release build  -> BLOCKS the commit (exit 2). `--no-incremental` is the
#      whole point: an incremental build can hide a warning that a full build reports,
#      which is how a "build is clean" claim has been wrong before.
#   2. `PublicAPI.Unshipped.txt` diff -> WARNS only. The PublicApiAnalyzers package is
#      enabled repo-wide, so an *undeclared* API change is already a build error and is
#      caught by check 1. The gap it cannot see is a change that was declared in
#      Unshipped.txt — that turns the build green while the break is still real.
#
# WARNING: this hook must fail open. Anything it cannot parse, locate, or run exits 0
# and lets the commit through. A verification hook that wedges the repo is worse than
# one that occasionally misses.
#
set -uo pipefail

SOLUTION="Bee.Library.slnx"

payload=$(cat 2>/dev/null) || exit 0

# Intercept only git commit. Matching the raw payload rather than a parsed field keeps
# this working without a JSON parser; a stray match (a commit string inside an echo)
# costs one build and nothing else.
printf '%s' "$payload" | grep -Eq '(^|[;&|"[:space:]])git[[:space:]]+commit' || exit 0

# The hook process starts in the Claude Code project directory, not in the directory the
# intercepted command will run in. Resolving the repository from the hook's own cwd
# therefore always landed on *this* repository, so a command like
# `cd ../other-repo && git commit ...` built this solution and blocked a commit this hook
# has no say over. Recover the intended directory from the last `cd` in the command.
target_dir=$PWD
cd_arg=$(printf '%s' "$payload" \
    | grep -oE '(^|[;&|"[:space:]])cd[[:space:]]+[^[:space:];&|"]+' \
    | tail -1 \
    | sed -E 's/^(.*[^[:alnum:]_])?cd[[:space:]]+//' 2>/dev/null)
if [ -n "$cd_arg" ]; then
    # The payload carries the command text verbatim, so a leading ~ is still literal.
    case "$cd_arg" in
        "~") cd_arg=$HOME ;;
        "~/"*) cd_arg="$HOME/${cd_arg#\~/}" ;;
    esac
    [ -d "$cd_arg" ] && target_dir=$cd_arg
fi

repo_root=$(git -C "$target_dir" rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$repo_root" 2>/dev/null || exit 0

# Guards the case where the agent is committing in some other repository — the plugin
# repo, a sample clone — where this solution does not exist and this hook has no say.
[ -f "$SOLUTION" ] || exit 0

command -v dotnet >/dev/null 2>&1 || exit 0

# ---------------------------------------------------------------------------
# Check 1 — full, non-incremental Release build. Blocking.
# ---------------------------------------------------------------------------
build_log=$(mktemp -t bee-precommit-build) || exit 0
trap 'rm -f "$build_log"' EXIT

if ! dotnet build "$SOLUTION" --configuration Release --no-incremental -v q -nologo \
        >"$build_log" 2>&1; then
    {
        echo "COMMIT 已阻擋：clean Release build 失敗。"
        echo
        grep -Ei "error|warning" "$build_log" | head -30
        echo
        echo "注意：本 repo 設定 TreatWarningsAsErrors=true，警告即為失敗。"
        echo "此為 --no-incremental 完整建置，結果不受既有 obj/ 快取影響。"
        echo "修正後重新 commit。"
    } >&2
    exit 2
fi

# ---------------------------------------------------------------------------
# Check 2 — public API surface changes. Advisory only.
# ---------------------------------------------------------------------------
api_files=$(git diff HEAD --name-only -- '*PublicAPI.Unshipped.txt' 2>/dev/null)
[ -n "$api_files" ] || exit 0

api_diff=$(git diff HEAD -- '*PublicAPI.Unshipped.txt' 2>/dev/null \
           | grep -E '^[+-][^+-]' | head -40)

notice=$(printf '%s\n' \
    "PublicAPI.Unshipped.txt 有異動（未阻擋，需明確判定相容性）：" \
    "" \
    "$api_diff" \
    "" \
    "分析器只保證變更『已申報』，不保證變更『相容』。對既有 public 成員增加" \
    "optional 參數、更動簽章或型別，即使申報後 build 轉綠，仍是二進位不相容。" \
    "請於 commit message 或回覆中說明相容性判定。")

printf '%s\n' "$notice" >&2

# stderr is not reliably surfaced on a non-blocking exit, so also emit the notice as a
# systemMessage. python3 builds the JSON to keep the embedded diff correctly escaped;
# if it is unavailable the stderr copy above still stands.
if command -v python3 >/dev/null 2>&1; then
    NOTICE="$notice" python3 -c \
        'import json, os; print(json.dumps({"systemMessage": os.environ["NOTICE"]}))' \
        2>/dev/null || true
fi

exit 0
