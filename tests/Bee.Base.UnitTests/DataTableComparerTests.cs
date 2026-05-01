using System.ComponentModel;
using System.Data;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
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
