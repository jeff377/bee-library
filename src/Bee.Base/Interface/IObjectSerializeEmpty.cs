namespace Bee.Base
{
    /// <summary>
    /// 物件序化列時，判斷是否為空資料介面。
    /// </summary>
    public interface IObjectSerializeEmpty
    {
        /// <summary>
        /// 序化列時是否為空資料。
        /// </summary>
        bool IsSerializeEmpty { get; }
    }
}
