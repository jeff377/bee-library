using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// A parsed form schema definition file, exposing the parts the analyzers need.
    /// </summary>
    /// <remarks>
    /// Holds on to the underlying elements and attributes rather than copying their values, because
    /// diagnostics have to be reported at the position of the offending attribute and that position is
    /// only recoverable from the attribute itself.
    /// </remarks>
    internal sealed class FormSchemaModel
    {
        private HashSet<string>? _declaredFieldNames;

        private FormSchemaModel(string path, SourceText text, XElement root, string progId)
        {
            Path = path;
            Text = text;
            Root = root;
            ProgId = progId;
        }

        /// <summary>
        /// Gets the path of the definition file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the source text of the definition file, needed to compute diagnostic locations.
        /// </summary>
        public SourceText Text { get; }

        /// <summary>
        /// Gets the document root element.
        /// </summary>
        public XElement Root { get; }

        /// <summary>
        /// Gets the program identifier, falling back to the file name when the attribute is absent.
        /// </summary>
        public string ProgId { get; }

        /// <summary>
        /// Gets the declared database scope, or <c>null</c> when the attribute is absent.
        /// </summary>
        public string? CategoryId => Root.Attribute("CategoryId")?.Value;

        /// <summary>
        /// Gets every <c>FormTable</c> element in the schema.
        /// </summary>
        public IEnumerable<XElement> Tables => Root.Descendants("FormTable");

        /// <summary>
        /// Gets every <c>FormField</c> element across all tables.
        /// </summary>
        public IEnumerable<XElement> Fields => Root.Descendants("FormField");

        /// <summary>
        /// Gets every <c>FieldMapping</c> element across all fields.
        /// </summary>
        public IEnumerable<XElement> FieldMappings => Root.Descendants("FieldMapping");

        /// <summary>
        /// Gets the names of every field declared in the schema, compared case-insensitively.
        /// </summary>
        /// <remarks>
        /// Field names are matched case-insensitively throughout the analyzers: definition files use
        /// lower-case names by convention but the framework's own lookups are not uniformly ordinal, so
        /// a stricter comparison here would report differences the framework tolerates.
        /// </remarks>
        public HashSet<string> DeclaredFieldNames
        {
            get
            {
                if (_declaredFieldNames is null)
                {
                    _declaredFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // NOTE: Not named `field` — in C# 14 that identifier is a keyword inside a property
                    // accessor and binds to the synthesised backing field.
                    foreach (var formField in Fields)
                    {
                        var name = formField.Attribute("FieldName")?.Value;
                        if (!string.IsNullOrEmpty(name))
                            _declaredFieldNames.Add(name!);
                    }
                }

                return _declaredFieldNames;
            }
        }

        /// <summary>
        /// Attempts to parse the specified additional file as a form schema.
        /// </summary>
        /// <param name="file">The additional file to parse.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The parsed model, or <c>null</c> when the file is unreadable or not well-formed XML.
        /// </returns>
        public static FormSchemaModel? TryCreate(AdditionalText file, CancellationToken cancellationToken)
        {
            var text = file.GetText(cancellationToken);
            if (text is null)
                return null;

            var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
            if (root is null)
                return null;

            var progId = root.Attribute("ProgId")?.Value;
            if (string.IsNullOrEmpty(progId))
                progId = DefinitionFileNames.GetProgIdFromSidecar(file.Path, DefinitionFileNames.FormSchemaSuffix);

            return new FormSchemaModel(file.Path, text, root, progId!);
        }

        /// <summary>
        /// Creates a location for the specified attribute within this definition file.
        /// </summary>
        /// <param name="attribute">The attribute to locate.</param>
        /// <returns>The location of the attribute.</returns>
        public Location CreateLocation(XAttribute attribute)
            => XmlAttributeLocator.Create(Path, Text, attribute);

        /// <summary>
        /// Splits a comma separated attribute value into its trimmed, non-empty entries.
        /// </summary>
        /// <param name="value">The attribute value, for example <c>sys_id,sys_name</c>.</param>
        /// <returns>The individual entries.</returns>
        public static IEnumerable<string> SplitFieldList(string value)
            => value.Split(',')
                .Select(entry => entry.Trim())
                .Where(entry => entry.Length > 0);
    }
}
