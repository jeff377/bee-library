using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 Unchanged 列只攜帶 Current 一份值。
    /// </summary>
    /// <remarks>
    /// 這不是罕見情形：<c>DataFormRepository.GetData</c> 回傳前呼叫 <c>AcceptChanges()</c>
    /// （回應契約明載），因此**每一筆從資料庫讀出來的列**都是 Unchanged。先前 Unchanged 與
    /// Modified 共用同一個 case、送出兩份完全相同的值，而還原端只讀 Current —— payload 與
    /// 序列化 CPU 都是兩倍，且沒有任何讀取端用得到那一份。
    /// </remarks>
    public class UnchangedRowPayloadTests
    {
        private static DataTable BuildTable()
        {
            var table = new DataTable("Order");
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Rows.Add(1, "alpha");
            table.Rows.Add(2, "beta");
            table.AcceptChanges();   // 讀取路徑的實際狀態
            return table;
        }

        [Fact]
        [DisplayName("Unchanged 列不應攜帶 OriginalValues")]
        public void Unchanged_CarriesCurrentOnly()
        {
            var serializable = SerializableDataTable.FromDataTable(BuildTable());

            Assert.All(serializable.Rows!, row =>
            {
                Assert.Equal(DataRowState.Unchanged, row.RowState);
                Assert.NotNull(row.CurrentValues);
                Assert.Null(row.OriginalValues);
            });
        }

        [Fact]
        [DisplayName("Modified 列仍應同時攜帶 Current 與 Original")]
        public void Modified_StillCarriesBoth()
        {
            var table = BuildTable();
            table.Rows[0]["name"] = "changed";

            var serializable = SerializableDataTable.FromDataTable(table);
            var modified = serializable.Rows!.Single(r => r.RowState == DataRowState.Modified);

            Assert.Equal("changed", modified.CurrentValues!["name"]);
            Assert.Equal("alpha", modified.OriginalValues!["name"]);
        }

        [Fact]
        [DisplayName("Unchanged 列去掉 Original 後 round-trip 仍應完全還原")]
        public void Unchanged_RoundTripsUnaffected()
        {
            var original = BuildTable();

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<DataTable>(bytes)!;

            Assert.Equal(2, restored.Rows.Count);
            Assert.All(restored.Rows.Cast<DataRow>(), r => Assert.Equal(DataRowState.Unchanged, r.RowState));
            Assert.Equal(1, restored.Rows[0]["id"]);
            Assert.Equal("alpha", restored.Rows[0]["name"]);
            Assert.Equal("beta", restored.Rows[1]["name"]);

            // Original 版本仍取得到 —— AcceptChanges 讓兩個版本相等，這正是不必送第二份的理由。
            Assert.Equal("alpha", restored.Rows[0]["name", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("Unchanged 列的 payload 應明顯小於送兩份時")]
        public void Unchanged_PayloadIsSmaller()
        {
            var unchanged = BuildTable();

            var modified = BuildTable();
            modified.Rows[0]["name"] = "x";
            modified.Rows[1]["name"] = "y";

            var unchangedSize = MessagePackCodec.Serialize(unchanged).Length;
            var modifiedSize = MessagePackCodec.Serialize(modified).Length;

            // 同樣兩列同樣欄位，差別只在 Modified 多送一份 Original。
            Assert.True(unchangedSize < modifiedSize,
                $"Unchanged payload ({unchangedSize} B) 應小於 Modified ({modifiedSize} B)。");
        }
    }
}
