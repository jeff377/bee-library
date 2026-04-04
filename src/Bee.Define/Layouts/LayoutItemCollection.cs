using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Layouts
{
    /// <summary>
    /// 排版項目集合。
    /// </summary>
    [Serializable]
    [Description("排版項目集合。")]
    [TreeNode("排版項目", false)]
    public class LayoutItemCollection : CollectionBase<LayoutItemBase>
    {

    }
}
