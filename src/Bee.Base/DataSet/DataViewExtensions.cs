using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataView 的擴充方法。
    /// </summary>
    public static class DataViewExtensions
    {
        /// <summary>
        /// 刪除檢視表中所有的資料列。
        /// </summary>
        /// <param name="dataView">檢視表。</param>
        /// <param name="acceptChanges">是否同意變更。</param>
        public static void DeleteRows(this DataView dataView, bool acceptChanges)
        {
            for (int N1 = dataView.Count - 1; N1 >= 0; N1 += -1)
                dataView.Delete(N1);

            if (acceptChanges)
                dataView.Table.AcceptChanges();
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="dataView">檢視資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(this DataView dataView, string fieldName)
        {
            return dataView.Table.HasField(fieldName);
        }

        /// <summary>
        /// 判斷檢視表是否無資料。
        /// </summary>
        /// <param name="dataView">要判斷的檢視表。</param>
        public static bool IsEmpty(this DataView dataView)
        {
            //檢視表為 Null 或資料列數為零，皆視為無資料
            return (dataView == null || (dataView.Count == 0));
        }
    }
}
