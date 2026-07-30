using System.Runtime.CompilerServices;
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
        /// Parsed documents, keyed by the source text they came from.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Several analyzers read the same definition files, and each rule would otherwise re-parse
        /// every file on every compilation. Roslyn hands out the same <see cref="SourceText"/> instance
        /// for an unchanged <c>AdditionalText</c>, so it works as the cache key.
        /// </para>
        /// <para>
        /// A <see cref="ConditionalWeakTable{TKey, TValue}"/> rather than a dictionary because entries
        /// must not outlive the source text: an edited file produces a new <see cref="SourceText"/>, and
        /// a strong-keyed cache would retain every historical version for the life of the process.
        /// </para>
        /// <para>
        /// WARNING: The returned document is shared between analyzers and must be treated as read-only.
        /// Mutating it would leak changes into unrelated rules. Analyzers only ever read.
        /// </para>
        /// </remarks>
        private static readonly ConditionalWeakTable<SourceText, XDocument?> s_documents = new ConditionalWeakTable<SourceText, XDocument?>();

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
        /// file whose real problem the compiler or the framework will surface elsewhere. A failed
        /// parse is cached as well, so malformed files are not retried once per rule.
        /// </para>
        /// <para>
        /// WARNING: DTD processing and external entity resolution must stay disabled. Definition files
        /// are ordinary files in the consumer's repository and are not necessarily trusted input.
        /// </para>
        /// </remarks>
        public static XDocument? TryLoad(SourceText text)
        {
            if (s_documents.TryGetValue(text, out var cached))
                return cached;

            var document = Parse(text);

            // A concurrent call may have populated the entry already; either instance is equivalent, so
            // the loser of the race simply takes the winner's.
            try
            {
                s_documents.Add(text, document);
            }
            catch (ArgumentException)
            {
                s_documents.TryGetValue(text, out document);
            }

            return document;
        }

        private static XDocument? Parse(SourceText text)
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
