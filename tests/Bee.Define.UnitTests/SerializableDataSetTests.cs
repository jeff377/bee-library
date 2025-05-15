using System.Data;
using MessagePack;
using Bee.Base;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Define.UnitTests
{
    public class SerializableDataSetTests
    {
        static SerializableDataSetTests()
        {
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        /// <summary>
        /// 測試從 DataSet 轉換為可序列化格式，並能正確還原回 DataSet。
        /// </summary>
        [Fact]
        public void FromDataSet_And_ToDataSet_ShouldBeEquivalent()
        {
            // 建立原始 DataSet
            var ds = new DataSet("TestDataSet");

            var table = new DataTable("Employees");
            table.Columns.Add(new DataColumn("Id", typeof(int)) { ReadOnly = true });
            table.Columns.Add(new DataColumn("Name", typeof(string)) { MaxLength = 50, DefaultValue = "Unknown" });
            table.Columns.Add(new DataColumn("BirthDate", typeof(DateTime)) { AllowDBNull = true });
            table.SetPrimaryKey("Id");

            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "John";
            row["BirthDate"] = new DateTime(1990, 1, 1);
            table.Rows.Add(row);

            ds.Tables.Add(table);

            // 轉換為可序列化格式
            var serializable = TSerializableDataSet.FromDataSet(ds);

            // 序列化與反序列化
            var bytes = MessagePackSerializer.Serialize(serializable);
            var deserialized = MessagePackSerializer.Deserialize<TSerializableDataSet>(bytes);

            // 還原回 DataSet
            var restored = TSerializableDataSet.ToDataSet(deserialized);

            // 驗證名稱
            Assert.Equal(ds.DataSetName, restored.DataSetName);
            Assert.Single(restored.Tables);
            Assert.Equal("Employees", restored.Tables[0].TableName);
            Assert.Equal(3, restored.Tables[0].Columns.Count);
            Assert.Single(restored.Tables[0].Rows);
            Assert.Equal("John", restored.Tables[0].Rows[0]["Name"]);
            Assert.Equal(new DateTime(1990, 1, 1), restored.Tables[0].Rows[0]["BirthDate"]);
        }

        /// <summary>
        /// 測試 DbNull.Value 是否能正確轉換為 null，並確認轉換後資料能夠正確寫回資料庫。
        /// </summary>
        [Fact]
        public void FromDataTable_And_ToDataTable_Should_Handle_DbNull_Correctly()
        {
            // Arrange：建立含 DBNull 的 DataTable
            var dt = new DataTable("TestTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));

            var row = dt.NewRow();
            row["Id"] = 1;
            row["Name"] = DBNull.Value; // 模擬空值
            dt.Rows.Add(row);

            // Act：轉為序列化格式，再轉回 DataTable
            var serializable = TSerializableDataTable.FromDataTable(dt);
            var restored = TSerializableDataTable.ToDataTable(serializable);

            // Assert：確認還原後的值為 DBNull.Value
            Assert.Equal(1, restored.Rows[0]["Id"]);
            Assert.True(restored.Rows[0].IsNull("Name")); // 正確為 DBNull.Value
        }

        /// <summary>
        /// 測試 MessagePack 是否能正確序列化與反序列化 DataTable。
        /// </summary>
        [Fact]
        public void Test_DataTable_Serialization()
        {
            // 建立範例 DataTable 並加入測試資料
            var table = new DataTable("TestTable");
            table.Columns.Add("Column1", typeof(string));
            table.Columns.Add("Column2", typeof(int));
            table.Rows.Add("Test1", 100);
            table.Rows.Add("Test2", 200);

            // 使用 MessagePackHelper 進行序列化
            byte[] serialized = MessagePackHelper.Serialize(table);
            // 反序列化回 DataTable
            var deserialized = MessagePackHelper.Deserialize<DataTable>(serialized);

            // 驗證資料是否正確
            Assert.Equal(2, deserialized.Rows.Count);
            Assert.Equal("Test1", deserialized.Rows[0]["Column1"]);
            Assert.Equal(100, deserialized.Rows[0]["Column2"]);
            Assert.Equal("Test2", deserialized.Rows[1]["Column1"]);
            Assert.Equal(200, deserialized.Rows[1]["Column2"]);
        }

        /// <summary>
        /// 測試 MessagePack 是否能正確序列化與反序列化 DataSet。
        /// </summary>
        [Fact]
        public void Test_DataSet_Serialization()
        {
            // 建立範例 DataSet 並加入兩個 DataTable
            var dataSet = new DataSet("TestDataSet");

            var table1 = new DataTable("Table1");
            table1.Columns.Add("Name", typeof(string));
            table1.Columns.Add("Age", typeof(int));
            table1.Rows.Add("Alice", 30);
            table1.Rows.Add("Bob", 40);

            var table2 = new DataTable("Table2");
            table2.Columns.Add("Product", typeof(string));
            table2.Columns.Add("Price", typeof(decimal));
            table2.Rows.Add("Pen", 1.5m);
            table2.Rows.Add("Notebook", 3.2m);

            dataSet.Tables.Add(table1);
            dataSet.Tables.Add(table2);

            // 使用 MessagePackHelper 進行序列化
            byte[] serialized = MessagePackHelper.Serialize(dataSet);

            // 反序列化回 DataSet
            var deserialized = MessagePackHelper.Deserialize<DataSet>(serialized);

            // 驗證資料是否正確
            Assert.Equal(2, deserialized.Tables.Count);

            var dt1 = deserialized.Tables["Table1"];
            Assert.Equal(2, dt1.Rows.Count);
            Assert.Equal("Alice", dt1.Rows[0]["Name"]);
            Assert.Equal(30, dt1.Rows[0]["Age"]);
            Assert.Equal("Bob", dt1.Rows[1]["Name"]);
            Assert.Equal(40, dt1.Rows[1]["Age"]);

            var dt2 = deserialized.Tables["Table2"];
            Assert.Equal(2, dt2.Rows.Count);
            Assert.Equal("Pen", dt2.Rows[0]["Product"]);
            Assert.Equal(1.5m, dt2.Rows[0]["Price"]);
            Assert.Equal("Notebook", dt2.Rows[1]["Product"]);
            Assert.Equal(3.2m, dt2.Rows[1]["Price"]);
        }

        [Fact(DisplayName = "DataTable 序列化還原後應保留 RowState 狀態")]
        public void SerializeDeserialize_DataTable_ShouldPreserveRowState()
        {
            var table = new DataTable("SampleTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            // 新增第一筆
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["Name"] = "資料1";
            table.Rows.Add(row1);

            // 新增第二筆
            var row2 = table.NewRow();
            row2["Id"] = 2;
            row2["Name"] = "資料2";
            table.Rows.Add(row2);

            // 先 AcceptChanges，兩筆變成 Unchanged
            table.AcceptChanges();

            // 修改第一筆 (RowState -> Modified)
            table.Rows[0]["Name"] = "修改後資料1";

            // 刪除第二筆 (RowState -> Deleted)
            table.Rows[1].Delete();

            // 新增第三筆 (RowState -> Added)
            var row3 = table.NewRow();
            row3["Id"] = 3;
            row3["Name"] = "新增資料3";
            table.Rows.Add(row3);

            // Serialize & Deserialize
            var bytes = MessagePackHelper.Serialize(table);
            var restored = MessagePackHelper.Deserialize<DataTable>(bytes);

            if (!DataTableComparer.IsEqual(table, restored))
            {
                Assert.Fail("序列化還原後的 DataTable 與原始 DataTable 不相等");
            }
        }




    }
}

