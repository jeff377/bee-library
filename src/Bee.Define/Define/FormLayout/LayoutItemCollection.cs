using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
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
