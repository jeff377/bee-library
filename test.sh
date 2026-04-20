#!/usr/bin/env bash
# 本機測試 wrapper：若已設定 Docker SQL Server container，先啟動它再跑 dotnet test。
# 其他人沒 Docker 或沒此 container 時自動跳過啟動步驟，DbFact 測試會依
# .runsettings 的 BEE_TEST_DB_CONNSTR 是否可連線自動 skip。
#
# 容器名稱可用環境變數 override（未設則預設 sql2025）：
#   BEE_TEST_SQL_CONTAINER=my-mssql ./test.sh
set -euo pipefail

CONTAINER="${BEE_TEST_SQL_CONTAINER:-sql2025}"

start_sql_container() {
  if ! command -v docker >/dev/null 2>&1; then
    return
  fi
  if ! docker inspect "$CONTAINER" >/dev/null 2>&1; then
    return
  fi
  if docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
    return
  fi

  echo "Starting container $CONTAINER..."
  docker start "$CONTAINER" >/dev/null
  echo -n "Waiting for SQL Server on localhost:1433"
  for _ in {1..30}; do
    if nc -z localhost 1433 2>/dev/null; then
      echo " ready."
      return
    fi
    echo -n "."
    sleep 1
  done
  echo " timeout (DbFact tests may be skipped)."
}

start_sql_container

dotnet test --configuration Release --settings .runsettings "$@"
