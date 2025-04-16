using System.IO;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 字串寫入器，預設為 UTF8 編碼。
    /// </summary>
    public class TStringWriter : StringWriter
    {
        /// <summary>
        /// 預設為 UTF8 編碼。
        /// </summary>
        public override Encoding Encoding
        {
            get { return new UTF8Encoding(false); }
        }
    }
}
