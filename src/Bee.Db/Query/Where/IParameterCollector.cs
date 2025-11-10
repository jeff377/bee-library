using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// 參數收集器介面（供 Builder 產生具名參數）。
    /// </summary>
    public interface IParameterCollector
    {
        /// <summary>加入一個參數值，回傳參數名稱（含前綴符號）。</summary>
        string Add(object value);

        /// <summary>取得已加入的參數字典。</summary>
        IDictionary<string, object> GetAll();
    }
}
