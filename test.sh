#!/usr/bin/env bash
# 本機測試 wrapper：偵測本機是否有對應 Docker container，有的話啟動再跑 dotnet test。
# 沒 Docker 或沒對應 container 時自動跳過啟動步驟，[DbFact(DatabaseType.X)] 測試會依
# .runsettings 中各 BEE_TEST_CONNSTR_{DBTYPE} 是否可連線自動 skip。
#
# 容器名稱可用環境變數 override（未設則使用預設值）：
#   BEE_TEST_SQL_CONTAINER=my-mssql ./test.sh
#   BEE_TEST_PG_CONTAINER=my-pg     ./test.sh
set -euo pipefail

SQL_CONTAINER="${BEE_TEST_SQL_CONTAINER:-sql2025}"
PG_CONTAINER="${BEE_TEST_PG_CONTAINER:-pgvector-db}"

start_container() {
  local name="$1"
  local port="$2"

  if ! command -v docker >/dev/null 2>&1; then
    return
  fi
  if ! docker inspect "$name" >/dev/null 2>&1; then
    return
  fi
  if docker ps --format '{{.Names}}' | grep -qx "$name"; then
    return
  fi

  echo "Starting container $name..."
  docker start "$name" >/dev/null
  echo -n "Waiting for $name on localhost:$port"
  for _ in {1..30}; do
    if nc -z localhost "$port" 2>/dev/null; then
      echo " ready."
      return
    fi
    echo -n "."
    sleep 1
  done
  echo " timeout (DbFact tests for this DB may be skipped)."
}

start_container "$SQL_CONTAINER" 1433
start_container "$PG_CONTAINER"  5432

dotnet test --configuration Release --settings .runsettings "$@"
