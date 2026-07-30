using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// A parsed table schema definition file, exposing the parts the analyzers need.
    /// </summary>
    internal sealed class TableSchemaModel
    {
        private HashSet<string>? _declaredFieldNames;

        private TableSchemaModel(string path, SourceText text, XElement root, string tableName, string? categoryId)
        {
            Path = path;
            Text = text;
            Root = root;
            TableName = tableName;
            CategoryId = categoryId;
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
        /// Gets the physical table name.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Gets the database scope implied by the containing folder, or <c>null</c> when the file is not
        /// inside a scope folder.
        /// </summary>
        public string? CategoryId { get; }

        /// <summary>
        /// Gets every <c>DbField</c> element in the schema.
        /// </summary>
        public IEnumerable<XElement> Fields => Root.Descendants("DbField");

        /// <summary>
        /// Gets the names of every column declared in the schema, compared case-insensitively.
        /// </summary>
        public HashSet<string> DeclaredFieldNames
        {
            get
            {
                if (_declaredFieldNames is null)
                {
                    _declaredFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var column in Fields)
                    {
                        var name = column.Attribute("FieldName")?.Value;
                        if (!string.IsNullOrEmpty(name))
                            _declaredFieldNames.Add(name!);
                    }
                }

                return _declaredFieldNames;
            }
        }

        /// <summary>
        /// Attempts to parse the specified additional file as a table schema.
        /// </summary>
        /// <param name="file">The additional file to parse.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The parsed model, or <c>null</c> when the file is unreadable or not well-formed XML.
        /// </returns>
        public static TableSchemaModel? TryCreate(AdditionalText file, CancellationToken cancellationToken)
        {
            var text = file.GetText(cancellationToken);
            if (text is null)
                return null;

            var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
            if (root is null)
                return null;

            var tableName = root.Attribute("TableName")?.Value;
            if (string.IsNullOrEmpty(tableName))
                tableName = DefinitionFileNames.GetProgIdFromSidecar(file.Path, DefinitionFileNames.TableSchemaSuffix);

            return new TableSchemaModel(
                file.Path,
                text,
                root,
                tableName!,
                DefinitionFileNames.GetParentFolderName(file.Path));
        }

        /// <summary>
        /// Creates a location for the specified attribute within this definition file.
        /// </summary>
        /// <param name="attribute">The attribute to locate.</param>
        /// <returns>The location of the attribute.</returns>
        public Location CreateLocation(XAttribute attribute)
            => XmlAttributeLocator.Create(Path, Text, attribute);
    }
}
