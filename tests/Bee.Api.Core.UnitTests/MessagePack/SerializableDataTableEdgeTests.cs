using System;
using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// SerializableDataTable 邊界與錯誤路徑測試：
    /// 涵蓋 Detached row 略過、DBNull default 轉 null、主鍵為空或含不存在欄位、
    /// 以及 Modified/Deleted row 在 round-trip 後保留 original / current 值。
    /// </summary>
    public class SerializableDataTableEdgeTests
    {
        [Fact]
        [DisplayName("FromDataTable 欄位 DBNull default 應轉為 null")]
        public void FromDataTable_ColumnDBNullDefault_BecomesNull()
        {
            var dt = new DataTable("T");
            var col = new DataColumn("Name", typeof(string));
            // DataColumn 的 DefaultValue 預設即為 DBNull.Value
            Assert.Equal(DBNull.Value, col.DefaultValue);
            dt.Columns.Add(col);

            var sdt = SerializableDataTable.FromDataTable(dt);
            Assert.Single(sdt.Columns);
            Assert.Null(sdt.Columns[0].DefaultValue);
        }

        [Fact]
        [DisplayName("FromDataTable 欄位有具體 default 應保留")]
        public void FromDataTable_ColumnConcreteDefault_IsPreserved()
        {
            var dt = new DataTable("T");
            var col = new DataColumn("Age", typeof(int)) { DefaultValue = 42 };
            dt.Columns.Add(col);

            var sdt = SerializableDataTable.FromDataTable(dt);
            Assert.Equal(42, sdt.Columns[0].DefaultValue);

            // ToDataTable 還原後預設值也應一致
            var restored = SerializableDataTable.ToDataTable(sdt);
            Assert.Equal(42, restored.Columns["Age"]!.DefaultValue);
        }

        [Fact]
        [DisplayName("ToDataTable 主鍵為空清單時應不設定 PrimaryKey")]
        public void ToDataTable_EmptyPrimaryKeys_NoPrimaryKeyApplied()
        {
            var sdt = new SerializableDataTable { TableName = "T" };
            sdt.Columns.Add(new SerializableDataColumn
            {
                ColumnName = "Id",
                DataType = Bee.Base.Data.FieldDbType.Integer,
                AllowDBNull = true,
                ReadOnly = false,
                MaxLength = -1
            });

            var dt = SerializableDataTable.ToDataTable(sdt);
            Assert.Empty(dt.PrimaryKey);
        }

        [Fact]
        [DisplayName("ToDataTable 主鍵參照不存在欄位時應過濾後為空")]
        public void ToDataTable_PrimaryKeyWithMissingColumn_IsFilteredOut()
        {
            var sdt = new SerializableDataTable { TableName = "T" };
            sdt.Columns.Add(new SerializableDataColumn
            {
                ColumnName = "Id",
                DataType = Bee.Base.Data.FieldDbType.Integer,
                AllowDBNull = true,
                ReadOnly = false,
                MaxLength = -1
            });
            sdt.PrimaryKeys.Add("Ghost");

            var dt = SerializableDataTable.ToDataTable(sdt);
            Assert.Empty(dt.PrimaryKey);
        }

        [Fact]
        [DisplayName("ToDataTable 主鍵混合存在與不存在欄位時應只套用存在欄位")]
        public void ToDataTable_PrimaryKeyMixedValidAndMissing_AppliesOnlyValid()
        {
            var sdt = new SerializableDataTable { TableName = "T" };
            sdt.Columns.Add(new SerializableDataColumn
            {
                ColumnName = "Id",
                DataType = Bee.Base.Data.FieldDbType.Integer,
                AllowDBNull = true,
                ReadOnly = false,
                MaxLength = -1
            });
            sdt.PrimaryKeys.Add("Id");
            sdt.PrimaryKeys.Add("Ghost");

            var dt = SerializableDataTable.ToDataTable(sdt);
            Assert.Single(dt.PrimaryKey);
            Assert.Equal("Id", dt.PrimaryKey[0].ColumnName);
        }

        [Fact]
        [DisplayName("FromDataTable→ToDataTable 對 Modified row 應保留 Original 與 Current 值")]
        public void RoundTrip_ModifiedRow_PreservesOriginalAndCurrent()
        {
            var dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "原始");
            dt.AcceptChanges();
            dt.Rows[0]["Name"] = "修改後";

            var restored = SerializableDataTable.ToDataTable(SerializableDataTable.FromDataTable(dt));
            var row = restored.Rows[0];
            Assert.Equal(DataRowState.Modified, row.RowState);
            Assert.Equal("原始", row["Name", DataRowVersion.Original]);
            Assert.Equal("修改後", row["Name", DataRowVersion.Current]);
        }

        [Fact]
        [DisplayName("FromDataTable→ToDataTable 對 Deleted row 應還原為 Deleted 且保留 Original")]
        public void RoundTrip_DeletedRow_PreservesOriginal()
        {
            var dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "待刪除");
            dt.AcceptChanges();
            dt.Rows[0].Delete();

            var restored = SerializableDataTable.ToDataTable(SerializableDataTable.FromDataTable(dt));
            var row = restored.Rows[0];
            Assert.Equal(DataRowState.Deleted, row.RowState);
            Assert.Equal(1, row["Id", DataRowVersion.Original]);
            Assert.Equal("待刪除", row["Name", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("FromDataTable→ToDataTable 對 Unchanged row 應保留 Unchanged")]
        public void RoundTrip_UnchangedRow_PreservesState()
        {
            var dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            dt.AcceptChanges();

            var restored = SerializableDataTable.ToDataTable(SerializableDataTable.FromDataTable(dt));
            Assert.Equal(DataRowState.Unchanged, restored.Rows[0].RowState);
        }
    }
}
