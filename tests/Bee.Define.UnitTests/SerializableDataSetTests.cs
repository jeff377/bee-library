using System.Data;
using MessagePack;
using Bee.Base;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Define.UnitTests
{
    public class SerializableDataSetTests
    {
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
            // 使用自訂的 TFormatterResolver 搭配 StandardResolver 組合解析器
            var resolver = CompositeResolver.Create(
                [
                    new TDataTableFormatter() // 自訂 formatter
                ],
                [
                    TFormatterResolver.Instance, // 自訂 resolver
                    StandardResolver.Instance    // 標準 resolver 做為後援
                ]);

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            // 建立範例 DataTable 並加入測試資料
            var table = new DataTable("TestTable");
            table.Columns.Add("Column1", typeof(string));
            table.Columns.Add("Column2", typeof(int));
            table.Rows.Add("Test1", 100);
            table.Rows.Add("Test2", 200);

            // 序列化 DataTable
            byte[] serialized = MessagePackSerializer.Serialize(table, options);

            // 反序列化回 DataTable
            var deserialized = MessagePackSerializer.Deserialize<DataTable>(serialized, options);

            // 驗證資料是否正確
            Assert.Equal(2, deserialized.Rows.Count);
            Assert.Equal("Test1", deserialized.Rows[0]["Column1"]);
            Assert.Equal(100, deserialized.Rows[0]["Column2"]);
            Assert.Equal("Test2", deserialized.Rows[1]["Column1"]);
            Assert.Equal(200, deserialized.Rows[1]["Column2"]);
        }

    }
}

