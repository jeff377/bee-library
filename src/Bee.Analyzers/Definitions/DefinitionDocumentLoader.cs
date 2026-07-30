using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Parses definition files supplied through <c>AdditionalFiles</c> into XML documents that carry
    /// line information.
    /// </summary>
    internal static class DefinitionDocumentLoader
    {
        /// <summary>
        /// Attempts to parse the specified source text as a definition document.
        /// </summary>
        /// <param name="text">The source text of the definition file.</param>
        /// <returns>
        /// The parsed document, or <c>null</c> when the text is not well-formed XML.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Malformed input returns <c>null</c> rather than throwing: an analyzer that throws is
        /// reported to the user as an analyzer crash, which is a far worse outcome than skipping a
        /// file whose real problem the compiler or the framework will surface elsewhere.
        /// </para>
        /// <para>
        /// WARNING: DTD processing and external entity resolution must stay disabled. Definition files
        /// are ordinary files in the consumer's repository and are not necessarily trusted input.
        /// </para>
        /// </remarks>
        public static XDocument? TryLoad(SourceText text)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = false
            };

            try
            {
                using var stringReader = new StringReader(text.ToString());
                using var xmlReader = XmlReader.Create(stringReader, settings);
                return XDocument.Load(xmlReader, LoadOptions.SetLineInfo);
            }
            catch (XmlException)
            {
                return null;
            }
        }
    }
}
