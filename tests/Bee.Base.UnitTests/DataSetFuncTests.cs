using System.ComponentModel;
using System.Data;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    public class DataSetFuncTests
    {
        private static readonly string[] s_copyColumns = { "NAME", "ID" };

        [Fact]
        [DisplayName("CreateDataSet 預設名稱應為 \"DataSet\"")]
        public void CreateDataSet_Default_UsesDefaultName()
        {
            var ds = DataSetFunc.CreateDataSet();
            Assert.Equal("DataSet", ds.DataSetName);
        }

        [Fact]
        [DisplayName("CreateDataSet 應使用指定名稱")]
        public void CreateDataSet_WithName_UsesProvidedName()
        {
            var ds = DataSetFunc.CreateDataSet("Orders");
            Assert.Equal("Orders", ds.DataSetName);
        }

        [Fact]
        [DisplayName("CreateDataTable 預設名稱應為 \"DataTable\"")]
        public void CreateDataTable_Default_UsesDefaultName()
        {
            var table = DataSetFunc.CreateDataTable();
            Assert.Equal("DataTable", table.TableName);
        }

        [Fact]
        [DisplayName("CreateDataTable 應使用指定名稱")]
        public void CreateDataTable_WithName_UsesProvidedName()
        {
            var table = DataSetFunc.CreateDataTable("Customers");
            Assert.Equal("Customers", table.TableName);
        }

        [Fact]
        [DisplayName("CopyDataTable 應僅保留指定欄位並還原欄位排序")]
        public void CopyDataTable_KeepsSpecifiedColumnsInOrder()
        {
            var source = new DataTable("Source");
            source.Columns.Add("Id", typeof(int));
            source.Columns.Add("Name", typeof(string));
            source.Columns.Add("Age", typeof(int));
            source.Columns.Add("Extra", typeof(string));
            source.Rows.Add(1, "Alice", 30, "drop");
            source.Rows.Add(2, "Bob", 40, "drop");

            var copy = DataSetFunc.CopyDataTable(source, s_copyColumns);

            // CopyDataTable only filters and reorders columns; it does not rename them.
            // Case-insensitive matching is used to identify which source columns to keep.
            Assert.Equal(2, copy.Columns.Count);
            Assert.Equal("Name", copy.Columns[0].ColumnName);
            Assert.Equal("Id", copy.Columns[1].ColumnName);
            Assert.Equal(2, copy.Rows.Count);
            Assert.Equal("Alice", copy.Rows[0]["Name"]);
        }

        [Fact]
        [DisplayName("UpperColumnName 應將所有欄位名轉為大寫")]
        public void UpperColumnName_ConvertsAllColumnsToUpperCase()
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("age", typeof(int));

            DataSetFunc.UpperColumnName(table);

            Assert.Equal("NAME", table.Columns[0].ColumnName);
            Assert.Equal("AGE", table.Columns[1].ColumnName);
        }

        [Theory]
        [InlineData(FieldDbType.String, "")]
        [InlineData(FieldDbType.Text, "")]
        [InlineData(FieldDbType.Boolean, false)]
        [InlineData(FieldDbType.Integer, 0)]
        [InlineData(FieldDbType.Decimal, 0)]
        [InlineData(FieldDbType.Currency, 0)]
        [DisplayName("GetDefaultValue 應為基本型別回傳對應預設值")]
        public void GetDefaultValue_ReturnsExpectedForPrimitiveTypes(FieldDbType type, object expected)
        {
            Assert.Equal(expected, DataSetFunc.GetDefaultValue(type));
        }

        [Fact]
        [DisplayName("GetDefaultValue 對 Date/DateTime/Guid 應回傳合理預設")]
        public void GetDefaultValue_ReturnsExpectedForDateAndGuidTypes()
        {
            Assert.Equal(DateTime.Today, DataSetFunc.GetDefaultValue(FieldDbType.Date));

            var now = DataSetFunc.GetDefaultValue(FieldDbType.DateTime);
            Assert.IsType<DateTime>(now);

            Assert.Equal(Guid.Empty, DataSetFunc.GetDefaultValue(FieldDbType.Guid));
        }

        [Fact]
        [DisplayName("GetDefaultValue 於未對映型別應回傳 DBNull.Value")]
        public void GetDefaultValue_UnmappedType_ReturnsDbNull()
        {
            Assert.Equal(DBNull.Value, DataSetFunc.GetDefaultValue(FieldDbType.Binary));
            Assert.Equal(DBNull.Value, DataSetFunc.GetDefaultValue(FieldDbType.Unknown));
            Assert.Equal(DBNull.Value, DataSetFunc.GetDefaultValue(FieldDbType.AutoIncrement));
            Assert.Equal(DBNull.Value, DataSetFunc.GetDefaultValue(FieldDbType.Short));
            Assert.Equal(DBNull.Value, DataSetFunc.GetDefaultValue(FieldDbType.Long));
        }
    }

    public class DataTableComparerTests
    {
        private static DataTable BuildTable(string name = "T")
        {
            var table = new DataTable(name);
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, "a");
            table.Rows.Add(2, "b");
            table.AcceptChanges();
            return table;
        }

        [Fact]
        [DisplayName("IsEqual 相同結構與資料應回傳 true")]
        public void IsEqual_IdenticalTables_ReturnsTrue()
        {
            Assert.True(DataTableComparer.IsEqual(BuildTable(), BuildTable()));
        }

        [Fact]
        [DisplayName("IsEqual 任一方為 null 應回傳 false")]
        public void IsEqual_NullTable_ReturnsFalse()
        {
            Assert.False(DataTableComparer.IsEqual(null!, BuildTable()));
            Assert.False(DataTableComparer.IsEqual(BuildTable(), null!));
        }

        [Fact]
        [DisplayName("IsEqual 表名不同應回傳 false")]
        public void IsEqual_DifferentTableName_ReturnsFalse()
        {
            Assert.False(DataTableComparer.IsEqual(BuildTable("A"), BuildTable("B")));
        }

        [Fact]
        [DisplayName("IsEqual 欄位數或欄位型別不同應回傳 false")]
        public void IsEqual_DifferentSchema_ReturnsFalse()
        {
            var a = BuildTable();
            var b = BuildTable();
            b.Columns.Add("Extra", typeof(string));
            Assert.False(DataTableComparer.IsEqual(a, b));

            var c = BuildTable();
            c.Columns["Name"]!.ColumnName = "Title";
            Assert.False(DataTableComparer.IsEqual(BuildTable(), c));
        }

        [Fact]
        [DisplayName("IsEqual 列數不同應回傳 false")]
        public void IsEqual_DifferentRowCount_ReturnsFalse()
        {
            var a = BuildTable();
            var b = BuildTable();
            b.Rows.Add(3, "c");
            b.AcceptChanges();
            Assert.False(DataTableComparer.IsEqual(a, b));
        }

        [Fact]
        [DisplayName("IsEqual Modified 狀態應同時比對 Current 與 Original 值")]
        public void IsEqual_ModifiedState_ComparesCurrentAndOriginal()
        {
            var a = BuildTable();
            var b = BuildTable();

            a.Rows[0]["Name"] = "changed";
            b.Rows[0]["Name"] = "changed";

            Assert.True(DataTableComparer.IsEqual(a, b));

            var c = BuildTable();
            c.Rows[0]["Name"] = "different";
            Assert.False(DataTableComparer.IsEqual(a, c));
        }

        [Fact]
        [DisplayName("IsEqual Deleted 狀態應比對 Original 值")]
        public void IsEqual_DeletedState_ComparesOriginalValues()
        {
            var a = BuildTable();
            var b = BuildTable();
            a.Rows[0].Delete();
            b.Rows[0].Delete();

            Assert.True(DataTableComparer.IsEqual(a, b));
        }
    }
}
