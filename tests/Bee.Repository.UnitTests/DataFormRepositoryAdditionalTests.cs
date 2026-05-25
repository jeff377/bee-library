using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 補強 <see cref="DataFormRepository"/> 中尚未覆蓋的私有靜態方法
    /// （<c>DefaultSortForPaging</c>、<c>ExtractMasterRowId</c>）
    /// 以及 <see cref="DataFormRepository.GetNewData"/> 的 Detail 資料表與預設值路徑。
    /// 不需資料庫連線。
    /// </summary>
    public class DataFormRepositoryAdditionalTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        }

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class StubConnectionManager : IDbConnectionManager
        {
            public DbConnectionInfo GetConnectionInfo(string databaseId) => throw new NotImplementedException();
            public DbConnection CreateConnection(string databaseId) => throw new NotImplementedException();
            public bool Remove(string databaseId) => false;
            public void Clear() { }
            public bool Contains(string databaseId) => false;
            public int Count => 0;
        }

        private static readonly Type[] s_formSchemaParam = [typeof(FormSchema)];
        private static readonly Type[] s_extractMasterRowIdParams = [typeof(DataSet), typeof(string)];

        private static DataFormRepository CreateRepository(FormSchema schema)
        {
            return new DataFormRepository(
                schema.ProgId,
                schema,
                new StubDefineAccess(),
                new StubDbAccessFactory(),
                new StubConnectionManager(),
                "testdb");
        }

        #endregion

        #region DefaultSortForPaging（私有靜態方法）

        [Fact]
        [DisplayName("DefaultSortForPaging 傳入無 MasterTable 的 Schema 應拋 InvalidOperationException")]
        public void DefaultSortForPaging_SchemaWithoutMasterTable_ThrowsInvalidOperationException()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "DefaultSortForPaging",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_formSchemaParam,
                null);
            Assert.NotNull(method);
            var schema = new FormSchema("NoMaster", "NoMaster");
            var ex = Record.Exception(() => method!.Invoke(null, new object[] { schema }));
            var innerEx = Assert.IsType<TargetInvocationException>(ex).InnerException;
            Assert.IsType<InvalidOperationException>(innerEx);
        }

        [Fact]
        [DisplayName("DefaultSortForPaging MasterTable 不含 sys_no 欄位時應拋 InvalidOperationException")]
        public void DefaultSortForPaging_MasterTableWithoutSysNoField_ThrowsInvalidOperationException()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "DefaultSortForPaging",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_formSchemaParam,
                null);
            Assert.NotNull(method);
            var schema = new FormSchema("Employee", "Employee");
            schema.Tables!.Add("Employee", "Employee");
            var ex = Record.Exception(() => method!.Invoke(null, new object[] { schema }));
            var innerEx = Assert.IsType<TargetInvocationException>(ex).InnerException;
            Assert.IsType<InvalidOperationException>(innerEx);
        }

        [Fact]
        [DisplayName("DefaultSortForPaging MasterTable 含 sys_no 欄位時應回傳 SortFieldCollection")]
        public void DefaultSortForPaging_MasterTableWithSysNoField_ReturnsSortFieldCollection()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "DefaultSortForPaging",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_formSchemaParam,
                null);
            Assert.NotNull(method);
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.No, "No", FieldDbType.Integer);
            var result = method!.Invoke(null, new object[] { schema });
            Assert.NotNull(result);
        }

        #endregion

        #region ExtractMasterRowId（私有靜態方法）

        [Fact]
        [DisplayName("ExtractMasterRowId DataSet 不含指定資料表時應回傳 null")]
        public void ExtractMasterRowId_DataSetWithoutMasterTable_ReturnsNull()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "ExtractMasterRowId",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_extractMasterRowIdParams,
                null);
            Assert.NotNull(method);
            var dataSet = new DataSet("Test");
            var result = method!.Invoke(null, new object[] { dataSet, "Employee" });
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("ExtractMasterRowId 資料表含有效 sys_rowid 的列時應回傳對應 Guid")]
        public void ExtractMasterRowId_MasterTableWithValidRowId_ReturnsGuid()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "ExtractMasterRowId",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_extractMasterRowIdParams,
                null);
            Assert.NotNull(method);
            var dataSet = new DataSet("Test");
            var table = dataSet.Tables.Add("Employee");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var expectedId = Guid.NewGuid();
            var row = table.NewRow();
            row[SysFields.RowId] = expectedId;
            table.Rows.Add(row);
            var result = method!.Invoke(null, new object[] { dataSet, "Employee" });
            Assert.Equal(expectedId, (Guid)result!);
        }

        [Fact]
        [DisplayName("ExtractMasterRowId 資料表只含 Deleted 列時應回傳 null")]
        public void ExtractMasterRowId_OnlyDeletedRows_ReturnsNull()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "ExtractMasterRowId",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_extractMasterRowIdParams,
                null);
            Assert.NotNull(method);
            var dataSet = new DataSet("Test");
            var table = dataSet.Tables.Add("Employee");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            table.Rows.Add(row);
            table.AcceptChanges();
            row.Delete();
            var result = method!.Invoke(null, new object[] { dataSet, "Employee" });
            Assert.Null(result);
        }

        #endregion

        #region GetNewData（補強路徑）

        [Fact]
        [DisplayName("GetNewData Schema 含 Detail 資料表時 DataSet 應包含該 Detail 資料表")]
        public void GetNewData_SchemaWithDetailTable_DataSetContainsDetailTable()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            var detail = schema.Tables.Add("OrderItem", "OrderItem");
            detail.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            detail.Fields.Add(SysFields.MasterRowId, "Master Row Id", FieldDbType.Guid);

            var repo = CreateRepository(schema);
            var dataSet = repo.GetNewData();

            Assert.True(dataSet.Tables.Contains("Order"));
            Assert.True(dataSet.Tables.Contains("OrderItem"));
        }

        [Fact]
        [DisplayName("GetNewData 欄位有字串預設值時 master 列應套用該預設值")]
        public void GetNewData_FieldWithStringDefaultValue_AppliesDefault()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            var nameField = master.Fields.Add("emp_status", "Status", FieldDbType.String);
            nameField.DefaultValue = "Active";

            var repo = CreateRepository(schema);
            var dataSet = repo.GetNewData();

            var masterRow = dataSet.Tables["Employee"]!.Rows[0];
            Assert.Equal("Active", masterRow["emp_status"]);
        }

        #endregion
    }
}
