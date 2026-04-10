using Bee.Definition.Filters;
using Bee.Base;
using Bee.Definition;
using Bee.Db.DbAccess;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;
using DbAccessObject = Bee.Db.DbAccess.DbAccess;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbTest
    {
        static DbTest()
        {
        }


        /// <summary>
        /// ���� SQL �d�ߡA�è��o DataTable�C
        /// </summary>
        [LocalOnlyFact]
        public void ExecuteDataTable()
        {
            // �� DbAccess �޲z�s�u
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccessObject("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            // �ѥ~���޲z�s�u
            using (var conn = DbFunc.CreateConnection("common"))
            {
                dbAccess = new DbAccessObject(conn);
                result = dbAccess.Execute(command);
                Assert.NotNull(result.Table);
            }

            sql = "SELECT * FROM st_user WHERE sys_id = {0} OR sys_id = {1} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            command.Parameters.Add("p1", "001");
            command.Parameters.Add("p2", "002");
            dbAccess = new DbAccessObject("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            command = new DbCommandSpec(DbCommandKind.DataTable, sql, "001", "002");
            dbAccess = new DbAccessObject("common");
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
        /// �D�P�B���� SQL �d�ߡA�è��o DataTable�C
        /// </summary>
        [LocalOnlyFact]
        public async Task ExecuteDataTableAsync()
        {
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccessObject("common");
            var reulst = await dbAccess.ExecuteAsync(command);
            var table = reulst.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0);
        }

        [LocalOnlyFact]
        public void ExecuteNonQuery()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccessObject("common");
            var result = dbAccess.Execute(command);
            int rows = result.RowsAffected;
        }

        [LocalOnlyFact]
        public async Task ExecuteNonQueryAsync()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccessObject("common");
            var result = await dbAccess.ExecuteAsync(command);
            int rows = result.RowsAffected;
        }

        [LocalOnlyFact]
        public void ExecuteScalar()
        {
            string sql = "Select note From st_user Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, "001");
            var dbAccess = new DbAccessObject("common");
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

        [LocalOnlyFact]
        public void Query()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccessObject("common");
            var list = dbAccess.Query<User>(command);
            var list3 = dbAccess.Query<User2>(command);
        }

        [LocalOnlyFact]
        public async Task QueryAsync()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccessObject("common");
            var list = await dbAccess.QueryAsync<User>(command);
            var list2 = await dbAccess.QueryAsync<User2>(command);
        }

        [LocalOnlyFact]
        public void UpdateDataTable()
        {
            var dbAccess = new DbAccessObject("common");

            // 1.�d�� st_user �Ҧ����
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var result = dbAccess.Execute(command);
            var table = result.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0, "st_user �������");

            // 2. �ק�Ĥ@�����
            int i = BaseFunc.RndInt(0, 100);
            var row = table.Rows[0];
            row["note"] = i.ToString();

            // 3. �� DbTableCommandBuilder ���� DataTableUpdateSpec
            var tableSchema = BackendInfo.DefineAccess.GetTableSchema("common", "st_user");
            var builder = new TableSchemaCommandBuilder(tableSchema);
            var updateSpec = builder.BuildUpdateSpec(table);

            // 4. ���� UpdateDataTable
            int affected = dbAccess.UpdateDataTable(updateSpec);

            Assert.True(affected > 0, "������ƳQ��s");
        }

        [LocalOnlyFact]
        public void ExecuteBacth()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccessObject("common");
            var result = dbAccess.ExecuteBatch(batch);
        }

        [LocalOnlyFact]
        public async Task ExecuteBacthAsync()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccessObject("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);
        }

        [LocalOnlyFact]
        public void SqlDbTableTest()
        {
            var helper = new SqlTableSchemaProvider("common");
            var dbTable = helper.GetTableSchema("st_user");
        }

        [LocalOnlyFact]
        public void FormCommandBuildTest()
        {
            var builder = new SqlFormCommandBuilder("Employee");
            var command = builder.BuildSelectCommand("Employee", string.Empty);
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name");
        }

        [LocalOnlyFact]
        public void FormCommandBuildWithFilterNodeTest()
        {
            var builder = new SqlFormCommandBuilder("Employee");

            // �إߤ@�� FilterCondition �z�� sys_id = '001'
            var filter = new FilterCondition
            {
                FieldName = "sys_id",
                Operator = ComparisonOperator.Equal,
                Value = "001"
            };

            // �إ߱Ƨ���춰�X
            var sortFields = new SortFieldCollection();
            sortFields.Add(new SortField("sys_id",  SortDirection.Asc)); // �� sys_id ���W�Ƨ�

            // �ǤJ filter node �P sortFields �� BuildSelectCommand
            var command = builder.BuildSelectCommand("Employee", string.Empty, filter, sortFields);
            Assert.NotNull(command);

            // �]�i���զh���P filter �P sortFields
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name", filter, sortFields);
            Assert.NotNull(command2);

            // ���� filter �D Select ���A�O�_�ॿ�T�إ� Join
            filter = new FilterCondition
            {
                FieldName = "ref_supervisor_id",
                Operator = ComparisonOperator.Equal,
                Value = "U001"
            };
            var command3 = builder.BuildSelectCommand("Employee", "sys_id,sys_name", filter, sortFields);
            Assert.NotNull(command2);
        }


    }
}