using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 針對 <see cref="DynamicGrid"/> 的結構性 smoke 測試與私有靜態輔助方法
    /// （<c>TryGetRowId</c>、<c>FormatCell</c>、<c>BuildColumnStyle</c>）的單元測試。
    /// OnRowClickAsync 需要 EventCallback delegate，留待 bUnit 整合測試覆蓋。
    /// </summary>
    public class DynamicGridTests
    {
        #region Reflection helpers

        // CA1861: Type[] 陣列抽成 static readonly，避免每次呼叫配置新陣列。
        private static readonly Type[] s_tryGetRowIdParams = [typeof(DataRow), typeof(Guid).MakeByRefType()];
        private static readonly Type[] s_formatCellParams = [typeof(DataRow), typeof(LayoutColumn)];
        private static readonly Type[] s_buildColumnStyleParams = [typeof(LayoutColumn)];

        private static (bool success, Guid rowId) InvokeTryGetRowId(DataRow row)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "TryGetRowId", BindingFlags.NonPublic | BindingFlags.Static, null, s_tryGetRowIdParams, null);
            Assert.NotNull(method);
            var args = new object?[] { row, Guid.Empty };
            var result = (bool)method!.Invoke(null, args)!;
            return (result, (Guid)args[1]!);
        }

        private static string InvokeFormatCell(DataRow row, LayoutColumn column)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "FormatCell", BindingFlags.NonPublic | BindingFlags.Static, null, s_formatCellParams, null);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object?[] { row, column })!;
        }

        private static string InvokeBuildColumnStyle(LayoutColumn column)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "BuildColumnStyle", BindingFlags.NonPublic | BindingFlags.Static, null, s_buildColumnStyleParams, null);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object?[] { column })!;
        }

        #endregion

        #region 結構性測試

        [Fact]
        [DisplayName("DynamicGrid 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(DynamicGrid)));
        }

        [Fact]
        [DisplayName("EmptyText 預設值為 'No data.'")]
        public void EmptyText_Default_IsNoData()
        {
            var grid = new DynamicGrid();
            Assert.Equal("No data.", grid.EmptyText);
        }

        [Theory]
        [InlineData(nameof(DynamicGrid.Layout))]
        [InlineData(nameof(DynamicGrid.Rows))]
        [InlineData(nameof(DynamicGrid.OnRowSelected))]
        [InlineData(nameof(DynamicGrid.EmptyText))]
        [DisplayName("公開屬性皆標註 [Parameter]")]
        public void PublicProperties_AreMarkedAsParameters(string name)
        {
            var property = typeof(DynamicGrid).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.NotNull(property!.GetCustomAttribute<ParameterAttribute>());
        }

        #endregion

        #region TryGetRowId

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 欄位不存在時應回傳 false")]
        public void TryGetRowId_ColumnNotPresent_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add("other_col", typeof(string));
            var row = table.NewRow();
            row["other_col"] = "value";
            table.Rows.Add(row);

            var (success, _) = InvokeTryGetRowId(row);

            Assert.False(success);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 值為 DBNull 時應回傳 false")]
        public void TryGetRowId_DBNullValue_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = DBNull.Value;
            table.Rows.Add(row);

            var (success, _) = InvokeTryGetRowId(row);

            Assert.False(success);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為 Guid 值時應回傳 true 並輸出正確 Guid")]
        public void TryGetRowId_GuidValue_ReturnsTrueWithGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);

            var (success, rowId) = InvokeTryGetRowId(row);

            Assert.True(success);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為有效 Guid 字串時應回傳 true 並解析出 Guid")]
        public void TryGetRowId_ValidGuidString_ReturnsTrueWithParsedGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = expected.ToString();
            table.Rows.Add(row);

            var (success, rowId) = InvokeTryGetRowId(row);

            Assert.True(success);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為無效字串時應回傳 false")]
        public void TryGetRowId_InvalidGuidString_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = "not-a-guid";
            table.Rows.Add(row);

            var (success, _) = InvokeTryGetRowId(row);

            Assert.False(success);
        }

        #endregion

        #region FormatCell

        [Fact]
        [DisplayName("FormatCell 欄位不在 DataTable 中時應回傳空字串")]
        public void FormatCell_ColumnNotInTable_ReturnsEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("other_col", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "missing" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 欄位值為 DBNull 時應回傳空字串")]
        public void FormatCell_DBNullValue_ReturnsEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("emp_name", typeof(string));
            var row = table.NewRow();
            row["emp_name"] = DBNull.Value;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "emp_name" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 字串欄位值應直接回傳字串內容")]
        public void FormatCell_StringValue_ReturnsString()
        {
            var table = new DataTable();
            table.Columns.Add("emp_name", typeof(string));
            var row = table.NewRow();
            row["emp_name"] = "Alice";
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "emp_name" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("Alice", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 無時間部分時應回傳 yyyy-MM-dd 格式")]
        public void FormatCell_DateOnlyDateTime_ReturnsDateOnlyFormat()
        {
            var dt = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Unspecified);
            var table = new DataTable();
            table.Columns.Add("hire_date", typeof(DateTime));
            var row = table.NewRow();
            row["hire_date"] = dt;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "hire_date" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("2026-05-24", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 含非零時間時應回傳 yyyy-MM-dd HH:mm:ss 格式")]
        public void FormatCell_DateTimeWithTime_ReturnsFullDateTimeFormat()
        {
            var dt = new DateTime(2026, 5, 24, 15, 30, 0, DateTimeKind.Unspecified);
            var table = new DataTable();
            table.Columns.Add("created_at", typeof(DateTime));
            var row = table.NewRow();
            row["created_at"] = dt;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "created_at" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("2026-05-24 15:30:00", result);
        }

        [Fact]
        [DisplayName("FormatCell 設有 DisplayFormat 時應套用數字格式化")]
        public void FormatCell_WithDisplayFormat_FormatsDecimal()
        {
            var table = new DataTable();
            table.Columns.Add("amount", typeof(decimal));
            var row = table.NewRow();
            row["amount"] = 1234.5m;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "amount", DisplayFormat = "F2" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("1234.50", result);
        }

        #endregion

        #region BuildColumnStyle

        [Fact]
        [DisplayName("BuildColumnStyle 寬度為 0 時應回傳空字串")]
        public void BuildColumnStyle_ZeroWidth_ReturnsEmpty()
        {
            var column = new LayoutColumn { Width = 0 };

            var result = InvokeBuildColumnStyle(column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("BuildColumnStyle 設定正整數寬度時應回傳 width:{n}px 樣式字串")]
        public void BuildColumnStyle_PositiveWidth_ReturnsStyleString()
        {
            var column = new LayoutColumn { Width = 120 };

            var result = InvokeBuildColumnStyle(column);

            Assert.Equal("width:120px", result);
        }

        #endregion
    }
}
