using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// A parsed language resource file, exposing the keys it translates.
    /// </summary>
    internal sealed class LanguageResourceModel
    {
        private HashSet<string>? _keys;

        private LanguageResourceModel(string path, SourceText text, XElement root, string resourceNamespace, string culture)
        {
            Path = path;
            Text = text;
            Root = root;
            Namespace = resourceNamespace;
            Culture = culture;
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
        /// Gets the resource namespace, which is the program identifier the resource translates.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the culture the resource provides, taken from the containing folder.
        /// </summary>
        public string Culture { get; }

        /// <summary>
        /// Gets every translation key in the resource.
        /// </summary>
        /// <remarks>
        /// Keys are compared ordinally: they are structural identifiers built from field and table names
        /// (<c>Field.sys_id.Caption</c>), not display text, and the framework resolves them exactly.
        /// </remarks>
        public HashSet<string> Keys
        {
            get
            {
                if (_keys is null)
                {
                    _keys = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var item in Root.Descendants("LanguageItem"))
                    {
                        var key = item.Attribute("Key")?.Value;
                        if (!string.IsNullOrEmpty(key))
                            _keys.Add(key!);
                    }
                }

                return _keys;
            }
        }

        /// <summary>
        /// Attempts to parse the specified additional file as a language resource.
        /// </summary>
        /// <param name="file">The additional file to parse.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The parsed model, or <c>null</c> when the file is unreadable, not well-formed XML, or not
        /// inside a culture folder.
        /// </returns>
        public static LanguageResourceModel? TryCreate(AdditionalText file, CancellationToken cancellationToken)
        {
            var text = file.GetText(cancellationToken);
            if (text is null)
                return null;

            var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
            if (root is null)
                return null;

            // The culture comes from the folder rather than the Lang attribute: the folder is what the
            // framework resolves by, so a mismatched attribute would make the analyzer group files
            // differently from how they are actually loaded.
            var culture = DefinitionFileNames.GetParentFolderName(file.Path);
            if (culture is null)
                return null;

            var resourceNamespace = root.Attribute("Namespace")?.Value;
            if (string.IsNullOrEmpty(resourceNamespace))
                resourceNamespace = DefinitionFileNames.GetProgIdFromSidecar(file.Path, DefinitionFileNames.LanguageSuffix);

            return new LanguageResourceModel(file.Path, text, root, resourceNamespace!, culture);
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
