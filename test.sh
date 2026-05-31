#!/usr/bin/env bash
# 本機測試 wrapper：偵測本機是否有對應 Docker container，有的話啟動再跑 dotnet test。
# 沒 Docker 或沒對應 container 時自動跳過啟動步驟，[DbFact(DatabaseType.X)] 測試會依
# .runsettings 中各 BEE_TEST_CONNSTR_{DBTYPE} 是否可連線自動 skip。
#
# 容器名稱可用環境變數 override（未設則使用預設值）：
#   BEE_TEST_SQL_CONTAINER=my-mssql    ./test.sh   # 預設 sql2025
#   BEE_TEST_PG_CONTAINER=my-pg        ./test.sh   # 預設 pgvector-db
#   BEE_TEST_MYSQL_CONTAINER=my-mysql  ./test.sh   # 預設 mysql8
#   BEE_TEST_ORACLE_CONTAINER=my-ora   ./test.sh   # 預設 oracle23ai
set -euo pipefail

SQL_CONTAINER="${BEE_TEST_SQL_CONTAINER:-sql2025}"
PG_CONTAINER="${BEE_TEST_PG_CONTAINER:-pgvector-db}"
MYSQL_CONTAINER="${BEE_TEST_MYSQL_CONTAINER:-mysql8}"
ORACLE_CONTAINER="${BEE_TEST_ORACLE_CONTAINER:-oracle23ai}"

start_container() {
  local name="$1"
  local port="$2"
  local timeout="${3:-30}"

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
  for _ in $(seq 1 "$timeout"); do
    if nc -z localhost "$port" 2>/dev/null; then
      echo " ready."
      return
    fi
    echo -n "."
    sleep 1
  done
  echo " timeout (DbFact tests for this DB may be skipped)."
}

# 確保 Docker daemon 就緒。daemon 已在時零開銷直接 return；macOS 上 daemon 未啟動時
# 自動拉起 Docker Desktop 並等待就緒。無 docker CLI 或非 macOS 時不介入，維持「無 docker
# 環境自動 skip」相容性；任何失敗只警告、不中止（讓 DbFact 照常 skip/fail）。
ensure_docker_daemon() {
  # 無 docker CLI：維持「無 docker 環境自動 skip」行為，不做任何事。
  if ! command -v docker >/dev/null 2>&1; then
    return
  fi
  # daemon 已就緒：no-op、零開銷，平常測試不受影響。
  if docker info >/dev/null 2>&1; then
    return
  fi
  # daemon 未啟動：僅在 macOS 自動拉起 Docker Desktop（GUI app）。Linux daemon 為 systemd
  # 服務、CI Linux 不走本腳本，故不自動拉起。
  if [ "$(uname)" != "Darwin" ]; then
    echo "Docker daemon 未啟動（非 macOS，不自動拉起）；DbFact 測試可能失敗或 skip。"
    return
  fi
  echo "Docker daemon 未啟動，啟動 Docker Desktop..."
  if ! open -a Docker 2>/dev/null; then
    echo "無法啟動 Docker Desktop（可能未安裝）；DbFact 測試可能失敗或 skip。"
    return
  fi
  echo -n "Waiting for Docker daemon"
  # Docker Desktop 冷啟動可能需 1-2 分鐘。
  for _ in $(seq 1 120); do
    if docker info >/dev/null 2>&1; then
      echo " ready."
      return
    fi
    echo -n "."
    sleep 1
  done
  echo " timeout（Docker daemon 仍未就緒）；DbFact 測試可能失敗或 skip。"
}

ensure_docker_daemon

start_container "$SQL_CONTAINER"    1433
start_container "$PG_CONTAINER"     5432
start_container "$MYSQL_CONTAINER"  3306
# Oracle 23ai 冷啟動可能需要 1-2 分鐘，給較長的 timeout。
start_container "$ORACLE_CONTAINER" 1521 180

dotnet test --configuration Release --settings .runsettings "$@"
