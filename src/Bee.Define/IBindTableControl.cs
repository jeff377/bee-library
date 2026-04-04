using System.Data;

namespace Bee.Define
{
    /// <summary>
    /// 繫結資料表的控制項介面。
    /// </summary>
    public interface IBindTableControl
    {
        /// <summary>
        /// 資料表名稱。
        /// </summary>
        string TableName { get; set; }

        /// <summary>
        /// 繫結資料表。
        /// </summary>
        DataTable DataTable { get; set; }

        /// <summary>
        /// 結束目前編輯。
        /// </summary>
        void EndEdit();
    }
}
