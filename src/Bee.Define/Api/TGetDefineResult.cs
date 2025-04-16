using System;

namespace Bee.Define
{
    /// <summary>
    ///  取得定義資料的傳出結果
    /// </summary>
    [Serializable]
    public class TGetDefineResult : TBusinessResult
    {
        /// <summary>
        /// 定義資料。
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
