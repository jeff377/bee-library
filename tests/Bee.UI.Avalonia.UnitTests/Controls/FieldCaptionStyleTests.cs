using System.ComponentModel;
using Avalonia.Media;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Behaviour checks for <see cref="FieldCaptionStyle"/>: required captions are blue, read-only
    /// captions stay the theme default (their cue is the parenthesised caption / editor underline),
    /// and read-only suppresses the required colour.
    /// </summary>
    public class FieldCaptionStyleTests
    {
        [Fact]
        [DisplayName("一般欄位（非唯讀非必填）標題不上色")]
        public void GetCaptionForeground_Normal_ReturnsNull()
        {
            Assert.Null(FieldCaptionStyle.GetCaptionForeground(readOnly: false, required: false));
        }

        [Fact]
        [DisplayName("唯讀欄位標題不上色（改以括號標示）")]
        public void GetCaptionForeground_ReadOnly_ReturnsNull()
        {
            Assert.Null(FieldCaptionStyle.GetCaptionForeground(readOnly: true, required: false));
        }

        [Fact]
        [DisplayName("必填欄位標題為藍色")]
        public void GetCaptionForeground_Required_ReturnsBlue()
        {
            var brush = Assert.IsType<ISolidColorBrush>(
                FieldCaptionStyle.GetCaptionForeground(readOnly: false, required: true), exactMatch: false);
            Assert.Equal(Color.FromRgb(0x25, 0x63, 0xEB), brush.Color);
        }

        [Fact]
        [DisplayName("唯讀＋必填時唯讀優先（不套用必填藍色）")]
        public void GetCaptionForeground_ReadOnlyAndRequired_ReturnsNull()
        {
            Assert.Null(FieldCaptionStyle.GetCaptionForeground(readOnly: true, required: true));
        }

        [Fact]
        [DisplayName("唯讀欄位標題以括號包覆，例如 Amount → (Amount)")]
        public void FormatCaption_ReadOnly_WrapsInParentheses()
        {
            Assert.Equal("(Amount)", FieldCaptionStyle.FormatCaption("Amount", readOnly: true));
        }

        [Fact]
        [DisplayName("可編輯欄位標題維持原樣")]
        public void FormatCaption_Editable_ReturnsPlain()
        {
            Assert.Equal("Amount", FieldCaptionStyle.FormatCaption("Amount", readOnly: false));
        }

        [Fact]
        [DisplayName("唯讀但標題為空時不加括號")]
        public void FormatCaption_ReadOnlyEmptyCaption_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, FieldCaptionStyle.FormatCaption(string.Empty, readOnly: true));
        }
    }
}
