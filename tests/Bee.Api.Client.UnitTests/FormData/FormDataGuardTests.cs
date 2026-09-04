using System.ComponentModel;
using System.Data;
using Bee.Definition;

namespace Bee.Api.Client.UnitTests.FormData
{
    /// <summary>
    /// <see cref="FormDataGuard"/> 的前置條件與訊息。
    /// </summary>
    /// <remarks>
    /// 訊息文字本身就是重點：它是開發者把表單接錯時唯一讀得到的東西，
    /// 兩份副本會漂成對同一個錯誤的兩種說法。
    /// </remarks>
    public class FormDataGuardTests
    {
        [Fact]
        [DisplayName("沒有 connector 時應說明是哪個操作、以及該從哪裡補")]
        public void RequireConnector_Null_ThrowsNamingOperationAndRemedy()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => FormDataGuard.RequireConnector(null, "LoadAsync"));

            Assert.Contains("LoadAsync", ex.Message, StringComparison.Ordinal);
            Assert.Contains("FormDataObject constructor", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("沒有載入主檔列時應明講「沒有主檔列」")]
        public void RequireMasterRowId_NoRow_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => FormDataGuard.RequireMasterRowId(null));

            Assert.Contains("No master row is loaded", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("主檔表缺 rowid 欄位時，訊息要與「沒有主檔列」區分開")]
        public void RequireMasterRowId_MissingColumn_ThrowsDistinctMessage()
        {
            var table = new DataTable("master");
            table.Columns.Add("other", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);

            var ex = Assert.Throws<InvalidOperationException>(() => FormDataGuard.RequireMasterRowId(row));

            Assert.Contains("missing", ex.Message, StringComparison.Ordinal);
            Assert.Contains(SysFields.RowId, ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("No master row is loaded", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("rowid 為 null 時，訊息要與「缺欄位」區分開")]
        public void RequireMasterRowId_NullValue_ThrowsDistinctMessage()
        {
            var row = NewMasterRow(DBNull.Value);

            var ex = Assert.Throws<InvalidOperationException>(() => FormDataGuard.RequireMasterRowId(row));

            Assert.Contains("null", ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("missing", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("rowid 為 Guid 或字串都應取得同一個值")]
        public void RequireMasterRowId_GuidOrString_BothParse()
        {
            var id = Guid.NewGuid();

            Assert.Equal(id, FormDataGuard.RequireMasterRowId(NewMasterRow(id, typeof(Guid))));
            Assert.Equal(id, FormDataGuard.RequireMasterRowId(NewMasterRow(id.ToString(), typeof(string))));
        }

        private static DataRow NewMasterRow(object value, Type columnType = null!)
        {
            var table = new DataTable("master");
            table.Columns.Add(SysFields.RowId, columnType ?? typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = value;
            table.Rows.Add(row);
            return row;
        }
    }
}
