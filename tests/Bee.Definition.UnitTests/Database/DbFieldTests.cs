using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Database
{
    /// <summary>
    /// DbField 建構、屬性預設、Clone、Compare 與 ToString 的測試。
    /// </summary>
    public class DbFieldTests
    {
        [Fact]
        [DisplayName("DbField 預設建構子應以 FieldDbType.String 作為 DbType 預設值")]
        public void DefaultConstructor_HasExpectedDefaults()
        {
            var field = new DbField();

            Assert.Equal(string.Empty, field.FieldName);
            Assert.Equal(string.Empty, field.Caption);
            Assert.Equal(string.Empty, field.OriginalFieldName);
            Assert.Equal(FieldDbType.String, field.DbType);
            Assert.Equal(0, field.Length);
            Assert.Equal(18, field.Precision);
            Assert.Equal(0, field.Scale);
            Assert.False(field.AllowNull);
            Assert.Equal(string.Empty, field.DefaultValue);
            Assert.Equal(DbUpgradeAction.None, field.UpgradeAction);
        }

        [Fact]
        [DisplayName("DbField 參數化建構子應依序指派 FieldName、Caption、DbType")]
        public void ParameterizedConstructor_AssignsCoreProperties()
        {
            var field = new DbField("sys_id", "編號", FieldDbType.Integer);

            Assert.Equal("sys_id", field.FieldName);
            Assert.Equal("編號", field.Caption);
            Assert.Equal(FieldDbType.Integer, field.DbType);
        }

        [Fact]
        [DisplayName("FieldName 屬性應透明讀寫底層 Key")]
        public void FieldName_MirrorsKey()
        {
            var field = new DbField { FieldName = "k1" };

            Assert.Equal("k1", field.Key);

            field.Key = "k2";
            Assert.Equal("k2", field.FieldName);
        }

        [Fact]
        [DisplayName("Clone 應完整複製所有欄位值")]
        public void Clone_CopiesAllFields()
        {
            var source = new DbField("amount", "金額", FieldDbType.Decimal)
            {
                OriginalFieldName = "legacy_amount",
                Length = 0,
                Precision = 19,
                Scale = 4,
                AllowNull = true,
                DefaultValue = "0"
            };

            var clone = source.Clone();

            Assert.NotSame(source, clone);
            Assert.Equal(source.FieldName, clone.FieldName);
            Assert.Equal(source.Caption, clone.Caption);
            Assert.Equal(source.OriginalFieldName, clone.OriginalFieldName);
            Assert.Equal(source.DbType, clone.DbType);
            Assert.Equal(source.Length, clone.Length);
            Assert.Equal(source.Precision, clone.Precision);
            Assert.Equal(source.Scale, clone.Scale);
            Assert.Equal(source.AllowNull, clone.AllowNull);
            Assert.Equal(source.DefaultValue, clone.DefaultValue);
        }

        [Fact]
        [DisplayName("Compare 所有關鍵欄位相同時應回傳 true")]
        public void Compare_SameFields_ReturnsTrue()
        {
            var a = new DbField("name", "名稱", FieldDbType.String)
            {
                Length = 100,
                AllowNull = true,
                DefaultValue = "N/A"
            };
            var b = a.Clone();

            Assert.True(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare DbType 不同應回傳 false")]
        public void Compare_DifferentDbType_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.String);
            var b = new DbField("x", "x", FieldDbType.Integer);

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare AllowNull 不同應回傳 false")]
        public void Compare_DifferentAllowNull_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.String) { AllowNull = true };
            var b = new DbField("x", "x", FieldDbType.String) { AllowNull = false };

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare String 型別 Length 不同應回傳 false")]
        public void Compare_StringDifferentLength_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.String) { Length = 50 };
            var b = new DbField("x", "x", FieldDbType.String) { Length = 100 };

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare 非 String 型別時 Length 差異不影響結果")]
        public void Compare_NonStringLengthIgnored()
        {
            var a = new DbField("x", "x", FieldDbType.Integer) { Length = 10 };
            var b = new DbField("x", "x", FieldDbType.Integer) { Length = 20 };

            Assert.True(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare Decimal Precision 不同應回傳 false")]
        public void Compare_DecimalDifferentPrecision_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var b = new DbField("x", "x", FieldDbType.Decimal) { Precision = 19, Scale = 4 };

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare Decimal Scale 不同應回傳 false")]
        public void Compare_DecimalDifferentScale_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.Decimal) { Precision = 18, Scale = 2 };
            var b = new DbField("x", "x", FieldDbType.Decimal) { Precision = 18, Scale = 4 };

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare 非 Decimal 型別時 Precision/Scale 差異不影響結果")]
        public void Compare_NonDecimalPrecisionScaleIgnored()
        {
            var a = new DbField("x", "x", FieldDbType.String) { Precision = 10, Scale = 2 };
            var b = new DbField("x", "x", FieldDbType.String) { Precision = 20, Scale = 8 };

            Assert.True(a.Compare(b));
        }

        [Fact]
        [DisplayName("Compare DefaultValue 不同應回傳 false")]
        public void Compare_DifferentDefaultValue_ReturnsFalse()
        {
            var a = new DbField("x", "x", FieldDbType.String) { DefaultValue = "A" };
            var b = new DbField("x", "x", FieldDbType.String) { DefaultValue = "B" };

            Assert.False(a.Compare(b));
        }

        [Fact]
        [DisplayName("ToString 應回傳 FieldName - Caption 格式")]
        public void ToString_ReturnsFieldNameAndCaption()
        {
            var field = new DbField("sys_id", "編號", FieldDbType.String);

            Assert.Equal("sys_id - 編號", field.ToString());
        }

        [Fact]
        [DisplayName("Compare OriginalFieldName 不同不影響結果（僅為 rename 提示）")]
        public void Compare_OriginalFieldNameIgnored()
        {
            var a = new DbField("x", "x", FieldDbType.String) { OriginalFieldName = "old_x" };
            var b = new DbField("x", "x", FieldDbType.String);

            Assert.True(a.Compare(b));
        }
    }
}
