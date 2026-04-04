namespace Bee.Define
{
    /// <summary>
    /// 繫結欄位的控制項介面。
    /// </summary>
    public interface IBindFieldControl
    {
        /// <summary>
        /// 欄位名稱．
        /// </summary>
        string FieldName { get; set; }

        /// <summary>
        /// 欄位值。
        /// </summary>
        object FieldValue { get; set; }
    }
}
