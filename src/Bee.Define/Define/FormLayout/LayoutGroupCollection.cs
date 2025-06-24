using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 排版群組集合。
    /// </summary>
    [Serializable]
    [Description("表單欄位集合。")]
    [TreeNode("群組", false)]
    public class LayoutGroupCollection : CollectionBase<LayoutGroup>
    {
        
    }
}
