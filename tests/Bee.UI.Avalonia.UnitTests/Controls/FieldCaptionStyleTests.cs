using System.ComponentModel;
using Avalonia.Media;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Behaviour checks for <see cref="FieldCaptionStyle"/>: the shared caption-colour
    /// convention (read-only = brown, required = blue, read-only wins) applied to master
    /// field captions and detail grid headers.
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
        [DisplayName("唯讀欄位標題為棕色")]
        public void GetCaptionForeground_ReadOnly_ReturnsBrown()
        {
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(
                FieldCaptionStyle.GetCaptionForeground(readOnly: true, required: false));
            Assert.Equal(Color.FromRgb(0xA0, 0x52, 0x2D), brush.Color);
        }

        [Fact]
        [DisplayName("必填欄位標題為藍色")]
        public void GetCaptionForeground_Required_ReturnsBlue()
        {
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(
                FieldCaptionStyle.GetCaptionForeground(readOnly: false, required: true));
            Assert.Equal(Color.FromRgb(0x25, 0x63, 0xEB), brush.Color);
        }

        [Fact]
        [DisplayName("唯讀＋必填時唯讀（棕）優先")]
        public void GetCaptionForeground_ReadOnlyAndRequired_ReadOnlyWins()
        {
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(
                FieldCaptionStyle.GetCaptionForeground(readOnly: true, required: true));
            Assert.Equal(Color.FromRgb(0xA0, 0x52, 0x2D), brush.Color);
        }
    }
}
