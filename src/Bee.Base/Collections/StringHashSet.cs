using System;
using System.Collections.Generic;

namespace Bee.Base
{
    /// <summary>
    /// 不允許重覆的字串集合，字串忽略大小寫。
    /// </summary>
    public class StringHashSet : HashSet<string>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public StringHashSet() : base(StringComparer.InvariantCultureIgnoreCase)
        { }

        #endregion

        /// <summary>
        /// 加入集合字串為成員。
        /// </summary>
        /// <param name="s">字串。</param>
        /// <param name="delimiter">分隔符號。</param>
        public void Add(string s, string delimiter)
        {
            string[] oValues;

            if (StrFunc.IsEmpty(s)) { return; }

            oValues = StrFunc.Split(s, delimiter);
            foreach (string value in oValues)
                this.Add(value);
        }
    }
}
