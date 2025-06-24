using System;
using System.ComponentModel;
using Bee.Base;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 自訂屬性集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [Description("自訂屬性集合。")]
    public class PropertyCollection : KeyCollectionBase<Property>
    {
        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="value">屬性值。</param>
        public void Add(string name, string value)
        {
            base.Add(new Property(name, value));
        }

        /// <summary>
        /// 取得屬性值。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        public string GetValue(string name, string defaultValue)
        {
            if (this.Contains(name))
                return this[name].Value;
            else
                return defaultValue;
        }

        /// <summary>
        /// 取得屬性值。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        public bool GetValue(string name, bool defaultValue)
        {
            if (this.Contains(name))
                return BaseFunc.CBool(this[name].Value);
            else
                return defaultValue;
        }

        /// <summary>
        /// 取得屬性值。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        public int GetValue(string name, int defaultValue)
        {
            if (this.Contains(name))
                return BaseFunc.CInt(this[name].Value);
            else
                return defaultValue;
        }
    }
}
