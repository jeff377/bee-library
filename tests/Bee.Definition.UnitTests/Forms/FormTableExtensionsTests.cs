using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// 驗證 <see cref="FormTableExtensions.ApplyFieldDbTypes"/>（路徑一：框架依 schema 標記）。
    /// ADO.NET 把 date 欄位一律回報為 System.DateTime，故 SQL 取回的 DataTable 必須把 schema
    /// 的欄位型別重播上去，才會與依 schema 自建的空白 DataTable 同構。
    /// </summary>
    public class FormTableExtensionsTests
    {
        private static FormTable BuildFormTable()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add("order_date", "Order Date", FieldDbType.Date);
            table.Fields.Add("created_at", "Created At", FieldDbType.DateTime);
            table.Fields.Add("amount", "Amount", FieldDbType.Currency);
            return table;
        }

        private static DataTable BuildProviderTable()
        {
            // 模擬 ADO.NET 回報的型別：Date 與 DateTime 都是 DateTime、Currency 是 decimal。
            var table = new DataTable("Order");
            table.Columns.Add("order_date", typeof(DateTime));
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("amount", typeof(decimal));
            return table;
        }

        [Fact]
        [DisplayName("ApplyFieldDbTypes 應把 schema 的欄位型別重播到 SQL 取回的欄位上")]
        public void ApplyFieldDbTypes_MarksColumnsFromSchema()
        {
            var table = BuildProviderTable();

            BuildFormTable().ApplyFieldDbTypes(table);

            Assert.Equal(FieldDbType.Date, table.Columns["order_date"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.DateTime, table.Columns["created_at"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.Currency, table.Columns["amount"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("ApplyFieldDbTypes 不改動欄位的 CLR 型別")]
        public void ApplyFieldDbTypes_LeavesClrTypesUnchanged()
        {
            var table = BuildProviderTable();

            BuildFormTable().ApplyFieldDbTypes(table);

            Assert.Equal(typeof(DateTime), table.Columns["order_date"]!.DataType);
            Assert.Equal(typeof(DateTime), table.Columns["created_at"]!.DataType);
        }

        [Fact]
        [DisplayName("schema 未涵蓋的欄位應保持未標記，不擲例外")]
        public void ApplyFieldDbTypes_ColumnsOutsideSchema_LeftUnmarked()
        {
            // 查詢可能回傳超出宣告欄位的欄（彙總、運算式），這些欄仍走 CLR 型別反推。
            var table = BuildProviderTable();
            table.Columns.Add("row_count", typeof(int));

            BuildFormTable().ApplyFieldDbTypes(table);

            Assert.Null(table.Columns["row_count"]!.GetDeclaredFieldDbType());
            Assert.Equal(FieldDbType.Integer, table.Columns["row_count"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("schema 宣告了但查詢未回傳的欄位不應擲例外")]
        public void ApplyFieldDbTypes_FieldsMissingFromResult_DoNotThrow()
        {
            // 部分欄位查詢（如只 SELECT sys_rowid）是常態，不可因此失敗。
            var table = new DataTable("Order");
            table.Columns.Add("order_date", typeof(DateTime));

            var exception = Record.Exception(() => BuildFormTable().ApplyFieldDbTypes(table));

            Assert.Null(exception);
            Assert.Equal(FieldDbType.Date, table.Columns["order_date"]!.ResolveFieldDbType());
        }
    }
}
