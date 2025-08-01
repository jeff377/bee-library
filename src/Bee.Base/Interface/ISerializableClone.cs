namespace Bee.Base
{
    /// <summary>
    /// 定義資料在序列化前需要複製的標記介面。
    /// </summary>
    public interface ISerializableClone
    {
        /// <summary>
        /// 複製出一份序列化用的物件 (深拷貝)。
        /// </summary>
        object CreateSerializableCopy();
    }

}
