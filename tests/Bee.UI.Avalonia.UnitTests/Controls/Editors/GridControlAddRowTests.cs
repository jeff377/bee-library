using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// GridControl.AddRow 對新明細列的初始化：每列指派自己的 sys_rowid（主鍵），並連結
    /// 母表的 sys_master_rowid，避免新增明細存檔時違反 NOT NULL / 唯一鍵或與母表脫鉤。
    /// </summary>
    public class GridControlAddRowTests
    {
        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var master = schema.Tables!.Add("Order", "訂單");
            master.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            var detail = schema.Tables!.Add("OrderLine", "訂單明細");
            detail.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            detail.Fields!.Add(new FormField(SysFields.MasterRowId, "主檔識別", FieldDbType.Guid));
            detail.Fields!.Add(new FormField("qty", "數量", FieldDbType.Integer));
            return schema;
        }

        [Fact]
        [DisplayName("AddRow 應指派新 sys_rowid 並連結母表 sys_master_rowid")]
        public void AddRow_AssignsRowIdAndMasterLink()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var masterRowId = Guid.NewGuid();
            dataObject.MasterRow![SysFields.RowId] = masterRowId;

            var layout = schema.GetFormLayout().Details![0];
            var grid = new GridControl { AllowEdit = true, EditMode = GridEditMode.InCell };
            grid.Bind(dataObject, layout);

            grid.AddRow();

            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var added = lineTable.Rows[^1];
            Assert.NotEqual(Guid.Empty, (Guid)added[SysFields.RowId]);
            Assert.Equal(masterRowId, (Guid)added[SysFields.MasterRowId]);
        }

        [Fact]
        [DisplayName("AddRow 連續新增兩列應得到不同的 sys_rowid")]
        public void AddRow_TwoRows_GetDistinctRowIds()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            dataObject.MasterRow![SysFields.RowId] = Guid.NewGuid();

            var layout = schema.GetFormLayout().Details![0];
            var grid = new GridControl { AllowEdit = true, EditMode = GridEditMode.InCell };
            grid.Bind(dataObject, layout);

            grid.AddRow();
            grid.AddRow();

            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var first = (Guid)lineTable.Rows[0][SysFields.RowId];
            var second = (Guid)lineTable.Rows[1][SysFields.RowId];
            Assert.NotEqual(Guid.Empty, first);
            Assert.NotEqual(Guid.Empty, second);
            Assert.NotEqual(first, second);
        }
    }
}
