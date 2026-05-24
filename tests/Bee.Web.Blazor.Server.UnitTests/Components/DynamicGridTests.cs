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
    /// 結構與純邏輯測試，涵蓋 <see cref="DynamicGrid"/> 的靜態輔助方法
    /// (<c>TryGetRowId</c>、<c>FormatCell</c>、<c>BuildColumnStyle</c>) 以及
    /// 私有計算屬性 <c>VisibleColumns</c>。
    /// 需要 Blazor 渲染器的 render cycle 測試留待 bUnit 整合測試覆蓋。
    /// </summary>
    public class DynamicGridTests
    {
        private static (bool Success, Guid RowId) InvokeTryGetRowId(DataRow row)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "TryGetRowId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var args = new object?[] { row, Guid.Empty };
            var success = (bool)method!.Invoke(null, args)!;
            return (success, (Guid)args[1]!);
        }

        private static string InvokeFormatCell(DataRow row, LayoutColumn column)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "FormatCell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object[] { row, column })!;
        }

        private static string InvokeBuildColumnStyle(LayoutColumn column)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "BuildColumnStyle", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object[] { column })!;
        }

        // ──────────────────────────────────────────────────────────────
        // TryGetRowId
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("TryGetRowId 資料表無 sys_rowid 欄位時應回傳 false")]
        public void TryGetRowId_NoRowIdColumn_ReturnsFalse()
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
        [DisplayName("TryGetRowId sys_rowid 欄位值為 DBNull 時應回傳 false")]
        public void TryGetRowId_DbNullValue_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(object));
            var row = table.NewRow();
            row[SysFields.RowId] = DBNull.Value;
            table.Rows.Add(row);
            var (success, _) = InvokeTryGetRowId(row);
            Assert.False(success);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 欄位為 Guid 型別時應回傳 true 並輸出對應值")]
        public void TryGetRowId_GuidValue_ReturnsTrueAndOutputsGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);
            var (success, actual) = InvokeTryGetRowId(row);
            Assert.True(success);
            Assert.Equal(expected, actual);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為有效 Guid 字串時應回傳 true 並輸出對應值")]
        public void TryGetRowId_ValidGuidString_ReturnsTrueAndOutputsGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = expected.ToString();
            table.Rows.Add(row);
            var (success, actual) = InvokeTryGetRowId(row);
            Assert.True(success);
            Assert.Equal(expected, actual);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為無效 Guid 字串時應回傳 false")]
        public void TryGetRowId_InvalidGuidString_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = "not-a-guid";
            table.Rows.Add(row);
            var (success, rowId) = InvokeTryGetRowId(row);
            Assert.False(success);
            Assert.Equal(Guid.Empty, rowId);
        }

        // ──────────────────────────────────────────────────────────────
        // FormatCell
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("FormatCell 資料表無對應欄位時應回傳空字串")]
        public void FormatCell_MissingColumn_ReturnsEmpty()
        {
            var table = new DataTable();
            var row = table.NewRow();
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "missing_col" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 欄位值為 DBNull 時應回傳空字串")]
        public void FormatCell_DbNullValue_ReturnsEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = DBNull.Value;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "name" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 無時間部分時應格式化為 yyyy-MM-dd")]
        public void FormatCell_DateTimeWithNoTime_ReturnsDateOnly()
        {
            var table = new DataTable();
            table.Columns.Add("hire_date", typeof(DateTime));
            var row = table.NewRow();
            row["hire_date"] = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "hire_date" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal("2024-03-15", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 含時間部分時應格式化為 yyyy-MM-dd HH:mm:ss")]
        public void FormatCell_DateTimeWithTime_ReturnsDateTimeFormat()
        {
            var table = new DataTable();
            table.Columns.Add("created_at", typeof(DateTime));
            var row = table.NewRow();
            row["created_at"] = new DateTime(2024, 3, 15, 14, 30, 45, DateTimeKind.Utc);
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "created_at" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal("2024-03-15 14:30:45", result);
        }

        [Fact]
        [DisplayName("FormatCell 欄位為字串型別時應回傳原始字串值")]
        public void FormatCell_StringValue_ReturnsRawString()
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "Alice";
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "name" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal("Alice", result);
        }

        [Fact]
        [DisplayName("FormatCell 設定 DisplayFormat 時應優先使用 DisplayFormat 格式化值")]
        public void FormatCell_WithDisplayFormat_UsesDisplayFormat()
        {
            var table = new DataTable();
            table.Columns.Add("amount", typeof(double));
            var row = table.NewRow();
            row["amount"] = 3.14;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "amount", DisplayFormat = "F2" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal("3.14", result);
        }

        [Fact]
        [DisplayName("FormatCell 設定 NumberFormat 時應使用 NumberFormat 格式化數值")]
        public void FormatCell_WithNumberFormat_UsesNumberFormat()
        {
            var table = new DataTable();
            table.Columns.Add("price", typeof(double));
            var row = table.NewRow();
            row["price"] = 0.5;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "price", NumberFormat = "F1" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal("0.5", result);
        }

        // ──────────────────────────────────────────────────────────────
        // BuildColumnStyle
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("BuildColumnStyle Width 為 0 時應回傳空字串")]
        public void BuildColumnStyle_ZeroWidth_ReturnsEmpty()
        {
            var column = new LayoutColumn { Width = 0 };
            var result = InvokeBuildColumnStyle(column);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("BuildColumnStyle Width 為正值時應回傳含寬度的 CSS 樣式字串")]
        public void BuildColumnStyle_PositiveWidth_ReturnsWidthStyle()
        {
            var column = new LayoutColumn { Width = 120 };
            var result = InvokeBuildColumnStyle(column);
            Assert.Equal("width:120px", result);
        }

        // ──────────────────────────────────────────────────────────────
        // VisibleColumns (private computed property)
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("VisibleColumns Layout 為 null 時應回傳空序列")]
        public void VisibleColumns_NullLayout_ReturnsEmpty()
        {
            var component = new DynamicGrid();
            var prop = typeof(DynamicGrid).GetProperty(
                "VisibleColumns", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(prop);
            var result = prop!.GetValue(component) as IEnumerable<LayoutColumn>;
            Assert.NotNull(result);
            Assert.Empty(result!);
        }

        [Fact]
        [DisplayName("VisibleColumns 應只回傳 Visible 為 true 的欄位")]
        public void VisibleColumns_MixedVisibility_ReturnsOnlyVisible()
        {
            var component = new DynamicGrid();
            var layout = new LayoutGrid();
            layout.Columns!.Add(new LayoutColumn { FieldName = "col_visible", Visible = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "col_hidden", Visible = false });
            typeof(DynamicGrid)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, layout);
            var prop = typeof(DynamicGrid).GetProperty(
                "VisibleColumns", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(prop);
            var result = prop!.GetValue(component) as IEnumerable<LayoutColumn>;
            Assert.NotNull(result);
            var list = result!.ToList();
            Assert.Single(list);
            Assert.Equal("col_visible", list[0].FieldName);
        }

        // ──────────────────────────────────────────────────────────────
        // Component surface
        // ──────────────────────────────────────────────────────────────

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
            var component = new DynamicGrid();
            Assert.Equal("No data.", component.EmptyText);
        }

        [Theory]
        [InlineData(nameof(DynamicGrid.Layout))]
        [InlineData(nameof(DynamicGrid.Rows))]
        [InlineData(nameof(DynamicGrid.OnRowSelected))]
        [InlineData(nameof(DynamicGrid.EmptyText))]
        [DisplayName("公開屬性皆標有 [Parameter]")]
        public void PublicProperties_AreMarkedAsParameters(string name)
        {
            var property = typeof(DynamicGrid).GetProperty(
                name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.NotNull(property!.GetCustomAttribute<ParameterAttribute>());
        }
    }
}
