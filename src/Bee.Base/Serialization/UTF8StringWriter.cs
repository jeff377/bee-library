using System.IO;
using System.Text;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// String writer that uses UTF-8 encoding.
    /// </summary>
    public class UTF8StringWriter : StringWriter
    {
        /// <summary>
        /// Gets the default UTF-8 encoding (without BOM).
        /// </summary>
        public override Encoding Encoding
        {
            get { return new UTF8Encoding(false); }
        }
    }
}
