using System.Data;
using MessagePack;
using Bee.Base;

namespace Bee.Define.UnitTests
{
    public class SerializableDataSetTests
    {
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
    }
}

