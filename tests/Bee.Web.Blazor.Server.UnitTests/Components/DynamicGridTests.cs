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
    /// Structural smoke tests and reflection-based checks for <see cref="DynamicGrid"/>.
    /// Covers parameter surface, default values, and the private static helper methods
    /// <c>TryGetRowId</c>, <c>FormatCell</c>, and <c>BuildColumnStyle</c>.
    /// </summary>
    public class DynamicGridTests
    {
        private static readonly Type[] s_dataRowGuidOutParam = [typeof(DataRow), typeof(Guid).MakeByRefType()];
        private static readonly Type[] s_dataRowColumnParam = [typeof(DataRow), typeof(LayoutColumn)];
        private static readonly Type[] s_layoutColumnParam = [typeof(LayoutColumn)];

        private static PropertyInfo GetPublicProperty(string name)
        {
            var property = typeof(DynamicGrid).GetProperty(
                name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        private static MethodInfo GetTryGetRowId()
        {
            var method = typeof(DynamicGrid).GetMethod(
                "TryGetRowId",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_dataRowGuidOutParam,
                null);
            Assert.NotNull(method);
            return method!;
        }

        private static MethodInfo GetFormatCell()
        {
            var method = typeof(DynamicGrid).GetMethod(
                "FormatCell",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_dataRowColumnParam,
                null);
            Assert.NotNull(method);
            return method!;
        }

        private static MethodInfo GetBuildColumnStyle()
        {
            var method = typeof(DynamicGrid).GetMethod(
                "BuildColumnStyle",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_layoutColumnParam,
                null);
            Assert.NotNull(method);
            return method!;
        }

        // ---- 結構性測試 ----

        [Fact]
        [DisplayName("DynamicGrid 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(DynamicGrid)));
        }

        [Theory]
        [InlineData(nameof(DynamicGrid.Layout))]
        [InlineData(nameof(DynamicGrid.Rows))]
        [InlineData(nameof(DynamicGrid.OnRowSelected))]
        [InlineData(nameof(DynamicGrid.EmptyText))]
        [DisplayName("公開屬性皆標註 [Parameter]")]
        public void PublicProperties_AreMarkedAsParameters(string name)
        {
            var property = GetPublicProperty(name);
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("EmptyText 屬性預設值為 \"No data.\"")]
        public void EmptyText_DefaultValue_IsNoData()
        {
            var grid = new DynamicGrid();
            Assert.Equal("No data.", grid.EmptyText);
        }

        // ---- TryGetRowId ----

        [Fact]
        [DisplayName("TryGetRowId 列無 sys_rowid 欄位應回傳 false")]
        public void TryGetRowId_NoRowIdColumn_ReturnsFalse()
        {
            var table = new DataTable();
            var row = table.NewRow();
            var method = GetTryGetRowId();
            var args = new object[] { row, Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;
            Assert.False(result);
        }

        [Fact]
        [DisplayName("TryGetRowId 列含 Guid 型別的 sys_rowid 應回傳 true 及正確 Guid")]
        public void TryGetRowId_GuidRowId_ReturnsTrueWithCorrectGuid()
        {
            var expected = new Guid("11111111-2222-3333-4444-555555555555");
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);

            var method = GetTryGetRowId();
            var args = new object[] { table.Rows[0], Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.True(result);
            Assert.Equal(expected, (Guid)args[1]);
        }

        [Fact]
        [DisplayName("TryGetRowId 列含 string 型別的有效 GUID 應回傳 true 及解析後 Guid")]
        public void TryGetRowId_StringRowId_ReturnsTrueWithParsedGuid()
        {
            var expected = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = expected.ToString();
            table.Rows.Add(row);

            var method = GetTryGetRowId();
            var args = new object[] { table.Rows[0], Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.True(result);
            Assert.Equal(expected, (Guid)args[1]);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 值為 DBNull 應回傳 false")]
        public void TryGetRowId_DbNullRowId_ReturnsFalse()
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = DBNull.Value;
            table.Rows.Add(row);

            var method = GetTryGetRowId();
            var args = new object[] { table.Rows[0], Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;
            Assert.False(result);
        }

        // ---- FormatCell ----

        [Fact]
        [DisplayName("FormatCell 欄位不存在於列時應回傳空字串")]
        public void FormatCell_ColumnNotInTable_ReturnsEmpty()
        {
            var table = new DataTable();
            var row = table.NewRow();
            var column = new LayoutColumn { FieldName = "missing_field" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { row, column })!;
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 欄位值為 DBNull 應回傳空字串")]
        public void FormatCell_DbNullValue_ReturnsEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("status", typeof(string));
            var row = table.NewRow();
            row["status"] = DBNull.Value;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "status" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { table.Rows[0], column })!;
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 字串值應直接回傳字串內容")]
        public void FormatCell_StringValue_ReturnsStringAsIs()
        {
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "Alice";
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "name" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { table.Rows[0], column })!;
            Assert.Equal("Alice", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 無時間部分應回傳 yyyy-MM-dd 格式")]
        public void FormatCell_DateOnlyDateTime_ReturnsDateOnlyFormat()
        {
            var dt = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            var table = new DataTable();
            table.Columns.Add("hire_date", typeof(DateTime));
            var row = table.NewRow();
            row["hire_date"] = dt;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "hire_date" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { table.Rows[0], column })!;
            Assert.Equal("2024-03-15", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 含時間部分應回傳 yyyy-MM-dd HH:mm:ss 格式")]
        public void FormatCell_DateTimeWithTime_ReturnsFullDateTimeFormat()
        {
            var dt = new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc);
            var table = new DataTable();
            table.Columns.Add("updated_at", typeof(DateTime));
            var row = table.NewRow();
            row["updated_at"] = dt;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "updated_at" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { table.Rows[0], column })!;
            Assert.Equal("2024-03-15 14:30:00", result);
        }

        [Fact]
        [DisplayName("FormatCell 設定 DisplayFormat 時應套用格式")]
        public void FormatCell_WithDisplayFormat_AppliesFormat()
        {
            var table = new DataTable();
            table.Columns.Add("seq_no", typeof(int));
            var row = table.NewRow();
            row["seq_no"] = 42;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "seq_no", DisplayFormat = "D5" };
            var method = GetFormatCell();
            var result = (string)method.Invoke(null, new object[] { table.Rows[0], column })!;
            Assert.Equal("00042", result);
        }

        // ---- BuildColumnStyle ----

        [Fact]
        [DisplayName("BuildColumnStyle 寬度大於 0 應回傳 width:{n}px 樣式字串")]
        public void BuildColumnStyle_PositiveWidth_ReturnsWidthStyle()
        {
            var column = new LayoutColumn { Width = 120 };
            var method = GetBuildColumnStyle();
            var result = (string)method.Invoke(null, new object[] { column })!;
            Assert.Equal("width:120px", result);
        }

        [Fact]
        [DisplayName("BuildColumnStyle 寬度為 0 應回傳空字串")]
        public void BuildColumnStyle_ZeroWidth_ReturnsEmpty()
        {
            var column = new LayoutColumn { Width = 0 };
            var method = GetBuildColumnStyle();
            var result = (string)method.Invoke(null, new object[] { column })!;
            Assert.Equal(string.Empty, result);
        }
    }
}
