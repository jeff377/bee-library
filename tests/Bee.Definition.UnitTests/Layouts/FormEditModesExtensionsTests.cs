using System.ComponentModel;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// FormEditModesExtensions 單元測試。
    /// </summary>
    public class FormEditModesExtensionsTests
    {
        [Theory]
        [InlineData(FormEditModes.All, SingleFormMode.Add, true)]
        [InlineData(FormEditModes.All, SingleFormMode.Edit, true)]
        [InlineData(FormEditModes.All, SingleFormMode.View, false)]
        [InlineData(FormEditModes.Add, SingleFormMode.Add, true)]
        [InlineData(FormEditModes.Add, SingleFormMode.Edit, false)]
        [InlineData(FormEditModes.Add, SingleFormMode.View, false)]
        [InlineData(FormEditModes.Edit, SingleFormMode.Add, false)]
        [InlineData(FormEditModes.Edit, SingleFormMode.Edit, true)]
        [InlineData(FormEditModes.Edit, SingleFormMode.View, false)]
        [InlineData(FormEditModes.None, SingleFormMode.Add, false)]
        [InlineData(FormEditModes.None, SingleFormMode.Edit, false)]
        [InlineData(FormEditModes.None, SingleFormMode.View, false)]
        [DisplayName("Allows 依旗標與表單模式回傳是否可編輯；View 永遠 false")]
        public void Allows_FlagAndMode_ReturnsExpected(FormEditModes modes, SingleFormMode formMode, bool expected)
        {
            Assert.Equal(expected, modes.Allows(formMode));
        }
    }
}
