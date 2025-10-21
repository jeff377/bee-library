using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbTest
    {
        static DbTest()
        {
        }


        /// <summary>
        /// 執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public void ExecuteDataTable()
        {
            // 由 DbAccess 管理連線
            string sql = "SELECT * FROM ts_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            // 由外部管理連線
            using (var conn = DbFunc.CreateConnection("common"))
            {
                dbAccess = new DbAccess(conn);
                result = dbAccess.Execute(command);
                Assert.NotNull(result.Table);
            }

            sql = "SELECT * FROM ts_user WHERE sys_id = {0} OR sys_id = {1} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            command.Parameters.Add("p1", "001");
            command.Parameters.Add("p2", "002");
            dbAccess = new DbAccess("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            command = new DbCommandSpec(DbCommandKind.DataTable, sql, "001", "002");
            dbAccess = new DbAccess("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            var parameters = new Dictionary<string, object>
            {
                { "p1", "001" },
                { "p2", "002" }
            };
            sql = "SELECT * FROM ts_user WHERE sys_id = {p1} OR sys_id = {p2} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);
        }

        /// <summary>
        /// 非同步執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public async Task ExecuteDataTableAsync()
        {
            string sql = "SELECT * FROM ts_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var reulst = await dbAccess.ExecuteAsync(command);
            var table = reulst.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0);
        }

        [Fact]
        public void ExecuteNonQuery()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update ts_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            int rows = result.RowsAffected;
        }

        [Fact]
        public async Task ExecuteNonQueryAsync()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update ts_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteAsync(command);
            int rows = result.RowsAffected;
        }

        [Fact]
        public void ExecuteScalar()
        {
            string sql = "Select note From ts_user Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, "001");
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            var value = result.Scalar;
        }

        public class User
        {
            public string? UserID { get; set; }
            public string? UserName { get; set; }
            public DateTime InsertTime { get; set; }
        }

        public class User2
        {
            public string? UserID { get; set; }
            public string? UserName { get; set; }
            public string? AccessToken { get; set; }
        }

        [Fact]
        public void Query()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = dbAccess.Query<User>(command);
            var list3 = dbAccess.Query<User2>(command);
        }

        [Fact]
        public async Task QueryAsync()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = await dbAccess.QueryAsync<User>(command);
            var list2 = await dbAccess.QueryAsync<User2>(command);
        }

        [Fact]
        public void UpdateDataTable()
        {
            var dbAccess = new DbAccess("common");

            // 1.查詢 ts_user 所有欄位
            string sql = "SELECT * FROM ts_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var result = dbAccess.Execute(command);
            var table = result.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0, "ts_user 應有資料");

            // 2. 修改第一筆資料
            int i = BaseFunc.RndInt(0, 100);
            var row = table.Rows[0];
            row["note"] = i.ToString();

            // 3. 用 DbTableCommandBuilder 產生 DataTableUpdateSpec
            var dbTable = CacheFunc.GetDbTable("common", "ts_user");
            var builder = new DbTableCommandBuilder(dbTable);
            var updateSpec = builder.BuildUpdateSpec(table);

            // 4. 執行 UpdateDataTable
            int affected = dbAccess.UpdateDataTable(updateSpec);

            Assert.True(affected > 0, "應有資料被更新");
        }

        [Fact]
        public void ExecuteBacth()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM ts_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE ts_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = dbAccess.ExecuteBatch(batch);
        }

        [Fact]
        public async Task ExecuteBacthAsync()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM ts_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE ts_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);
        }

        [Fact]
        public void SqlDbTableTest()
        {
            var helper = new SqlTableSchemaProvider("common");
            var dbTable = helper.GetTableSchema("ts_user");
        }

        [Fact]
        public void SelectContextTest()
        {
            var formDefine = CacheFunc.GetFormDefine("Employee");
            var builder = new SelectContextBuilder(formDefine.MasterTable);         
            var context = builder.Build();
        }

        [Fact]
        public void SelectCommandTest()
        {
            var formDefine = CacheFunc.GetFormDefine("Employee");
            var builder = new SqlSelectCommandBuilder(formDefine);
            var command = builder.Build("Employee",string.Empty, null, null);
        }

        [Fact]
        public void FormCommandBuildTest()
        {
            var formDefine = CacheFunc.GetFormDefine("Employee");
            var builder = new SqlFormCommandBuilder(formDefine);
            var command = builder.BuildSelectCommand("Employee", string.Empty);
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_supervisor_name");
        }

        [Fact]
        public void FormCommandBuildWithFilterNodeTest()
        {
            var formDefine = CacheFunc.GetFormDefine("Employee");
            var builder = new SqlFormCommandBuilder(formDefine);

            // 建立一個 FilterCondition 篩選 sys_id = '001'
            var filter = new FilterCondition
            {
                FieldName = "sys_id",
                Operator = ComparisonOperator.Equal,
                Value = "001"
            };

            // 建立排序欄位集合
            var sortFields = new SortFIeldCollection();
            sortFields.Add(new SortField("sys_id",  SortDirection.Asc)); // 依 sys_id 遞增排序

            // 傳入 filter node 與 sortFields 給 BuildSelectCommand
            var command = builder.BuildSelectCommand("Employee", string.Empty, filter, sortFields);
            Assert.NotNull(command);

            // 也可測試多欄位與 filter 與 sortFields
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_supervisor_name", filter, sortFields);
            Assert.NotNull(command2);
        }
    }
}