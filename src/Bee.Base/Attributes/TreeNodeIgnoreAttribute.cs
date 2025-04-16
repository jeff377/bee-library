using System;

namespace Bee.Base
{
    /// <summary>
    /// 套用於屬性，表示忽略樹狀結構生成的自訂屬性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TreeNodeIgnoreAttribute : Attribute
    {
    }
}
