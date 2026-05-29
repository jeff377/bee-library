using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.Storage
{
    /// <summary>
    /// Read-only define storage for the tenant customization-override layer. Resolves files
    /// strictly under <c>{CustomizePath}/{custCode}/</c> (via <see cref="CustomizeOnlyPathOptions"/>)
    /// and serves only the three customizable types: Language, FormLayout, ProgramSettings.
    /// </summary>
    /// <remarks>
    /// A missing customization file is a normal scenario, so the three supported getters return
    /// <c>null</c> rather than throwing or falling back to the base layer. Every other member
    /// throws <see cref="NotSupportedException"/> — the override layer never owns FormSchema,
    /// TableSchema, DbCategorySettings, nor any write operation.
    /// </remarks>
    public sealed class CustomizeOnlyStorage : IDefineStorage
    {
        private const string ReadOnlyMsg = ReadOnlyMsg;
        private readonly CustomizeOnlyPathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="CustomizeOnlyStorage"/> bound to the supplied
        /// customization path options.
        /// </summary>
        /// <param name="paths">The customization-rooted path options.</param>
        public CustomizeOnlyStorage(CustomizeOnlyPathOptions paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the customization override of the form layout, or <c>null</c> when the tenant
        /// provides no override for the given layout.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        public FormLayout? GetFormLayout(string layoutId)
        {
            string filePath = _paths.GetFormLayoutFilePath(layoutId);
            if (!File.Exists(filePath))
                return null;
            return XmlCodec.DeserializeFromFile<FormLayout>(filePath);
        }

        /// <summary>
        /// Gets the customization override of the language resource, or <c>null</c> when the
        /// tenant provides no override for the given language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public LanguageResource? GetLanguage(string lang, string ns)
        {
            string filePath = _paths.GetLanguageFilePath(lang, ns);
            if (!File.Exists(filePath))
                return null;
            return XmlCodec.DeserializeFromFile<LanguageResource>(filePath);
        }

        /// <summary>Not supported — the override layer never owns database category settings.</summary>
        public DbCategorySettings? GetDbCategorySettings()
            => throw new NotSupportedException("The customization-override layer does not serve DbCategorySettings.");

        /// <summary>Not supported — the override layer never owns table schema.</summary>
        public TableSchema? GetTableSchema(string categoryId, string tableName)
            => throw new NotSupportedException("The customization-override layer does not serve TableSchema.");

        /// <summary>Not supported — the override layer never owns form schema.</summary>
        public FormSchema? GetFormSchema(string progId)
            => throw new NotSupportedException("The customization-override layer does not serve FormSchema.");

        /// <summary>Not supported — the override layer is strictly read-only.</summary>
        public void SaveDbCategorySettings(DbCategorySettings settings)
            => throw new NotSupportedException(ReadOnlyMsg);

        /// <summary>Not supported — the override layer is strictly read-only.</summary>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema)
            => throw new NotSupportedException(ReadOnlyMsg);

        /// <summary>Not supported — the override layer is strictly read-only.</summary>
        public void SaveFormSchema(FormSchema formSchema)
            => throw new NotSupportedException(ReadOnlyMsg);

        /// <summary>Not supported — the override layer is strictly read-only.</summary>
        public void SaveFormLayout(FormLayout formLayout)
            => throw new NotSupportedException(ReadOnlyMsg);

        /// <summary>Not supported — the override layer is strictly read-only.</summary>
        public void SaveLanguage(LanguageResource resource)
            => throw new NotSupportedException(ReadOnlyMsg);
    }
}
