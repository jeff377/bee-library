using System.Xml;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Detects the pre-flattening <c>ProgramSettings.xml</c> layout so a host fails fast instead of
    /// starting with a silently empty type registry.
    /// </summary>
    /// <remarks>
    /// <see cref="ProgramSettings"/> used to nest its entries under
    /// <c>&lt;Categories&gt;&lt;ProgramCategory&gt;</c>. XmlSerializer ignores elements it does not
    /// recognise, so an un-migrated file deserializes without error into a registry with zero
    /// entries — every progId would then resolve to the framework default and the cause would be
    /// invisible. Detecting the old root element turns that into a startup error naming the
    /// migration command.
    /// </remarks>
    public static class ProgramSettingsFormat
    {
        private const string LegacyElementName = "Categories";

        /// <summary>
        /// Returns whether the supplied XML uses the pre-flattening nested layout.
        /// </summary>
        /// <param name="xml">The raw <c>ProgramSettings.xml</c> content.</param>
        /// <remarks>
        /// Parses rather than string-matches: <c>Categories</c> may legitimately appear in a comment
        /// or an attribute value, and only a real child element of the document root means the file
        /// is in the old shape.
        /// </remarks>
        public static bool IsLegacyFormat(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) { return false; }

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, settings);

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) { continue; }
                // Depth 1 is a child of the document root; the legacy layout puts Categories there.
                if (reader.Depth == 1)
                    return string.Equals(reader.LocalName, LegacyElementName, StringComparison.Ordinal);
                if (reader.Depth > 1)
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Throws when the supplied XML is in the pre-flattening layout; otherwise returns silently.
        /// </summary>
        /// <param name="xml">The raw <c>ProgramSettings.xml</c> content.</param>
        /// <param name="source">Where the content came from, used in the error message (a file path, a database key).</param>
        /// <exception cref="NotSupportedException">Thrown when the content is in the legacy layout.</exception>
        public static void EnsureCurrentFormat(string xml, string source)
        {
            if (!IsLegacyFormat(xml)) { return; }

            throw new NotSupportedException(
                $"'{source}' uses the obsolete nested ProgramSettings layout (<Categories><ProgramCategory>). " +
                "The registry is now a flat <Items> list and the menu moved to MenuSettings.xml. " +
                "Run 'dotnet bee defines split-menu --path <DefinePath>' to migrate.");
        }
    }
}
