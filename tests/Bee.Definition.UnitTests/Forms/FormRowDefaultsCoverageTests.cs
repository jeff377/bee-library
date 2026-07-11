using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// <see cref="FormRowDefaults"/> 補測：guard 分支（null formTable / null Fields / 欄位無對應資料欄）
    /// 與 <see cref="FormRowDefaults.DefaultForDbType"/> 對每一個 <see cref="FieldDbType"/> 的型別預設值。
    /// 純記憶體、無資料庫。
    /// </summary>
    public class FormRowDefaultsCoverageTests
    {
        private static DataRow NewSingleColumnRow()
        {
            var table = new DataTable("T");
            table.Columns.Add("sys_rowid", typeof(Guid));
            return table.NewRow();
        }

        [Fact]
        [DisplayName("Apply：formTable 為 null 時直接返回、不丟例外")]
        public void Apply_NullFormTable_NoThrow()
        {
            var row = NewSingleColumnRow();

            var ex = Record.Exception(() => FormRowDefaults.Apply(null!, row));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("Apply：formTable.Fields 為 null（序列化空集合）時直接返回、不丟例外")]
        public void Apply_NullFields_NoThrow()
        {
            var schema = new FormSchema("Order", "Order");
            var formTable = schema.Tables!.Add("Order", "Order");
            formTable.SetSerializeState(SerializeState.Serialize);   // 空 Fields → getter 回 null
            var row = NewSingleColumnRow();

            var ex = Record.Exception(() => FormRowDefaults.Apply(formTable, row));

            Assert.Null(ex);
            Assert.Null(formTable.Fields);
        }

        [Fact]
        [DisplayName("Apply：欄位在資料表無對應資料欄時略過該欄，其餘欄位仍套用預設")]
        public void Apply_FieldColumnAbsent_SkipsMissingColumn()
        {
            var schema = new FormSchema("Order", "Order");
            var formTable = schema.Tables!.Add("Order", "Order");
            formTable.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            formTable.Fields.Add("ghost", "Ghost", FieldDbType.String);   // 無對應資料欄
            formTable.Fields.Add("qty", "Qty", FieldDbType.Integer);

            var table = new DataTable("Order");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("qty", typeof(int));   // 故意不含 ghost 欄
            var row = table.NewRow();

            var ex = Record.Exception(() => FormRowDefaults.Apply(formTable, row));

            Assert.Null(ex);
            Assert.NotEqual(Guid.Empty, (Guid)row[SysFields.RowId]);
            Assert.Equal(0, row["qty"]);
        }

        [Theory]
        [InlineData(FieldDbType.String)]
        [InlineData(FieldDbType.Text)]
        [DisplayName("DefaultForDbType：文字型別回傳空字串")]
        public void DefaultForDbType_TextTypes_ReturnEmptyString(FieldDbType dbType)
        {
            Assert.Equal(string.Empty, FormRowDefaults.DefaultForDbType(dbType));
        }

        [Fact]
        [DisplayName("DefaultForDbType：布林回傳 false")]
        public void DefaultForDbType_Boolean_ReturnsFalse()
        {
            Assert.Equal(false, FormRowDefaults.DefaultForDbType(FieldDbType.Boolean));
        }

        [Fact]
        [DisplayName("DefaultForDbType：整數家族回傳對應零值（short / int / long）")]
        public void DefaultForDbType_IntegerFamily_ReturnsZero()
        {
            Assert.Equal((short)0, FormRowDefaults.DefaultForDbType(FieldDbType.Short));
            Assert.Equal(0, FormRowDefaults.DefaultForDbType(FieldDbType.Integer));
            Assert.Equal(0L, FormRowDefaults.DefaultForDbType(FieldDbType.Long));
        }

        [Fact]
        [DisplayName("DefaultForDbType：Decimal / Currency 回傳 0m")]
        public void DefaultForDbType_DecimalTypes_ReturnZeroDecimal()
        {
            Assert.Equal(0m, FormRowDefaults.DefaultForDbType(FieldDbType.Decimal));
            Assert.Equal(0m, FormRowDefaults.DefaultForDbType(FieldDbType.Currency));
        }

        [Fact]
        [DisplayName("DefaultForDbType：Date 回傳今天、DateTime 回傳 DateTime 值")]
        public void DefaultForDbType_DateTypes_ReturnTodayAndNow()
        {
            Assert.Equal(DateTime.Today, FormRowDefaults.DefaultForDbType(FieldDbType.Date));
            Assert.IsType<DateTime>(FormRowDefaults.DefaultForDbType(FieldDbType.DateTime));
        }

        [Fact]
        [DisplayName("DefaultForDbType：Guid 回傳 Guid.Empty、Binary 回傳空位元組陣列")]
        public void DefaultForDbType_GuidAndBinary_ReturnEmptyValues()
        {
            Assert.Equal(Guid.Empty, FormRowDefaults.DefaultForDbType(FieldDbType.Guid));
            var binary = Assert.IsType<byte[]>(FormRowDefaults.DefaultForDbType(FieldDbType.Binary));
            Assert.Empty(binary);
        }

        [Theory]
        [InlineData(FieldDbType.AutoIncrement)]
        [InlineData(FieldDbType.Unknown)]
        [DisplayName("DefaultForDbType：無自然空值的型別回傳 DBNull.Value")]
        public void DefaultForDbType_NoNaturalDefault_ReturnsDBNull(FieldDbType dbType)
        {
            Assert.Equal(DBNull.Value, FormRowDefaults.DefaultForDbType(dbType));
        }
    }
}
