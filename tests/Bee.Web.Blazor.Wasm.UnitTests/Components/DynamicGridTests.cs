using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Wasm.Components;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 針對 <see cref="DynamicGrid"/> 私有靜態方法（TryGetRowId / FormatCell / BuildColumnStyle）
    /// 的邏輯測試，使用 reflection 呼叫；結構性 smoke 測試確認元件繼承關係與預設值。
    /// OnRowClickAsync 依賴 EventCallback，需 bUnit 驅動，留待後續整合測試覆蓋。
    /// </summary>
    public class DynamicGridTests
    {
        private static readonly MethodInfo s_tryGetRowId =
            typeof(DynamicGrid).GetMethod("TryGetRowId",
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo s_formatCell =
            typeof(DynamicGrid).GetMethod("FormatCell",
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo s_buildColumnStyle =
            typeof(DynamicGrid).GetMethod("BuildColumnStyle",
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static bool InvokeTryGetRowId(DataRow row, out Guid rowId)
        {
            var args = new object?[] { row, Guid.Empty };
            bool result = (bool)s_tryGetRowId.Invoke(null, args)!;
            rowId = (Guid)args[1]!;
            return result;
        }

        private static string InvokeFormatCell(DataRow row, LayoutColumn column)
            => (string)s_formatCell.Invoke(null, new object[] { row, column })!;

        private static string InvokeBuildColumnStyle(LayoutColumn column)
            => (string)s_buildColumnStyle.Invoke(null, new object[] { column })!;

        // ---- Component structure ----

        [Fact]
        [DisplayName("DynamicGrid 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(DynamicGrid)));
        }

        [Fact]
        [DisplayName("DynamicGrid EmptyText 預設值為 'No data.'")]
        public void EmptyText_Default_IsNoData()
        {
            var grid = new DynamicGrid();
            Assert.Equal("No data.", grid.EmptyText);
        }

        // ---- TryGetRowId ----

        [Fact]
        [DisplayName("TryGetRowId：DataRow 不含 sys_rowid 欄位應回傳 false")]
        public void TryGetRowId_MissingColumn_ReturnsFalse()
        {
            using var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "test";
            table.Rows.Add(row);

            bool result = InvokeTryGetRowId(row, out var rowId);

            Assert.False(result);
            Assert.Equal(Guid.Empty, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 為 DBNull 應回傳 false")]
        public void TryGetRowId_DbNullValue_ReturnsFalse()
        {
            using var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = DBNull.Value;
            table.Rows.Add(row);

            bool result = InvokeTryGetRowId(row, out var rowId);

            Assert.False(result);
            Assert.Equal(Guid.Empty, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 含 Guid 值應回傳 true 並輸出正確 Guid")]
        public void TryGetRowId_GuidValue_ReturnsTrueWithGuid()
        {
            var expected = Guid.NewGuid();
            using var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);

            bool result = InvokeTryGetRowId(row, out var rowId);

            Assert.True(result);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 含有效 Guid 字串應回傳 true 並解析為 Guid")]
        public void TryGetRowId_GuidString_ReturnsTrueWithParsedGuid()
        {
            var expected = Guid.NewGuid();
            using var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = expected.ToString();
            table.Rows.Add(row);

            bool result = InvokeTryGetRowId(row, out var rowId);

            Assert.True(result);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 含無效 Guid 字串應回傳 false")]
        public void TryGetRowId_InvalidGuidString_ReturnsFalse()
        {
            using var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = "not-a-valid-guid";
            table.Rows.Add(row);

            bool result = InvokeTryGetRowId(row, out _);

            Assert.False(result);
        }

        // ---- FormatCell ----

        [Fact]
        [DisplayName("FormatCell：欄位不存在 DataTable 中應回傳空字串")]
        public void FormatCell_MissingColumn_ReturnsEmpty()
        {
            using var table = new DataTable();
            table.Columns.Add("other_col", typeof(string));
            var row = table.NewRow();
            row["other_col"] = "value";
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "missing_col" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell：欄位值為 DBNull 應回傳空字串")]
        public void FormatCell_DbNullValue_ReturnsEmpty()
        {
            using var table = new DataTable();
            table.Columns.Add("price", typeof(decimal));
            var row = table.NewRow();
            row["price"] = DBNull.Value;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "price" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell：欄位有 DisplayFormat 時套用 DisplayFormat 格式化數值")]
        public void FormatCell_WithDisplayFormat_UsesDisplayFormat()
        {
            using var table = new DataTable();
            table.Columns.Add("amount", typeof(decimal));
            var row = table.NewRow();
            row["amount"] = 12345.678m;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "amount", DisplayFormat = "N2" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("12,345.68", result);
        }

        [Fact]
        [DisplayName("FormatCell：有 NumberFormat 且 DisplayFormat 為空時套用 NumberFormat")]
        public void FormatCell_WithNumberFormatNoDisplay_UsesNumberFormat()
        {
            using var table = new DataTable();
            table.Columns.Add("rate", typeof(double));
            var row = table.NewRow();
            row["rate"] = 1.5;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "rate", NumberFormat = "F2" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("1.50", result);
        }

        [Fact]
        [DisplayName("FormatCell：DateTime 時間部分為零應格式化為 yyyy-MM-dd")]
        public void FormatCell_DateOnlyDateTime_FormatsAsDate()
        {
            using var table = new DataTable();
            table.Columns.Add("hire_date", typeof(DateTime));
            var row = table.NewRow();
            row["hire_date"] = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Unspecified);
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "hire_date" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("2024-03-15", result);
        }

        [Fact]
        [DisplayName("FormatCell：DateTime 含時間部分應格式化為 yyyy-MM-dd HH:mm:ss")]
        public void FormatCell_FullDateTime_FormatsAsDatetime()
        {
            using var table = new DataTable();
            table.Columns.Add("created_at", typeof(DateTime));
            var row = table.NewRow();
            row["created_at"] = new DateTime(2024, 3, 15, 14, 30, 45, DateTimeKind.Unspecified);
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "created_at" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("2024-03-15 14:30:45", result);
        }

        [Fact]
        [DisplayName("FormatCell：整數值無格式設定應以 IFormattable 回傳 InvariantCulture 字串")]
        public void FormatCell_IntValueNoFormat_ReturnsInvariantString()
        {
            using var table = new DataTable();
            table.Columns.Add("count", typeof(int));
            var row = table.NewRow();
            row["count"] = 42;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "count" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("42", result);
        }

        [Fact]
        [DisplayName("FormatCell：字串值應原樣回傳（走 _ 分支）")]
        public void FormatCell_StringValue_ReturnsAsIs()
        {
            using var table = new DataTable();
            table.Columns.Add("emp_name", typeof(string));
            var row = table.NewRow();
            row["emp_name"] = "Alice";
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "emp_name" };

            var result = InvokeFormatCell(row, column);

            Assert.Equal("Alice", result);
        }

        // ---- BuildColumnStyle ----

        [Fact]
        [DisplayName("BuildColumnStyle：Width 為 0 應回傳空字串")]
        public void BuildColumnStyle_ZeroWidth_ReturnsEmpty()
        {
            var column = new LayoutColumn { Width = 0 };

            var result = InvokeBuildColumnStyle(column);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("BuildColumnStyle：Width 大於 0 應回傳 'width:{N}px' CSS 字串")]
        public void BuildColumnStyle_PositiveWidth_ReturnsWidthStyle()
        {
            var column = new LayoutColumn { Width = 120 };

            var result = InvokeBuildColumnStyle(column);

            Assert.Equal("width:120px", result);
        }
    }
}
