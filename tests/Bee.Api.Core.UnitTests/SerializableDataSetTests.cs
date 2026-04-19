using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// SerializableDataSet 的 FromDataSet/ToDataSet 與 DataRelation round-trip 測試。
    /// </summary>
    public class SerializableDataSetTests
    {
        private static readonly string[] ExpectedParentColumns = ["Id"];
        private static readonly string[] ExpectedChildColumns = ["CustomerId"];

        private static DataSet BuildMasterDetailDataSet()
        {
            var ds = new DataSet("Orders");

            var master = new DataTable("Customer");
            master.Columns.Add("Id", typeof(int));
            master.Columns.Add("Name", typeof(string));
            master.Rows.Add(1, "Alice");
            master.Rows.Add(2, "Bob");
            ds.Tables.Add(master);

            var detail = new DataTable("Order");
            detail.Columns.Add("OrderId", typeof(int));
            detail.Columns.Add("CustomerId", typeof(int));
            detail.Columns.Add("Amount", typeof(decimal));
            detail.Rows.Add(101, 1, 10.5m);
            detail.Rows.Add(102, 2, 20.5m);
            ds.Tables.Add(detail);

            ds.Relations.Add(new DataRelation(
                "FK_Customer_Order",
                master.Columns["Id"]!,
                detail.Columns["CustomerId"]!));

            return ds;
        }

        [Fact]
        [DisplayName("FromDataSet 應保留 DataSetName、Tables 與 Relations")]
        public void FromDataSet_PreservesNameTablesRelations()
        {
            var ds = BuildMasterDetailDataSet();

            var sds = SerializableDataSet.FromDataSet(ds);

            Assert.Equal("Orders", sds.DataSetName);
            Assert.Equal(2, sds.Tables.Count);
            Assert.Single(sds.Relations);

            var rel = sds.Relations[0];
            Assert.Equal("FK_Customer_Order", rel.RelationName);
            Assert.Equal("Customer", rel.ParentTable);
            Assert.Equal("Order", rel.ChildTable);
            Assert.Equal(ExpectedParentColumns, rel.ParentColumns);
            Assert.Equal(ExpectedChildColumns, rel.ChildColumns);
        }

        [Fact]
        [DisplayName("ToDataSet 應還原 DataSet 並重建 Relations")]
        public void ToDataSet_RestoresDataSetAndRelations()
        {
            var ds = BuildMasterDetailDataSet();
            var sds = SerializableDataSet.FromDataSet(ds);

            var restored = SerializableDataSet.ToDataSet(sds);

            Assert.Equal("Orders", restored.DataSetName);
            Assert.Equal(2, restored.Tables.Count);
            Assert.Single(restored.Relations.Cast<DataRelation>());

            var rel = restored.Relations[0];
            Assert.Equal("FK_Customer_Order", rel.RelationName);
            Assert.Equal("Customer", rel.ParentTable.TableName);
            Assert.Equal("Order", rel.ChildTable.TableName);
            Assert.Equal("Id", rel.ParentColumns[0].ColumnName);
            Assert.Equal("CustomerId", rel.ChildColumns[0].ColumnName);
        }

        [Fact]
        [DisplayName("含 Relation 的 DataSet 經 MessagePack round-trip 應保持結構")]
        public void DataSet_WithRelation_MessagePackRoundTrip_PreservesStructure()
        {
            var ds = BuildMasterDetailDataSet();

            var bytes = MessagePackHelper.Serialize(ds);
            var restored = MessagePackHelper.Deserialize<DataSet>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(2, restored.Tables.Count);
            Assert.Single(restored.Relations.Cast<DataRelation>());
            Assert.Equal("FK_Customer_Order", restored.Relations[0].RelationName);
            Assert.Equal(2, restored.Tables["Customer"]!.Rows.Count);
            Assert.Equal(2, restored.Tables["Order"]!.Rows.Count);
        }

        [Fact]
        [DisplayName("無 Relation 的 DataSet 轉換 Relations 應為空集合")]
        public void FromDataSet_NoRelations_ReturnsEmptyRelations()
        {
            var ds = new DataSet("Simple");
            var t = new DataTable("T");
            t.Columns.Add("X", typeof(int));
            ds.Tables.Add(t);

            var sds = SerializableDataSet.FromDataSet(ds);

            Assert.Empty(sds.Relations);
            Assert.Single(sds.Tables);
        }
    }
}
