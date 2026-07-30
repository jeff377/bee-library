using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Maps XML nodes in a definition file onto Roslyn <see cref="Location"/> values so that
    /// diagnostics reported against definition files can be navigated to in an IDE and rendered
    /// with a file and line number in build output.
    /// </summary>
    /// <remarks>
    /// Diagnostics on C# get their location from the syntax tree. Definition files arrive as
    /// <c>AdditionalFiles</c> with no syntax tree, so the line and column reported by
    /// <see cref="IXmlLineInfo"/> has to be converted into an absolute <see cref="TextSpan"/> by hand.
    /// </remarks>
    internal static class XmlAttributeLocator
    {
        /// <summary>
        /// Creates a location spanning the <c>name="value"</c> text of the specified attribute.
        /// </summary>
        /// <param name="filePath">The path of the definition file the attribute belongs to.</param>
        /// <param name="text">The source text of that file.</param>
        /// <param name="attribute">The attribute to locate.</param>
        /// <returns>
        /// The location of the attribute, or <see cref="Location.None"/> when line information is
        /// unavailable or points outside the document.
        /// </returns>
        /// <remarks>
        /// The document must have been loaded with <see cref="LoadOptions.SetLineInfo"/>, otherwise
        /// no line information is present and <see cref="Location.None"/> is returned.
        /// </remarks>
        public static Location Create(string filePath, SourceText text, XAttribute attribute)
        {
            var lineInfo = (IXmlLineInfo)attribute;
            if (!lineInfo.HasLineInfo())
                return Location.None;

            // XML line and column numbers are one-based; SourceText lines are zero-based.
            var lineIndex = lineInfo.LineNumber - 1;
            if (lineIndex < 0 || lineIndex >= text.Lines.Count)
                return Location.None;

            var line = text.Lines[lineIndex];
            var start = line.Start + lineInfo.LinePosition - 1;
            if (start < line.Start || start >= text.Length)
                return Location.None;

            var end = FindAttributeEnd(text, start);
            var span = TextSpan.FromBounds(start, end);
            return Location.Create(filePath, span, text.Lines.GetLinePositionSpan(span));
        }

        /// <summary>
        /// Walks an attribute starting at its name and returns the offset just past its closing quote.
        /// </summary>
        /// <param name="text">The source text being scanned.</param>
        /// <param name="start">The offset of the first character of the attribute name.</param>
        /// <returns>
        /// The offset just past the closing quote, or the end of the attribute name when the text does
        /// not have the expected shape.
        /// </returns>
        /// <remarks>
        /// Falling back to the end of the attribute name keeps the reported span inside the document
        /// even for malformed input, which matters because a span past the end of the file makes the
        /// diagnostic unreportable.
        /// </remarks>
        private static int FindAttributeEnd(SourceText text, int start)
        {
            var index = start;
            while (index < text.Length && text[index] != '=' && text[index] != '>' && !char.IsWhiteSpace(text[index]))
                index++;

            var nameEnd = index;

            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            if (index >= text.Length || text[index] != '=')
                return nameEnd;

            index++;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            if (index >= text.Length || (text[index] != '"' && text[index] != '\''))
                return nameEnd;

            var quote = text[index];
            index++;
            while (index < text.Length && text[index] != quote)
                index++;

            return index < text.Length ? index + 1 : nameEnd;
        }
    }
}
