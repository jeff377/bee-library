using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// A <see cref="FieldDbType.Time"/> field must reach the UI as a time editor, otherwise the
    /// semantic marker buys nothing at the layer it exists to serve (ADR-033).
    /// </summary>
    public class TimeControlTypeTests
    {
        [Fact]
        [DisplayName("Time 欄位的 Auto 控件型別應解析為 TimeEdit")]
        public void ResolveControlType_TimeField_ResolvesToTimeEdit()
        {
            Assert.Equal(ControlType.TimeEdit,
                LayoutColumnFactory.ResolveControlType(ControlType.Auto, FieldDbType.Time));
        }

        [Fact]
        [DisplayName("明確指定的控件型別優先於 Time 的預設")]
        public void ResolveControlType_ExplicitType_Wins()
        {
            Assert.Equal(ControlType.TextEdit,
                LayoutColumnFactory.ResolveControlType(ControlType.TextEdit, FieldDbType.Time));
        }

        [Fact]
        [DisplayName("TimeEdit 必須位於 ControlType 尾端，避免既有 payload 位移")]
        public void TimeEdit_IsAppendedAtEndOfEnum()
        {
            var values = Enum.GetValues<ControlType>();
            Assert.Equal(ControlType.TimeEdit, values[^1]);
        }
    }
}
