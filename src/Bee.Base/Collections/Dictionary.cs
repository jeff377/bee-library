using System;
using System.Collections.Generic;

namespace Bee.Base
{
    /// <summary>
    /// 索引鍵和值的集合(鍵值不區分大小寫)。
    /// </summary>
    /// <typeparam name="T">成員型別。</typeparam>
    public class Dictionary<T> : Dictionary<string, T>
    {
        /// <summary>
        /// 建構函式
        /// </summary>
        public Dictionary() : base(StringComparer.CurrentCultureIgnoreCase) { }
    }
}
