using System.ComponentModel;
using System.Data;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Scenarios;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="SaveScenario"/>.
    /// </summary>
    public class SaveScenarioTests
    {
        private static VirtualUserPool Pool() => new(new AuthOptions());

        private static DataSet CreateDataSet(bool withNameColumn = true, bool withRow = true)
        {
            var dataSet = new DataSet { Locale = System.Globalization.CultureInfo.InvariantCulture };
            var table = new DataTable("ft_customer")
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            table.Columns.Add("sys_id", typeof(string));
            if (withNameColumn) { table.Columns.Add("sys_name", typeof(string)); }
            dataSet.Tables.Add(table);

            if (withRow)
            {
                var row = table.NewRow();
                row["sys_id"] = "a";
                if (withNameColumn) { row["sys_name"] = "original"; }
                table.Rows.Add(row);
                // A row read back from the server arrives Unchanged; reproduce that here so the
                // transition to Modified is what the test actually observes.
                table.AcceptChanges();
            }
            return dataSet;
        }

        [Fact]
        [DisplayName("改值後列狀態轉為 Modified，存檔才會真的寫入")]
        public void Touch_MarksRowModified()
        {
            var dataSet = CreateDataSet();
            Assert.Equal(DataRowState.Unchanged, dataSet.Tables[0].Rows[0].RowState);

            SaveScenario.Touch(dataSet, 7);

            Assert.Equal(DataRowState.Modified, dataSet.Tables[0].Rows[0].RowState);
            Assert.Equal("updated-7", dataSet.Tables[0].Rows[0]["sys_name"]);
        }

        [Fact]
        [DisplayName("沒有 sys_name 欄位時不擲例外")]
        public void Touch_WithoutNameColumn_DoesNotThrow()
        {
            var dataSet = CreateDataSet(withNameColumn: false);

            SaveScenario.Touch(dataSet, 1);

            Assert.Equal(DataRowState.Unchanged, dataSet.Tables[0].Rows[0].RowState);
        }

        [Fact]
        [DisplayName("資料表沒有列時不擲例外")]
        public void Touch_WithoutRows_DoesNotThrow()
        {
            var exception = Record.Exception(() => SaveScenario.Touch(CreateDataSet(withRow: false), 1));

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("空 DataSet 不擲例外")]
        public void Touch_EmptyDataSet_DoesNotThrow()
        {
            var exception = Record.Exception(() => SaveScenario.Touch(new DataSet(), 1));

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("沒有可用的鍵時拒絕建構，而不是跑出空結果")]
        public void Constructor_NoRowIds_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() => new SaveScenario(Pool(), "Customer", []));

            Assert.Contains("prepare", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("場景名稱與設定檔中的名稱一致")]
        public void Name_MatchesConfigurationKey()
        {
            Assert.Equal("Save", new SaveScenario(Pool(), "Customer", [Guid.NewGuid()]).Name);
        }
    }
}
