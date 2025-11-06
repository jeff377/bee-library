using Bee.Base;
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
            string sql = "SELECT * FROM st_user";
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

            sql = "SELECT * FROM st_user WHERE sys_id = {0} OR sys_id = {1} ";
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
            sql = "SELECT * FROM st_user WHERE sys_id = {p1} OR sys_id = {p2} ";
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
            string sql = "SELECT * FROM st_user";
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
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            int rows = result.RowsAffected;
        }

        [Fact]
        public async Task ExecuteNonQueryAsync()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteAsync(command);
            int rows = result.RowsAffected;
        }

        [Fact]
        public void ExecuteScalar()
        {
            string sql = "Select note From st_user Where sys_id = {0}";
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
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = dbAccess.Query<User>(command);
            var list3 = dbAccess.Query<User2>(command);
        }

        [Fact]
        public async Task QueryAsync()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = await dbAccess.QueryAsync<User>(command);
            var list2 = await dbAccess.QueryAsync<User2>(command);
        }

        [Fact]
        public void UpdateDataTable()
        {
            var dbAccess = new DbAccess("common");

            // 1.查詢 st_user 所有欄位
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var result = dbAccess.Execute(command);
            var table = result.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0, "st_user 應有資料");

            // 2. 修改第一筆資料
            int i = BaseFunc.RndInt(0, 100);
            var row = table.Rows[0];
            row["note"] = i.ToString();

            // 3. 用 DbTableCommandBuilder 產生 DataTableUpdateSpec
            var dbTable =  BackendInfo.DefineAccess.GetDbTable("common", "st_user");
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
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = dbAccess.ExecuteBatch(batch);
        }

        [Fact]
        public async Task ExecuteBacthAsync()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);
        }

        [Fact]
        public void SqlDbTableTest()
        {
            var helper = new SqlTableSchemaProvider("common");
            var dbTable = helper.GetTableSchema("st_user");
        }

        [Fact]
        public void SelectContextTest()
        {
            var formDefine = BackendInfo.DefineAccess.GetFormDefine("Employee");
            var builder = new SelectContextBuilder(formDefine.MasterTable);         
            var context = builder.Build();
        }

        [Fact]
        public void FormCommandBuildTest()
        {
            var builder = new SqlFormCommandBuilder("Employee");
            var command = builder.BuildSelectCommand("Employee", string.Empty);
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name");
        }

        [Fact]
        public void FormCommandBuildWithFilterNodeTest()
        {
            var builder = new SqlFormCommandBuilder("Employee");

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
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name", filter, sortFields);
            Assert.NotNull(command2);

            // 測試 filter 非 Select 欄位，是否能正確建立 Join
            filter = new FilterCondition
            {
                FieldName = "ref_supervisor_id",
                Operator = ComparisonOperator.Equal,
                Value = "U001"
            };
            var command3 = builder.BuildSelectCommand("Employee", "sys_id,sys_name", filter, sortFields);
            Assert.NotNull(command2);
        }

        [Fact]
        public void BuildSelectCommand()
        {
            var builder = new SqlFormCommandBuilder("Project");
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");
        }

        [Fact]
        public void BuildSelectCommand_SelectOnlyMasterFields()
        {
            // 測試：只 Select 主檔欄位，不應產生任何 JOIN
            var builder = new SqlFormCommandBuilder("Project");
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");
            
            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 不包含 JOIN 關鍵字
            Assert.DoesNotContain("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_WhereOnReferencedField()
        {
            // 測試：Select 主檔欄位，但 Where 條件使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 查詢 PM 的專案資料，PM 姓名開頭為「張」
            var filter = new FilterCondition("ref_pm_name", ComparisonOperator.StartsWith, "張");
            // 建立 Select 語法
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filter);
            
            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Where 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_OrderByReferencedField()
        {
            // 測試：Select 主檔欄位，但 Order By 使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 以 PM 姓名做排序
            var sortFields = new SortFIeldCollection();
            sortFields.Add(new SortField("ref_pm_name", SortDirection.Asc));
            
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, sortFields);
            
            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Order By 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_SelectWithMultipleReferences()
        {
            // 測試：Select 包含多個參考欄位，應 JOIN 對應的多個參考表
            var builder = new SqlFormCommandBuilder("Project");

            // 假設 ref_owner_dept_name 和 ref_pm_dept_name 來自不同的參考表
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name,ref_owner_dept_name,ref_pm_dept_name");
            
            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含多個 JOIN
            var joinCount = System.Text.RegularExpressions.Regex.Matches(
                command.CommandText, 
                "JOIN", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Count;
            Assert.True(joinCount >= 2, $"應包含至少 2 個 JOIN，實際: {joinCount}");
        }

        [Fact]
        public void BuildSelectCommand_FilterGroupWithMultipleConditions()
        {
            // 測試：FilterGroup 包含多個條件，使用不同參考欄位
            var builder = new SqlFormCommandBuilder("Project");
            
            var filterGroup = FilterGroup.All(
                FilterCondition.Contains("sys_name", "專案"),
                FilterCondition.Equal("ref_pm_name", "張三")
            );
            
            var sortFields = new SortFIeldCollection
            {
                new SortField("sys_id", SortDirection.Asc)
            };
            
            var command = builder.BuildSelectCommand(
                "Project", 
                "sys_id,sys_name", 
                filterGroup, 
                sortFields
            );
            
            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證只 JOIN ref_pm_name 相關的表（因為 Select 不需要其他參考欄位）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }
    }
}