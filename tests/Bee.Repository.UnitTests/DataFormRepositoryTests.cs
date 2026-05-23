using System.ComponentModel;
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
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="DataFormRepository"/> 建構子驗證、<see cref="DataFormRepository.GetNewData"/> 以及
    /// 私有 static 純方法（<c>DefaultSortForPaging</c>、<c>ConvertDefaultValue</c>、
    /// <c>TryCoerceToGuid</c>）的單元測試。
    /// </summary>
    public class DataFormRepositoryTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            public DatabaseSettings GetDatabaseSettings() => new();
            public FormSchema GetFormSchema(string progId) => new();
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
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

        #endregion

        private static FormSchema BuildSchemaWithRowId(string progId = "Employee")
        {
            var schema = new FormSchema(progId, progId);
            var master = schema.Tables!.Add(progId, progId);
            master.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            master.Fields.Add("emp_id", "ID", FieldDbType.String);
            return schema;
        }

        private static DataFormRepository CreateRepository(string progId = "Employee", FormSchema? schema = null)
        {
            schema ??= BuildSchemaWithRowId(progId);
            return new DataFormRepository(
                progId, schema,
                new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(),
                "common_sqlserver");
        }

        // ---- 建構子引數驗證 ----

        [Fact]
        [DisplayName("建構子傳入 null progId 應拋 ArgumentNullException")]
        public void Constructor_NullProgId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    null!, BuildSchemaWithRowId(),
                    new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), "db"));
        }

        [Fact]
        [DisplayName("建構子傳入 null schema 應拋 ArgumentNullException")]
        public void Constructor_NullSchema_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    "Employee", null!,
                    new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), "db"));
        }

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    "Employee", BuildSchemaWithRowId(),
                    null!, new StubDbAccessFactory(), new StubConnectionManager(), "db"));
        }

        [Fact]
        [DisplayName("建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    "Employee", BuildSchemaWithRowId(),
                    new StubDefineAccess(), null!, new StubConnectionManager(), "db"));
        }

        [Fact]
        [DisplayName("建構子傳入 null connectionManager 應拋 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    "Employee", BuildSchemaWithRowId(),
                    new StubDefineAccess(), new StubDbAccessFactory(), null!, "db"));
        }

        [Fact]
        [DisplayName("建構子傳入 null databaseId 應拋 ArgumentNullException")]
        public void Constructor_NullDatabaseId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(
                    "Employee", BuildSchemaWithRowId(),
                    new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), null!));
        }

        [Fact]
        [DisplayName("建構子傳入有效引數應正確設定 ProgId 屬性")]
        public void Constructor_ValidArguments_SetsProgId()
        {
            var repo = CreateRepository("SalesOrder");
            Assert.Equal("SalesOrder", repo.ProgId);
        }

        // ---- GetNewData ----

        [Fact]
        [DisplayName("GetNewData Schema 的 MasterTable 為 null 時應拋 InvalidOperationException")]
        public void GetNewData_SchemaWithNoMasterTable_ThrowsInvalidOperationException()
        {
            // FormSchema() without ProgId → MasterTable returns null
            var schema = new FormSchema();
            var repo = new DataFormRepository(
                "Employee", schema,
                new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), "db");

            Assert.Throws<InvalidOperationException>(() => repo.GetNewData());
        }

        [Fact]
        [DisplayName("GetNewData 有效 Schema 應回傳含主表名稱的 DataSet")]
        public void GetNewData_ValidSchema_ReturnsMasterDataSet()
        {
            var repo = CreateRepository();
            var dataSet = repo.GetNewData();

            Assert.NotNull(dataSet);
            Assert.Equal("Employee", dataSet.DataSetName);
            Assert.True(dataSet.Tables.Contains("Employee"));
        }

        [Fact]
        [DisplayName("GetNewData 主表應有一筆新增列且 sys_rowid 為有效的非 Empty Guid")]
        public void GetNewData_ValidSchema_MasterRowHasNonEmptyRowId()
        {
            var repo = CreateRepository();
            var dataSet = repo.GetNewData();
            var masterTable = dataSet.Tables["Employee"]!;

            Assert.Single(masterTable.Rows);
            var rowId = masterTable.Rows[0][SysFields.RowId];
            Assert.IsType<Guid>(rowId);
            Assert.NotEqual(Guid.Empty, (Guid)rowId);
        }

        [Fact]
        [DisplayName("GetNewData Schema 含 Detail Table 時應回傳含主表與明細表的 DataSet")]
        public void GetNewData_SchemaWithDetailTable_ReturnsDataSetWithBothTables()
        {
            var schema = BuildSchemaWithRowId("Order");
            var detail = schema.Tables!.Add("OrderDetail", "Order Detail");
            detail.Fields!.Add("item_no", "Item No", FieldDbType.String);

            var repo = new DataFormRepository(
                "Order", schema,
                new StubDefineAccess(), new StubDbAccessFactory(), new StubConnectionManager(), "db");

            var dataSet = repo.GetNewData();

            Assert.True(dataSet.Tables.Contains("Order"));
            Assert.True(dataSet.Tables.Contains("OrderDetail"));
        }

        // ---- DefaultSortForPaging (private static，透過 Reflection 測試) ----

        private static readonly Type[] s_formSchemaParam = [typeof(FormSchema)];

        private static MethodInfo GetDefaultSortForPaging()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "DefaultSortForPaging",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_formSchemaParam,
                null);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("DefaultSortForPaging Schema 無 MasterTable 應拋 InvalidOperationException")]
        public void DefaultSortForPaging_NoMasterTable_ThrowsInvalidOperationException()
        {
            var schema = new FormSchema();
            var method = GetDefaultSortForPaging();
            var outer = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { schema }));
            Assert.IsType<InvalidOperationException>(outer.InnerException);
        }

        [Fact]
        [DisplayName("DefaultSortForPaging MasterTable 無 sys_no 欄位應拋 InvalidOperationException")]
        public void DefaultSortForPaging_NoSysNoField_ThrowsInvalidOperationException()
        {
            var schema = new FormSchema("Employee", "Employee");
            schema.Tables!.Add("Employee", "Employee");
            var method = GetDefaultSortForPaging();
            var outer = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { schema }));
            Assert.IsType<InvalidOperationException>(outer.InnerException);
        }

        [Fact]
        [DisplayName("DefaultSortForPaging MasterTable 含 sys_no 應回傳單一 sys_no ASC 排序")]
        public void DefaultSortForPaging_WithSysNoField_ReturnsSysNoAscSort()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.No, "No", FieldDbType.Integer);

            var method = GetDefaultSortForPaging();
            var result = (SortFieldCollection)method.Invoke(null, new object[] { schema })!;

            Assert.Single(result);
            Assert.Equal(SysFields.No, result[0].FieldName);
            Assert.Equal(SortDirection.Asc, result[0].Direction);
        }

        // ---- ConvertDefaultValue (private static，透過 Reflection 測試) ----

        private static MethodInfo GetConvertDefaultValue()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "ConvertDefaultValue",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("ConvertDefaultValue targetType 為 string 應回傳原始字串")]
        public void ConvertDefaultValue_StringType_ReturnsRawString()
        {
            var method = GetConvertDefaultValue();
            var result = method.Invoke(null, new object[] { "hello", typeof(string) });
            Assert.Equal("hello", result);
        }

        [Fact]
        [DisplayName("ConvertDefaultValue targetType 為 Guid 且輸入有效 GUID 字串應回傳解析後的 Guid")]
        public void ConvertDefaultValue_GuidTypeValidString_ReturnsParsedGuid()
        {
            var expected = new Guid("12345678-1234-1234-1234-123456789012");
            var method = GetConvertDefaultValue();
            var result = method.Invoke(null, new object[] { expected.ToString(), typeof(Guid) });
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("ConvertDefaultValue targetType 為 Guid 且輸入無效字串應回傳 Guid.Empty")]
        public void ConvertDefaultValue_GuidTypeInvalidString_ReturnsGuidEmpty()
        {
            var method = GetConvertDefaultValue();
            var result = method.Invoke(null, new object[] { "not-a-guid", typeof(Guid) });
            Assert.Equal(Guid.Empty, result);
        }

        [Fact]
        [DisplayName("ConvertDefaultValue targetType 為 int 且輸入無效字串應回傳 DBNull.Value")]
        public void ConvertDefaultValue_IntTypeInvalidString_ReturnsDbNull()
        {
            var method = GetConvertDefaultValue();
            var result = method.Invoke(null, new object[] { "abc", typeof(int) });
            Assert.Equal(DBNull.Value, result);
        }

        [Fact]
        [DisplayName("ConvertDefaultValue targetType 為 int 且輸入有效字串應回傳轉換後的 int")]
        public void ConvertDefaultValue_IntTypeValidString_ReturnsInt()
        {
            var method = GetConvertDefaultValue();
            var result = method.Invoke(null, new object[] { "42", typeof(int) });
            Assert.Equal(42, result);
        }

        // ---- TryCoerceToGuid (private static，透過 Reflection 測試) ----

        private static MethodInfo GetTryCoerceToGuid()
        {
            var method = typeof(DataFormRepository).GetMethod(
                "TryCoerceToGuid",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入 Guid 值應回傳相同 Guid")]
        public void TryCoerceToGuid_GuidValue_ReturnsGuid()
        {
            var id = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var method = GetTryCoerceToGuid();
            var result = (Guid?)method.Invoke(null, new object?[] { id });
            Assert.Equal(id, result);
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入有效 GUID 字串應回傳解析後的 Guid")]
        public void TryCoerceToGuid_ValidGuidString_ReturnsParsedGuid()
        {
            var id = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var method = GetTryCoerceToGuid();
            var result = (Guid?)method.Invoke(null, new object?[] { id.ToString() });
            Assert.Equal(id, result);
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入非 Guid 相容物件應回傳 null")]
        public void TryCoerceToGuid_NonGuidValue_ReturnsNull()
        {
            var method = GetTryCoerceToGuid();
            int intValue = 42;
            var result = (Guid?)method.Invoke(null, new object?[] { intValue });
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入 null 應回傳 null")]
        public void TryCoerceToGuid_NullValue_ReturnsNull()
        {
            var method = GetTryCoerceToGuid();
            object? nullObj = null;
            var result = (Guid?)method.Invoke(null, new object?[] { nullObj });
            Assert.Null(result);
        }
    }
}
