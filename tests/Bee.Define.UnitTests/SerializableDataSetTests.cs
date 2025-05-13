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
    }
}

