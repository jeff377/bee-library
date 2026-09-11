using System.Text;
using System.Xml;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Forwards to another <see cref="XmlWriter"/>, replacing every unpaired surrogate in text content
    /// with U+FFFD, the Unicode replacement character.
    /// </summary>
    /// <remarks>
    /// An unpaired surrogate is malformed UTF-16 that XML has no way to carry: a character reference
    /// to a surrogate code point is not a character either, so the inner writer rejects it even with
    /// <see cref="XmlWriterSettings.CheckCharacters"/> turned off. Every other character is left to the
    /// inner writer. A <see cref="global::System.Data.DataSet"/> hands column values to its writer through
    /// <see cref="WriteString"/>, which is where the replacement happens; <see cref="WriteChars"/> is
    /// routed the same way for a caller that writes text in chunks.
    /// </remarks>
    internal sealed class LoneSurrogateReplacingXmlWriter : XmlWriter
    {
        private const char ReplacementCharacter = (char)0xFFFD;

        private readonly XmlWriter _inner;

        /// <summary>
        /// Initializes a new instance that owns <paramref name="inner"/> and disposes it with itself.
        /// </summary>
        /// <param name="inner">The writer that receives the output.</param>
        public LoneSurrogateReplacingXmlWriter(XmlWriter inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc/>
        public override WriteState WriteState => _inner.WriteState;

        /// <inheritdoc/>
        public override XmlWriterSettings? Settings => _inner.Settings;

        /// <inheritdoc/>
        public override XmlSpace XmlSpace => _inner.XmlSpace;

        /// <inheritdoc/>
        public override string? XmlLang => _inner.XmlLang;

        /// <inheritdoc/>
        public override void WriteString(string? text) => _inner.WriteString(ReplaceLoneSurrogates(text));

        /// <inheritdoc/>
        public override void WriteChars(char[] buffer, int index, int count)
            => WriteString(new string(buffer, index, count));

        /// <inheritdoc/>
        public override void Flush() => _inner.Flush();

        /// <inheritdoc/>
        public override string? LookupPrefix(string ns) => _inner.LookupPrefix(ns);

        /// <inheritdoc/>
        public override void WriteBase64(byte[] buffer, int index, int count) => _inner.WriteBase64(buffer, index, count);

        /// <inheritdoc/>
        public override void WriteCData(string? text) => _inner.WriteCData(text);

        /// <inheritdoc/>
        public override void WriteCharEntity(char ch) => _inner.WriteCharEntity(ch);

        /// <inheritdoc/>
        public override void WriteComment(string? text) => _inner.WriteComment(text);

        /// <inheritdoc/>
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
            => _inner.WriteDocType(name, pubid, sysid, subset);

        /// <inheritdoc/>
        public override void WriteEndAttribute() => _inner.WriteEndAttribute();

        /// <inheritdoc/>
        public override void WriteEndDocument() => _inner.WriteEndDocument();

        /// <inheritdoc/>
        public override void WriteEndElement() => _inner.WriteEndElement();

        /// <inheritdoc/>
        public override void WriteEntityRef(string name) => _inner.WriteEntityRef(name);

        /// <inheritdoc/>
        public override void WriteFullEndElement() => _inner.WriteFullEndElement();

        /// <inheritdoc/>
        public override void WriteProcessingInstruction(string name, string? text)
            => _inner.WriteProcessingInstruction(name, text);

        /// <inheritdoc/>
        public override void WriteRaw(char[] buffer, int index, int count) => _inner.WriteRaw(buffer, index, count);

        /// <inheritdoc/>
        public override void WriteRaw(string data) => _inner.WriteRaw(data);

        /// <inheritdoc/>
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
            => _inner.WriteStartAttribute(prefix, localName, ns);

        /// <inheritdoc/>
        public override void WriteStartDocument() => _inner.WriteStartDocument();

        /// <inheritdoc/>
        public override void WriteStartDocument(bool standalone) => _inner.WriteStartDocument(standalone);

        /// <inheritdoc/>
        public override void WriteStartElement(string? prefix, string localName, string? ns)
            => _inner.WriteStartElement(prefix, localName, ns);

        /// <inheritdoc/>
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
            => _inner.WriteSurrogateCharEntity(lowChar, highChar);

        /// <inheritdoc/>
        public override void WriteWhitespace(string? ws) => _inner.WriteWhitespace(ws);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Returns <paramref name="text"/> with each unpaired surrogate replaced by U+FFFD; surrogate
        /// pairs are kept. The input is returned as is when it holds no surrogate at all.
        /// </summary>
        private static string? ReplaceLoneSurrogates(string? text)
        {
            if (text == null || !text.AsSpan().ContainsAnyInRange((char)0xD800, (char)0xDFFF))
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            int index = 0;
            while (index < text.Length)
            {
                if (char.IsSurrogatePair(text, index))
                {
                    builder.Append(text, index, 2);
                    index += 2;
                }
                else
                {
                    builder.Append(char.IsSurrogate(text[index]) ? ReplacementCharacter : text[index]);
                    index++;
                }
            }
            return builder.ToString();
        }
    }
}
