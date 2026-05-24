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
    /// 針對 <see cref="DynamicGrid"/> 的結構性 smoke 測試與私有靜態輔助方法驗證。
    /// 渲染層（VisibleColumns/OnRowClickAsync）需 Blazor 渲染器驅動，留待 bUnit 整合測試。
    /// </summary>
    public class DynamicGridTests
    {
        private static readonly Type[] s_tryGetRowIdParams =
            [typeof(DataRow), typeof(Guid).MakeByRefType()];
        private static readonly Type[] s_formatCellParams =
            [typeof(DataRow), typeof(LayoutColumn)];
        private static readonly Type[] s_buildColumnStyleParams =
            [typeof(LayoutColumn)];

        private static MethodInfo GetStaticMethod(string name, Type[] paramTypes)
        {
            var method = typeof(DynamicGrid).GetMethod(
                name, BindingFlags.NonPublic | BindingFlags.Static, null, paramTypes, null);
            Assert.NotNull(method);
            return method!;
        }

        private static DataTable BuildTableWithRowId(Type columnType)
        {
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, columnType);
            return table;
        }

        #region 結構性檢查

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
        [DisplayName("公開屬性皆標有 [Parameter]")]
        public void PublicProperties_HaveParameterAttribute(string propertyName)
        {
            var property = typeof(DynamicGrid).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.NotNull(property!.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("EmptyText 預設值為 \"No data.\"")]
        public void EmptyText_Default_IsNoDotData()
        {
            var grid = new DynamicGrid();
            Assert.Equal("No data.", grid.EmptyText);
        }

        [Fact]
        [DisplayName("Layout 預設為 null")]
        public void Layout_Default_IsNull()
        {
            var grid = new DynamicGrid();
            Assert.Null(grid.Layout);
        }

        [Fact]
        [DisplayName("Rows 預設為 null")]
        public void Rows_Default_IsNull()
        {
            var grid = new DynamicGrid();
            Assert.Null(grid.Rows);
        }

        #endregion

        #region TryGetRowId

        [Fact]
        [DisplayName("TryGetRowId 列不含 sys_rowid 欄位時應回傳 false")]
        public void TryGetRowId_RowWithoutRowIdColumn_ReturnsFalse()
        {
            var method = GetStaticMethod("TryGetRowId", s_tryGetRowIdParams);
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "test";
            table.Rows.Add(row);

            var args = new object[] { row, Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.False(result);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為 Guid 型別時應回傳 true 與對應 Guid")]
        public void TryGetRowId_RowWithGuidValue_ReturnsTrueAndGuid()
        {
            var method = GetStaticMethod("TryGetRowId", s_tryGetRowIdParams);
            var expected = Guid.NewGuid();
            var table = BuildTableWithRowId(typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);

            var args = new object[] { row, Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.True(result);
            Assert.Equal(expected, (Guid)args[1]);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為有效 Guid 字串時應回傳 true 與解析後的 Guid")]
        public void TryGetRowId_RowWithGuidString_ReturnsTrueAndParsedGuid()
        {
            var method = GetStaticMethod("TryGetRowId", s_tryGetRowIdParams);
            var expected = Guid.NewGuid();
            var table = BuildTableWithRowId(typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = expected.ToString();
            table.Rows.Add(row);

            var args = new object[] { row, Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.True(result);
            Assert.Equal(expected, (Guid)args[1]);
        }

        [Fact]
        [DisplayName("TryGetRowId sys_rowid 為 DBNull 時應回傳 false")]
        public void TryGetRowId_RowWithDbNull_ReturnsFalse()
        {
            var method = GetStaticMethod("TryGetRowId", s_tryGetRowIdParams);
            var table = BuildTableWithRowId(typeof(string));
            var row = table.NewRow();
            row[SysFields.RowId] = DBNull.Value;
            table.Rows.Add(row);

            var args = new object[] { row, Guid.Empty };
            var result = (bool)method.Invoke(null, args)!;

            Assert.False(result);
        }

        #endregion

        #region FormatCell

        [Fact]
        [DisplayName("FormatCell 欄位名稱不在 DataTable 中應回傳空字串")]
        public void FormatCell_ColumnNotInTable_ReturnsEmpty()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("other", typeof(string));
            var row = table.NewRow();
            row["other"] = "value";
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "nonexistent" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell 值為 DBNull 時應回傳空字串")]
        public void FormatCell_DbNullValue_ReturnsEmpty()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("amount", typeof(double));
            var row = table.NewRow();
            row["amount"] = DBNull.Value;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "amount" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 無時間部分應格式化為 yyyy-MM-dd")]
        public void FormatCell_DateTimeNoTime_ReturnsDateOnly()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("hire_date", typeof(DateTime));
            var row = table.NewRow();
            row["hire_date"] = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "hire_date" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal("2024-03-15", result);
        }

        [Fact]
        [DisplayName("FormatCell DateTime 有時間部分應格式化為 yyyy-MM-dd HH:mm:ss")]
        public void FormatCell_DateTimeWithTime_ReturnsFullDateTime()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("created_at", typeof(DateTime));
            var row = table.NewRow();
            row["created_at"] = new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc);
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "created_at" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal("2024-03-15 14:30:00", result);
        }

        [Fact]
        [DisplayName("FormatCell 整數值無格式應以 InvariantCulture 回傳字串")]
        public void FormatCell_IntegerValue_ReturnsInvariantString()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("count", typeof(int));
            var row = table.NewRow();
            row["count"] = 42;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "count" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal("42", result);
        }

        [Fact]
        [DisplayName("FormatCell 設有 NumberFormat 且值為數值時應套用格式")]
        public void FormatCell_WithNumberFormat_ReturnsFormattedNumber()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("price", typeof(double));
            var row = table.NewRow();
            row["price"] = 1234.5;
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "price", NumberFormat = "F2" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal("1234.50", result);
        }

        [Fact]
        [DisplayName("FormatCell 字串值應直接回傳原始字串")]
        public void FormatCell_StringValue_ReturnsOriginalString()
        {
            var method = GetStaticMethod("FormatCell", s_formatCellParams);
            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "Alice";
            table.Rows.Add(row);

            var column = new LayoutColumn { FieldName = "name" };
            var result = (string)method.Invoke(null, new object[] { row, column })!;

            Assert.Equal("Alice", result);
        }

        #endregion

        #region BuildColumnStyle

        [Fact]
        [DisplayName("BuildColumnStyle 寬度為 0 應回傳空字串")]
        public void BuildColumnStyle_ZeroWidth_ReturnsEmpty()
        {
            var method = GetStaticMethod("BuildColumnStyle", s_buildColumnStyleParams);
            var column = new LayoutColumn { Width = 0 };
            var result = (string)method.Invoke(null, new object[] { column })!;

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("BuildColumnStyle 正數寬度應回傳 width:{n}px 格式字串")]
        public void BuildColumnStyle_PositiveWidth_ReturnsWidthPxString()
        {
            var method = GetStaticMethod("BuildColumnStyle", s_buildColumnStyleParams);
            var column = new LayoutColumn { Width = 120 };
            var result = (string)method.Invoke(null, new object[] { column })!;

            Assert.Equal("width:120px", result);
        }

        #endregion
    }
}
