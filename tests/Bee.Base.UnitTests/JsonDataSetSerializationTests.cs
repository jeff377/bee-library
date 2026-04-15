using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// JSON DataSet/DataTable 序列化測試。
    /// 驗證自訂 DataTableJsonConverter / DataSetJsonConverter 透過 SerializeFunc 的往返正確性，
    /// 涵蓋所有 FieldDbType 欄位型別、RowState 保留、DBNull 處理、DataRelation、PrimaryKey 及邊界條件。
    /// 已遷移至 System.Text.Json，作為自訂 Converter 的回歸驗收標準。
    /// </summary>
    public class JsonDataSetSerializationTests
    {
        #region Helper

        /// <summary>
        /// 透過 SerializeFunc 執行 DataTable JSON 序列化往返。
        /// </summary>
        private static DataTable JsonRoundTripTable(DataTable table)
        {
            string json = SerializeFunc.ObjectToJson(table, includeTypeName: false);
            return SerializeFunc.JsonToObject<DataTable>(json, includeTypeName: false);
        }

        /// <summary>
        /// 透過 SerializeFunc 執行 DataSet JSON 序列化往返。
        /// </summary>
        private static DataSet JsonRoundTripDataSet(DataSet dataSet)
        {
            string json = SerializeFunc.ObjectToJson(dataSet, includeTypeName: false);
            return SerializeFunc.JsonToObject<DataSet>(json, includeTypeName: false);
        }

        #endregion

        #region 一、DataTable 基本序列化

        /// <summary>
        /// 測試 DataTable 基本序列化往返，驗證 TableName、欄位數、列數、值正確還原。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化往返")]
        public void DataTable_JsonSerialize_RoundTrip()
        {
            var table = new DataTable("TestTable");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Rows.Add("Alice", 30);
            table.Rows.Add("Bob", 40);

            var restored = JsonRoundTripTable(table);

            Assert.Equal("TestTable", restored.TableName);
            Assert.Equal(2, restored.Columns.Count);
            Assert.Equal(2, restored.Rows.Count);
            Assert.Equal("Alice", restored.Rows[0]["Name"]);
            Assert.Equal(30, restored.Rows[0]["Age"]);
            Assert.Equal("Bob", restored.Rows[1]["Name"]);
            Assert.Equal(40, restored.Rows[1]["Age"]);
        }

        /// <summary>
        /// 測試 DataTable 含 DBNull 值的序列化，還原後 IsNull 為 true。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化處理 DBNull 值")]
        public void DataTable_JsonSerializeWithDbNull_PreservesValues()
        {
            var table = new DataTable("TestTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = DBNull.Value;
            table.Rows.Add(row);

            var restored = JsonRoundTripTable(table);

            Assert.Equal(1, restored.Rows[0]["Id"]);
            Assert.True(restored.Rows[0].IsNull("Name"));
        }

        /// <summary>
        /// 測試 DataTable 序列化保留四種 RowState（Added / Modified / Deleted / Unchanged）。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化保留 RowState")]
        public void DataTable_JsonSerializeWithRowState_PreservesState()
        {
            var table = new DataTable("SampleTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            // 新增兩筆後 AcceptChanges -> Unchanged
            table.Rows.Add(1, "資料1");
            table.Rows.Add(2, "資料2");
            table.AcceptChanges();

            // Modified
            table.Rows[0]["Name"] = "修改後資料1";

            // Deleted
            table.Rows[1].Delete();

            // Added
            table.Rows.Add(3, "新增資料3");

            var restored = JsonRoundTripTable(table);

            Assert.True(DataTableComparer.IsEqual(table, restored),
                "序列化還原後的 DataTable 與原始 DataTable 不相等");
        }

        #endregion

        #region 二、FieldDbType 全型別覆蓋

        /// <summary>
        /// 提供所有 FieldDbType 對應的測試資料。
        /// </summary>
        public static IEnumerable<object[]> AllFieldDbTypeTestData()
        {
            yield return new object[] { "StringCol", typeof(string), "Hello 測試" };
            yield return new object[] { "TextCol", typeof(string), "Long text content with 中文字" };
            yield return new object[] { "BoolCol", typeof(bool), true };
            yield return new object[] { "AutoIncCol", typeof(int), 42 };
            yield return new object[] { "ShortCol", typeof(short), (short)12345 };
            yield return new object[] { "IntCol", typeof(int), int.MaxValue };
            yield return new object[] { "LongCol", typeof(long), long.MaxValue };
            yield return new object[] { "DecimalCol", typeof(decimal), 123456.789m };
            yield return new object[] { "CurrencyCol", typeof(decimal), 99999.99m };
            yield return new object[] { "DateCol", typeof(DateTime), new DateTime(2026, 4, 15) };
            yield return new object[] { "DateTimeCol", typeof(DateTime), new DateTime(2026, 4, 15, 10, 30, 45) };
            yield return new object[] { "GuidCol", typeof(Guid), new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") };
            yield return new object[] { "BinaryCol", typeof(byte[]), new byte[] { 0x01, 0x02, 0xAB, 0xFF } };
        }

        /// <summary>
        /// 測試所有 FieldDbType 對應的 .NET 型別皆能正確序列化往返。
        /// </summary>
        [Theory]
        [MemberData(nameof(AllFieldDbTypeTestData))]
        [DisplayName("DataTable JSON 序列化支援所有 FieldDbType 欄位型別")]
        public void DataTable_JsonSerialize_AllFieldDbTypes(string columnName, Type columnType, object testValue)
        {
            var table = new DataTable("TypeTestTable");
            table.Columns.Add(columnName, columnType);

            var row = table.NewRow();
            row[columnName] = testValue;
            table.Rows.Add(row);

            var restored = JsonRoundTripTable(table);

            Assert.Equal(1, restored.Rows.Count);
            Assert.Equal(columnType, restored.Columns[columnName]!.DataType);

            var restoredValue = restored.Rows[0][columnName];

            if (testValue is byte[] expectedBytes)
            {
                Assert.IsType<byte[]>(restoredValue);
                Assert.Equal(expectedBytes, (byte[])restoredValue);
            }
            else
            {
                Assert.Equal(testValue, restoredValue);
            }
        }

        #endregion

        #region 三、DataSet 多表與關聯

        /// <summary>
        /// 測試 DataSet 含多個 DataTable 的序列化往返。
        /// </summary>
        [Fact]
        [DisplayName("DataSet JSON 序列化往返")]
        public void DataSet_JsonSerialize_RoundTrip()
        {
            var dataSet = new DataSet("TestDataSet");

            var table1 = new DataTable("Orders");
            table1.Columns.Add("OrderId", typeof(int));
            table1.Columns.Add("Customer", typeof(string));
            table1.Rows.Add(1, "Alice");
            table1.Rows.Add(2, "Bob");

            var table2 = new DataTable("Products");
            table2.Columns.Add("ProductId", typeof(int));
            table2.Columns.Add("Price", typeof(decimal));
            table2.Rows.Add(101, 19.99m);
            table2.Rows.Add(102, 45.50m);

            dataSet.Tables.Add(table1);
            dataSet.Tables.Add(table2);

            var restored = JsonRoundTripDataSet(dataSet);

            Assert.Equal("TestDataSet", restored.DataSetName);
            Assert.Equal(2, restored.Tables.Count);

            var rt1 = restored.Tables["Orders"];
            Assert.NotNull(rt1);
            Assert.Equal(2, rt1.Rows.Count);
            Assert.Equal("Alice", rt1.Rows[0]["Customer"]);
            Assert.Equal(2, rt1.Rows[1]["OrderId"]);

            var rt2 = restored.Tables["Products"];
            Assert.NotNull(rt2);
            Assert.Equal(2, rt2.Rows.Count);
            Assert.Equal(19.99m, rt2.Rows[0]["Price"]);
            Assert.Equal(45.50m, rt2.Rows[1]["Price"]);
        }

        /// <summary>
        /// 測試 DataSet 含 Master-Detail DataRelation 的序列化保留。
        /// </summary>
        [Fact]
        [DisplayName("DataSet JSON 序列化保留 DataRelation")]
        public void DataSet_JsonSerializeWithRelation_PreservesRelation()
        {
            var dataSet = new DataSet("OrderSystem");

            var master = new DataTable("Order");
            master.Columns.Add("OrderId", typeof(int));
            master.Columns.Add("Customer", typeof(string));
            master.PrimaryKey = new[] { master.Columns["OrderId"]! };
            master.Rows.Add(1, "Alice");

            var detail = new DataTable("OrderDetail");
            detail.Columns.Add("DetailId", typeof(int));
            detail.Columns.Add("OrderId", typeof(int));
            detail.Columns.Add("Product", typeof(string));
            detail.Rows.Add(10, 1, "Pen");
            detail.Rows.Add(11, 1, "Notebook");

            dataSet.Tables.Add(master);
            dataSet.Tables.Add(detail);
            dataSet.Relations.Add("Order_Detail",
                master.Columns["OrderId"]!,
                detail.Columns["OrderId"]!);

            var restored = JsonRoundTripDataSet(dataSet);

            Assert.Single(restored.Relations);
            var rel = restored.Relations[0];
            Assert.Equal("Order_Detail", rel.RelationName);
            Assert.Equal("Order", rel.ParentTable.TableName);
            Assert.Equal("OrderDetail", rel.ChildTable.TableName);
            Assert.Equal("OrderId", rel.ParentColumns[0].ColumnName);
            Assert.Equal("OrderId", rel.ChildColumns[0].ColumnName);
        }

        #endregion

        #region 四、欄位中繼資料保留

        /// <summary>
        /// 測試 DataTable 序列化保留欄位中繼資料（AllowDBNull、ReadOnly、MaxLength、Caption）。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化保留欄位中繼資料")]
        public void DataTable_JsonSerialize_PreservesColumnMetadata()
        {
            var table = new DataTable("MetaTable");

            var col1 = new DataColumn("Code", typeof(string))
            {
                AllowDBNull = false,
                MaxLength = 20,
                Caption = "代碼"
            };

            var col2 = new DataColumn("Amount", typeof(decimal))
            {
                ReadOnly = true,
                Caption = "金額"
            };

            table.Columns.Add(col1);
            table.Columns.Add(col2);
            table.Rows.Add("A001", 100.50m);

            var restored = JsonRoundTripTable(table);

            var rc1 = restored.Columns["Code"]!;
            Assert.False(rc1.AllowDBNull);
            Assert.Equal(20, rc1.MaxLength);
            Assert.Equal("代碼", rc1.Caption);

            var rc2 = restored.Columns["Amount"]!;
            Assert.True(rc2.ReadOnly);
            Assert.Equal("金額", rc2.Caption);
        }

        /// <summary>
        /// 測試 DataTable 序列化保留 PrimaryKey 設定。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化保留 PrimaryKey")]
        public void DataTable_JsonSerialize_PreservesPrimaryKey()
        {
            var table = new DataTable("PkTable");
            table.Columns.Add("CompanyId", typeof(string));
            table.Columns.Add("DeptId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.PrimaryKey = new[]
            {
                table.Columns["CompanyId"]!,
                table.Columns["DeptId"]!
            };
            table.Rows.Add("C01", "D01", "研發部");

            var restored = JsonRoundTripTable(table);

            Assert.Equal(2, restored.PrimaryKey.Length);
            Assert.Equal("CompanyId", restored.PrimaryKey[0].ColumnName);
            Assert.Equal("DeptId", restored.PrimaryKey[1].ColumnName);
        }

        #endregion

        #region 五、RowState 細節驗證

        /// <summary>
        /// 測試 Modified 資料列序列化後保留 Original 與 Current 值。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化 Modified 資料列保留原始值")]
        public void DataTable_JsonSerialize_ModifiedRow_PreservesOriginalValues()
        {
            var table = new DataTable("ModifiedTest");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, "原始值");
            table.AcceptChanges();

            table.Rows[0]["Name"] = "修改後";

            var restored = JsonRoundTripTable(table);

            var row = restored.Rows[0];
            Assert.Equal(DataRowState.Modified, row.RowState);
            Assert.Equal("修改後", row["Name", DataRowVersion.Current]);
            Assert.Equal("原始值", row["Name", DataRowVersion.Original]);
            Assert.Equal(1, row["Id", DataRowVersion.Current]);
        }

        /// <summary>
        /// 測試 Deleted 資料列序列化後保留 Original 值。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化 Deleted 資料列保留原始值")]
        public void DataTable_JsonSerialize_DeletedRow_PreservesOriginalValues()
        {
            var table = new DataTable("DeletedTest");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, "待刪除");
            table.AcceptChanges();

            table.Rows[0].Delete();

            var restored = JsonRoundTripTable(table);

            var row = restored.Rows[0];
            Assert.Equal(DataRowState.Deleted, row.RowState);
            Assert.Equal(1, row["Id", DataRowVersion.Original]);
            Assert.Equal("待刪除", row["Name", DataRowVersion.Original]);
        }

        #endregion

        #region 六、邊界條件

        /// <summary>
        /// 測試有欄位定義但無資料列的空 DataTable 序列化。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化空資料表")]
        public void DataTable_JsonSerializeEmptyTable_RoundTrip()
        {
            var table = new DataTable("EmptyTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            var restored = JsonRoundTripTable(table);

            Assert.Equal("EmptyTable", restored.TableName);
            Assert.Equal(2, restored.Columns.Count);
            Assert.Equal(0, restored.Rows.Count);
        }

        /// <summary>
        /// 測試所有欄位皆為 DBNull 的資料列序列化。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化全 null 資料列")]
        public void DataTable_JsonSerializeAllNullRow_RoundTrip()
        {
            var table = new DataTable("NullTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));

            var row = table.NewRow();
            // 所有欄位皆保持 DBNull
            table.Rows.Add(row);

            var restored = JsonRoundTripTable(table);

            Assert.Equal(1, restored.Rows.Count);
            Assert.True(restored.Rows[0].IsNull("Id"));
            Assert.True(restored.Rows[0].IsNull("Name"));
            Assert.True(restored.Rows[0].IsNull("Amount"));
        }

        /// <summary>
        /// 測試無任何 DataTable 的空 DataSet 序列化。
        /// </summary>
        [Fact]
        [DisplayName("DataSet JSON 序列化空 DataSet")]
        public void DataSet_JsonSerializeEmptyDataSet_RoundTrip()
        {
            var dataSet = new DataSet("EmptySet");

            var restored = JsonRoundTripDataSet(dataSet);

            Assert.Equal("EmptySet", restored.DataSetName);
            Assert.Empty(restored.Tables);
        }

        /// <summary>
        /// 測試 null DataTable 序列化後還原為 null。
        /// </summary>
        [Fact]
        [DisplayName("DataTable JSON 序列化 null 值")]
        public void DataTable_JsonSerialize_Null_ReturnsNull()
        {
            string json = SerializeFunc.ObjectToJson((DataTable?)null, includeTypeName: false);
            var restored = SerializeFunc.JsonToObject<DataTable?>(json, includeTypeName: false);

            Assert.Null(restored);
        }

        #endregion
    }
}
