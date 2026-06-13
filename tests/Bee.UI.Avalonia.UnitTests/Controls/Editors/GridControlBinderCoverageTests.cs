using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="GridControlBinder"/> 的 ambient 綁定路徑覆蓋率：透過反射對
    /// <see cref="GridControl"/> 的私有 _binder 呼叫 NotifyAttached / NotifyDetached，
    /// 模擬 OnAttachedToLogicalTree / OnDetachedFromLogicalTree 的觸發路徑。
    /// 同時驗證 OnBindingContextChanged 在已附加後切換 DataObject 的重新綁定行為。
    /// </summary>
    public class GridControlBinderCoverageTests
    {
        private static FormDataObject BuildDataObjectWithDetail(string phone = "02-1234")
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            dataObject.DataSet.Tables["EmployeePhone"]!.Rows.Add(phone);
            return dataObject;
        }

        private static object GetBinder(GridControl grid)
        {
            var field = typeof(GridControl).GetField("_binder", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field!.GetValue(grid)!;
        }

        private static void InvokeNotifyAttached(object binder)
        {
            var method = binder.GetType().GetMethod("NotifyAttached");
            Assert.NotNull(method);
            method!.Invoke(binder, null);
        }

        private static void InvokeNotifyDetached(object binder)
        {
            var method = binder.GetType().GetMethod("NotifyDetached");
            Assert.NotNull(method);
            method!.Invoke(binder, null);
        }

        private static object? GetBinderDataObject(object binder)
        {
            var prop = binder.GetType().GetProperty("DataObject");
            Assert.NotNull(prop);
            return prop!.GetValue(binder);
        }

        [Fact]
        [DisplayName("NotifyAttached：ambient DataObject + TableName 已設定時自動綁定網格並載入明細表")]
        public void NotifyAttached_WithAmbientDataObjectAndTableName_BindsGridToDetailTable()
        {
            var dataObject = BuildDataObjectWithDetail("02-1234");
            var grid = new GridControl();
            grid.TableName = "EmployeePhone";
            FormScope.SetDataObject(grid, dataObject);

            InvokeNotifyAttached(GetBinder(grid));

            Assert.NotNull(grid.DataTable);
            Assert.Equal("EmployeePhone", grid.DataTable!.TableName);
        }

        [Fact]
        [DisplayName("NotifyAttached：ambient DataObject 未設定時不綁定，DataTable 維持 null")]
        public void NotifyAttached_NoAmbientDataObject_DoesNotBind()
        {
            var grid = new GridControl();
            grid.TableName = "EmployeePhone";

            InvokeNotifyAttached(GetBinder(grid));

            Assert.Null(grid.DataTable);
        }

        [Fact]
        [DisplayName("NotifyDetached：ambient 綁定後呼叫 NotifyDetached，Binder 釋放 DataObject")]
        public void NotifyDetached_AfterAmbientBind_ClearsBinderDataObject()
        {
            var dataObject = BuildDataObjectWithDetail("02-1234");
            var grid = new GridControl();
            grid.TableName = "EmployeePhone";
            FormScope.SetDataObject(grid, dataObject);
            var binder = GetBinder(grid);
            InvokeNotifyAttached(binder);
            Assert.NotNull(grid.DataTable);

            InvokeNotifyDetached(binder);

            Assert.Null(GetBinderDataObject(binder));
        }

        [Fact]
        [DisplayName("OnBindingContextChanged：附加後切換 DataObject，網格重新綁定到新 DataObject 的明細表")]
        public void OnBindingContextChanged_AfterAttach_NewDataObject_Rebinds()
        {
            var dataObject1 = BuildDataObjectWithDetail("phone-1");
            var dataObject2 = BuildDataObjectWithDetail("phone-2");

            var grid = new GridControl();
            grid.TableName = "EmployeePhone";
            FormScope.SetDataObject(grid, dataObject1);
            InvokeNotifyAttached(GetBinder(grid));
            Assert.Same(dataObject1.DataSet.Tables["EmployeePhone"], grid.DataTable);

            // 切換 DataObject → DataObjectProperty.Changed class handler → OnBindingContextChanged → TryAmbientBind → 重新綁定
            FormScope.SetDataObject(grid, dataObject2);

            Assert.Same(dataObject2.DataSet.Tables["EmployeePhone"], grid.DataTable);
        }

        [Fact]
        [DisplayName("OnBindingContextChanged：未附加時（_attached=false）不觸發綁定，DataTable 維持 null")]
        public void OnBindingContextChanged_NotAttached_IsNoOp()
        {
            var dataObject = BuildDataObjectWithDetail("02-1234");
            var grid = new GridControl();
            grid.TableName = "EmployeePhone";

            // 未呼叫 NotifyAttached → _attached = false → class handler 觸發但 OnBindingContextChanged 早期回傳
            FormScope.SetDataObject(grid, dataObject);

            Assert.Null(grid.DataTable);
        }
    }
}
