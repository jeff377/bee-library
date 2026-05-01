using System.ComponentModel;
using System.Data;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    public class DataRowExtensionsTests
    {
        private static DataRow BuildRow()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("Nullable", typeof(int));
            table.Columns["Nullable"]!.AllowDBNull = true;

            var row = table.NewRow();
            row["Id"] = 5;
            row["Name"] = "Alice";
            row["Amount"] = 12.5m;
            row["Nullable"] = DBNull.Value;
            table.Rows.Add(row);
            return row;
        }

        [Fact]
        [DisplayName("GetFieldValue<T> 應取得欄位值並進行型別轉換")]
        public void GetFieldValue_ReturnsTypedValue()
        {
            var row = BuildRow();

            Assert.Equal(5, row.GetFieldValue<int>("Id"));
            Assert.Equal("Alice", row.GetFieldValue<string>("Name"));
            Assert.Equal(12.5m, row.GetFieldValue<decimal>("Amount"));
        }

        [Fact]
        [DisplayName("GetFieldValue 於 DBNull 應回傳型別預設值")]
        public void GetFieldValue_DbNull_ReturnsDefault()
        {
            var row = BuildRow();
            Assert.Equal(0, row.GetFieldValue<int>("Nullable"));
        }

        [Fact]
        [DisplayName("GetFieldValue 於空欄位名稱應拋出 ArgumentNullException")]
        public void GetFieldValue_EmptyColumnName_Throws()
        {
            var row = BuildRow();
            Assert.Throws<ArgumentNullException>(() => row.GetFieldValue<int>(string.Empty));
        }

        [Fact]
        [DisplayName("GetFieldValue 於欄位不存在應拋出 InvalidOperationException")]
        public void GetFieldValue_MissingColumn_Throws()
        {
            var row = BuildRow();
            Assert.Throws<InvalidOperationException>(() => row.GetFieldValue<int>("Missing"));
        }

        [Fact]
        [DisplayName("GetFieldValue 於型別無法轉換應拋出 InvalidOperationException")]
        public void GetFieldValue_InvalidConversion_Throws()
        {
            var row = BuildRow();
            Assert.Throws<InvalidOperationException>(() => row.GetFieldValue<Guid>("Name"));
        }

        [Fact]
        [DisplayName("GetFieldValue(defaultValue) 於欄位不存在應回傳預設值")]
        public void GetFieldValue_WithDefault_MissingColumn_ReturnsDefault()
        {
            var row = BuildRow();
            Assert.Equal(-1, row.GetFieldValue<int>("Missing", -1));
        }

        [Fact]
        [DisplayName("GetFieldValue(defaultValue) 於空欄位名稱應拋出 ArgumentNullException")]
        public void GetFieldValue_WithDefault_EmptyColumnName_Throws()
        {
            var row = BuildRow();
            Assert.Throws<ArgumentNullException>(() => row.GetFieldValue<int>(string.Empty, -1));
        }

        [Fact]
        [DisplayName("GetFieldValue(defaultValue) 於欄位存在且值有效時應回傳轉型後的值")]
        public void GetFieldValue_WithDefault_ExistingColumn_ReturnsTypedValue()
        {
            var row = BuildRow();
            Assert.Equal(5, row.GetFieldValue<int>("Id", -1));
            Assert.Equal("Alice", row.GetFieldValue<string>("Name", "fallback"));
        }

        [Fact]
        [DisplayName("GetFieldValue(defaultValue) 於欄位為 DBNull 時應回傳型別預設值")]
        public void GetFieldValue_WithDefault_DbNull_ReturnsTypeDefault()
        {
            var row = BuildRow();
            Assert.Equal(0, row.GetFieldValue<int>("Nullable", -1));
        }

        [Fact]
        [DisplayName("GetFieldValue(defaultValue) 於型別無法轉換應拋出 InvalidOperationException")]
        public void GetFieldValue_WithDefault_InvalidConversion_Throws()
        {
            var row = BuildRow();
            Assert.Throws<InvalidOperationException>(() => row.GetFieldValue<Guid>("Name", Guid.Empty));
        }
    }

    public class DataTableExtensionsTests
    {
        [Fact]
        [DisplayName("AddColumn 以 FieldDbType 加入欄位，欄名轉大寫並套用預設值")]
        public void AddColumn_FieldDbType_AppliesDefaultsAndUppercase()
        {
            var table = new DataTable();
            var col = table.AddColumn("name", FieldDbType.String);

            Assert.Equal("NAME", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue);
            Assert.False(col.AllowDBNull);
        }

        [Fact]
        [DisplayName("AddColumn 指定預設值應影響 AllowDBNull")]
        public void AddColumn_WithExplicitDefault_SetsAllowDbNull()
        {
            var table = new DataTable();
            var col = table.AddColumn("id", FieldDbType.Integer, 0);

            Assert.Equal("ID", col.ColumnName);
            Assert.Equal(0, col.DefaultValue);
            Assert.False(col.AllowDBNull);
        }

        [Fact]
        [DisplayName("AddColumn 以 caption 參數應套用欄位標題")]
        public void AddColumn_WithCaption_AppliesCaption()
        {
            var table = new DataTable();
            var col = table.AddColumn("title", "Title", FieldDbType.String, "");
            Assert.Equal("Title", col.Caption);
        }

        [Fact]
        [DisplayName("HasField 應依欄位存在與否回傳結果")]
        public void HasField_ReflectsSchema()
        {
            var table = new DataTable();
            table.AddColumn("id", FieldDbType.Integer);

            Assert.True(table.HasField("ID"));
            Assert.False(table.HasField("MISSING"));
        }

        [Fact]
        [DisplayName("SetPrimaryKey 應解析逗號分隔欄位名並設定主鍵")]
        public void SetPrimaryKey_ParsesCommaSeparatedColumns()
        {
            var table = new DataTable();
            table.AddColumn("id", FieldDbType.Integer);
            table.AddColumn("code", FieldDbType.String);

            table.SetPrimaryKey("ID,CODE");

            Assert.Equal(2, table.PrimaryKey.Length);
            Assert.Equal("ID", table.PrimaryKey[0].ColumnName);
            Assert.Equal("CODE", table.PrimaryKey[1].ColumnName);
        }

        [Fact]
        [DisplayName("IsEmpty 應依列數回傳 true/false")]
        public void IsEmpty_ReflectsRowCount()
        {
            var table = new DataTable();
            table.AddColumn("id", FieldDbType.Integer);

            Assert.True(table.IsEmpty());

            table.Rows.Add(1);
            Assert.False(table.IsEmpty());
        }

        [Fact]
        [DisplayName("UppercaseColumnNames 應將所有欄位名轉為大寫")]
        public void UppercaseColumnNames_ConvertsAllColumnsToUpperCase()
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("age", typeof(int));

            table.UppercaseColumnNames();

            Assert.Equal("NAME", table.Columns[0].ColumnName);
            Assert.Equal("AGE", table.Columns[1].ColumnName);
        }
    }

    public class DataSetExtensionsTests
    {
        [Fact]
        [DisplayName("GetMasterTable 應回傳與 DataSetName 同名的資料表")]
        public void GetMasterTable_ReturnsTableNamedLikeDataSet()
        {
            var ds = new DataSet("Orders");
            ds.Tables.Add(new DataTable("Detail"));
            ds.Tables.Add(new DataTable("Orders"));

            var master = ds.GetMasterTable();
            Assert.NotNull(master);
            Assert.Equal("Orders", master!.TableName);
        }

        [Fact]
        [DisplayName("GetMasterTable 無對應表時應回傳 null")]
        public void GetMasterTable_MissingTable_ReturnsNull()
        {
            var ds = new DataSet("Orders");
            Assert.Null(ds.GetMasterTable());
        }

        [Fact]
        [DisplayName("GetMasterRow 應回傳 master 的第一列")]
        public void GetMasterRow_ReturnsFirstRow()
        {
            var ds = new DataSet("Orders");
            var table = new DataTable("Orders");
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(1);
            table.Rows.Add(2);
            ds.Tables.Add(table);

            var row = ds.GetMasterRow();
            Assert.NotNull(row);
            Assert.Equal(1, row!["Id"]);
        }

        [Fact]
        [DisplayName("GetMasterRow 當 master 為空時應回傳 null")]
        public void GetMasterRow_EmptyTable_ReturnsNull()
        {
            var ds = new DataSet("Orders");
            ds.Tables.Add(new DataTable("Orders"));
            Assert.Null(ds.GetMasterRow());
        }

        [Fact]
        [DisplayName("IsEmpty 應綜合判斷 Tables 與 master rows")]
        public void IsEmpty_ConsidersTablesAndMasterRows()
        {
            Assert.True(new DataSet().IsEmpty());

            var ds = new DataSet("Orders");
            ds.Tables.Add(new DataTable("Orders"));
            Assert.True(ds.IsEmpty());

            ds.Tables["Orders"]!.Columns.Add("Id", typeof(int));
            ds.Tables["Orders"]!.Rows.Add(1);
            Assert.False(ds.IsEmpty());
        }
    }

    public class DataViewExtensionsTests
    {
        private static DataTable BuildTable()
        {
            var table = new DataTable("T");
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(1);
            table.Rows.Add(2);
            table.Rows.Add(3);
            table.AcceptChanges();
            return table;
        }

        [Fact]
        [DisplayName("DeleteRows 應刪除 View 中所有列，依參數決定是否 AcceptChanges")]
        public void DeleteRows_RemovesAllRowsAndOptionallyAccepts()
        {
            var table = BuildTable();
            var view = new DataView(table);

            view.DeleteRows(acceptChanges: true);

            Assert.Equal(0, table.Rows.Count);
        }

        [Fact]
        [DisplayName("HasField 應轉呼叫底層 DataTable.HasField")]
        public void HasField_DelegatesToTable()
        {
            var view = new DataView(BuildTable());
            Assert.True(view.HasField("Id"));
            Assert.False(view.HasField("Missing"));
        }

        [Fact]
        [DisplayName("IsEmpty 應反映 DataView.Count")]
        public void IsEmpty_ReflectsRowCount()
        {
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("Id", typeof(int));
            Assert.True(new DataView(emptyTable).IsEmpty());

            Assert.False(new DataView(BuildTable()).IsEmpty());
        }
    }

    public class DataRowViewExtensionsTests
    {
        [Fact]
        [DisplayName("DataRowView.GetFieldValue 應轉呼叫底層 DataRow.GetFieldValue")]
        public void GetFieldValue_DelegatesToRow()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(7);
            var view = new DataView(table);

            Assert.Equal(7, view[0].GetFieldValue<int>("Id"));
            Assert.Equal(-1, view[0].GetFieldValue<int>("Missing", -1));
        }
    }
}
