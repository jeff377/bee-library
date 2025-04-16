using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    ///  參數項目集合，支援序列化。
    /// </summary>
    [Serializable]
    public class TParameterCollection : TKeyCollectionBase<TParameter>
    {
        /// <summary>
        /// 加入參數。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public void Add(string name, object value)
        {
            TParameter oItem;

            if (this.Contains(name))
            {
                oItem = this[name];
                oItem.Value = value;
            }
            else
            {
                oItem = new TParameter(name, value);
                this.Add(oItem);
            }
        }

        /// <summary>
        /// 取得參數值。
        /// </summary>
        /// <typeparam name="T">參數型別。</typeparam>
        /// <param name="name">參數名稱。</param>
        public T GetValue<T>(string name)
        {
            if (this.Contains(name))
                return (T)this[name].Value;
            else
                throw new TException("'{0}' 參數不存在", name);
        }

        /// <summary>
        /// 取得參數值。
        /// </summary>
        /// <typeparam name="T">參數型別。</typeparam>
        /// <param name="name">參數名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        public T GetValue<T>(string name, object defaultValue)
        {
            if (this.Contains(name))
                return (T)this[name].Value;
            else
                return (T)defaultValue;
        }
    }
}
