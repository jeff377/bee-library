using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 類別具有 Tag 屬性的介面。
    /// </summary>
    public interface ITagProperty
    {
        /// <summary>
        /// 儲存額外資訊。
        /// </summary>
        object Tag { get; set; }
    }
}
