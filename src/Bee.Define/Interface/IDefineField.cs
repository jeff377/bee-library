using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 定義欄位介面。
    /// </summary>
    public interface IDefineField
    {
        /// <summary>
        /// 欄位名稱。
        /// </summary>
        string FieldName { get; set; }

        /// <summary>
        /// 標題文字．
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// 資料型別。
        /// </summary>
        EFieldDbType DbType { get; set; }
    }
}