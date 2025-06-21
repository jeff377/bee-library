using System;
using System.Collections.Generic;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    ///  參數項目集合，支援序列化。
    /// </summary>
    [MessagePackObject]
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
            if (this.Contains(name))
            {
                this[name].Value = value;
            }
            else
            {
                this.Add(new TParameter(name, value));
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
                throw new KeyNotFoundException($"Parameter '{name}' does not exist.");
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
