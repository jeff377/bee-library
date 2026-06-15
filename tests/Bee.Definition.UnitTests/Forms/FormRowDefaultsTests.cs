using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// <see cref="FormRowDefaults"/> seeding: identity / link columns and type defaults, and in
    /// particular that a detail row's <c>sys_master_rowid</c> preserves the master row id's exact
    /// representation. A re-parsed Guid would lowercase a string key and orphan the detail under a
    /// case-sensitive comparison (SQLite stores GUIDs as case-sensitive TEXT).
    /// </summary>
    public class FormRowDefaultsTests
    {
        private static FormTable BuildDetailTable()
        {
            var schema = new FormSchema("Order", "Order");
            var detail = schema.Tables!.Add("OrderLine", "Lines");
            detail.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            detail.Fields.Add(SysFields.MasterRowId, "Master", FieldDbType.Guid);
            detail.Fields.Add("qty", "Qty", FieldDbType.Integer);
            return detail;
        }

        [Fact]
        [DisplayName("Apply 應原樣保留 master sys_rowid 的字串大小寫於 sys_master_rowid")]
        public void Apply_PreservesMasterRowIdStringCasing()
        {
            var detail = BuildDetailTable();
            // Mimic a provider that surfaces GUIDs as case-sensitive TEXT (e.g. SQLite): string columns.
            var table = new DataTable("OrderLine");
            table.Columns.Add(SysFields.RowId, typeof(string));
            table.Columns.Add(SysFields.MasterRowId, typeof(string));
            table.Columns.Add("qty", typeof(int));
            var row = table.NewRow();

            const string masterRowId = "6689B38C-39D5-43B0-9682-27F9ADEEEDC5";   // UPPERCASE
            FormRowDefaults.Apply(detail, row, masterRowId);

            Assert.Equal(masterRowId, row[SysFields.MasterRowId]);   // exact, casing preserved
            Assert.Equal(0, row["qty"]);                            // type default still applied
        }

        [Fact]
        [DisplayName("Apply 以 Guid master row id 應原樣寫入 sys_master_rowid")]
        public void Apply_GuidMasterRowId_WritesVerbatim()
        {
            var detail = BuildDetailTable();
            var table = new DataTable("OrderLine");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add(SysFields.MasterRowId, typeof(Guid));
            table.Columns.Add("qty", typeof(int));
            var row = table.NewRow();

            var masterRowId = Guid.NewGuid();
            FormRowDefaults.Apply(detail, row, masterRowId);

            Assert.Equal(masterRowId, (Guid)row[SysFields.MasterRowId]);
            Assert.NotEqual(Guid.Empty, (Guid)row[SysFields.RowId]);
        }
    }
}
